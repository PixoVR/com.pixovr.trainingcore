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
   "com.alelievr.NodeGraphProcessor": "https://github.com/alelievr/NodeGraphProcessor.git?path=/Assets/com.alelievr.NodeGraphProcessor#1.3.1",
   "com.pixovr.trainingcore": "https://github.com/PixoVR/com.pixovr.trainingcore.git",
   ```

   Or via Package Manager → *Add package from git URL* (add NGP first).

2. Optional integrations light up automatically when present:

   | Package | define | Enables |
   |---|---|---|
   | Photon PUN2 (`PHOTON_UNITY_NETWORKING`) | asmdef `defineConstraints` | `Runtime/Photon` networking layer |
   | `com.pixovr.apexunitysdk` | `PIXO_APEX_SDK` | `Runtime/Apex` platform session |
   | `com.unity.xr.interaction.toolkit` | `PIXO_XRI` | `Runtime/XRI` interaction behaviours |

## Migrating a Luminous project

The editor migration tool (`Pixo > Training Core > Migrate from Luminous…`, wave 2) rewrites
`m_Script` refs and managed-reference `type:` blocks in scenes/prefabs/assets. Serialized
field names and `serializedGuid` byte arrays are intentionally kept identical so saved data
survives. See `DESIGN.md`.
