# P3M4B2A — Scene Local Player Activity Lifecycle

Status: Pending Unity validation  
Type: Runtime integration / product lifecycle  
Depends on: P3M4A PASS and P3M4B1 PASS

## Objective

Connect `SceneLocalPlayerAdmissionAuthoring` configured as `On Activity Enter` to the
canonical Activity Content Execution pipeline without creating a second Player lifecycle.

This subcut supports Activities whose Player requirement is at most:

```text
SelectedActors
```

External Logical Actor adoption and gameplay readiness remain a separate gate because the
current canonical materialization adapter owns and destroys framework-instantiated Actors.
That ownership contract must not be reused implicitly for scene-owned objects.

## Runtime flow

Enter order:

```text
resolve surfaces declared by the Activity Content Profile
sort by configured Player Slot order
admit exact scene Hosts
select exact Actor Profiles
execute canonical Activity Player lifecycle
```

Exit order:

```text
execute canonical Activity Player lifecycle exit
clear Activity-owned Actor selections
release Scene Local Player admissions in reverse order
restore Slots to Available
```

The composite participant preserves the canonical content id:

```text
framework.player-actor.activity-lifecycle
```

The generic Activity executor still receives one required participant. Scene admission is
an implementation detail of that participant rather than a second competing lifecycle.

## Activity ownership

A surface participates automatically only when all conditions are true:

```text
Admission Timing = On Activity Enter
surface GameObject belongs to a loaded scene
that scene is explicitly declared by the Activity Content Profile
Slot, Host, Actor Profile and scene Actor evidence are valid
```

No lookup by Player name, tag, hierarchy convention or singleton is used. Scene traversal
only locates product-surface declarations; all functional objects remain serialized typed
references owned by each declaration.

## Transaction and compensation

If admission or selection fails during Enter:

```text
clear selections already applied by this enter
release admissions already committed by this enter
return a blocking diagnostic
```

If the canonical Player participant fails after Scene admission:

```text
roll back Scene selection and admission
report canonical failure and rollback evidence together
```

If Exit fails after releasing part of a multi-Player set:

```text
re-admit released entries
restore their Actor selections
retain the Activity record
return a blocking failure for deterministic retry
```

Physical scene objects are never created, destroyed, enabled or disabled by this subcut.

## Explicit boundary

Activities requiring any of the following are rejected explicitly in P3M4B2A:

```text
LogicalActorsPrepared
GameplayReady
```

Those requirements need P3M4B2B, which will introduce typed adoption evidence for an
externally owned Logical Actor and a release path that unregisters framework context without
destroying the scene object.

## Files and product surface

Affected product surface:

```text
Scene Local Player Admission
  Admission Timing = On Activity Enter
```

Runtime additions:

```text
SceneLocalPlayerAdmissionActivityLifecycleRuntime
SceneLocalPlayerAdmissionCompositeLifecycleParticipant
SceneLocalPlayerAdmissionActivityLifecycleResult
SceneLocalPlayerAdmissionActivityLifecycleStatus
```

Composition:

```text
FrameworkRuntimeHost
-> PlayerActorPreparationRuntimeHostModule
-> canonical Player Activity participant
   composed with Scene Local Player lifecycle
```

Late `LocalPlayerProvisioningAuthoring` binding recomposes the combined source so the
provisioned and scene-existing physical paths coexist without overwriting each other.

## Out of scope

```text
external Actor adoption into PlayerActorPreparationRuntimeContext
RuntimeContent registration for a scene-owned Actor
gameplay occupancy
input binding
camera eligibility and publication
gameplay admission
FIRSTGAME integration
```

## QA gate

Expected smoke:

```text
[P3M4B2A_SCENE_LOCAL_PLAYER_ACTIVITY_LIFECYCLE_SMOKE]
status='Passed'
cases='11'
```

The smoke proves:

```text
canonical participant identity preserved
ordered two-Player Activity enter
Actor selections committed before canonical enter
public joining remains closed
idempotent enter
selection cleared after canonical exit
admissions released after canonical exit
external Hosts and Actors preserved
idempotent exit
Actor-adoption requirements rejected explicitly
no retained admission token
```

## Acceptance

```text
compiles in Framework and QA
P3M4B2A smoke PASS
P3M4B1 smoke PASS 10
P3M4A smoke PASS 7
C9M PASS 6
C9R PASS 11
P3 canonical PASS 31
no Missing Script
no Slot stranded in Reserved, Joined or Leaving after Exit
no Actor selection retained after Exit
no external Host or Actor destruction or active-state mutation
```

## Suggested commit

```text
Feat: integrate Scene Local Player Activity admission lifecycle
```
