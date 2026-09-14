# PixoVR Training Core

Pixo-owned re-implementation of the LuminousCore training stack (`com.luminous.core` DLLs +
`com.luminous.core.middlemen*`), targeting Unity **6000.3.15f1**.

Provides: per-object GUID identity, an interaction event bus, undoable command history,
platform-agnostic interaction middlemen (grab/snap/tap/teleport/valve/gaze), a platform
session abstraction, multiuser (Photon-ready) abstractions, and a NodeGraphProcessor-based
step-graph runtime (wave 2).

## Install

1. Add NodeGraphProcessor (not on a registry — install via git URL). In your project's
   `Packages/manifest.json`:

   ```json
   "com.alelievr.node-graph-processor": "https://github.com.alelievr.node-graph-processor.git?path=/Assets/com.alelievr.node-graph-processor#1.3.1",
   "com.pixovr.apexunitysdk": "https://github.com/PixoVR/com.pixovr.apexunitysdk.git",
   "com.pixovr.trainingcore": "https://github.com/PixoVR/com.pixovr.trainingcore.git",
   ```

   Or via Package Manager → *Add package from git URL* (add NGP and the Apex SDK first).

   `com.pixovr.apexunitysdk` is a **required** dependency — UPM cannot express git URLs
   in a package's `dependencies`, so the consuming project's manifest must add it via
   the git URL above. It provides `Runtime/Apex`'s `ApexPlatformSession`, the default
   platform session (installed in-scene by `ApexPlatformBootstrap`).

2. Optional integrations light up automatically when present:

   | Package | define | Enables |
   |---|---|---|
   | Photon PUN2 (`PHOTON_UNITY_NETWORKING`) | asmdef `defineConstraints` | `Runtime/Photon` networking layer |
   | `com.unity.xr.interaction.toolkit` 3.x | `PIXO_XRI` (versionDefine) | `Runtime/XRI` interaction behaviours |
   | HighlightPlus (Asset Store asset, project-owned) | `HIGHLIGHT_PLUS` scripting define | `Runtime/Highlight` `HighlightPlusHighlighter` |

HighlightPlus is a licensed Asset Store asset — it is **not** part of this package. Drop it into
`Assets/Plugins/HighlightPlus` and add `HIGHLIGHT_PLUS` to *Project Settings → Player →
Scripting Define Symbols*. The migration tool does both automatically (see below).

## Migrating a Luminous project

The editor migration tool (`Pixo > Training Core > Migrate from Luminous…`) rewrites
`m_Script` refs and managed-reference `type:` blocks in scenes/prefabs/assets. Serialized
field names and `serializedGuid` byte arrays are intentionally kept identical so saved data
survives. See `DESIGN.md`.

Options (`MigrationOptions` / migration window):

- `DryRun` (default) — writes `Logs/luminous-migration-report.csv` without touching files.
- `RelocateThirdParty` (default on) — moves `Luminous Packages/…/HighlightPlus`,
  `…/Ultimate Replay`, `…/Ultimate Replay 2.0` and the loose
  `HighlightPlusRenderPassFeature.cs` into `Assets/Plugins/<Name>/`, preserving `.meta`
  GUIDs so existing `HighlightEffect`/`HighlightPlusRenderPassFeature` refs keep resolving
  (reported as `kept (relocated)` in the CSV, not unmapped).
- `AddHighlightPlusDefine` (default on) — adds `HIGHLIGHT_PLUS` to the scripting define
  symbols of every installed build target group.
- `DeleteLuminousPackages` (default off) — deletes `Luminous Packages/` after rewriting.

Batchmode entry point: `-executeMethod PixoVR.TrainingCore.Editor.Migration.LuminousMigrator.RunFromCommandLine`
with `-luminousProjectRoot <path>`, `-luminousDryRun`, `-luminousPackageSource <dep spec>`.
