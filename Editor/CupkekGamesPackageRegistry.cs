#if UNITY_EDITOR
using System.Linq;

namespace CupkekGames.PackageManager.Editor
{
    /// <summary>Tag constants used by <see cref="CupkekGamesPackageRegistry.Entry.Tags"/>.</summary>
    public static class PackageTags
    {
        /// <summary>Required by the Luna GameFull sample.</summary>
        public const string GameFull = "GameFull";
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
        public static readonly Entry[] Entries = new[]
        {
            new Entry("com.cupkekgames.singletons",       "Singleton",       PackageTags.GameFull),
            new Entry("com.cupkekgames.pool",            "Pool",            PackageTags.GameFull),
            new Entry("com.cupkekgames.fadeables",        "Fadeable",        PackageTags.GameFull),
            new Entry("com.cupkekgames.keyvaluedatabases","KeyValueDatabase",PackageTags.GameFull),
            new Entry("com.cupkekgames.prefabloaders",    "PrefabLoader",    PackageTags.GameFull),
            new Entry("com.cupkekgames.assetfinder",     "AssetFinder",     PackageTags.GameFull),
            new Entry("com.cupkekgames.transforms",      "Transforms",      PackageTags.GameFull),
            new Entry("com.cupkekgames.editorui",        "EditorUI",        PackageTags.GameFull),
            new Entry("com.cupkekgames.editorinspector", "EditorInspector", PackageTags.GameFull),
            new Entry("com.cupkekgames.services",  "ServiceLocator",  PackageTags.GameFull),
            new Entry("com.cupkekgames.data",            "Data",            PackageTags.GameFull),
            new Entry("com.cupkekgames.gamesave",        "GameSave",        PackageTags.GameFull),
            new Entry("com.cupkekgames.newtonsoft",      "Newtonsoft",      PackageTags.GameFull),
            new Entry("com.cupkekgames.rpgstats",        "RPGStats",        PackageTags.GameFull),
            new Entry("com.cupkekgames.inventory",       "Inventory",       PackageTags.GameFull),
            new Entry("com.cupkekgames.addressableassets",    "Addressables",    PackageTags.GameFull),
            new Entry("com.cupkekgames.scenemanagement", "SceneManagement", PackageTags.GameFull),
            new Entry("com.cupkekgames.sequencer",       "Sequencer",       PackageTags.GameFull),
            new Entry("com.cupkekgames.settings",        "Settings",        PackageTags.GameFull),
            new Entry("com.cupkekgames.inkbridge",             "Ink",             PackageTags.GameFull),
            // Phase A — extracted from HM Plugins/CupkekGames/, 2026-04-30
            new Entry("com.cupkekgames.diagnostics",     "Diagnostics",     PackageTags.GameFull),
            new Entry("com.cupkekgames.textpopup",       "TextPopup",       PackageTags.GameFull),
            new Entry("com.cupkekgames.audio",           "Audio",           PackageTags.GameFull),
            new Entry("com.cupkekgames.animations",      "Animations",      PackageTags.GameFull),
            new Entry("com.cupkekgames.navigation",      "Navigation",      PackageTags.GameFull),
            new Entry("com.cupkekgames.quests",          "Quests",          PackageTags.GameFull),
            new Entry("com.cupkekgames.editortools",     "EditorTools",     PackageTags.GameFull),
            // Phase D — TimeSystem extraction
            new Entry("com.cupkekgames.timesystem",      "TimeSystem",      PackageTags.GameFull),
            // Phase C — Tier 2 bridges (third-party plugin adapters)
            new Entry("com.cupkekgames.textpopup.damagenumberspro", "TextPopup.DamageNumbersPro", PackageTags.GameFull),
            new Entry("com.cupkekgames.audio.sonity",              "Audio.Sonity",              PackageTags.GameFull),
            new Entry("com.cupkekgames.animations.animancer",      "Animations.Animancer",      PackageTags.GameFull),
            // Phase D — BehaviourTrees + StateMachines (pluralized to fix namespace=class collision)
            new Entry("com.cupkekgames.behaviourtrees",            "BehaviourTrees",            PackageTags.GameFull),
            new Entry("com.cupkekgames.statemachines",             "StateMachines",             PackageTags.GameFull),
        };

        /// <summary>Entries with the given tag, in registration order.</summary>
        public static Entry[] GetByTag(string tag)
            => Entries.Where(e => e.HasTag(tag)).ToArray();
    }
}
#endif
