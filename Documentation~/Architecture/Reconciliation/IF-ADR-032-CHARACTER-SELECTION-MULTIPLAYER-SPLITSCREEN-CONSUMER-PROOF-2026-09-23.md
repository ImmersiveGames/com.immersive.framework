# IF-ADR-032 — Character Selection Multiplayer Split-Screen Consumer Proof — 2026-09-23

Status: **CONSUMER UNITY PASS — CAMERA-032-D/E two-Output explicit-selection sample proven; terminal Camera teardown Unity revalidation PASS**

Normative authority:

- [IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle](../ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)

Consumer repository:

- `ImmersiveGames/planet-devourer`
- sample: `Assets/_Sample/PlayerSamples/CharacterSelectionMultiplayerSplitScreen/`
- relevant consumer commits: `e0160046a011861d34828b657aea8cb8c656904d`, `ddfb9c268dd7703a0ddbaf5b9c65127d04acef60`

## Scope

This record captures manual Unity Play Mode evidence for the concrete Character Selection + local multiplayer split-screen consumer after migration to IF-ADR-032.

The sample intentionally combines:

~~~text
Player Session
  HostProvisioning = ManagerProvisioned
  ActorResolution = LeaveUnresolved
  Slots = P1, P2

Camera Session
  Output P1
  Output P2

Player Output Bindings
  P1 -> Output P1
  P2 -> Output P2

Player Presentation Bindings
  P1 -> ThirdPerson P1
  P2 -> ThirdPerson P2

Route
  Route Presentation P1
  Route Presentation P2

Activity
  ThirdPerson Presentation P1
  ThirdPerson Presentation P2
~~~

Character selection remains explicit. Joining a Player does not choose or prepare an Actor automatically.

## Consumer UI boundary

The Join tutorial and Character Selection menu are independent presentation concerns.

~~~text
Join UI
  -> Open / Join / Leave
  -> observes current Session occupancy

Character Selection UI
  -> observes current Session Slots
  -> shows one Actor-choice panel only for a Joined Slot with no selected Actor
  -> Farmer / Cow buttons use PlayerSessionSelectActorCommandTrigger
~~~

The sample-owned selection controller treats `PlayerSessionObserver.Changed` as invalidation only and defers the canonical observation read to `Update`. This avoids observing a partially published Join mutation while Manager-provisioned Host evidence is still being committed.

The UI does not own Session mutation, Actor preparation, Camera Subject publication, Camera Presentation selection or split-screen layout.

## Runtime evidence

### Boot

Observed:

~~~text
actorResolutionPolicy = LeaveUnresolved
Camera Session Outputs materialized = 2
Route Presentation P1 materialized -> Output P1
Route Presentation P2 materialized -> Output P2
Activity ThirdPerson P1 materialized -> Output P1
Activity ThirdPerson P2 materialized -> Output P2
Player Presentation selection attached independently for P1 and P2
~~~

No Actor is selected at boot.

### P1 Join and explicit selection

Observed:

~~~text
Join P1
  -> SucceededJoined
  -> logical Actor remains unprepared
  -> ownership diagnostic = Succeeded
  -> selected Actor = none

P1 selects Cow
  -> SucceededSelected
  -> exact Slot = player.multi.1
  -> ActorProfile = actor-profile.cow
  -> Actor prepared/materialized
  -> GameplayReady
~~~

There was no automatic default-Actor selection.

### P2 Join and independent explicit selection

Observed:

~~~text
Join P2
  -> SucceededJoined
  -> logical Actor remains unprepared
  -> ownership diagnostic = Succeeded

Before P2 selection
  -> joined = 2
  -> selected = 1
  -> readinessReason = WaitingForActorSelection

P2 selects Farmer
  -> SucceededSelected
  -> exact Slot = player.multi.2
  -> ActorProfile = actor-profile.farmer

After preparation
  -> joined = 2
  -> selected = 2
  -> readiness = Completed
~~~

This proves that P1 readiness does not substitute for P2 and that each Player-specific Presentation consumes its own explicit Player binding.

### Leave, rejoin and fresh explicit choice

Observed for P1:

~~~text
Leave P1
  -> SucceededLeft
  -> activityReleased = True
  -> provisioningReleased = True
  -> terminalCommitted = True
  -> partialRelease = False
  -> Slot Available
  -> technicalHost = False
  -> selectedActor = none
  -> logicalActorPrepared = False
  -> physicallyMaterialized = False
  -> gameplayAdmitted = False

Rejoin P1
  -> SucceededJoined
  -> new technical Host
  -> selected Actor remains unresolved
  -> ownership diagnostic = Succeeded
  -> readinessReason = WaitingForActorSelection

P1 selects Farmer
  -> SucceededSelected
  -> selection revision advances
  -> Actor prepared/materialized again
~~~

The consumer therefore proves that Leave clears the old Actor selection and that Rejoin requires a fresh explicit Actor choice. Exact stale CameraSubject-token rejection remains a focused QA criterion; this consumer proof does not replace that lower-level evidence.

### Final zero-Player state

Observed:

~~~text
Leave P1
Leave P2
  -> both SucceededLeft
  -> hostCount = 0
  -> joined = 0
  -> selected = 0
  -> both Slots Available
  -> no prepared/materialized/gameplay-admitted Actor remains
~~~

## Ownership diagnostic regression

Before the sample UI refresh fix, synchronous observation from `PlayerSessionObserver.Changed` could query input ownership during the middle of a Join transaction and emit transient:

~~~text
FRAMEWORK_PLAYER_INPUT_OWNERSHIP_DIAG
status = Failed
stage = RegisteredHost.NotRegistered
~~~

After consumer commit `ddfb9c268dd7703a0ddbaf5b9c65127d04acef60`, the same Join/Rejoin flow reports:

~~~text
FRAMEWORK_PLAYER_INPUT_OWNERSHIP_DIAG
status = Succeeded
stage = Succeeded
~~~

The fix is sample-owned presentation scheduling only. Framework ownership semantics were not changed.

## Application-quit teardown observation

The functional lifecycle completed successfully before application shutdown.

On exiting Play Mode, one Route Camera Presentation release attempted normal request arbitration after Unity had already made the Output Default CinemachineCamera unavailable as a valid loaded-scene object:

~~~text
Lifecycle Camera Presentation release failed
Default CinemachineCamera 'Cinemachine Camera' is not part of a valid loaded scene
~~~

This is terminal application-shutdown ordering debt, not Character Selection / Player / split-screen authoring debt.

The normal runtime cases above remain valid because the warning occurs only from `FrameworkRuntimeHost.OnApplicationQuit` after the final zero-Player state was already reached.

Framework terminal-shutdown cleanup was implemented after the original failing capture. `CameraPresentationLifecycleRuntime` now has an application-quit-only terminal release path that disposes Presentation subscriptions/ownership without requiring normal CameraRequest winner restoration after Unity physical Camera scene validity is gone. Session Presentation cleanup uses the same terminal path.

A Unity rerun on 2026-09-23 validated the correction. Application quit released both Activity ThirdPerson Presentation occurrences and both Route Presentation occurrences cleanly, then tore down both Session Outputs. The previous `Lifecycle Camera Presentation release failed` / invalid Default CinemachineCamera diagnostic did not recur.

This does not change normal Route / Activity / Session release semantics.

## Result

~~~text
Character Selection + two-Output split-screen consumer
  Implemented                                      PASS
  Unity import / Play Mode                         PASS
  ActorResolution = LeaveUnresolved                PASS
  P1 explicit Actor selection                      PASS
  P2 explicit Actor selection                      PASS
  P1/P2 independent Output participation           PASS
  two-Player split-screen                          PASS
  Leave exact occurrence                           PASS
  Rejoin requires fresh explicit Actor choice      PASS
  final 0-Player cleanup                           PASS
  transient ownership warning regression           PASS
  clean application-quit Camera teardown           PASS — Unity rerun 2026-09-23
~~~

This consumer closes practical sample proof for CAMERA-032-D/E. It does not by itself close the full IF-ADR-032 certification matrix. Later on 2026-09-23, CAMERA-032-C focused Game Flow QA separately closed force-default transition continuity and Route replacement/restoration with 16/16 PASS plus canonical restore PASS. A later focused Subject + Transaction run then closed fresh same-logical-id Subject occurrence, stale availability/selection/membership rejection, stale rollback protection and transaction integrity with 17/17 PASS. CAMERA-032-F cleanup, remaining authoring/static reconciliation and aggregate recertification remain separate closure work.

Subsequent evidence:

- [IF-ADR-032-C Game Flow Lifecycle Focused Validation — 2026-09-23](IF-ADR-032-C-GAME-FLOW-LIFECYCLE-FOCUSED-VALIDATION-2026-09-23.md)
- [IF-ADR-032 Subject + Transaction Focused Validation — 2026-09-23](IF-ADR-032-SUBJECT-TRANSACTION-FOCUSED-VALIDATION-2026-09-23.md)
