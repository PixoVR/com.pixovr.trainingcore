# com.pixovr.trainingcore — design

Pixo-owned replacement for the LuminousCore stack (`com.luminous.core` DLLs + `com.luminous.core.middlemen*`).
Clean re-implementation. The Luminous decompile (`~/luminous-inventory/api-spec/*`) is a **behavioural reference only**:
read `BEHAVIOUR.md`/`GRAPH-FORMAT.md`/stubs to learn what a type must do and which fields are serialized, then write
fresh code. Never paste decompiled bodies.

## Targets & dependencies
- Unity **2021.3 LTS** (`"unity": "2021.3"`, matching the apexunitysdk 2021.1 minimum), C# 9, `PixoVR.TrainingCore` root namespace.
- Hard deps (package.json `dependencies`): `com.unity.inputsystem` 1.15.0, `com.unity.nuget.newtonsoft-json` 3.2.1,
  `com.unity.addressables` 1.28.0, `com.unity.timeline` 1.8.9, `com.unity.textmeshpro` 3.0.9
  (ugui is a built-in module on 2021.3; ugui 2.0.0 exists only on Unity 6),
  NodeGraphProcessor via git URL `https://github.com/alelievr/NodeGraphProcessor.git?path=/Assets/com.alelievr.NodeGraphProcessor#1.3.1`
  (git deps can't go in `dependencies`; document in README and provide `Samples~`/`manifest` snippet; asmdef references it by name).
- Required deps: Apex SDK (`com.pixovr.apexunitysdk` — added by the consuming project's manifest
  via git URL; UPM cannot express git deps) backs `Runtime/Apex`'s `ApexPlatformSession`, the
  default platform session installed by `ApexPlatformBootstrap`.
  `NullPlatformSession` exists for tests only — `PlatformSessionBase.EnsureActive()` installs it
  with a `Log.Warning` when no provider is present.
- Optional deps via `versionDefines` / `defineConstraints` (separate asmdefs, compile to nothing if absent):
  Photon PUN2 (`PHOTON_UNITY_NETWORKING`),
  XR Interaction Toolkit (`com.unity.xr.interaction.toolkit` → `PIXO_XRI`).

## Repo / package layout (repo root == package root, like com.pixovr.apexunitysdk)
```
package.json  README.md  CHANGELOG.md  LICENSE.md  DESIGN.md (this file)
Runtime/
  Core/                        PixoVR.TrainingCore.asmdef  (autoReferenced true)
    Identity/                  GuidComponent, GuidRegistry (static), GuidReference, GuidExtensions (GetGuid())
    Events/                    ObservableSubject, Subject, EventBus (was ObserverCore), IEventObserver, InteractionEventArgs + subclasses
    Commands/                  ICommand, CommandBase, CommandHistory (was CommandManager), concrete commands
    Interactions/              InteractableBase, Grabbable, Snappable, Snapzone, Tappable, Teleporter, Valve, GazeTarget, ColliderTrigger, GenericStepTrigger, GenericActionTrigger + IGrabBehaviour/ISnapBehaviour/ITeleportBehaviour/IValveBehaviour
    Multiuser/                 NetworkManager (abstract), Room (abstract), LobbyBase (abstract), MultiuserPlayer, NetworkEvents (static hub), IFlowEventHandler, IFreezeBehaviour, InstructorControls, StepInteractionArguments, EventSyncData
    Platform/                  IPlatformSession, PlatformSessionBase (MonoBehaviour), NullPlatformSession, SessionContext (was LuminousPlatformPersistence), SessionStatus enum, PlatformEvents
    Flow/                      Step, StepExecution, Action, Flow/NormalFlow/FailHandlerFlow, GraphIterator, GraphData, GraphRunner (was GraphFlowManager), StepCounter, skipping behaviours
    Graph/                     TrainingGraph (BaseGraph), node classes (BaseNode subclasses), GraphParser, SceneReferences, ExposedParameters
    GameModes/                 GameMode enum, GameModeManager (static), GameModeData
    SceneManagement/           SceneLoader, SceneFlow (static, was SceneManager), EnvironmentLoader
    Settings/                  FadeSettings, PlacerSettings, TrainingConfig (SO; was ConfigData/ConfigManager), PlatformFeatureSet
    Utility/                   FadeManager, AudioManager, TimelinePlayer, Displayer, DisplayObjectPlacer, Billboard, ArrowPlacer, Log (was LumDebug), RandomManager, Singleton helpers
    Bootstrap/                 GameManager, UserInterfaceManager
  Photon/                      PixoVR.TrainingCore.Photon.asmdef — PhotonNetworkManager, PhotonRoom, PhotonLobby, PhotonCommandSerializer(Json), PhotonNetworkUpdateHandler, sync views (valve/snappable), freeze behaviours
  Apex/                        PixoVR.TrainingCore.Apex.asmdef — ApexPlatformSession : PlatformSessionBase (login, heartbeat, xAPI statements for module/step start/complete/fail, session status)
  XRI/                         PixoVR.TrainingCore.XRI.asmdef — XRI implementations of I*Behaviour (GrabbableXRI, SnapzoneXRI, TeleportAreaXRI, ValveTurnXRI, TapXRI), controller input helpers
Editor/
  PixoVR.TrainingCore.Editor.asmdef — graph window (NGP views), Interactable Object editor, settings provider (TrainingConfig, logging, platforms)
  Migration/                     LuminousMigrationWindow + LuminousMigrator (YAML rewrite), Mapping/luminous-mapping.json
Tests/
  Editor/  PixoVR.TrainingCore.Tests.asmdef (UNITY_INCLUDE_TESTS) — NUnit edit-mode tests for pure logic
```

## Cross-cutting rules
- Serialized **field names** of components/graph nodes stay identical to the Luminous ones (see stubs' `[SerializeField]`/public fields) so
  migrated YAML keeps its data; class/namespace names are ours. Where a field is renamed anyway, add `[FormerlySerializedAs]`.
- Serialized GUIDs: `byte[] serializedGuid` (16 bytes) exactly as Luminous, since 413 components carry it.
- No `FindObjectOfType` in hot paths; singletons via a tiny `SingletonBehaviour<T>` (Instance set in Awake, cleared in OnDestroy).
- Events: C# `event Action<...>` for code, `UnityEvent` only where designers wire them in the inspector (NetworkManager events, GameManager events).
- No BestHTTP, no SignalR, no Google TTS, no file analytics, no Ultimate Replay, no MRTK, no Assessment namespace.
- Logging through `Log` static with `LogCategory` flags (Interaction, Flow, Network, Platform, Scene) and a runtime mask in `TrainingConfig`.
- Every public type gets a one-line XML `<summary>`.

## Key APIs (wave 1)
```csharp
namespace PixoVR.TrainingCore.Identity {
  [ExecuteInEditMode, DisallowMultipleComponent]
  public sealed class GuidComponent : MonoBehaviour, ISerializationCallbackReceiver {
    [SerializeField] byte[] serializedGuid;              // 16 bytes, same as Luminous
    public Guid GetGuid();                                // lazily rehydrate; new guid if empty and not a prefab asset
    // Awake/OnValidate → EnsureGuid(); OnDestroy → GuidRegistry.Remove. Duplicate → regenerate.
  }
  public static class GuidRegistry {                      // static map Guid→GameObject with OnAdded/OnRemoved callbacks
    public static bool Add(GuidComponent c); public static void Remove(GuidComponent c);
    public static GameObject Resolve(Guid g, Action<GameObject> onAdded = null, Action onRemoved = null);
    public static void Clear();
  }
  [Serializable] public class GuidReference { [SerializeField] byte[] serializedGuid; public Guid Guid; public GameObject GameObject {get;} }
  public static class GuidExtensions { public static Guid GetGuid(this GameObject go); public static string GetGuidString(this GameObject go); }
}
namespace PixoVR.TrainingCore.Events {
  public interface IEventObserver { void OnEvent(InteractionEventArgs args); }
  public class InteractionEventArgs : EventArgs { public string SubjectId; public int StepNumber; public bool IgnoreFailure; public virtual ICommand ToCommand(); }
  // Grab/Snap/Tap/Teleport/ValveTurn/Collision/Gaze/Use/Generic/Answer event args subclasses mirroring Luminous.Observer.SubjectEvents fields
  public sealed class Subject { public string Id; public IReadOnlyList<IEventObserver> Observers; public void Attach/Detach; public void Notify(InteractionEventArgs) }
  public sealed class EventBus {                            // was ObserverCore
    public static EventBus Instance; public const string GlobalSubjectId = "Global";
    public void Register(Subject s); public void Unregister(Subject s);
    public void Subscribe(string subjectId, IEventObserver o); public void Unsubscribe(string subjectId, IEventObserver o);
    public void Publish(string subjectId, InteractionEventArgs args, bool alsoGlobal = true, bool toNetwork = true, bool isSyncReplay = false);
    public IReadOnlyList<InteractionEventArgs> History; public List<InteractionEventArgs> UnsyncedEvents; public void Reset();
    public event Action<InteractionEventArgs> OnPublished;  // NetworkManager hooks this to broadcast when toNetwork
  }
  [RequireComponent(typeof(GuidComponent))]
  public sealed class ObservableSubject : MonoBehaviour { public string Id; public Subject Subject; public Snapzone CurrentSnapzone; public Stack<SavedTransform> TransformHistory; }
}
namespace PixoVR.TrainingCore.Commands {
  public interface ICommand { int StepId {get;} string SubjectId {get;} void Execute(); void Unexecute(); }
  public abstract class CommandBase : ICommand { protected CommandBase(string subjectId) { StepId = StepCounter.Current; } ... }
  public sealed class CommandHistory {                       // was CommandManager
    public static CommandHistory Instance; public IReadOnlyCollection<ICommand> Executed; public IReadOnlyCollection<ICommand> Undone;
    public void Record(ICommand c);                          // merge rule for consecutive UseObject on same subject+location
    public void Undo(); public void Redo(); public void UndoStep(int stepId); public void UndoUntil(int stepId); public void Reset();
    public event Action<ICommand> OnRecorded; public event Action<int> OnStepChanged;
  }
}
namespace PixoVR.TrainingCore.Platform {
  public enum SessionStatus { Unknown, Scheduled, InProgress, Completed, Cancelled, Failed }   // map from Luminous Status where used
  public interface IPlatformSession {
    bool IsConnected {get;} string UserId {get;} string SessionId {get;}
    event Action Connected, Disconnected, ConnectionFailed, NoActiveSession;
    event Action<string, SessionStatus, string> StatusUpdated;   // (sessionName, status, moduleScene)
    Task<bool> LoginAsync(string username, string password); Task<bool> LoginWithPinAsync(int pin); Task<bool> LoginWithTokenAsync(string token);
    Task RefreshSessionAsync(); Task SetStatusAsync(string sessionId, SessionStatus status, string moduleScene);
    Task ModuleStartedAsync(string mode, string scenario, string module); Task ModuleEndedAsync(string mode, string scenario, string module, bool passed);
    Task StepStartedAsync(Step step); Task StepCompletedAsync(Step step); Task StepFailedAsync(Step step, string reason);
    Task DisconnectAsync();
  }
  public abstract class PlatformSessionBase : MonoBehaviour, IPlatformSession { public static IPlatformSession Instance; ... }  // Awake registers Instance; NullPlatformSession logs only
  public sealed class SessionContext : MonoBehaviour {      // was LuminousPlatformPersistence; same public field names
    public static SessionContext Instance; public bool Initialised; public int AdminMode; public bool bypassComplete; public string username; public int userPin; public string displayName;
    public bool isMultiuser, isInModule, isLoggedIn, firstTimeLobby, isPotentialLeadUser, isCurrentLeadUser;
    public string RoomId, SessionId, Scenario, Module, Mode, ScheduledTime, SceneToLoad, ImageURL;
    public event Action Initialised; public void ResetPortalCommsFields(); public void ResetSessionInfoFields();
  }
}
namespace PixoVR.TrainingCore.Multiuser { /* NetworkManager/Room/LobbyBase/MultiuserPlayer with the same public member names as the Luminous stubs (project code calls ~40 of them); NetworkEvents static hub with UserInteraction/SkipForward/SkipBackward/SkipToStep events */ }
namespace PixoVR.TrainingCore.Interactions { /* InteractableBase : MonoBehaviour [RequireComponent(ObservableSubject)] with protected Subject, Publish(args) helper = CommandHistory.Record(args.ToCommand()) + EventBus.Publish; six concrete classes per BEHAVIOUR.md */ }
```

## Graph (wave 2)
- `TrainingGraph : GraphProcessor.BaseGraph`; node classes one-to-one with Luminous node types listed in GRAPH-FORMAT.md census, same serialized field names,
  `[NodeMenuItem]` names under "Pixo/…". Runtime twins (`Step*`, `Action*`) in Flow/. `GraphParser` builds `GraphData` from nodes+edges.
- Migration rewrites the `references:` table `type: {class, ns, asm}` per mapping, and `m_Script` refs.

## Migration tool (wave 2)
- `Editor/Migration/Mapping/luminous-mapping.json`: `[{ "luminous": {"ns","class","fileID"}, "pixo": {"ns","class","asmdef","scriptGuid"} }]` — generated from `~/luminous-inventory/serialized-usage.csv` + our `.cs.meta` GUIDs.
- `LuminousMigrator.MigrateProject(root)`: for every `.unity/.prefab/.asset` YAML, replace `m_Script: {fileID: <luminousFileID>, guid: <any of the 5 dll guids>, type: 3}` with `{fileID: 11500000, guid: <ours>, type: 3}`; rewrite managed-reference `type:` blocks; report unmapped. Also removes `com.luminous.*` from manifest.json and the Luminous scoped registry. Runnable from menu `Pixo/Training Core/Migrate from Luminous…` and from batchmode (`-executeMethod PixoVR.TrainingCore.Editor.Migration.LuminousMigrator.RunFromCommandLine`).

## Verification (no Unity license on the box)
- `~/unity-harness/build.sh <dir>` compiles each asmdef folder against Unity 2021.3 DLLs + deps; 0 errors required per asmdef.
- Pure-logic NUnit tests (GuidRegistry, EventBus, CommandHistory, GraphIterator, GraphParser on a fixture graph, LuminousMigrator on fixture YAML) run with `dotnet test` in the harness.
- Full Unity import/compile/play verification happens in the sa-collect-gas-sample upgrade PR.
