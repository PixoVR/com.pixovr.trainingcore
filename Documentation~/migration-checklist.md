# Migration checklist — the manual work

Everything the migrator cannot infer. Format: symptom / why / fix / where it was hit.
`project#N` = sa-collect-gas-sample PR, `trainingcore#N` = com.pixovr.trainingcore PR.

## Scenario catalog (project#27, trainingcore#28)

- **Why:** the Apex backend has no scenario/module catalog; `GetUserScenarios` is fed
  by a `ScenarioCatalog` asset and the lobby shows nothing without it.
- **Fix:** create `Assets/Resources/Scenario Catalog.asset` (one `Scenarios` entry:
  `ScenarioName`, `ScenarioId`, `Modules[]` with `Name` + `SceneToLoad`), assign it to
  the `ApexPlatformSession`/`ScenarioCatalog` field in the boot scene.
- **Note:** the catalog module `Name` is the JoinSession scenario-id prefix
  (`"<module> - <mode>"`) — name it what you want in Apex session reporting.

## XR rig (project#15, trainingcore#19–#27)

- **Why:** the Luminous rig cannot be migrated — XRI 3.0.8 serialized shapes differ.
- **Fix:** replace with `Runtime/XRI/Prefabs/XRRig.prefab`; make the boot scene scene 0
  in Build Settings; verify `XROrigin.m_Camera` binds the Camera component
  (trainingcore#20) and the TrackedPoseDriver binds Head actions (trainingcore#21).
- **Scene-instance overrides we needed:** reticle refs for teleport line visuals
  (project, `eee75573`); snap turn provider (trainingcore#27); UI ray length /
  auto-retract on non-interactable UI (trainingcore#27); teleport-mode phase polling
  (trainingcore#24).

## InputActionReferences (project#21)

- Any `m_Reference`/`Input:` fields pointing at the deleted Luminous `.inputactions`
  asset — now handled by the shipped `assets` map entry
  (`c348712b…` → `3d1634cb…`). Verify zero remain post-run (grep the old guid).

## Addressables (project#14, trainingcore#14/#16)

- `AddressableAssetSettings.m_BuildAddressablesWithPlayerBuild: 1` so player builds
  produce content (project#14).
- `EnvironmentLoader` fires `LoadingDone` when no Addressables location exists and
  releases the scene handle (trainingcore#16) — modules must still load in-editor.

## Android (project#6/#7/#18)

- `ProjectSettings.minSdkVersion = 29` — required by apexunitysdk's `psdkman.aar`
  (project#7).
- OVR external-composition serialized fields mismatch (project#6); MRTK Standard
  shader GLES3 multiview+instancing compile error (project#18); Oculus NSC patch
  (`enableNSCConfig`) writes a `network_sec_config.xml` that was never in the repo —
  disable it (project#18).

## Apex SDK embedding (project#5/#10)

- `com.pixovr.apexunitysdk` and `com.pixovr.trainingcore` are embedded as git
  submodules under `Packages/` so `PixoVR > Setup` can write into them; do not also
  add the git URL to manifest.json (project#5/#10, `420ab94f`).

## Mapping kind / serialized shadowing

- ScriptableObject vs MonoBehaviour targets now report `target-kind-mismatch` instead
  of rewriting wrong — check those rows (the CredentialLoader case, project `5e7d2ca6`).
- Serialized `Config: {fileID: 0}` on ApexCredentialsLoader shadowed the
  Editor-assigned config asset — drop the empty serialized field (project `ef002dbb`).

## UnityEvent targets to project types (project#30)

- `Assembly-CSharp` event targets are *reported* (`unmapped-event-target` if
  `Luminous.*`), not renamed — do the namespace rename in the project.
- MessageBox `OnClick` → `Luminous.Unity.DisplayInteractionMiddleman` is mapped to
  `PixoVR.TrainingCore.Utility.Display.DisplayInteraction` (project#30).

## Graphs (project#34/#36, trainingcore#41)

- SetComponentState `assemblyQualifiedName` — now a rewriter rule; re-run the migrator
  on graphs still carrying `Luminous.*` names (project#34).
- GenericAction targets whose script was deleted resolve to nothing — restore or
  rewire (project#36 `01f3855d`: GenericAction → GenericActionTrigger was just an
  `unresolved-pixo-script` that resolved once the class existed).
- Group membership lives in `groupNodeFunctionality.Stacks[].nodeGUIDs`
  (trainingcore#41); an empty Stacks = no grouped steps (AndGroupStep warns and
  completes). Check group nodes in the graph editor.
- **Dangling GUID refs** that were already broken in Luminous: to audit, extract the
  `serializedGuid` of every `GuidComponent` in the scene (including prefab-instance
  overrides) and diff against every guid referenced in the graph asset.

## Prefab-instance overrides (project#38)

- `m_RemovedComponents` entries hide real bugs: the UpstreamSampleSupply valve had
  `Valve`/`XRIValveBehaviour`/`ObservableSubject` stripped by an authored override —
  steps 19/20 could never turn it (project#38). The migrator only reports
  `info-removed-components`; review every row.

## Multiuser (project#36)

- `PhotonView.m_ObservedComponents` must list `PhotonSyncView` on each valve prefab —
  otherwise `IPunObservable` never fires and remote turns don't replicate (manual;
  `0c7fcad8` stays manual). Grab replication works via `NetworkGrabManager` events —
  no prefab change needed.
- Two headsets are required to actually test: late-join catch-up, remote grab,
  remote valve turns, owner-gated resets.

## Trigger-only colliders (trainingcore#44)

- XRI 3.0.8 strips trigger colliders from `m_Colliders` → hover never fires on
  trigger-only tap/grab targets (gas-canister end-cap nuts, hand-grab zones).
  `XRIGrabBehaviour` back-fills them; the rig's direct interactors use
  `PhysicsTriggerInteraction = Collide triggers` and `StateChange` select. Custom
  rigs must set both.

## Valves (trainingcore#35/#39/#40)

- `Clamps` must be authored (Open/Closed values) or completion never fires.
- `InitialRotation` applies in `Awake` regardless of `ReinitializeOnEnable`; freeze
  gates only the interactive driver — network/reset rotations still apply (#40).
- Driver angle is measured around the wheel pivot in parent space; tune
  `RotationMultiplier` per valve (#35).
- Attach-point offset is the interactable-local formula — a zone-space version put
  wheel keys several cm off in rotated AttachPoints (#39).
- Snap zones are trigger-based (trainingcore#30).

## Inactive objects / SubjectId (trainingcore#33/#37)

- Inactive `GuidComponent`s are pre-registered before graph parse — teleport visuals,
  highlights, and tap targets that start disabled are resolvable. But event
  subscriptions key on `SubjectId` = guid — a graph-authored subject that doesn't
  match the object guid still misses.

## Session/tracking (trainingcore#37/#43/#45)

- Login callbacks wait for the module-access check; `CompleteSession` fires on module
  exit via `StopFlow`; scenario id is `"<catalog module name> - <GameMode>"` using
  `SelectedModuleName` (the lobby selection), not the scene/graph name.
- Diagnose platform issues via `[Apex Diag]` logs.

## Residual `Luminous` strings

- Harmless: comments, `Assembly-CSharp` event-target type names, display strings.
- Not harmless: `type:` managed refs, `m_Script` refs, `assemblyQualifiedName`,
  `m_TargetAssemblyTypeName`. Use the `residual Luminous strings by file:` report
  section to triage.

## Runtime validation (mandatory)

Static compile proves nothing — every item above was found on a Focus 3. Smoke-test
order used on device:

1. Login → catalog populated (Single User pages non-empty).
2. Module load, lobby environment unloads.
3. Assisted Training progression: task cards clear on step advance, card audio stops,
   highlights toggle.
4. Grabs → snaps → toolbox return.
5. Valves (hand-driven and wheel-key driven) complete at authored clamps.
6. Teleport steps.
7. Wrist-menu next/prev — no orphaned cards/highlights.
8. Scenario end → `CompleteSession` (`[Apex Diag]` log).
9. Multiuser: second headset late-joins, catch-up, remote grabs/valves.
