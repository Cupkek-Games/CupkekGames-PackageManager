#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace CupkekGames.PackageManager.Editor
{
    /// <summary>
    /// Optional third-party dependencies that unlock features in CupkekGames
    /// sibling packages. Listed in the package-manager window so consumers can
    /// see what's missing and either auto-install (Unity Registry / git URL)
    /// or jump to the Asset Store page (paid / Asset-Store-only assets).
    /// </summary>
    public class CupkekGamesExternalDependency
    {
        /// <summary>Human-readable name shown in the row.</summary>
        public string DisplayName;

        /// <summary>
        /// UPM package id (e.g. <c>com.unity.cinemachine</c>) or a git URL
        /// accepted by <c>UnityEditor.PackageManager.Client.Add</c>. <c>null</c>
        /// means "no UPM install path available — Asset Store only".
        /// </summary>
        public string PackageId;

        /// <summary>
        /// Asmdef assembly name used to detect "installed" when the dependency
        /// isn't a UPM package (e.g. Animancer, DamageNumbersPro shipped via
        /// the Asset Store). Falls back to PackageId-based detection when set.
        /// </summary>
        public string AsmdefName;

        /// <summary>
        /// Optional Asset Store deep-link URL. Shown as a secondary action when
        /// set. For paid assets this is the only install path.
        /// </summary>
        public string AssetStoreUrl;

        /// <summary>Which CupkekGames sibling package(s) consume this dep.</summary>
        public string UsedBy;

        /// <summary>Marker for UI ("Asset Store ($)" badge).</summary>
        public bool IsPaid;
    }

    public static partial class CupkekGamesExternalDependencyRegistry
    {
        [NoAutoStaticsCleanup]
        private static readonly CupkekGamesExternalDependency[] _all = new[]
        {
            new CupkekGamesExternalDependency
            {
                DisplayName    = "Cinemachine",
                PackageId      = "com.unity.cinemachine",
                AsmdefName     = "Unity.Cinemachine",
                UsedBy         = "Cameras (CinemachineManager / screen-shake impulses)",
                IsPaid         = false,
            },
            new CupkekGamesExternalDependency
            {
                DisplayName    = "UniTask",
                PackageId      = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                AsmdefName     = "UniTask",
                UsedBy         = "TimeSystem (DelayAsync helpers); used widely as the async/await primitive across CupkekGames",
                IsPaid         = false,
            },
            new CupkekGamesExternalDependency
            {
                DisplayName    = "PrimeTween",
                PackageId      = null, // Asset Store / OpenUPM — no canonical auto-install path.
                AsmdefName     = "PrimeTween.Runtime",
                AssetStoreUrl  = "https://assetstore.unity.com/packages/tools/animation/primetween-high-performance-animations-and-sequences-252960",
                UsedBy         = "TimeSystem (TimeScaleTween — only enables Tween scaling)",
                IsPaid         = false,
            },
            new CupkekGamesExternalDependency
            {
                DisplayName    = "Animancer Pro",
                PackageId      = null,
                AsmdefName     = "Kybernetik.Animancer",
                AssetStoreUrl  = "https://assetstore.unity.com/packages/tools/animation/animancer-pro-v8-293522",
                UsedBy         = "Animations (Animancer bridge)",
                IsPaid         = true,
            },
            new CupkekGamesExternalDependency
            {
                DisplayName    = "DamageNumbersPro",
                PackageId      = null,
                AsmdefName     = "DamageNumbersPro",
                AssetStoreUrl  = "https://assetstore.unity.com/packages/tools/gui/damage-numbers-pro-numbers-text-popups-186220",
                UsedBy         = "TextPopup (DamageNumbersPro bridge)",
                IsPaid         = true,
            },
            new CupkekGamesExternalDependency
            {
                DisplayName    = "Ink Unity Integration",
                PackageId      = null,
                AsmdefName     = "Ink-Libraries",
                AssetStoreUrl  = "https://assetstore.unity.com/packages/tools/integration/ink-integration-for-unity-60055",
                UsedBy         = "InkBridge (visual-novel runtime)",
                IsPaid         = false,
            },
        };

        public static IReadOnlyList<CupkekGamesExternalDependency> All => _all;
    }
}
#endif
