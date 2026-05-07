#if UNITY_EDITOR
using System.Linq;

namespace CupkekGames.PackageManager.Editor
{
    /// <summary>Tag constants used by <see cref="CupkekGamesPackageRegistry.Entry.Tags"/>.</summary>
    public static class PackageTags
    {
        /// <summary>
        /// The Luna UI package itself. Headline UI Toolkit framework — pulls in its
        /// bundled foundation deps (singletons, pool, fadeables, keyvaluedatabases,
        /// prefabloaders, input, etc.) transitively via package.json. Shown as its
        /// own top section in the Package Manager window.
        /// </summary>
        public const string Luna = "Luna";

        /// <summary>
        /// Required by the Luna GameFull sample. Lean set — only the cupkek packages
        /// the sample actually uses (foundation primitives + data layer + inventory +
        /// settings + scene transitions). Pure Luna-style: no third-party Asset Store
        /// or external deps required to compile.
        /// </summary>
        public const string GameFull = "GameFull";

        /// <summary>
        /// Every CupkekGames sibling package. Used by the "Install All" bulk button
        /// for HeroManager-style projects that want the entire stack. Most of these
        /// require Asset Store / git deps (PrimeTween, UniTask, Animancer, etc.) —
        /// the user is expected to install those separately via the External
        /// Dependencies section.
        /// </summary>
        public const string All = "All";
    }

    public static class CupkekGamesPackageRegistry
    {
        public readonly struct Entry
        {
            public readonly string PackageId;
            public readonly string DisplayName;
            public readonly string[] Tags;

            public Entry(string packageId, string displayName, params string[] tags)
            {
                PackageId = packageId;
                DisplayName = displayName;
                Tags = tags ?? System.Array.Empty<string>();
            }

            public bool HasTag(string tag)
            {
                if (Tags == null) return false;
                for (int i = 0; i < Tags.Length; i++)
                    if (Tags[i] == tag) return true;
                return false;
            }
        }

        // Distributed via the CupkekGames UPM scoped registry at
        // https://www.docs.cupkek.games/upm. Tarballs in each repo's GitHub
        // Releases. See Documentation/CREATING_A_PACKAGE.md for the release flow.
        // Order matters: leaf deps first, packages that depend on them after.
        //
        // Tag policy:
        //   - GameFull: ONLY packages the Luna GameFull sample actually uses.
        //               Lean set (~20) — installs cleanly with no third-party deps.
        //   - All:      every cupkek package. Most of the heavier domain packages
        //               (combat, character, vfx, etc.) need third-party deps;
        //               consumers using "Install All" must install those manually
        //               via the External Dependencies section.
        public static readonly Entry[] Entries = new[]
        {
            // ── Luna UI (headline package) ──
            // Tagged Luna only so the PM window renders it as its own top
            // section. GameFull packages transitively depend on Luna via
            // their package.json so it auto-installs alongside them anyway.
            new Entry("com.cupkekgames.luna",               "Luna UI",           PackageTags.Luna, PackageTags.All),

            // ── Foundation (needed by GameFull) ──
            new Entry("com.cupkekgames.singletons",         "Singleton",         PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.pool",               "Pool",              PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.fadeables",          "Fadeable",          PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.keyvaluedatabases",  "KeyValueDatabase",  PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.prefabloaders",      "PrefabLoader",      PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.assetfinder",        "AssetFinder",       PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.transforms",         "Transforms",        PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.editorui",           "EditorUI",          PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.editorinspector",    "EditorInspector",   PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.input",              "Input",             PackageTags.GameFull, PackageTags.All),

            // ── Data layer (needed by GameFull) ──
            new Entry("com.cupkekgames.services",           "ServiceLocator",    PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.data",               "Data",              PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.gamesave",           "GameSave",          PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.newtonsoft",         "Newtonsoft",        PackageTags.GameFull, PackageTags.All),

            // ── Inventory + RPG Stats (needed by GameFull) ──
            new Entry("com.cupkekgames.rpgstats",           "RPGStats",          PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.inventory",          "Inventory",         PackageTags.GameFull, PackageTags.All),

            // ── Scene + sequence + settings (needed by GameFull) ──
            new Entry("com.cupkekgames.addressableassets",  "Addressables",      PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.scenemanagement",    "SceneManagement",   PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.sequencer",          "Sequencer",         PackageTags.GameFull, PackageTags.All),
            new Entry("com.cupkekgames.settings",           "Settings",          PackageTags.GameFull, PackageTags.All),

            // ── All-only packages (require third-party deps; not part of GameFull sample) ──

            // Phase A — extracted from HM Plugins/CupkekGames/, 2026-04-30
            new Entry("com.cupkekgames.diagnostics",        "Diagnostics",       PackageTags.All),
            new Entry("com.cupkekgames.textpopup",          "TextPopup",         PackageTags.All),
            new Entry("com.cupkekgames.audio",              "Audio",             PackageTags.All),
            new Entry("com.cupkekgames.animations",         "Animations",        PackageTags.All),
            new Entry("com.cupkekgames.navigation",         "Navigation",        PackageTags.All),
            new Entry("com.cupkekgames.quests",             "Quests",            PackageTags.All),
            new Entry("com.cupkekgames.editortools",        "EditorTools",       PackageTags.All),

            // Phase D — TimeSystem extraction
            new Entry("com.cupkekgames.timesystem",         "TimeSystem",        PackageTags.All),

            // Phase D — BehaviourTrees + StateMachines
            new Entry("com.cupkekgames.behaviourtrees",     "BehaviourTrees",    PackageTags.All),
            new Entry("com.cupkekgames.statemachines",      "StateMachines",     PackageTags.All),

            // Phase J — final HM framework extraction batch, 2026-05-02
            new Entry("com.cupkekgames.cameras",            "Cameras",           PackageTags.All),
            new Entry("com.cupkekgames.character",          "Character",         PackageTags.All),
            new Entry("com.cupkekgames.units",              "Units",             PackageTags.All),
            new Entry("com.cupkekgames.shapes",             "Shapes",            PackageTags.All),
            new Entry("com.cupkekgames.shapedrawing",       "ShapeDrawing",      PackageTags.All),
            new Entry("com.cupkekgames.shapedrawing.shapes","ShapeDrawing.Shapes", PackageTags.All),
            new Entry("com.cupkekgames.vfx",                "VFX",               PackageTags.All),
            new Entry("com.cupkekgames.vfx.mktoon",         "VFX.MKToon",        PackageTags.All),
            new Entry("com.cupkekgames.vfx.potatoon",       "VFX.PotaToon",      PackageTags.All),
            new Entry("com.cupkekgames.combat",             "Combat",            PackageTags.All),
            new Entry("com.cupkekgames.inventorysystem.crafting","InventorySystem.Crafting", PackageTags.All),
            new Entry("com.cupkekgames.inventorysystem.farming", "InventorySystem.Farming",  PackageTags.All),
            new Entry("com.cupkekgames.localization",       "Localization",      PackageTags.All),

            // Phase C — Tier 2 bridges (third-party plugin adapters)
            new Entry("com.cupkekgames.inkbridge",          "Ink",               PackageTags.All),
            new Entry("com.cupkekgames.textpopup.damagenumberspro", "TextPopup.DamageNumbersPro", PackageTags.All),
            new Entry("com.cupkekgames.audio.sonity",       "Audio.Sonity",      PackageTags.All),
            new Entry("com.cupkekgames.animations.animancer","Animations.Animancer", PackageTags.All),
        };

        /// <summary>Entries with the given tag, in registration order.</summary>
        public static Entry[] GetByTag(string tag)
            => Entries.Where(e => e.HasTag(tag)).ToArray();
    }
}
#endif
