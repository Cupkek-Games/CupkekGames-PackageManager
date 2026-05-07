#if UNITY_EDITOR
using CupkekGames.EditorUI;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;

namespace CupkekGames.PackageManager.Editor
{
    public class CupkekGamesPackageManagerWindow : EditorWindow
    {
        private const string FirstRunSeenKey = "CupkekGamesPackageManager_FirstRunSeen";

        private VisualElement _headerCount;
        private Button _installGameFullButton;
        private Button _installAllButton;
        private Button _updateAllButton;
        private Button _refreshButton;
        private VisualElement _rowsContainer;
        private Label _errorLabel;

        private Dictionary<string, CupkekGamesPackageInstaller.PackageVersionInfo> _installedPackages;
        private readonly List<Button> _rowInstallButtons = new();

        [MenuItem("Tools/CupkekGames/Package Manager", false, 4)]
        public static CupkekGamesPackageManagerWindow ShowWindow()
        {
            CupkekGamesPackageManagerWindow wnd = GetWindow<CupkekGamesPackageManagerWindow>();
            wnd.titleContent = new GUIContent("CupkekGames Packages");
            wnd.minSize = new Vector2(540, 380);
            return wnd;
        }

        [InitializeOnLoadMethod]
        private static void AutoOpenIfPackagesMissing()
        {
            // Defer until editor is settled so Client.List succeeds and ProjectSettings load.
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(FirstRunSeenKey, false)) return;
                if (HasOpenInstances<CupkekGamesPackageManagerWindow>()) return;

                CupkekGamesPackageInstaller.GetInstalledPackages(installed =>
                {
                    int missing = 0;
                    foreach (CupkekGamesPackageRegistry.Entry e in CupkekGamesPackageRegistry.GetByTag(PackageTags.GameFull))
                    {
                        if (installed == null || !installed.ContainsKey(e.PackageId))
                        {
                            missing++;
                        }
                    }

                    if (missing > 0)
                    {
                        EditorPrefs.SetBool(FirstRunSeenKey, true);
                        ShowWindow();
                    }
                });
            };
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            StyleSheet palette = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.cupkekgames.editorui/Editor/EditorColorPalette.uss");
            StyleSheet windowUss = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.cupkekgames.packagemanager/Editor/CupkekGamesPackageManagerWindow.uss");

            if (palette != null) root.styleSheets.Add(palette);
            if (windowUss != null) root.styleSheets.Add(windowUss);

            BuildHeader(root);
            BuildContent(root);

            Refresh();
        }

        private void BuildHeader(VisualElement root)
        {
            VisualElement header = new VisualElement();
            header.AddToClassList("pm-header");
            root.Add(header);

            Label title = new Label("CupkekGames Package Manager");
            title.AddToClassList("pm-header-title");
            header.Add(title);

            Label subtitle = new Label(
                "Sibling CupkekGames packages installed via the CupkekGames UPM scoped registry " +
                "(www.docs.cupkek.games/upm). Use \"Install GameFull Packages\" for the Luna " +
                "GameFull sample's lean cupkek-only set, or \"Install All\" for every CupkekGames " +
                "package. Installing here writes the registry block to your Packages/manifest.json " +
                "automatically.");
            subtitle.AddToClassList("pm-header-subtitle");
            header.Add(subtitle);
        }

        private void BuildContent(VisualElement root)
        {
            VisualElement content = new VisualElement();
            content.AddToClassList("pm-content");
            root.Add(content);

            // Toolbar row: count + Install GameFull Packages + Refresh
            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("pm-toolbar");
            content.Add(toolbar);

            _headerCount = new VisualElement();
            _headerCount.AddToClassList("pm-toolbar-count");
            toolbar.Add(_headerCount);

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);

            _refreshButton = new Button(Refresh);
            _refreshButton.text = "Refresh";
            _refreshButton.AddToClassList("pm-toolbar-btn");
            toolbar.Add(_refreshButton);

            _updateAllButton = new Button(OnUpdateAll);
            _updateAllButton.AddToClassList("pm-toolbar-btn");
            _updateAllButton.AddToClassList("pm-toolbar-btn--update");
            _updateAllButton.text = "No updates";
            _updateAllButton.SetEnabled(false);
            // Hidden by default — only relevant after a Refresh has populated
            // latest-version info. Refresh online → Show; on offline data, hide.
            _updateAllButton.style.display = DisplayStyle.None;
            toolbar.Add(_updateAllButton);

            _installGameFullButton = new Button(OnInstallGameFullPackages);
            _installGameFullButton.AddToClassList("pm-toolbar-btn");
            _installGameFullButton.AddToClassList("pm-toolbar-btn--primary");
            toolbar.Add(_installGameFullButton);

            _installAllButton = new Button(OnInstallAllPackages);
            _installAllButton.AddToClassList("pm-toolbar-btn");
            _installAllButton.text = "Install All";
            _installAllButton.tooltip =
                "Install every CupkekGames sibling package. Most domain packages " +
                "(combat, character, vfx, navigation, …) require Asset Store / git " +
                "third-party deps; you'll need to install those manually after " +
                "via the External Dependencies section.";
            toolbar.Add(_installAllButton);

            // Scrollable rows
            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("pm-rows-scroll");
            content.Add(scroll);

            _rowsContainer = scroll.contentContainer;

            // Footer error
            _errorLabel = new Label();
            _errorLabel.AddToClassList("pm-error-label");
            _errorLabel.style.display = DisplayStyle.None;
            content.Add(_errorLabel);
        }

        private void Refresh()
        {
            if (_rowsContainer == null) return;

            _rowsContainer.Clear();
            _rowInstallButtons.Clear();
            Label loading = new Label("Checking installed packages and registry for updates…");
            loading.AddToClassList("pm-loading-text");
            _rowsContainer.Add(loading);

            // Refresh runs the online list (offlineMode: false) so we get
            // latest-compatible versions for update detection. Costs ~0.5–2s
            // network on the first call after editor start; subsequent calls
            // hit Unity's package-manager cache and are near-instant.
            CupkekGamesPackageInstaller.GetInstalledPackages(installed =>
            {
                _installedPackages = installed;
                BuildRows();
                UpdateToolbar();
                UpdateErrorFooter();

                // Re-enable the refresh button (BuildRows / UpdateToolbar handle
                // their own enable state for install buttons).
                if (_refreshButton != null) _refreshButton.SetEnabled(true);

                // If an install is still in flight (e.g. user opened the window
                // mid-install, or refresh fired while AddAndRemove was still
                // resolving), preserve the busy UI so buttons don't flash live.
                if (CupkekGamesPackageInstaller.IsAddInFlight)
                    EnterBusyState(null, "Installing…");
            }, checkLatestVersions: true);
        }

        private void BuildRows()
        {
            _rowsContainer.Clear();
            _rowInstallButtons.Clear();
            HashSet<string> renderedIds = new HashSet<string>();

            // Section 1: Luna UI — the headline package. Foundation deps install
            // transitively via Luna's package.json, so a single Install here
            // brings in the bundled set (singletons, pool, fadeables, etc.).
            CupkekGamesPackageRegistry.Entry[] lunaEntries =
                CupkekGamesPackageRegistry.GetByTag(PackageTags.Luna);
            if (lunaEntries.Length > 0)
            {
                VisualElement lunaHeader = new VisualElement();
                lunaHeader.AddToClassList("pm-section-header");
                Label lunaHeaderText = new Label("Luna UI");
                lunaHeaderText.AddToClassList("pm-section-header-text");
                lunaHeader.Add(lunaHeaderText);
                Label lunaHeaderSub = new Label(
                    "Headline UI Toolkit framework. Bundled foundation deps " +
                    "(Singletons, Pool, Fadeables, KeyValueDatabases, " +
                    "PrefabLoaders, Input, …) install transitively.");
                lunaHeaderSub.AddToClassList("pm-section-header-sub");
                lunaHeader.Add(lunaHeaderSub);
                _rowsContainer.Add(lunaHeader);

                foreach (CupkekGamesPackageRegistry.Entry entry in lunaEntries)
                {
                    _rowsContainer.Add(BuildRow(entry));
                    renderedIds.Add(entry.PackageId);
                }
            }

            // Section 2: GameFull packages — the lean cupkek set the Luna GameFull
            // sample uses (foundation primitives + data layer + inventory + scene
            // transitions + settings). No third-party deps required to compile.
            VisualElement gfHeader = new VisualElement();
            gfHeader.AddToClassList("pm-section-header");
            Label gfHeaderText = new Label("GameFull Packages");
            gfHeaderText.AddToClassList("pm-section-header-text");
            gfHeader.Add(gfHeaderText);
            Label gfHeaderSub = new Label(
                "What the Luna GameFull sample needs. Pure CupkekGames packages — " +
                "no Asset Store / external deps required.");
            gfHeaderSub.AddToClassList("pm-section-header-sub");
            gfHeader.Add(gfHeaderSub);
            _rowsContainer.Add(gfHeader);

            foreach (CupkekGamesPackageRegistry.Entry entry in
                CupkekGamesPackageRegistry.GetByTag(PackageTags.GameFull))
            {
                if (renderedIds.Contains(entry.PackageId)) continue;
                _rowsContainer.Add(BuildRow(entry));
                renderedIds.Add(entry.PackageId);
            }

            // Section 2: Other CupkekGames packages — domain packages that
            // generally require third-party Asset Store / git deps (combat,
            // character, vfx, navigation, …). Shown so users can install them
            // individually or via the toolbar's "Install All" button.
            CupkekGamesPackageRegistry.Entry[] otherEntries =
                CupkekGamesPackageRegistry.Entries
                    .Where(e => !renderedIds.Contains(e.PackageId))
                    .ToArray();
            if (otherEntries.Length > 0)
            {
                VisualElement otherHeader = new VisualElement();
                otherHeader.AddToClassList("pm-section-header");
                Label otherHeaderText = new Label("Other CupkekGames Packages");
                otherHeaderText.AddToClassList("pm-section-header-text");
                otherHeader.Add(otherHeaderText);
                Label otherHeaderSub = new Label(
                    "Domain packages — most require third-party deps " +
                    "(see External Dependencies below). Install individually " +
                    "or use the toolbar's \"Install All\" button.");
                otherHeaderSub.AddToClassList("pm-section-header-sub");
                otherHeader.Add(otherHeaderSub);
                _rowsContainer.Add(otherHeader);

                foreach (CupkekGamesPackageRegistry.Entry entry in otherEntries)
                {
                    _rowsContainer.Add(BuildRow(entry));
                }
            }

            // Section 3: External (third-party) dependencies for specific
            // packages above. Cinemachine + UniTask auto-install via UPM / git
            // URL; Animancer / DamageNumbersPro / PrimeTween / Ink open the
            // Asset Store. Each row's "used by" hint shows which cupkek package
            // unlocks when the dep is installed.
            VisualElement extHeader = new VisualElement();
            extHeader.AddToClassList("pm-section-header");
            Label extHeaderText = new Label("External Dependencies (for specific packages)");
            extHeaderText.AddToClassList("pm-section-header-text");
            extHeader.Add(extHeaderText);
            Label extHeaderSub = new Label(
                "Required by some of the \"Other CupkekGames Packages\" above. " +
                "Not needed for the GameFull sample.");
            extHeaderSub.AddToClassList("pm-section-header-sub");
            extHeader.Add(extHeaderSub);
            _rowsContainer.Add(extHeader);

            HashSet<string> presentAsmdefs = GetCompiledAsmdefNames();
            foreach (CupkekGamesExternalDependency dep in CupkekGamesExternalDependencyRegistry.All)
            {
                _rowsContainer.Add(BuildExternalRow(dep, presentAsmdefs));
            }
        }

        private VisualElement BuildRow(CupkekGamesPackageRegistry.Entry entry)
        {
            bool isInstalled = _installedPackages != null && _installedPackages.ContainsKey(entry.PackageId);

            VisualElement row = new VisualElement();
            row.AddToClassList("pm-row");
            row.AddToClassList(isInstalled ? "pm-row--installed" : "pm-row--missing");

            // Status icon (not the badge — icon-sized)
            VisualElement icon = new VisualElement();
            icon.AddToClassList("pm-row-icon");
            icon.AddToClassList(isInstalled ? "pm-row-icon--installed" : "pm-row-icon--missing");
            row.Add(icon);

            // Display name
            Label displayName = new Label(entry.DisplayName);
            displayName.AddToClassList("pm-row-displayname");
            row.Add(displayName);

            // Package id (smaller, secondary)
            Label packageId = new Label(entry.PackageId);
            packageId.AddToClassList("pm-row-packageid");
            row.Add(packageId);

            // Tag chips
            if (entry.Tags != null && entry.Tags.Length > 0)
            {
                VisualElement tagContainer = new VisualElement();
                tagContainer.AddToClassList("pm-row-tags");
                for (int i = 0; i < entry.Tags.Length; i++)
                {
                    Label tag = new Label(entry.Tags[i]);
                    tag.AddToClassList("pm-row-tag");
                    tagContainer.Add(tag);
                }
                row.Add(tagContainer);
            }

            // Spacer
            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            row.Add(spacer);

            if (isInstalled && _installedPackages.TryGetValue(entry.PackageId, out var info))
            {
                Label v = new Label("v" + info.Installed);
                v.AddToClassList("pm-row-version");
                row.Add(v);

                // Update button — only when registry advertised a newer version.
                if (info.IsOutdated)
                {
                    string installId = entry.PackageId;
                    Button update = null;
                    update = new Button(() => OnInstallSingle(installId, update));
                    update.text = "Update v" + info.Latest;
                    update.AddToClassList("pm-row-install-btn");
                    update.AddToClassList("pm-row-install-btn--update");
                    update.tooltip = $"{installId} — installed {info.Installed}, registry has {info.Latest}";
                    row.Add(update);
                    _rowInstallButtons.Add(update);
                }
            }
            else
            {
                string installId = entry.PackageId;
                Button install = null;
                install = new Button(() => OnInstallSingle(installId, install));
                install.text = "Install";
                install.AddToClassList("pm-row-install-btn");
                install.tooltip = installId;
                row.Add(install);
                _rowInstallButtons.Add(install);
            }

            return row;
        }

        private void UpdateToolbar()
        {
            CupkekGamesPackageRegistry.Entry[] entries = CupkekGamesPackageRegistry.GetByTag(PackageTags.GameFull);
            int total = entries.Length;
            int installed = entries.Count(e =>
                _installedPackages != null && _installedPackages.ContainsKey(e.PackageId));
            int missing = total - installed;

            // Count outdated across ALL cupkek packages (not just GameFull) so
            // Update All catches stuff installed via "Install All" too.
            int outdated = CupkekGamesPackageRegistry.Entries.Count(e =>
                _installedPackages != null
                && _installedPackages.TryGetValue(e.PackageId, out var info)
                && info.IsOutdated);

            _headerCount.Clear();
            string countText = $"{installed}/{total} installed";
            if (outdated > 0) countText += $"  ·  {outdated} outdated";
            Label countLabel = new Label(countText);
            countLabel.AddToClassList("pm-toolbar-count-text");
            countLabel.AddToClassList(missing == 0 && outdated == 0
                ? "pm-toolbar-count-text--ok"
                : "pm-toolbar-count-text--missing");
            _headerCount.Add(countLabel);

            if (missing > 0)
            {
                _installGameFullButton.text = $"Install GameFull Packages ({missing})";
                _installGameFullButton.SetEnabled(true);
            }
            else
            {
                _installGameFullButton.text = "GameFull Packages Installed";
                _installGameFullButton.SetEnabled(false);
            }

            // Install All — total count of cupkek packages still missing.
            CupkekGamesPackageRegistry.Entry[] allEntries =
                CupkekGamesPackageRegistry.GetByTag(PackageTags.All);
            int allMissing = allEntries.Count(e =>
                _installedPackages == null || !_installedPackages.ContainsKey(e.PackageId));
            if (_installAllButton != null)
            {
                if (allMissing > 0)
                {
                    _installAllButton.text = $"Install All ({allMissing})";
                    _installAllButton.SetEnabled(true);
                }
                else
                {
                    _installAllButton.text = "All Installed";
                    _installAllButton.SetEnabled(false);
                }
            }

            // Update All — visible when latest-version data exists. Enabled
            // and labelled with the count when any GameFull package is outdated.
            if (_updateAllButton != null)
            {
                bool haveLatestData = _installedPackages != null
                    && _installedPackages.Values.Any(v => !string.IsNullOrEmpty(v.Latest));
                if (haveLatestData)
                {
                    _updateAllButton.style.display = DisplayStyle.Flex;
                    if (outdated > 0)
                    {
                        _updateAllButton.text = $"Update All ({outdated})";
                        _updateAllButton.SetEnabled(true);
                    }
                    else
                    {
                        _updateAllButton.text = "Up to date";
                        _updateAllButton.SetEnabled(false);
                    }
                }
                else
                {
                    _updateAllButton.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateErrorFooter()
        {
            string lastError = CupkekGamesPackageInstaller.LastError;
            if (string.IsNullOrEmpty(lastError))
            {
                _errorLabel.style.display = DisplayStyle.None;
                return;
            }
            _errorLabel.text = "Last install error: " + lastError;
            _errorLabel.style.display = DisplayStyle.Flex;
        }

        // ─────────────────────────────────────────
        //  External-dep row + install
        // ─────────────────────────────────────────

        private VisualElement BuildExternalRow(CupkekGamesExternalDependency dep, HashSet<string> presentAsmdefs)
        {
            // Detect installed-or-not. Prefer the UPM list (it knows the
            // resolved version) and fall back to the asmdef-name probe so
            // Asset-Store-only assets like Animancer are also picked up.
            bool installed = false;
            CupkekGamesPackageInstaller.PackageVersionInfo info = default;
            if (!string.IsNullOrEmpty(dep.PackageId)
                && _installedPackages != null
                && _installedPackages.TryGetValue(dep.PackageId, out info))
            {
                installed = true;
            }
            else if (!string.IsNullOrEmpty(dep.AsmdefName) && presentAsmdefs.Contains(dep.AsmdefName))
            {
                installed = true;
            }

            VisualElement row = new VisualElement();
            row.AddToClassList("pm-row");
            row.AddToClassList(installed ? "pm-row--installed" : "pm-row--missing");

            VisualElement icon = new VisualElement();
            icon.AddToClassList("pm-row-icon");
            icon.AddToClassList(installed ? "pm-row-icon--installed" : "pm-row-icon--missing");
            row.Add(icon);

            Label displayName = new Label(dep.DisplayName);
            displayName.AddToClassList("pm-row-displayname");
            row.Add(displayName);

            // "used by" hint in the secondary id slot
            if (!string.IsNullOrEmpty(dep.UsedBy))
            {
                Label usedBy = new Label("used by " + dep.UsedBy);
                usedBy.AddToClassList("pm-row-packageid");
                row.Add(usedBy);
            }

            // Tag chip — Asset Store paid marker, otherwise nothing
            if (dep.IsPaid)
            {
                VisualElement tagContainer = new VisualElement();
                tagContainer.AddToClassList("pm-row-tags");
                Label paidTag = new Label("Asset Store ($)");
                paidTag.AddToClassList("pm-row-tag");
                tagContainer.Add(paidTag);
                row.Add(tagContainer);
            }

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            row.Add(spacer);

            if (installed)
            {
                Label v = new Label(string.IsNullOrEmpty(info.Installed) ? "Installed" : "v" + info.Installed);
                v.AddToClassList("pm-row-version");
                row.Add(v);

                // Update button — only when this dep is UPM-tracked AND the
                // registry advertised a newer version. Asset-Store deps fall
                // through to the asmdef-only branch above and never set Latest.
                if (info.IsOutdated && !string.IsNullOrEmpty(dep.PackageId))
                {
                    string upgradeId = dep.PackageId;
                    Button update = null;
                    update = new Button(() => OnInstallSingle(upgradeId, update));
                    update.text = "Update v" + info.Latest;
                    update.AddToClassList("pm-row-install-btn");
                    update.AddToClassList("pm-row-install-btn--update");
                    update.tooltip = $"{upgradeId} — installed {info.Installed}, registry has {info.Latest}";
                    row.Add(update);
                    _rowInstallButtons.Add(update);
                }
            }
            else
            {
                // Show whichever action(s) make sense:
                //   - Auto-install via UPM/git when PackageId is set
                //   - Asset Store deep-link when AssetStoreUrl is set
                // Some deps offer both (Ink); paid assets only have the link.
                if (!string.IsNullOrEmpty(dep.PackageId))
                {
                    string installId = dep.PackageId;
                    Button install = null;
                    install = new Button(() => OnInstallSingle(installId, install));
                    install.text = "Install";
                    install.tooltip = installId;
                    install.AddToClassList("pm-row-install-btn");
                    row.Add(install);
                    _rowInstallButtons.Add(install);
                }

                if (!string.IsNullOrEmpty(dep.AssetStoreUrl))
                {
                    string url = dep.AssetStoreUrl;
                    Button asUrl = new Button(() => Application.OpenURL(url));
                    asUrl.text = "Asset Store";
                    asUrl.tooltip = url;
                    asUrl.AddToClassList("pm-row-install-btn");
                    row.Add(asUrl);
                }
            }

            return row;
        }

        /// <summary>
        /// Snapshot of asmdef assembly names currently compiled for the Player.
        /// Used to detect Asset-Store-only deps (Animancer, DamageNumbersPro,
        /// PrimeTween) that don't appear in the UPM package list.
        /// </summary>
        private static HashSet<string> GetCompiledAsmdefNames()
        {
            HashSet<string> set = new HashSet<string>();
            foreach (Assembly a in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                set.Add(a.name);
            }
            return set;
        }

        private void OnInstallSingle(string packageId, Button activeButton)
        {
            EnterBusyState(activeButton, "Installing…");
            CupkekGamesPackageInstaller.InstallByPackageId(packageId, (ok, msg) =>
            {
                if (!ok)
                {
                    Debug.LogError($"[CupkekGames] Failed to install {packageId}: {msg}");
                }
                Refresh();
            });
        }

        private void OnUpdateAll()
        {
            // Update across every cupkek package — not just GameFull — so users who
            // pulled in domain packages via Install All also benefit.
            List<string> ids = CupkekGamesPackageRegistry.Entries
                .Where(e => _installedPackages != null
                    && _installedPackages.TryGetValue(e.PackageId, out var info)
                    && info.IsOutdated)
                .Select(e => e.PackageId)
                .ToList();
            if (ids.Count == 0) return;

            EnterBusyState(_updateAllButton, $"Updating {ids.Count} package(s)…");
            CupkekGamesPackageInstaller.InstallByPackageIds(ids, (ok, msg) =>
            {
                if (!ok)
                {
                    Debug.LogError($"[CupkekGames] Bulk update failed: {msg}");
                }
                Refresh();
            });
            // Reuses InstallByPackageIds — Client.AddAndRemove with the same
            // id list resolves to "upgrade these to latest" because UPM picks
            // the highest compatible version when no explicit version is pinned.
        }

        private void OnInstallGameFullPackages()
        {
            CupkekGamesPackageRegistry.Entry[] entries = CupkekGamesPackageRegistry.GetByTag(PackageTags.GameFull);
            List<string> ids = entries
                .Where(e => _installedPackages == null || !_installedPackages.ContainsKey(e.PackageId))
                .Select(e => e.PackageId)
                .ToList();
            if (ids.Count == 0) return;

            EnterBusyState(_installGameFullButton, $"Installing {ids.Count} package(s)…");
            CupkekGamesPackageInstaller.InstallByPackageIds(ids, (ok, msg) =>
            {
                if (!ok)
                {
                    Debug.LogError($"[CupkekGames] Bulk install failed: {msg}");
                }
                Refresh();
            });
            // Single Client.AddAndRemove call → one manifest write, one domain
            // reload, all transitive deps within com.cupkekgames scope resolve
            // atomically via the scoped registry.
        }

        private void OnInstallAllPackages()
        {
            CupkekGamesPackageRegistry.Entry[] entries = CupkekGamesPackageRegistry.GetByTag(PackageTags.All);
            List<string> ids = entries
                .Where(e => _installedPackages == null || !_installedPackages.ContainsKey(e.PackageId))
                .Select(e => e.PackageId)
                .ToList();
            if (ids.Count == 0) return;

            // Most cupkek domain packages need third-party Asset Store / git deps
            // (PrimeTween, UniTask, Animancer, DamageNumbersPro, Ink, Cinemachine).
            // Without those, packages like combat/character/vfx/etc. produce CS0246
            // until the user installs them. Make the trade-off explicit before
            // committing to ~45 package installs.
            bool confirm = EditorUtility.DisplayDialog(
                "Install All CupkekGames Packages?",
                $"This installs {ids.Count} CupkekGames package(s).\n\n" +
                "Most domain packages (combat, character, vfx, animations, " +
                "audio bridges, etc.) require third-party dependencies like " +
                "PrimeTween, UniTask, Animancer Pro, DamageNumbersPro, Ink, " +
                "or Cinemachine.\n\n" +
                "After install, the External Dependencies section below will " +
                "show which third-party deps you still need. Some are one-click " +
                "(Cinemachine, UniTask); others require an Asset Store visit.\n\n" +
                "Until those third-party deps are installed, the affected " +
                "CupkekGames packages will produce compile errors. Continue?",
                "Install",
                "Cancel");
            if (!confirm) return;

            EnterBusyState(_installAllButton, $"Installing {ids.Count} package(s)…");
            CupkekGamesPackageInstaller.InstallByPackageIds(ids, (ok, msg) =>
            {
                if (!ok)
                {
                    Debug.LogError($"[CupkekGames] Install-all failed: {msg}");
                }
                Refresh();
            });
        }

        // ─────────────────────────────────────────
        //  Busy state
        // ─────────────────────────────────────────

        /// <summary>
        /// Disable every install action and visibly mark <paramref name="activeButton"/>
        /// (if non-null) as the in-flight one with <paramref name="busyText"/>. The next
        /// <see cref="Refresh"/> rebuilds rows and naturally exits busy state — installed
        /// packages render as version-tagged rows; still-missing ones get fresh enabled
        /// install buttons.
        /// </summary>
        private void EnterBusyState(Button activeButton, string busyText)
        {
            if (_installGameFullButton != null)
            {
                _installGameFullButton.SetEnabled(false);
                if (activeButton == _installGameFullButton)
                    _installGameFullButton.text = busyText;
            }

            if (_installAllButton != null)
            {
                _installAllButton.SetEnabled(false);
                if (activeButton == _installAllButton)
                    _installAllButton.text = busyText;
            }

            if (_updateAllButton != null)
            {
                _updateAllButton.SetEnabled(false);
                if (activeButton == _updateAllButton)
                    _updateAllButton.text = busyText;
            }

            if (_refreshButton != null)
                _refreshButton.SetEnabled(false);

            for (int i = 0; i < _rowInstallButtons.Count; i++)
            {
                Button b = _rowInstallButtons[i];
                if (b == null) continue;
                b.SetEnabled(false);
                if (b == activeButton) b.text = busyText;
            }

            // Toolbar count gets a subtle "(working…)" suffix so the user has a
            // top-of-window indicator even when the in-flight button is offscreen.
            if (_headerCount != null && _headerCount.childCount > 0
                && _headerCount[0] is Label countLabel
                && !countLabel.text.EndsWith("(working…)"))
            {
                countLabel.text = countLabel.text + "  (working…)";
            }
        }
    }
}
#endif
