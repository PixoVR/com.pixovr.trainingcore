# Architecture

How TrainingCore works, end to end, per assembly. All names below are greppable in
`Runtime/` — if a name here does not grep, this file is stale, not the code.

## Assemblies

| asmdef | Contents | References |
|---|---|---|
| `PixoVR.TrainingCore` (`Runtime/Core`) | Identity, EventBus, commands, Flow runtime (steps/actions/iterators), Graph parsing, interactions, platform abstraction, display, game modes, scene management | InputSystem, Addressables, Timeline, TextMeshPro, UnityEngine.UI, Newtonsoft.Json, NodeGraphProcessor.Runtime |
| `PixoVR.TrainingCore.XRI` (`Runtime/XRI`) | XRI 3.0.8 middlemen: XRIGrabBehaviour, XRISnapZone, XRIValveBehaviour, XRITeleportBehaviour, XRRig.prefab, ControllerModeManager, bundled input actions | Core + XR.Interaction.Toolkit, CoreUtils, Mathematics, InputSystem |
| `PixoVR.TrainingCore.Photon` (`Runtime/Photon`) | PhotonNetworkManager, PhotonSyncView, event relay, grab/valve replication | Core + XRI + PhotonUnityNetworking, PhotonRealtime |
| `PixoVR.TrainingCore.Apex` (`Runtime/Apex`) | ApexPlatformSession (IPlatformSession over the Apex SDK), ApexPlatformBootstrap, ApexCredentialsLoader/Config | Core + PixoVR.ApexUnitySDK |
| `PixoVR.TrainingCore.HighlightPlus` (`Runtime/Highlight`) | HighlightPlusHighlighter | Core + project-owned `HighlightPlus` asmdef; `HIGHLIGHT_PLUS` define constraint |
| `PixoVR.TrainingCore.Editor` (`Editor`) | Migration tool (window, map, YAML rewriter), editor-only | Core + NGP Editor + Addressables.Editor |
| `PixoVR.TrainingCore.Tests` (`Tests/Editor`) | NUnit tests | all of the above + TestRunner |

## Identity

- `GuidComponent` (Identity) stamps a `serializedGuid` byte array on every
  graph-referenced scene object; the field names and byte layout are kept identical
  to Luminous so saved data survives migration.
- `GuidRegistry` is a static guid→GameObject map; `GuidComponent` registers on
  enable/validate. `GuidRegistry.Resolve(string)` looks a guid up.
- `GuidReference` wraps that guid as a serialized field on graph nodes/interactables;
  `GuidReference.Equals` compares by guid, so a scene-authored reference equals a
  runtime `new GuidReference(go)`.
- `GraphFlowManager` calls `FindObjectsOfType<GuidComponent>(true)` before parsing
  (trainingcore#33) so **inactive** objects referenced by the graph (teleport
  visuals, highlights, tap nuts) are registered before `GraphParser` resolves them.
- `InteractableBase.SubjectId` derives from the object's guid, not Awake order, so
  event subjects are stable (trainingcore#37).

## Events and commands

- `ObservableSubject` (per GameObject) + `Subject` + `EventBus` (the global subject
  observers attach to): interactables publish `InteractionEventArgs` subclasses
  (GrabEventArgs, SnapEventArgs, TapEventArgs, TeleportEventArgs, ValveTurnEventArgs,
  GazeEventArgs, UseEventArgs…). `OnPublished` fires only for `toNetwork:true`
  non-remote events; `OnAllObserversNotified` fires at the end of each `Publish`;
  `ClearHistory()` resets recorded history for catch-up replay.
- `CommandHistory` + `ICommand` (in `Runtime/Core/Commands`): every state change that
  must be undoable goes through a command (`ExecuteAndRecord`): HighlightObjectCommand,
  SetComponentStateCommand, SetObjectActiveStateCommand, GenericActionCommand,
  FadeCommand, Grab/Snap/Teleport commands. `UndoStep(stepNumber)` rewinds one step's
  worth; `IsUndoing` suppresses failure detection during rewind.

## Interactions (Runtime/Core/Interactions)

`InteractableBase` is the shared MonoBehaviour (SubjectId, Publish helper). Concrete
kinds: `Grabbable`, `Snappable`, `Snapzone`, `Tappable`, `Teleportable`, `Valve`,
`GazeTarget`, `Usable`, `GenericActionTrigger`, `InfoPointBase`, `LostObject`.
Each publishes its `*EventArgs` on the EventBus; steps subscribe by SubjectId —
the global subject receives every publish when `alsoGlobal` is set.

## XRI layer (Runtime/XRI)

- **XRRig.prefab**: XROrigin + XRI `LocomotionMediator`/`XRBodyTransformer` +
  `SnapTurnProvider` + per-hand Base/Teleport/Interface controllers under
  `ControllerModeManager`. Direct interactors use `m_SelectActionTrigger: 1`
  (StateChange — State caused a per-frame select ping-pong on shared grabs) and
  `m_PhysicsTriggerInteraction: 2` so trigger-only tap/grab targets are detected
  (trainingcore#44). The Main Camera's TrackedPoseDriver binds the XRI **Head**
  actions — binding Left made the head follow the left hand (trainingcore#21);
  `XROrigin.m_Camera` references the Camera component (trainingcore#20).
- **XRIGrabBehaviour** (XRGrabInteractable + IGrabBehaviour): grab/snap state
  machine. *Snapped-hold*: an object grabbed while snapped stays in its zone; the
  holding interactor is exposed via `IsGrabbed`/`HoldingInteractor` so e.g. the
  valve can read it (trainingcore#35). The snapped-hold path ignores repeat
  `SelectEnter` (XRI's dup-guard only checks `isSelected`, which stays false) and
  skips `Drop()` on release so the still-snapped key is not reparented
  (trainingcore#40). `Awake` back-fills `colliders` from child trigger colliders
  when XRI's auto-register left the list empty (trainingcore#44).
- **XRISnapZone**: trigger-based zone (XRI zone replaced — Luminous parity,
  trainingcore#30). Attach-point offset uses the interactable-local formula
  `-interactableTransform.worldToLocalMatrix.MultiplyPoint(attachTransform.position)`
  scaled by resize — the zone-space version landed wheel keys several cm off in
  rotated AttachPoints (trainingcore#39).
- **XRIValveBehaviour**: rotation driver = hand grab or snapped wheel key, measured
  around the wheel pivot in the parent space (`RotationMultiplier` for tuning,
  #35; `parent.InverseTransformVector` preserves mirrored parents). `InitialRotation`
 /`Clamps`/`ReinitializeOnEnable`; `Awake` applies `InitialRotation` unconditionally,
  freeze gates only the interactive driver path — programmatic/network
  `SetRotation`/`SetRotationFromNetwork` always apply (#40).
- **XRITeleportBehaviour** generates teleport requests (area hit point / anchor).
- **ControllerModeManager** polls InputAction *phase* (not `.triggered`, which never
  fired on device — #24) and latches autoray only on Interface entry.
- **Bundled input actions** (`Runtime/XRI/Input/XRI Default Input Actions.inputactions`)
  carry the Luminous action ids/names: `Menu Toggle`, `Held Item Toggle`, `Joystick` (#23).

## Graph + flow

- `TrainingGraph` (NodeGraphProcessor `BaseGraph`) is the serialized asset; `GraphParser`
  (`GraphReferencesManager.cs`) → `GraphData` → runtime `StepBase`/`ActionBase` twins in
  `Runtime/Core/Flow`. `type: {class,ns,asm}` managed refs resolve via the map.
- Group nodes store membership in Luminous's `groupNodeFunctionality.Stacks[].nodeGUIDs`
  (BaseStackNode) — reading the `executes` port instead made the next step a child
  *and* a successor, entering it twice (#41). `AndGroupStep` enters children in
  parallel and stops early if one completes synchronously inside `OnEnter` (#42).
- Skip semantics (hand-menu next/prev): forward-skip calls `SkipForwardOnExit()` on
  entered steps only — start actions are NOT re-run — and start actions get
  `OnStepForward()` (audio stop, timeline-to-last-frame); backward-skip uses
  `CommandHistory.UndoStep` + `EventBus` history pruning (#42, #34).
- `FailureDetectionManager` (global EventBus observer) matches events against the
  current step's fail exceptions with Luminous value equality, defers `Fail` to
  `OnAllObserversNotified`, then `GameModeManager.Fail` (#37).
- `StopFlow` reports module end on exit; `GraphRunning`/`GraphStarted` gate catch-up.
- `Step entered:` info log (`LogCategory.Flow`) on every `OnEnter` (StepBase.cs).

## Display / UI

`DisplayObjectPlacer` + display steps spawn task cards; `HandMenu/` drives the wrist
menu (`SetHandMenuTextAction` via `SetHandMenuTextCommand`); `AudioManager`/
`AudioClipPlaybackManager` play card audio sequences and are stopped by
`OnStepForward`/`OnStepCompleted`; `FadeManager` drives a designer-supplied or
default CanvasGroup overlay.

## Game modes

`GameMode` enum: `Training` (guided), `Practice`, `Assessment`. `GameModeManager`
(static) raises mode/step/module events; `ToChar()`/`ToDisplayName()` extensions
map modes to 'T'/'P'/'A' and report labels.

## Platform (Core/Platform + Apex)

`IPlatformSession` (PlatformSessionBase.cs) + `PlatformSessionBase` + `SessionContext`.
`ScenarioCatalog` (ScriptableObject) drives `GetUserScenarios` — the Apex backend has
no scenario/module catalog, so the provider synthesizes `ScheduledSessions` from the
catalog asset (trainingcore#28). `ApexPlatformSession` flow: login → module-access
check gate → `JoinSession(scenarioId)` where the id is
`"<catalog module name> - <GameMode enum name>"` — e.g. `Collect a Gas Sample -
Training` (`SelectedModuleName` is the lobby-selected module, #45; the mode label is
the enum name, #43) → step/module reports → `CompleteSession`/`ModuleEnded` (also on
exit via `StopFlow`). `[Apex Diag]` prefixes every platform log. `ApexCredentialsLoader`
(MonoBehaviour, in-scene) assigns the serialized `ApexCredentialsConfig`
(ScriptableObject).

## Multiuser (Runtime/Photon)

`NetworkManager`/`PhotonNetworkManager` + `PhotonSyncView`: one active static event
relay (first-wins; the lobby instance persists across additive loads); late-join
catch-up is requested after the graph starts (not in the lobby), and immediately if
`GraphRunning` is already true; interaction events are trusted from any room member
(sender over payload — same trust model as Luminous); lost-object resets are gated
to the object's owner; grabs replicate via `NetworkGrabManager` events + a grab-sync
event code; valve rotation replicates via `IPunObservable` on `PhotonSyncView` —
which requires `PhotonView.m_ObservedComponents` to include it on each valve prefab.

## Scene management

`EnvironmentLoader` loads environment scenes via Addressables `AssetReference`
(additive, releases the handle; fires `LoadingDone` even when no location exists —
trainingcore#14/#16). `SceneLoading` tracks every loaded scene so loading a module
unloads the lobby environment (trainingcore#29).

## Logging

`Utility.Log` with `LogCategory` (Multiuser, StepLogic, GameModeFailExceptions,
GameManagerLogic, Hardware, ScenarioSpecific, …). Diagnostic prefixes:
`[XRI Diag]` (XRI layer), `[Apex Diag]` (platform), `Step entered:` (flow).
On device: `adb logcat -s Unity` and grep the prefix.
