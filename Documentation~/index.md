# TrainingCore documentation

PixoVR TrainingCore (`com.pixovr.trainingcore`) is the Pixo-owned replacement for the
LuminousCore training stack: the closed-source `com.luminous.core` DLLs and the
`com.luminous.core.middlemen*` script packages, re-implemented as source you own. It
targets Unity 2021.3 LTS, URP 12, XR Interaction Toolkit 3.0.8, Input System 1.15,
OpenXR 1.15, Addressables 1.28, and (optionally) Photon PUN 2.40 for multiuser.
`com.pixovr.apexunitysdk` is a required dependency and must be in the consuming
project's manifest (UPM cannot express git-URL dependencies).

## Contents

| File | What it covers |
|---|---|
| [architecture.md](architecture.md) | How TrainingCore works: assemblies, identity, events/commands, interactions, XRI layer, graph/flow, display, game modes, platform/Apex, multiuser, scenes, logging |
| [migration-guide.md](migration-guide.md) | Running the Luminous migrator: options, CLI flags, report statuses, the map schema, overlay maps |
| [migration-checklist.md](migration-checklist.md) | Everything the migrator cannot do for you — manual fixes, each with symptom/why/fix and the PR that hit it |
| [troubleshooting.md](troubleshooting.md) | Symptom → cause → where to look → fix, plus the logcat commands |

## Start here

**Building a new module:** read `architecture.md` (identity, events/commands,
interactions, graph/flow are the parts you author against), then `troubleshooting.md`
when something misbehaves on device.

**Migrating a Luminous project:** read `migration-guide.md` end to end (dry run first),
then work through `migration-checklist.md` — most post-migration bugs are in that list.
Use `troubleshooting.md` during device testing.
