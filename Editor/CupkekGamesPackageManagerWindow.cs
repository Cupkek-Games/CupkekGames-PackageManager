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
        private Button _refreshButton;
        private VisualElement _rowsContainer;
        private Label _errorLabel;

        private Dictionary<string, string> _installedPackages;
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
                "(www.docs.cupkek.games/upm). Required for the Luna GameFull sample. " +
                "Installing here writes the registry block to your Packages/manifest.json automatically.");
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

            _installGameFullButton = new Button(OnInstallGameFullPackages);
            _installGameFullButton.AddToClassList("pm-toolbar-btn");
            _installGameFullButton.AddToClassList("pm-toolbar-btn--primary");
            toolbar.Add(_installGameFullButton);

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
            Label loading = new Label("Detecting installed packages…");
            loading.AddToClassList("pm-loading-text");
            _rowsContainer.Add(loading);

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
            });
        }

        private void BuildRows()
        {
            _rowsContainer.Clear();
            _rowInstallButtons.Clear();

            // Section 1: CupkekGames sibling packages (the GameFull bundle).
            VisualElement cupkekHeader = new VisualElement();
            cupkekHeader.AddToClassList("pm-section-header");
            Label cupkekHeaderText = new Label("CupkekGames Packages");
            cupkekHeaderText.AddToClassList("pm-section-header-text");
            cupkekHeader.Add(cupkekHeaderText);
            _rowsContainer.Add(cupkekHeader);

            CupkekGamesPackageRegistry.Entry[] entries = CupkekGamesPackageRegistry.GetByTag(PackageTags.GameFull);
            foreach (CupkekGamesPackageRegistry.Entry entry in entries)
            {
                _rowsContainer.Add(BuildRow(entry));
            }

            // Section 2: Optional third-party deps that unlock features in
            // sibling packages. Cinemachine + UniTask auto-install via UPM /
            // git URL; Animancer / DamageNumbersPro / PrimeTween / Ink need
            // an Asset Store visit.
            VisualElement extHeader = new VisualElement();
            extHeader.AddToClassList("pm-section-header");
            Label extHeaderText = new Label("External Dependencies (optional)");
            extHeaderText.AddToClassList("pm-section-header-text");
            extHeader.Add(extHeaderText);
            Label extHeaderSub = new Label(
                "Unlock extra features in sibling packages. Auto-installs the UPM/git deps; " +
                "Asset Store assets open in your browser.");
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

            if (isInstalled && _installedPackages.TryGetValue(entry.PackageId, out string version))
            {
                Label v = new Label("v" + version);
                v.AddToClassList("pm-row-version");
                row.Add(v);
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

            _headerCount.Clear();
            Label countLabel = new Label($"{installed}/{total} installed");
            countLabel.AddToClassList("pm-toolbar-count-text");
            countLabel.AddToClassList(missing == 0 ? "pm-toolbar-count-text--ok" : "pm-toolbar-count-text--missing");
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
            string installedVersion = null;
            if (!string.IsNullOrEmpty(dep.PackageId)
                && _installedPackages != null
                && _installedPackages.TryGetValue(dep.PackageId, out installedVersion))
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
                Label v = new Label(string.IsNullOrEmpty(installedVersion) ? "Installed" : "v" + installedVersion);
                v.AddToClassList("pm-row-version");
                row.Add(v);
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
