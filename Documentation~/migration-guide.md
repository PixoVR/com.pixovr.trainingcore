# Migrating a Luminous project

The migrator is a byte-preserving line rewriter (`YamlRewriter`) over a mapping table
(`luminous-map.json` → `MigrationMap`), plus a `Packages/manifest.json` rewriter and a
third-party asset relocator. Everything is driven by `LuminousMigrator.Run(MigrationOptions)`.

## 1. Prerequisites

- Unity **2021.3 LTS** project on a throwaway branch, tree clean (`git status`), LFS pulled.
- Manifest already resolves `com.pixovr.trainingcore`, `com.pixovr.apexunitysdk`, and
  NodeGraphProcessor via git URLs — the manifest rewriter only adds registry-style deps,
  so add these yourself first:

  ```json
  "com.alelievr.node-graph-processor": "https://github.com/alelievr/NodeGraphProcessor.git?path=/Assets/com.alelievr.NodeGraphProcessor#1.3.1",
  "com.pixovr.apexunitysdk": "https://github.com/PixoVR/com.pixovr.apexunitysdk.git",
  "com.pixovr.trainingcore": "https://github.com/PixoVR/com.pixovr.trainingcore.git",
  ```

- The `Luminous Packages/` folder still present — the map keys on the Luminous DLL
  guids and middlemen `.meta` guids.

## 2. Dry run first

Editor: `Tools/PixoVR/Migrate Luminous Project…` (the **window**, not a menu under Pixo).

| Option (`MigrationOptions`) | Default | Meaning |
|---|---|---|
| `DryRun` | `true` (window) / `false` (CLI) | report only, no file writes |
| `ExtendedRules` | `true` | qualified type strings, UnityEvent targets, asset guid remaps, residual-Luminous rows; off = script/managed-ref rewrites only |
| `ExtraMapPath` | — | project-specific overlay map merged over `luminous-map.json` |
| `RelocateThirdParty` | `true` | move HighlightPlus / Ultimate Replay folders + `HighlightPlusRenderPassFeature.cs` into `Assets/Plugins/`, preserving GUIDs |
| `AddHighlightPlusDefine` | `true` | add `HIGHLIGHT_PLUS` scripting define (all build groups) |
| `DeleteLuminousPackages` | `false` | delete `Luminous Packages/` after rewriting |
| `TrainingCoreDependency` | `file:../com.pixovr.trainingcore` | dependency spec written to manifest |
| `Extensions` | `.unity .prefab .asset .playable .controller .shadergraph .shadersubgraph .mat .overrideController .anim` | file types scanned |
| `ProjectRoot` | cwd | Unity project root |
| `ReportPath` | `<root>/Logs/luminous-migration-report.csv` | report output |

Batchmode (a dry run):

```
Unity -batchmode -quit -projectPath /path/to/project \
  -executeMethod PixoVR.TrainingCore.Editor.Migration.LuminousMigrator.RunFromCommandLine \
  -luminousProjectRoot /path/to/project -luminousDryRun \
  -luminousPackageSource "https://github.com/PixoVR/com.pixovr.trainingcore.git" \
  [-luminousNoExtendedRules] [-luminousExtraMap /path/to/extra-map.json]
```

Log line printed: `LuminousMigrator: <files> files, <refs> refs, <unmapped> unmapped,
<warnings> warnings, <residual> residual`.

## 3. Reading the report

`Logs/luminous-migration-report.csv` columns: `file,line,from,to,status`.

| status | Meaning | Action |
|---|---|---|
| `mapped` | `m_Script` or managed-ref `type:` rewritten to the Pixo class | none |
| `field-renamed` | serialized field renamed inside a mapped/kept component | none |
| `kept` | third-party (HighlightPlus/Ultimate Replay) ref left alone — the asset was relocated so the guid still resolves | none |
| `unmapped` | Luminous type/guid with no Pixo target in the map | decide: port the class, add a map entry, or remove |
| `unmapped-dll-guid` | `m_Script` pointing at a Luminous DLL fileID the map doesn't know | same |
| `unresolved-pixo-script` | mapping exists but no matching `.cs` (name+namespace) found in the package — e.g. the class didn't exist yet | create the class and re-run |
| `target-kind-mismatch` | resolved script is the wrong kind: ScriptableObject where a MonoBehaviour is needed (or vice versa) — the CredentialLoader/ApexCredentialsConfig case | point the map at the right class; the ref is NOT rewritten |
| `mapped-qualified-name` | `assemblyQualifiedName:` rewritten (graph SetComponentState etc.) | none |
| `unmapped-qualified-name` | `Luminous.*` assemblyQualifiedName with no map entry | port/remove |
| `mapped-event-target` | UnityEvent `m_TargetAssemblyTypeName` (or `value:` under that propertyPath) rewritten | none |
| `unmapped-event-target` | `Luminous.*` event-target type name unmapped | rename the project type / update the event |
| `mapped-asset-guid` | asset guid remapped (e.g. Luminous input-actions → bundled copy) | none |
| `info-removed-components` | PrefabInstance `m_RemovedComponents` with entries — overrides that strip components | **review each one**; sa-collect-gas-sample had a valve's components silently stripped (project#38) |
| `residual-luminous` | output line still contains `Luminous` | mostly harmless (comments, Assembly-CSharp names); check managed refs/m_Script leftovers |

After the CSV: `unmapped types:` groups unmapped rows by type; `residual Luminous
strings by file:` groups residual rows by file.

## 4. What it rewrites

**Core rules (always on):**

- `m_Script: {fileID: X, guid: <luminous dll/middleman guid>, type: 3}` → the mapped
  Pixo script's guid (fileID normalized to 11500000), with target-kind validation (R5).
- `type: {class, ns, asm}` managed-reference blocks → mapped class/ns and the mapping's
  `asm` (R0 — per-type assembly, so XRI/Photon/Apex targets land in the right asmdef).
- Serialized `fields` renames inside mapped/kept components (R4).

**Extended rules (`ExtendedRules`, `-luminousNoExtendedRules` to disable):**

- `assemblyQualifiedName: <ns.Class>, <asm>…` (R1):

  ```
  -        assemblyQualifiedName: Luminous.Interactables.GrabbableOpenXR, MiddlemanRuntime,
  +        assemblyQualifiedName: PixoVR.TrainingCore.XRI.XRIGrabBehaviour, PixoVR.TrainingCore.XRI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
  ```

- UnityEvent `m_TargetAssemblyTypeName` and the `propertyPath:…m_TargetAssemblyTypeName`
  + `value:` override form (R2):

  ```
  -      value: Luminous.Unity.DisplayInteractionMiddleman, CoreSystemRuntime
  +      value: PixoVR.TrainingCore.Utility.Display.DisplayInteraction, PixoVR.TrainingCore
  ```

- Any asset guid in `AssetGuids`, plain or JSON-escaped (R3), except on `m_Script` lines.
- `m_RemovedComponents` info rows (R6), residual-Luminous scan (R7).

**Deliberately not done** — see `migration-checklist.md`: new-asset authoring, rig
replacement, field semantics changes, project C# namespaces (`Assembly-CSharp` type
names are reported, not renamed), manifest settings (minSdk, Addressables flags),
`m_ObservedComponents` wiring, content fixes.

## 5. The map

`Editor/Migration/luminous-map.json`:

```json
{
  "dlls": ["<luminous dll guids>"],
  "luminousAsm": "CoreSystemRuntime",
  "pixoAsm": "PixoVR.TrainingCore",
  "types":   [{ "luminous": {"ns": "…", "class": "…"},
               "pixo": {"ns": "…", "class": "…", "asm": "…"},
               "fields": {"old": "new"}, "keep": true }],
  "scripts": [{ "guid": "<middleman .meta guid>",
               "luminous": {"ns": "…", "class": "…"}, "pixo": {…} }],
  "assets":  [{ "luminous": "<guid>", "pixo": "<guid>", "note": "…" }]
}
```

`pixo.asm` defaults to `pixoAsm` when absent (targets in `.XRI`/`.Photon`/`.Apex`/
`.HighlightPlus` carry their own). A `scripts` entry with `"keep": true` and `fields`
means "leave the m_Script alone but rename fields inside the component".

**Overlay map** (`-luminousExtraMap` / `ExtraMapPath`): same schema, merged later-wins —
good for project-local fixes the shipped map can't know. Example covering the
sa-collect-gas-sample cases:

```json
{
  "types": [
    { "luminous": { "ns": "MyProject.Old", "class": "OldHelper" },
      "pixo": { "ns": "MyProject", "class": "NewHelper" } }
  ],
  "scripts": [
    { "guid": "<RemoteController.cs meta guid>",
      "luminous": { "ns": "", "class": "RemoteController" },
      "pixo": null, "keep": true,
      "fields": { "LocomotionSystem": "LocomotionMediator" } }
  ],
  "assets": [
    { "luminous": "f97a256847be9424e8c66cb8c3d44306",
      "pixo": "1d3d39b017a09ae40bf9ade2eb1c405b",
      "note": "HighlightZone shadergraph → local HighlightPowerSubGraph" }
  ]
}
```

A missing overlay path logs `extra map not found: <path>` in `ManifestNotes`.

## 6. Execute

1. Dry run → review CSV → fix map/overlay for the high-frequency `unmapped*` rows.
2. Run without `-luminousDryRun`.
3. Reimport; fix project C# compile errors — `Luminous.*` namespaces become
   `PixoVR.TrainingCore.*` (the map's `types` list is the rename table; typical ones a
   project calls: `Luminous.Unity.DisplayInteractionMiddleman`→`Utility.Display.DisplayInteraction`,
   `Luminous.Core.Interactions.GenericActionFunctionality`→`Interactions.GenericActionTrigger`,
   `Luminous.Interactables.GrabbableOpenXR`→`XRI.XRIGrabBehaviour`,
   `Luminous.Interactables.ValveTurnOpenXR`→`XRI.XRIValveBehaviour`).
4. Re-run — it is idempotent; previously `unresolved-pixo-script` rows resolve once the
   target class exists.
5. Commit, then work `migration-checklist.md`.

## 7. Iterating

- Adding a mapping = a `types`/`scripts` entry (or an overlay file entry); no code needed.
- New rewrite behaviours live in `YamlRewriter` and are covered by
  `Tests/Editor/MigrationTests.cs` — the tests use real YAML snippets taken from
  sa-collect-gas-sample commit `a17adaf8`; copy a fixture line from your own project
  the same way when adding a rule.
