# Changelog

## [0.1.1] - Unreleased
### Added
- `Runtime/XRI/ControllerModeManager` — Base/Teleport/Interface per-hand controller mode
  switching (`ExternalStartRay`/`ExternalEndRay` for UI ray edges).
- `Runtime/XRI/Prefabs/XRRig.prefab` — XRI 3.0.8 rig (XROrigin + legacy locomotion stack +
  per-hand Base/Teleport/UI controllers, DeviceReferences, LostObjectManager, fade canvas)
  and a bundled `XRI Default Input Actions` copy under `Runtime/XRI/Input/`.
### Changed
- `AutoRayManager.ControllerManager` is now typed `ControllerModeManager` and drives
  `ExternalStartRay`/`ExternalEndRay` on UI-hit edges.
### Fixed
- `XRIGrabBehaviour` populates its collider list from child trigger colliders when XRI's
  auto-registration left it empty, so trigger-only tap/grab targets are hoverable.
- `XRRig.prefab` direct interactors now detect trigger colliders
  (`m_PhysicsTriggerInteraction: 2`) and use `StateChange` select trigger.

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
- Command layer parity: `ObjectStateCommands` (SetColor, SetObjectMaterial, SetObjectPosition,
  SetComponentState, HighlightObject, SetHandMenuText, GenericAction, Fade, FloatChanged,
  DisplayInteraction, GenericInteraction), `SpawnCommands` (Spawn, DestroySpawnedObject,
  DisplayObject, SpawnHandCoach, PlaceArrow, DestroyPlacedArrow) and `FlowCommands`
  (SkipToNext/SkipToBack); `GrabCommand` now captures/restores the grabbed pose and
  `SkipTimelineCommand` gained a `PlayableDirector`/`TimelineAsset` ctor;
  `CommandHistory` gained `ExecuteAndRecord`/`PrintHistory`; `luminous-map.json` maps
  `Luminous.Core.Command.*` onto these types.
- `HandCoachBase`/`HandCoach` interaction components (maps `Luminous.Core.Interactions.HandCoachMiddleman`).
- Graph coverage parity with `Luminous.GraphSystem` runtime:
  - Conditional steps + nodes: `IfConditionalStep`/`IfStepConditionalNode`,
    `ComparisonConditionalStep`/`ComparisonConditionalNode`,
    `RandomStepConditional`/`RandomStepConditionalNode` (weighted dynamic ports),
    `EnumConditionalStep` (maps the `EnumCondtionalStep` typo), `GameModeConditionalStep`/
    `GameModeStepNode`, `DynamicEnumConditionalStep`/`DynamicEnumConditionalStepNode`.
    `ConditionalStepNode.GetOutputBranches` feeds `GraphParser` → `OutputMappings` and
    `GraphIterator` expands conditionals (marking them visited) instead of entering them.
  - Steps + nodes: `BlankStep`, `FadeStep`, `HandMenuStep`, `InputActionStep`,
    `MoveToPositionStep` (polled via the new `GraphFlowManager.Tick`),
    `InfoPointStep`, `GroupedInfoPointsStep`, `ShowDisplayGroupStep`; `IGroupStepContainer`
    generalizes parser group wiring to every `IGroupNode`.
  - Actions + nodes: `FadeAction`, `LogAction`, `FailAction`, `SetColorAction`,
    `SetGameObjectMaterialAction`, `SetGameObjectPositionAction`, `SetHighlightZonesAction`
    (+ `HighlightZoneData`), `PlayTimelineAction`, `HandCoachAction`,
    `InstructionalArrowAction`, and `SetExposedParameterAction<T>` with the ten concrete
    parameter-setter nodes.
  - Global parameters: `GlobalParameters` ScriptableObject, `GlobalParameterManager`
    (reset on `GameManager` startup), `GlobalParameterNode`, and
    `ExposedParameterManager.ResolvePortValue` for lazy parameter-bound input ports.
  - `SkippingBehaviourBase`/`ForwardSkippingBehaviour`/`BackwardSkippingBehaviour`
    extracted from `FlowBase`; `GraphUtility.GetStepsStartingFrom` /
    `GraphExtensions.ToSteps`; `luminous-map.json` now maps all implemented
    `Luminous.GraphSystem` runtime types.
- Initial package scaffold.
- Core runtime: Identity (GuidComponent/GuidRegistry/GuidReference), Events (ObservableSubject/Subject/EventBus/InteractionEventArgs), Commands (ICommand/CommandHistory + concrete commands), Interactions middlemen + I*Behaviour interfaces, Platform (IPlatformSession/SessionContext), Multiuser abstractions, GameModes, SceneManagement, Settings, Utility.
