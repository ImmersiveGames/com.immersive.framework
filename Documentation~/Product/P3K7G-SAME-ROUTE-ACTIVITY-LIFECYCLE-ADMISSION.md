# P3K.7G — Same-Route Activity Lifecycle Admission Integration

Status: **closed — Unity compile and QA PASS (40 cases)**  
Type: **runtime integration + lifecycle transaction + technical QA**

## Objective

Integrate the official P3K.7F Session Player gameplay runtime into a real
same-Route Activity switch.

```text
current GameplayReady Activity
-> create target Activity runtime scope
-> stage target Player Actor candidates
-> reach P3K.7E ReadyToCommit
-> authorize transition presentation
-> prepare target Activity scenes
-> commit the multi-Slot handoff
-> adopt exact P3J/P3K evidence in P3J.6
-> publish the target Activity as current
-> release previous Activity scope and scenes
```

## Scope

This cut applies only when:

```text
an Activity is already active
requested target Activity belongs to the same active Route
target requirement level is GameplayReady
target projects one or more joined Player Slots
current projected Slots are GameplayReady under the current Activity owner
```

Non-`GameplayReady` Activities keep the existing lifecycle path.

## Pre-transition authority

`ActivityPlayerLifecycleAdmissionRuntimeContext` is Session-scoped and composed
by `PlayerGameplayRuntimeHostModule`.

Before `GameFlowRuntime` applies the transition gate or invokes the visual
transition, it:

```text
validates the current Activity-owned P3J/P3K chain
creates the target Activity scope
stages one inactive target candidate per projected Slot
begins the P3K.7E group
requires ReadyToCommit
```

The current Activity identity, scenes and lifecycle remain authoritative while the Player handoff stays explicitly reversible. No frame is yielded before an occluding transition begins when scene preparation is required.

Any failure before ownership commit rolls back:

```text
P3K.7E group progress
candidate materializations
target RuntimeContent root
```

No visual transition starts for a rejected or pending admission.

A `GameplayReady` switch that has asynchronous scene side effects may not use a
non-occluding `Seamless` operation. It must declare a transition mode whose
operation plan provides visual occlusion. Scene-free seamless switches remain
valid because the full begin/commit path is synchronous and yields no frame.

## Activity activation boundary

`ActivityFlowRuntime` now supports one explicit activation gate after target
scene composition and before target state publication, content lifecycle or
previous Activity cleanup.

```text
target scenes prepared
-> activation gate invokes P3K.7G Commit
-> allowed: publish target Activity and execute lifecycle
-> blocked: release target scenes and keep previous Activity current
```

The pre-created target RuntimeContent root remains owned by the P3K.7G
coordinator until the transaction commits or rolls back.

## P3J.6 adoption

The P3J.6 Activity lifecycle participant no longer rejects `GameplayReady` as an
unsupported future level.

After a successful group commit:

```text
previous Activity Exit
  recognizes the exact committed transaction
  does not release the already-superseded previous Actor again

target Activity Enter
  adopts the exact promoted preparation and admission tokens
  retains them as the active Activity Player Actor record
```

There is no second preparation call and no duplicate Actor materialization.

## Ordered Activity exit

A later normal exit of the adopted target Activity uses:

```text
P3K.5 admission release
-> camera request
-> camera eligibility
-> input binding
-> occupancy
-> P3J prepared Actor release
```

P3J release is blocked explicitly if the gameplay chain cannot release.

## Diagnostics

Public immutable evidence:

```text
ActivityPlayerLifecycleAdmissionToken
ActivityPlayerLifecycleAdmissionSlotSnapshot
ActivityPlayerLifecycleAdmissionSnapshot
ActivityPlayerLifecycleAdmissionResult
```

The snapshot records:

```text
previous and target Activity owners
transaction state and status
P3K.7E group evidence
transition authorization
previous lifecycle exit acknowledgement
target lifecycle adoption
per-Slot candidate, preparation and admission tokens
commit cleanup state
```

No public contract retains Unity object references.

## Product surface

No new designer-facing component is introduced.

Existing product authoring remains authoritative:

```text
ActivityAsset
Activity-owned Projection configuration
PlayerParticipationRequirementsProfile
PlayerSlotProfile
ActorProfile
LocalPlayerProvisioningAuthoring
Player gameplay camera/input authoring
```

The feature removes manual runtime assembly from consumers. It does not replace
Activity or Player authoring with a technical manager.

## Out of scope

```text
Route change and Route Startup Activity handoff
Activity restart integration
GameplayReady Activity with no current Activity
join/leave during the transaction
commit-cleanup retry product command
FIRSTGAME integration
new Activity Composer or wizard
```

## QA

Run in a fresh Play Mode session after framework boot:

```text
Immersive Framework
  > QA
    > Player
      > P3K.7G Run Same-Route Activity Lifecycle Admission Smoke
```

Expected:

```text
[P3K7G_SAME_ROUTE_ACTIVITY_LIFECYCLE_ADMISSION_SMOKE]
status='Passed'
cases='40'
```

## Next cut

```text
P3K.7H — Route Startup Activity Player Admission Integration
```

That cut must redesign the Route switch ordering so the destination Startup
Activity can stage and validate Player participation before the previous Route
and Activity are irreversibly torn down.
