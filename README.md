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
