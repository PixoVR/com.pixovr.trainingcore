# Changelog

## [0.1.0] - Unreleased
### Fixed
- `GameManager` now runs the startup sequence from `Start` (fade to black, environment
  load wait, `OnInitalSetUp`, step-counter init, `FlowManager.Initialize`/`StartGraph`,
  network-sync wait, fade to clear) and exposes `Relaunch()`; overlapping launches are
  cancelled, only one environment is instantiated, and `NetworkManager.ResetRandoms`
  delegates to `RandomManager`. `EventBus` is intentionally not reset there: scene
  `ObservableSubject`s register in `OnEnable` before `Start` runs.
- Scene loading no longer destroys the boot scene: `SceneLoading.Load` always loads
  additively and only unloads scenes it previously loaded, keeping the Unity-booted
  scene (PlatformSessionBehaviour, NetworkManager, XR rig) resident — matching the
  legacy plugin's behaviour. `SceneLoader` is a plain `MonoBehaviour` again with the
  serialized `LoadOnStart`/`AdditiveScene`/`SceneName` contract restored.
- `PlatformSessionBehaviour` auto-creates a `SessionContext` when none exists and fires
  `PersistenceInitialised` from `Start`, so lobby UI waiting on session init unblocks.
- `PixoVR.TrainingCore.HighlightPlus` now references the `HighlightPlus` assembly so the
  adapter compiles against the project-side HighlightPlus asmdef (still gated on the
  `HIGHLIGHT_PLUS` scripting define).

### Changed
- Retarget to Unity 2021.3 LTS (matches com.pixovr.apexunitysdk minimum); Addressables 1.28.0, Input System 1.15.0, Timeline 1.8.9, TextMeshPro 3.0.9 (ugui 2.0.0 dependency dropped).
- Split every `MonoBehaviour`/`ScriptableObject`/`EditorWindow`/`Editor` class into its own `<ClassName>.cs` so `m_Script` references resolve to the intended type (71 UnityEngine.Object classes; renamed files kept their .meta GUIDs).
- `LuminousMigrator.ResolvePixoGuid` now only accepts a class whose file name matches (required for correct MonoScript binding).
- Apex Unity SDK (`com.pixovr.apexunitysdk`) is now a required dependency (consuming projects
  add it via git URL); `Runtime/Apex` no longer gates on `PIXO_APEX_SDK`. The platform
  session falls back to `NullPlatformSession` (with a `Log.Warning`) only when no provider
  is installed — the null provider is intended for tests only.

### Added
- Initial package scaffold.
- Core runtime: Identity (GuidComponent/GuidRegistry/GuidReference), Events (ObservableSubject/Subject/EventBus/InteractionEventArgs), Commands (ICommand/CommandHistory + concrete commands), Interactions middlemen + I*Behaviour interfaces, Platform (IPlatformSession/SessionContext), Multiuser abstractions, GameModes, SceneManagement, Settings, Utility.
