# CupkekGames Package Manager

Editor window + installer for the CupkekGames UPM scoped registry. Extracted from `com.cupkekgames.core`.

## What's inside

**Editor** (`CupkekGames.PackageManager.Editor.asmdef`)
- `CupkekGamesPackageRegistry` — central list of `com.cupkekgames.*` packages with display names + tags
- `CupkekGamesPackageInstaller` — UPM client wrapper that writes the `scopedRegistries` block to `Packages/manifest.json`
- `CupkekGamesPackageManagerWindow` — `Tools > CupkekGames > Package Manager` UI

## Bootstrap

This package is the entry point for installing other `com.cupkekgames.*` packages via the scoped registry. Install it **first** via:

- Git URL: `https://github.com/Cupkek-Games/CupkekGames-PackageManager.git`
- Or local: `"com.cupkekgames.packagemanager": "file:../path/to/com.cupkekgames.packagemanager"`

Open `Tools > CupkekGames > Package Manager` to install the rest.

## Dependencies

None.

## Package naming conventions

When creating a new `com.cupkekgames.*` package, **the namespace's last segment must not match a class/struct/interface name defined inside it**. C# resolves enclosing-namespace names before file-scope using directives, so a collision means consumers in any `CupkekGames.*` namespace will see CS0118 ("X is a namespace but is used like a type") whenever they reference the bare class name.

Apply one of these patterns:

| Pattern | Example | Use when |
|---|---|---|
| **Pluralize the namespace** | namespace `CupkekGames.Units`, class `Unit` | Multiple instances of the type are common (collections, catalogs, pools). The pluralized container reads naturally. |
| **Different final segment** | namespace `CupkekGames.Services`, class `ServiceLocator` | Class name is iconic and shouldn't change; pluralizing wouldn't read well. |
| **Suffix the package id too** | `com.cupkekgames.units`, `com.cupkekgames.services` | Always — keep folder, package id, namespace, asmdef name in lockstep. |

Past renames driven by this rule (2026-04-29):
- `CupkekGames.ServiceLocator` → `CupkekGames.Services` (class `ServiceLocator` kept)
- `CupkekGames.Addressables` → `CupkekGames.AddressableAssets` (also avoids shadowing Unity's `Addressables` class)
- `CupkekGames.Fadeable` → `CupkekGames.Fadeables` (class `Fadeable` kept)
- `CupkekGames.PrefabLoader` → `CupkekGames.PrefabLoaders` (class `PrefabLoader<T>` kept)
- `CupkekGames.Singleton` → `CupkekGames.Singletons` (class `Singleton<T>` kept)
- `CupkekGames.KeyValueDatabase` → `CupkekGames.KeyValueDatabases` (class `KeyValueDatabase<,>` + `KeyValueDatabaseMono`/`SO`/`MonoSO` + `KeyValuePair` kept)
- `com.cupkekgames.ink` package id → `com.cupkekgames.inkbridge` (avoided Ink integration's `IsInkFile` matching the package folder by `.ink` suffix; namespace `CupkekGames.Luna.Ink` was unaffected)

Latent collisions still in the codebase (will surface as CS0118 the moment a consumer in a `CupkekGames.*` namespace tries to use the bare class name):

- `CupkekGames.Data.DropTable` (class `DropTable`) — sub-namespace, lower urgency since most consumers sit outside `CupkekGames.Data`
- `CupkekGames.BehaviourTree` (class `BehaviourTree`) — currently in `Assets/Plugins/CupkekGames/`, will surface when extracted
- `CupkekGames.StateMachine` (class `StateMachine`) — same

When extracting one of these, prefer the rename over the per-file `using TypeName = CupkekGames.Foo.TypeName;` alias workaround.
