# Creating a new CupkekGames package

End-to-end reference for adding a new `com.cupkekgames.*` package to the ecosystem — from empty GitHub repo to "shows up in the **Tools > CupkekGames > Package Manager** window with one-click install on consumer projects."

This doc supersedes the obsolete `CREATING_A_PACKAGE.md` from the dissolved `com.cupkekgames.core` package (Core split into 9 packages on 2026-04-28; the registry moved here, dep chains changed).

## The five touchpoints

A live package exists in five places:

| # | Touchpoint | Owns |
|---|---|---|
| 1 | GitHub repo `Cupkek-Games/CupkekGames-<Name>` (its own repo, not a folder in package-dev) | Source code, version history, release tags, GitHub Actions |
| 2 | `cupkekgames-package-dev/.gitmodules` | Submodule entry pinning a specific SHA. Other developers' clones get the same package contents you tested against |
| 3 | `luna-docs-next/src/lib/upm-packages.ts` | **Server-side** package list. The Next.js registry route (`docs.cupkek.games/upm/[package]`) only serves packages listed here. Without this entry, the registry returns `"package not found"` even though tarballs exist on GitHub Releases. Vercel auto-redeploys on push to main |
| 4 | `com.cupkekgames.packagemanager/Editor/CupkekGamesPackageRegistry.cs` | **Editor-side** list shown in the Package Manager window. Tag controls which bulk-install button picks it up |
| 5 | `luna-docs-next/src/content/<name>.md` + `src/lib/docs-menu.ts` | Optional but expected for user-facing packages. The PM window's "Docs" button links here |

Plus, on the consumer side: **HeroManager's `Packages/manifest.json`** needs the new package listed (as a version pin once published, or a `file:` override during dev).

**Easy mistake**: registering in touchpoint 4 (the Editor window) without touchpoint 3 (the server). The package shows up in the PM window's list but installing it fails because the registry route returns 404. Both are required.

## 1. Create the GitHub repo

- Owner / org: `Cupkek-Games`
- Repo name: `CupkekGames-<PascalCase>` (e.g. `CupkekGames-Resources`, `CupkekGames-Quests`)
- Default branch: `main`
- License: pick one. The Luna package ships a `Third-Party Notices.md` because it's distributed via Asset Store too; smaller siblings (rpgstats, gamesave, inventory, etc.) skip it. Add it only if the package bundles third-party code.

## 2. Folder layout

The repo IS the Unity package. Don't nest a Unity project inside it. Two layout flavors exist in the ecosystem — pick whichever fits:

### Flat layout — for single-asmdef packages

```
CupkekGames-<Name>/
├── package.json
├── README.md
├── AGENTS.md             ← recommended (AI agent instructions)
├── .github/workflows/release.yml
├── Runtime/
│   ├── CupkekGames.<Name>.asmdef
│   └── <runtime code>
└── Editor/               ← optional
    ├── CupkekGames.<Name>.Editor.asmdef
    └── <editor code>
```

### Multi-asmdef layout — when one repo ships multiple independent asmdefs

Used by `com.cupkekgames.data` (Data / Data.DropTable / Data.Primitives), `com.cupkekgames.resources` (Currencies / Experiences), `com.cupkekgames.gamesave` (GameSave). Each sub-folder is one asmdef; consumers cherry-pick which sub-asmdefs they reference.

```
CupkekGames-<Name>/
├── package.json
├── README.md
├── AGENTS.md
├── .github/workflows/release.yml
├── <SubA>/Runtime/CupkekGames.<Name>.<SubA>.asmdef
├── <SubA>/Editor/CupkekGames.<Name>.<SubA>.Editor.asmdef
├── <SubB>/Runtime/CupkekGames.<Name>.<SubB>.asmdef
└── <SubB>/Editor/CupkekGames.<Name>.<SubB>.Editor.asmdef
```

The two sub-asmdefs are usually **independent** — neither references the other. If they share types, those types live in a third "Core" asmdef both reference.

## 3. `package.json`

Minimum required fields:

```json
{
  "name": "com.cupkekgames.<name>",
  "displayName": "CupkekGames <Display Name>",
  "version": "0.1.0",
  "author": {
    "name": "CupkekGames",
    "url": "https://www.docs.cupkek.games"
  },
  "unity": "6000.0",
  "documentationUrl": "https://docs.cupkek.games/",
  "description": "<one-line summary of what the package provides>",
  "keywords": ["<Domain>", "CupkekGames"],
  "dependencies": {
    "com.cupkekgames.<some-foundation-package>": "<version>"
  }
}
```

Notes:

- `name` is lowercase, dotted, prefixed `com.cupkekgames.`.
- `dependencies` lists only **direct** dependencies. Resolve foundation deps transitively — e.g. depending on `com.cupkekgames.luna` pulls in `singletons`/`pool`/`fadeables`/etc. via Luna's own deps.
- For Asset-Store-distributed packages (only Luna today), also set `changelogUrl` and `repository`.
- If you ship samples, add a `samples` array — see Luna's `package.json` for the format.

**Common dependency pins** (check the latest version in each package's repo before pasting):

| Package | Notes |
|---|---|
| `com.cupkekgames.data` | `CatalogKey`, `IData`, `AssetCatalog<T>` — needed by almost every domain package |
| `com.cupkekgames.services` | `ServiceLocator`, `ServiceProviderSO` |
| `com.cupkekgames.luna` | UI Toolkit framework — pulls in foundation deps transitively |
| `com.cupkekgames.singletons` | `Singleton<T>` |
| `com.cupkekgames.pool` | `GameObjectPool*`, `IObjectPool` |
| `com.cupkekgames.editorinspector` | `MultiLineHeaderAttribute`, `FolderReference` |

There is no longer a `com.cupkekgames.core` dependency — Core was dissolved on 2026-04-28. Reference whichever specific foundation package you actually use.

## 4. Asmdefs

### Runtime asmdef

```json
{
    "name": "CupkekGames.<Name>",
    "rootNamespace": "",
    "references": [
        "GUID:<dep-1-guid>",
        "GUID:<dep-2-guid>"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

References use GUID form (`"GUID:abc..."`) — never raw asmdef names. Look up dep GUIDs in each dep package's `.asmdef.meta` file.

### Editor asmdef (optional)

```json
{
    "name": "CupkekGames.<Name>.Editor",
    "references": [
        "GUID:<your-runtime-asmdef-guid>",
        "GUID:<other-deps>"
    ],
    "includePlatforms": ["Editor"]
}
```

### Common GUIDs

| Asmdef | GUID |
|---|---|
| `CupkekGames.Data` (Runtime) | `a57eb40d9a9e0984a816dda1fdae8adf` |
| `CupkekGames.Data.DropTable` (Runtime) | `e8c4a1f2036b4d7e9a5b2c8d0e1f3a6b` |
| `CupkekGames.Data.Primitives` (Runtime) | `99af10b529824702946fb144b93bed73` |
| `CupkekGames.Services` (Runtime) | `c95c4fa533d6fdb47871e2223c77f6d0` |
| `CupkekGames.Luna` (Runtime) | `8c5a58f4ceeaeff428a5333f02ab4313` |
| `CupkekGames.Luna.Editor` | `84651a3751eca9349aac36a66bba901b` |

If a GUID drifts, look it up in the relevant `.asmdef.meta` file.

### Hand-creating asmdef `.meta` files

When you create an asmdef *outside* of Unity (typical when scripting package scaffolding), Unity generates the `.meta` file on next project open — but with a **random** GUID. If a consumer asmdef needs to reference your new asmdef in the same commit, the consumer's `"GUID:..."` ref points at a GUID that doesn't exist yet, which Unity then breaks.

Two workarounds:

1. **Pre-create the `.meta` file** with a chosen GUID (any 32-char hex string works — generate via `python -c "import uuid; print(uuid.uuid4().hex)"` or PowerShell `[guid]::NewGuid().ToString('N')`). Format:

   ```yaml
   fileFormatVersion: 2
   guid: <your-32-char-hex>
   AssemblyDefinitionImporter:
     externalObjects: {}
     userData:
     assetBundleName:
     assetBundleVariant:
   ```

   Then update the consumer asmdef in the same commit.

2. **Defer the consumer update** — let Unity generate the random GUID first, then update the consumer asmdef with the actual GUID in a second commit.

Option 1 is cleaner for atomic commits; option 2 is fewer moving parts.

## 5. Release workflow

Every package gets one file at `.github/workflows/release.yml`. It's the same across the ecosystem — the real work lives in a reusable workflow at `Cupkek-Games/.github`:

```yaml
name: Release

# Tag-driven publish: packs the package and uploads tarball + sidecar JSON to
# the GitHub Release. The Cupkek-Games/.github reusable workflow does all the
# real work — see https://github.com/Cupkek-Games/.github for details.
#
# To release:
#   1. Bump version in package.json
#   2. Commit, tag v<version>, push tag
#   3. Action runs, asset lands in Releases, registry route picks it up

on:
  push:
    tags: ['v*.*.*']

jobs:
  release:
    uses: Cupkek-Games/.github/.github/workflows/upm-release.yml@main
    permissions:
      contents: write
```

Copy verbatim. The reusable workflow handles tarball creation, sidecar JSON, GitHub Release upload, and the route into the UPM registry at `docs.cupkek.games/upm`.

## 6. Register as a submodule in `cupkekgames-package-dev`

```bash
cd cupkekgames-package-dev
git submodule add https://github.com/Cupkek-Games/CupkekGames-<Name>.git Packages/com.cupkekgames.<name>
git commit -m "Add com.cupkekgames.<name> submodule"
```

Submodules are pinned by SHA (no `branch = main` in `.gitmodules` on purpose — see the package-dev README). To update later: `cd Packages/com.cupkekgames.<name> && git pull` then commit the new submodule pointer from the outer repo.

## 7. Register in the UPM registry (server-side)

**Required.** The registry at `docs.cupkek.games/upm` is a Next.js handler in `luna-docs-next` that looks up a known package id and redirects to the latest GitHub Release tarball. The known-package list is at `luna-docs-next/src/lib/upm-packages.ts`:

```typescript
export const UPM_PACKAGES: Record<string, UpmPackageEntry> = {
  // ...existing entries...
  "com.cupkekgames.<name>":        { repo: "Cupkek-Games/CupkekGames-<Name>" },
};
```

Append one line. Commit + push to luna-docs-next's `main`. Vercel auto-redeploys within ~1 minute.

Verify with `curl -s https://www.docs.cupkek.games/upm/com.cupkekgames.<name>` — expect a JSON packument listing all versions you've released, not `{"error":"package not found in registry"}`.

If you skip this step, the package shows up in the in-Editor PM window list (touchpoint 8 below) but installing it fails because Unity can't fetch a tarball from a 404.

## 8. Register in the in-Editor Package Manager

Add an entry to `Packages/com.cupkekgames.packagemanager/Editor/CupkekGamesPackageRegistry.cs`:

```csharp
new Entry(
    "com.cupkekgames.<name>",      // package id
    "<Display Name>",              // shown in PM window
    PackageTags.GameFull,          // see PackageTags class — tag(s) determine which bulk-install button picks this up
    PackageTags.All                // usually also add PackageTags.All
).WithDocs(DocsBase + "/<docs-page-slug>"),  // omit .WithDocs(...) if no docs page exists yet
```

Order in the file matters: leaf dependencies first, packages that consume them after. Unity resolves bulk installs in declaration order.

### Tag policy (from existing registry comments)

- **`PackageTags.GameFull`** — only packages the Luna GameFull sample actually uses. Lean set (~20). Installs cleanly with no third-party Asset Store deps.
- **`PackageTags.All`** — every CupkekGames package. Heavier domain packages (combat, character, vfx, etc.) need third-party deps; consumers using "Install All" install those manually via the External Dependencies section.
- **`PackageTags.Luna`** — only Luna itself. Renders as its own top section in the PM window.

Most new packages get tagged with **both** `GameFull` (if relevant) and `All`.

## 8. Documentation page (optional but expected for user-facing packages)

Two files in `luna-docs-next/`:

1. **`src/content/<name>.md`** — the page itself.

   ```markdown
   ---
   title: <Display Name>
   ---

   <One-paragraph intro: what the package provides, who consumes it.>

   ## Includes (High-Level)

   - <key feature 1>
   - <key feature 2>

   ## Suggested Mental Model

   - **<Concept A>**: <one-line definition>
   - **<Concept B>**: <one-line definition>
   ```

   Match the tone of `src/content/rpgstats.md` or `src/content/gamesave.md` — short, scannable, links to deeper reference docs as needed.

2. **`src/lib/docs-menu.ts`** — sidebar entry:

   ```typescript
   {
       id: "<name>",
       label: "<Display Name>",
       icon: "lucide--package",
       url: "/<name>",
   },
   ```

   Place near related siblings (e.g. resources next to inventory and rpgstats).

After both files exist, the registry entry's `.WithDocs(DocsBase + "/<name>")` URL resolves.

## 9. Tag and publish

Once everything above is committed to the package's own repo:

```bash
# Inside the package's repo (not the package-dev outer repo)
git tag v0.1.0
git push origin main --tags
```

The GitHub Action triggers on the tag push. Within ~30 seconds, the GitHub Release page shows the tarball + sidecar JSON. The UPM registry at `docs.cupkek.games/upm` picks it up automatically (no extra step).

Verify:

```bash
curl -s https://docs.cupkek.games/upm/com.cupkekgames.<name>/-/com.cupkekgames.<name>-0.1.0.tgz | head -c 100
```

Should return tarball bytes, not 404.

## 10. Wire HeroManager (or other consumers)

Once published, HeroManager pins via version:

```jsonc
// HeroManager/Packages/manifest.json
{
  "dependencies": {
    "com.cupkekgames.<name>": "0.1.0",
    // ...
  }
}
```

### During dev — `file:` override

To work on a package and have HeroManager pick up uncommitted changes, override the version pin with a local path:

```jsonc
"com.cupkekgames.<name>": "file:../cupkekgames-package-dev/Packages/com.cupkekgames.<name>"
```

The relative path is from HeroManager's `Packages/` folder to the package-dev clone. Adjust if your folder layout differs.

This is the standard dev workflow per [MULTI_REPO.md](https://github.com/Cupkek-Games/HeroManager/blob/master/docs/MULTI_REPO.md). Switch back to the version pin before committing the manifest change to HeroManager.

## Naming conventions

See [README.md § Package naming conventions](../README.md#package-naming-conventions) for the full rule. Quick version:

**The namespace's last segment must not match a class/struct/interface defined inside it.** Otherwise C# resolves the enclosing namespace before file-scope `using` directives and consumers get CS0118 ("X is a namespace but is used like a type") when they reference the bare class name.

Apply one of:
- **Pluralize the namespace** — `CupkekGames.Units` for a class `Unit`. Most common.
- **Different final segment** — `CupkekGames.Services` for a class `ServiceLocator`. Use when pluralizing reads badly.
- **Suffix the package id too** — keep `com.cupkekgames.<name>`, namespace `CupkekGames.<Name>`, asmdef `CupkekGames.<Name>` in lockstep with the pluralization choice.

## Extracting from a game project

If the new package starts as code inside `Assets/`:

1. Create the empty repo (steps 1–5 above).
2. Clone it into the package-dev repo: `cd cupkekgames-package-dev && git submodule add ...`
3. **Move files with `git mv`** (or plain `Move-Item` if untracked) — preserves `.meta` GUIDs so existing prefab/scene references in the game keep resolving.
4. Update any namespaces (`MyGame.Foo` → `CupkekGames.<Name>.Foo`).
5. Add the new package's runtime asmdef ref to any game asmdefs that consume the moved code.
6. Verify the game still compiles + runs locally (with a `file:` override) before pushing the new repo and publishing v0.1.0.

## Checklist

Per-package work:

- [ ] GitHub repo `Cupkek-Games/CupkekGames-<Name>` exists, default branch `main`
- [ ] `package.json` with id, displayName, version, deps
- [ ] Runtime asmdef referencing actual deps (data / services / etc.)
- [ ] (If editor code) Editor asmdef
- [ ] README + AGENTS.md
- [ ] `.github/workflows/release.yml` copied verbatim from a sibling package
- [ ] Tagged `v0.1.0` and pushed (verifies the release workflow works)

Ecosystem integration:

- [ ] Submodule added to `cupkekgames-package-dev/.gitmodules`
- [ ] `Entry` in `CupkekGamesPackageRegistry.Entries` with appropriate `PackageTags`
- [ ] (If user-facing) Doc page in `luna-docs-next/src/content/<name>.md` + sidebar entry in `src/lib/docs-menu.ts`
- [ ] (Once published) Version pin added to HeroManager's `Packages/manifest.json`

## Common pitfalls

- **Forgetting the submodule SHA bump after pushing to the package.** Pushed a change to `com.cupkekgames.foo` but didn't update `cupkekgames-package-dev`'s submodule pointer → other devs cloning package-dev still see the old SHA.
- **Tagging without bumping `version` in `package.json`.** The release workflow uses the tag for the asset name but the manifest version for what UPM sees — mismatched and consumers get a confusing dep-resolve error.
- **Registering with `.WithDocs(...)` before the docs page exists.** Pretty harmless (the link 404s) but ugly — comment out `.WithDocs(...)` until the page lands.
- **Asmdef references to a sibling's GUID before that sibling's `.meta` exists.** See § 4 "Hand-creating asmdef `.meta` files".
- **Tag format.** Must be `v<X.Y.Z>` exactly. `0.1.0` (no `v`) or `v0.1` (missing patch) won't trigger the workflow.

## See also

- [`packagemanager/README.md`](../README.md) — registry overview + naming convention rules
- [`packagemanager/Editor/CupkekGamesPackageRegistry.cs`](../Editor/CupkekGamesPackageRegistry.cs) — where to register
- [`cupkekgames-package-dev/README.md`](https://github.com/Cupkek-Games/cupkekgames-package-dev/blob/main/README.md) — submodule workflow
- Luna's [`AGENTS.md`](https://github.com/Cupkek-Games/CupkekGames-Luna/blob/main/AGENTS.md) — Asset Store distribution model + cross-package URL rules
