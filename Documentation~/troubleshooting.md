# Troubleshooting

Symptom → likely cause → where to look → fix. Device logs: `adb logcat -s Unity`,
then grep the prefix in the last column. `project#N` = sa-collect-gas-sample PR,
`trainingcore#N` = TrainingCore PR.

| Symptom | Likely cause | Where to look | Fix / ref |
|---|---|---|---|
| Cards don't disappear when a step is skipped | start actions weren't command-backed; undo had nothing to rewind | `CommandHistory.UndoStep`, action `Undo()` | Highlight/SetComponentState/Generic actions are command-backed — trainingcore#42 |
| Two cards / next step entered twice | group node read the `executes` port instead of `Stacks[].nodeGUIDs` | `GroupNodeFunctionality.Stacks` in graph asset; `Step entered:` logs (Flow) | trainingcore#41 |
| Still-snapped key loses its snap on release | `XRGrabInteractable.Drop()` reparented it | XRIGrabBehaviour; `[XRI Diag]` | snapped-hold release skips Drop — trainingcore#40 |
| Valve turns without the key / completes on first nudge | `TotalRotation` never initialized; Clamps unauthored | XRIValveBehaviour `InitialRotation`/`Clamps` | trainingcore#40/#35 |
| Remote valve turn doesn't complete the step locally | `OnRemoteValveTurn` not publishing | `PhotonSyncView` serialization | trainingcore#38 |
| Valve turns locally but not on peer | `PhotonSyncView` missing from `PhotonView.m_ObservedComponents` | valve prefab | wire it manually — project#36 |
| Wheel key lands offset in the zone | zone-space offset vs interactable-local formula | XRISnapZone `FindAttachPointOffset`; rotated AttachPoints | trainingcore#39 |
| Nut taps / hand-grab zones never register | trigger colliders stripped from `m_Colliders`; interactor ignores triggers | component `m_Colliders`; rig `m_PhysicsTriggerInteraction` | trainingcore#44 |
| Per-frame select ping-pong on shared grab | `m_SelectActionTrigger: 0` (State) | rig direct interactors | StateChange — trainingcore#44 |
| "CompleteSession never sent" / "JoinSession failed: User access not verified" | login resolved before module-access check; or module-start race | `[Apex Diag]` logs | access-check gate + run token — trainingcore#37 |
| scenarioId is the scene name | `SelectedModuleName` (lobby choice) vs graph/scene name | `[Apex Diag] joining session` log | trainingcore#45 — catalog module name used |
| Single User pages empty | no `ScenarioCatalog` assigned | `GetUserScenarios`; `ApexPlatformSession` fields | create + assign catalog — trainingcore#28, project#27 |
| Teleport spot not highlighted / nut inactive target unresolved | inactive GuidComponents not registered before parse | `GuidRegistry`; `FindObjectsOfType<GuidComponent>(true)` | trainingcore#33; check `SubjectId` — #37 |
| SIGSEGV on Awake | `XROrigin.m_Camera` bound to GameObject not Camera | rig prefab | trainingcore#20 |
| Head follows left controller | TrackedPoseDriver bound to Left actions | Main Camera | bind Head — trainingcore#21 |
| Teleport mode never engages on device | polled `InputAction.triggered` | ControllerModeManager | poll action phase — trainingcore#24 |
| UI ray retracts on non-interactable UI | line-visual auto-retract | rig line visuals | trainingcore#27 |
| VR keyboard keys ignored | CurvedUI raycast hits child colliders | CurvedUI raycast | skip nested colliders — project#26 |
| Snap target won't accept object | zone wasn't trigger-based / occupant tracking missed pre-placed items | XRISnapZone; `SetStartingOccupant` | trainingcore#30/#35 |
| Snap/release on another headset does nothing | no grab replication | `NetworkGrabManager` events | trainingcore#38 |
| Late joiner starts from scratch | catch-up requested in lobby, before graph existed | `GraphStarted`/`GraphRunning`; PhotonNetworkManager | trainingcore#38 |
| Joining a running graph still waits | catch-up deferred to graph start that already fired | `GraphRunning` flag | trainingcore#38 |
| "grab storm" / hundreds of Grab logs per second | repeat SelectEnter on snapped-hold (isSelected stays false) | XRIGrabBehaviour `snappedHold` guard | trainingcore#40 |
| Random objects reset on a remote device | lost-object reset not owner-gated | `ExternalOwnershipGate` | trainingcore#38 |
| Wrist-menu prev doesn't un-highlight | Highlight/ComponentState/Generic actions mutated directly, no command | action `Undo()` | trainingcore#42 |
| `[Apex Diag]` shows `module end skipped` | module report latch cleared early | PlatformSessionBase latching | trainingcore#37 |
| Steps complete but flow "hangs" at a group | empty `Stacks` → group had no children | `AndGroupStep` warning `has no grouped steps` (Flow) | trainingcore#41/#42 |
| Fail step fires on the wrong event | exception matching was reference-equality | `FailExceptionParameter.Equals` | Luminous value-equality port — trainingcore#37 |
| Undoing fires failures | events published during history undo | `CommandHistory.IsUndoing` → `IgnoreFailure` | trainingcore#37 |
| Focus 3 build fails on MRTK Standard | GLES3 multiview+instancing compile error | shader | project#18 |
| minSdk/merge error on Android build | apexunitysdk `psdkman.aar` needs minSdk 29 | ProjectSettings | project#7 |
| "referenced script is missing" warnings | Luminous DLL-internal guid refs left in scenes | scene yaml | pre-existing dead refs; candidates for `m_RemovedComponents`/strip, not remap |

Useful greps while on device: `Step entered` (Flow), `[XRI Diag]` (grab/snap/valve/
ray state), `[Apex Diag]` (login/session/module reports).
