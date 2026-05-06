#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace CupkekGames.PackageManager.Editor
{
    public static class CupkekGamesPackageInstaller
    {
        private const string LastErrorKey = "CupkekGames_PackageInstallLastError";

        // CupkekGames UPM scoped registry. Hosted as a dynamic Next.js route
        // handler at luna-docs-next/src/app/upm/[package]/route.ts, backed by
        // each sibling repo's GitHub Releases.
        private const string RegistryName = "CupkekGames";
        private const string RegistryUrl = "https://www.docs.cupkek.games/upm";
        private const string RegistryScope = "com.cupkekgames";

        /// <summary>
        /// Per-package version status returned by <see cref="GetInstalledPackages"/>.
        /// <see cref="Installed"/> is the version currently resolved in the project;
        /// <see cref="Latest"/> is the highest compatible version the registry advertises
        /// (empty when the list call ran in offline mode or the registry didn't return one).
        /// </summary>
        public readonly struct PackageVersionInfo
        {
            public readonly string Installed;
            public readonly string Latest;

            public PackageVersionInfo(string installed, string latest)
            {
                Installed = installed;
                Latest = latest;
            }

            /// <summary>True when an update is available (installed != latest, both populated).</summary>
            public bool IsOutdated =>
                !string.IsNullOrEmpty(Installed) &&
                !string.IsNullOrEmpty(Latest) &&
                Installed != Latest;
        }

        private static ListRequest _listRequest;
        private static AddRequest _addRequest;
        private static AddAndRemoveRequest _addAndRemoveRequest;
        private static Action<Dictionary<string, PackageVersionInfo>> _listCallback;
        private static Action<bool, string> _addCallback;
        private static Action<bool, string> _addAndRemoveCallback;

        public static bool IsAddInFlight => _addRequest != null || _addAndRemoveRequest != null;
        public static bool IsListInFlight => _listRequest != null;

        public static string LastError
        {
            get => EditorPrefs.GetString(LastErrorKey, string.Empty);
            private set
            {
                if (string.IsNullOrEmpty(value)) EditorPrefs.DeleteKey(LastErrorKey);
                else EditorPrefs.SetString(LastErrorKey, value);
            }
        }

        /// <summary>
        /// List installed packages, optionally querying the registry for the latest
        /// compatible version of each. Online mode populates <see cref="PackageVersionInfo.Latest"/>
        /// and lets the caller detect outdated packages; offline mode returns immediately
        /// with <c>Latest</c> empty.
        /// </summary>
        public static void GetInstalledPackages(
            Action<Dictionary<string, PackageVersionInfo>> onCompleted,
            bool checkLatestVersions = false)
        {
            if (_listRequest != null) return;
            _listRequest = Client.List(offlineMode: !checkLatestVersions, includeIndirectDependencies: false);
            _listCallback = onCompleted;
            EditorApplication.update += PumpListRequest;
        }

        private static void PumpListRequest()
        {
            if (_listRequest == null || !_listRequest.IsCompleted) return;
            EditorApplication.update -= PumpListRequest;

            var dict = new Dictionary<string, PackageVersionInfo>();
            if (_listRequest.Status == StatusCode.Success)
            {
                foreach (var pkg in _listRequest.Result)
                {
                    // versions.latestCompatible is populated only when the list call
                    // ran in online mode (offlineMode: false). It can also be empty
                    // when the registry didn't advertise an upgrade path; we map
                    // either case to a null Latest so IsOutdated short-circuits.
                    string latest = pkg.versions != null ? pkg.versions.latestCompatible : null;
                    dict[pkg.name] = new PackageVersionInfo(pkg.version, latest);
                }
            }

            Action<Dictionary<string, PackageVersionInfo>> cb = _listCallback;
            _listRequest = null;
            _listCallback = null;
            cb?.Invoke(dict);
        }

        public static void InstallByPackageId(string packageId, Action<bool, string> onCompleted)
        {
            if (IsAddInFlight) return;
            if (string.IsNullOrEmpty(packageId))
            {
                onCompleted?.Invoke(false, "Empty package id.");
                return;
            }

            bool registryAdded = EnsureScopedRegistry();

            LastError = string.Empty;
            _addCallback = onCompleted;

            // If we just wrote a new scopedRegistries block to manifest.json,
            // defer the Add by one editor tick so Unity processes the change
            // before resolving the package. Otherwise UPM may not see the new
            // registry yet and fail with "invalid dependencies".
            if (registryAdded)
            {
                EditorApplication.delayCall += () =>
                {
                    _addRequest = Client.Add(packageId);
                    EditorApplication.update += PumpAddRequest;
                };
            }
            else
            {
                _addRequest = Client.Add(packageId);
                EditorApplication.update += PumpAddRequest;
            }
        }

        private static void PumpAddRequest()
        {
            if (_addRequest == null || !_addRequest.IsCompleted) return;
            EditorApplication.update -= PumpAddRequest;

            bool ok = _addRequest.Status == StatusCode.Success;
            string msg = ok
                ? (_addRequest.Result != null ? _addRequest.Result.name : string.Empty)
                : (_addRequest.Error != null ? _addRequest.Error.message : "Unknown error");

            if (!ok) LastError = msg;

            Action<bool, string> cb = _addCallback;
            _addRequest = null;
            _addCallback = null;
            cb?.Invoke(ok, msg);
        }

        // Bulk-install via Client.AddAndRemove — single manifest write + single
        // domain reload, and Unity resolves the whole graph atomically so all
        // transitive deps within the com.cupkekgames scope land together.
        public static void InstallByPackageIds(IEnumerable<string> packageIds, Action<bool, string> onCompleted = null)
        {
            if (IsAddInFlight) return;

            string[] toAdd = packageIds
                .Where(id => !string.IsNullOrEmpty(id))
                .ToArray();
            if (toAdd.Length == 0)
            {
                onCompleted?.Invoke(true, "No packages to install.");
                return;
            }

            bool registryAdded = EnsureScopedRegistry();

            LastError = string.Empty;
            _addAndRemoveCallback = onCompleted;

            // If we just wrote a new scopedRegistries block to manifest.json,
            // defer the AddAndRemove by one editor tick so Unity processes the
            // change before resolving packages. Otherwise UPM may not see the
            // new registry yet and fail with "invalid dependencies".
            if (registryAdded)
            {
                EditorApplication.delayCall += () =>
                {
                    _addAndRemoveRequest = Client.AddAndRemove(packagesToAdd: toAdd, packagesToRemove: null);
                    EditorApplication.update += PumpAddAndRemoveRequest;
                };
            }
            else
            {
                _addAndRemoveRequest = Client.AddAndRemove(packagesToAdd: toAdd, packagesToRemove: null);
                EditorApplication.update += PumpAddAndRemoveRequest;
            }
        }

        private static void PumpAddAndRemoveRequest()
        {
            if (_addAndRemoveRequest == null || !_addAndRemoveRequest.IsCompleted) return;
            EditorApplication.update -= PumpAddAndRemoveRequest;

            bool ok = _addAndRemoveRequest.Status == StatusCode.Success;
            string msg = ok
                ? $"Installed {_addAndRemoveRequest.Result?.Count() ?? 0} package(s)."
                : (_addAndRemoveRequest.Error != null ? _addAndRemoveRequest.Error.message : "Unknown error");

            if (!ok) LastError = msg;

            Action<bool, string> cb = _addAndRemoveCallback;
            _addAndRemoveRequest = null;
            _addAndRemoveCallback = null;
            cb?.Invoke(ok, msg);
        }

        /// <summary>
        /// Ensures the CupkekGames scoped registry block is present in the
        /// project's Packages/manifest.json. Idempotent. Safe to call before
        /// every Client.Add / Client.AddAndRemove.
        /// </summary>
        /// <returns>
        /// <c>true</c> if a new scopedRegistries entry was just written
        /// (caller should defer the next package-manager API call by one
        /// editor tick so Unity sees the change). <c>false</c> if the
        /// registry was already present — caller can proceed immediately.
        /// </returns>
        /// <remarks>
        /// String-based injection rather than full JSON parse: manifest.json is
        /// Unity-managed and predictable in shape (no comments, no trailing
        /// commas). This avoids adding a Newtonsoft.Json dep on a package whose
        /// pitch is "no external deps".
        /// </remarks>
        public static bool EnsureScopedRegistry()
        {
            string manifestPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"[CupkekGames] Packages/manifest.json not found at {manifestPath}; skipping scoped-registry bootstrap.");
                return false;
            }

            string content = File.ReadAllText(manifestPath);

            // Already present? Match on the exact URL — robust against
            // user reformatting the JSON or having a different `name`.
            if (content.Contains($"\"{RegistryUrl}\""))
            {
                return false;
            }

            string newEntry =
                "    {\n" +
                $"      \"name\": \"{RegistryName}\",\n" +
                $"      \"url\": \"{RegistryUrl}\",\n" +
                $"      \"scopes\": [\n" +
                $"        \"{RegistryScope}\"\n" +
                $"      ]\n" +
                "    }";

            string updated;
            int scopedRegistriesIdx = content.IndexOf("\"scopedRegistries\"");
            if (scopedRegistriesIdx >= 0)
            {
                // Append our entry to the existing array. Find the array's `[`
                // and inject after it.
                int arrayStart = content.IndexOf('[', scopedRegistriesIdx);
                if (arrayStart < 0)
                {
                    Debug.LogError("[CupkekGames] manifest.json has scopedRegistries key but no array — skipping.");
                    return false;
                }

                // Determine if the array is empty (next non-ws char is `]`).
                int probe = arrayStart + 1;
                while (probe < content.Length && char.IsWhiteSpace(content[probe])) probe++;
                bool empty = probe < content.Length && content[probe] == ']';

                string insertion = empty
                    ? "\n" + newEntry + "\n  "
                    : "\n" + newEntry + ",";
                updated = content.Insert(arrayStart + 1, insertion);
            }
            else
            {
                // Inject a fresh scopedRegistries key at the start of the root
                // object (right after the opening `{`). Trailing comma keeps
                // following keys (e.g. `dependencies`) syntactically valid.
                int rootBrace = content.IndexOf('{');
                if (rootBrace < 0)
                {
                    Debug.LogError("[CupkekGames] manifest.json has no opening `{` — skipping.");
                    return false;
                }

                string insertion =
                    "\n  \"scopedRegistries\": [\n" +
                    newEntry + "\n" +
                    "  ],";
                updated = content.Insert(rootBrace + 1, insertion);
            }

            File.WriteAllText(manifestPath, updated);
            Debug.Log($"[CupkekGames] Added scoped registry to {manifestPath}.");
            return true;
        }
    }
}
#endif
