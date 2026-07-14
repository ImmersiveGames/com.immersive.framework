# P3K.1 — Post-Materialization Binding Conformance Audit

**Status:** Completed / decision gate closed
**Phase:** P3K — Post-Materialization Bindings and Gameplay Readiness
**Date:** 2026-07-14
**Package:** `com.immersive.framework`
**QA:** `rinnocenti/QAFramework`
**Unity baseline:** Unity 6.5 / Input System 1.19.0
**Decision gate:** `DG-P3-07`

---

## P3K.3 code-level correction

The implementation inspection performed for P3K.3 supersedes the P2 control assumptions recorded in the original audit. The current accepted package does **not** contain `PlayerControlRuntimeContext`, and the older F52 binding/bridge/activation chain is not the operative product runtime.

The current reusable P2 surface is:

```text
explicit stable-host PlayerInput
PlayerActorDeclaration PlayerInput evidence
UnityPlayerInputGateAdapter
Pause / Transition Gate snapshots
game-owned movement
```

Accordingly, P3K.3 introduces a narrow `PlayerGameplayInputBindingRuntimeContext` backed by the live P3K.2 occupancy authority. It does not recreate the removed F52 chain and does not execute `PlayerComposer` at runtime. All later sections in this document must be read with this correction.

---

## P3K.4 code-level scope correction

P3K.4 is implemented as a pure prepared-Player camera eligibility authority.
It validates the live P3K.2 occupancy token, the live P3K.3 gameplay input
binding token, the generated Actor identity and explicit Actor-owned camera
authoring.

It derives the future request, lifetime and tie-break identities, but it does
not publish a `CameraRequest`. Publication is deferred to P3K.5 so the camera
request, `GameplayReady` aggregation and reverse rollback are owned by one
transaction rather than by a temporary partial admission.

The accepted P3K.4 surface is:

```text
PlayerGameplayCameraAuthoring
PlayerGameplayCameraEligibilityRuntimeContext
PlayerGameplayCameraEligibilityToken / summary / snapshot / result
```

`LocalPlayerCameraRequestBinding` remains outside the contextual P3 runtime.

---

## 1. Executive decision

P3K must add one explicit **post-preparation admission transaction** between the P3J Logical Actor preparation authority and the existing occupancy, Player control and camera subsystems.

The accepted boundary is:

```text
Activity lifecycle authority
-> P3J prepares one contextual Logical Player Actor
-> P3K validates the exact current preparation evidence
-> P3K confirms effective runtime occupancy
-> P3K binds control and the stable host PlayerInput
-> P3K activates gameplay input
-> P3K optionally publishes an eligible camera request
-> P3K publishes GameplayReady evidence
```

Release is strictly reversed before P3J destroys the Logical Actor:

```text
GameplayReady removed
-> camera request released
-> gameplay input activation cleared
-> PlayerInput bridge cleared
-> Player control binding cleared
-> effective runtime occupancy released
-> P3J releases the Logical Actor
```

P3K must not:

```text
turn PlayerSlotOccupancy into a mutable runtime authority
run PlayerComposer at runtime
require a scene-authored PlayerComposer for materialized Players
let LocalPlayerCameraRequestBinding publish during OnEnable
recreate the removed F52 Player binding chain
make camera universally mandatory
infer targets through name, tag or hierarchy conventions
treat LogicalActorsPrepared as GameplayReady
```

---

## 2. Audited current flow

The current official runtime already provides:

```text
Session Player participation
  configured Slots
  join/reservation state
  ActorProfile selection

Stable Local Player Host
  PlayerInput
  runtime PlayerSlotDeclaration
  explicit Actor Mount

P3J preparation
  explicit RuntimeScopeContext
  selected ActorProfile
  contextual ActorId
  RuntimeContent identity
  preparation token
  Logical Actor instance
  PlayerActorDeclaration
  transactional release and stale-token rejection
```

The P3J.6 QA proves:

```text
real PlayerInputManager join
Activity-owned Logical Actor preparation
Activity restart creates a new owner-bound Actor identity
stale preparation token rejection
Activity clear releases the Actor
stable host, PlayerInput, Slot and selection survive
negative required Slot remains NotReady without leaks
```

What remains absent is a typed transaction from this preparation evidence to:

```text
effective occupancy
Player control binding
PlayerInput bridge
gameplay action-map activation
camera eligibility
GameplayReady
```

---

## 3. Current component classification

| Surface | Current behavior | P3K decision |
|---|---|---|
| `PlayerActorPreparationRuntimeHostModule` | Registers stable joined hosts and coordinates Session selection/preparation. | Keep as P3J authority. Expose narrow internal current-preparation evidence to a sibling P3K module; do not absorb input/camera policy. |
| `LocalPlayerHostAuthoring` | Stable technical host owning exactly one `PlayerInput`, one Actor Mount and runtime joined Slot evidence. | Reuse as the stable PlayerInput owner and host endpoint. It remains non-Actor and does not become GameplayReady authority. |
| `PlayerActorMaterializationSnapshot` | Immutable Slot, ActorProfile, ActorId, RuntimeContent owner/identity, state and revision evidence. | Reuse as mandatory admission input. |
| `PlayerActorPreparationToken` | Guards preparation/release mutations and rejects stale operations. | Include in every P3K admission and release request. |
| `PlayerSlotOccupancy` | Passive authored relation. It explicitly does not change occupancy, spawn, replace or register runtime state. | Do not mutate or use as P3K authority. Preserve only for passive/pre-authored diagnostics and older authoring surfaces. |
| `PlayerSlotOccupancyDescriptor` | Passive description with `ChangesOccupancy == false`. | Not sufficient as effective runtime occupancy evidence. |
| legacy F52 PlayerBinding targets | Historical passive binding/bridge/activation evidence. They are not the accepted current Player product runtime. | Do not reintroduce them into the contextual P3 path. |
| `PlayerGameplayInputBindingRuntimeContext` | P3K.3 Session authority binding live occupancy to the stable-host `PlayerInput`. | Use as the narrow typed gameplay-input transaction, with reverse release and Gate-derived availability. |
| `PlayerComposer` | Designer authoring/apply surface for pre-authored Players. | Do not invoke at runtime. P3 logical prefabs need a smaller explicit endpoint surface. |
| `LocalPlayerCameraRequestBinding` | Converts PlayerComposer data into a camera request and may publish on `OnEnable`. | Do not use in the P3 materialized path. It hard-depends on PlayerComposer and serialized eligibility/request identity. |
| `CameraOutputSessionBinding` | Explicit session-scoped owner of `CameraOutputSession`; no global registration. | Reuse unchanged as camera output authority. |
| camera request/context/publisher primitives | Typed request arbitration and explicit release. | Reuse through a new P3K runtime adapter. |

---

## 4. Major findings

### 4.1 `PlayerSlotOccupancy` is not effective runtime occupancy

The component is intentionally passive:

```text
does not set occupants
does not clear occupants
does not spawn or destroy Actors
does not register runtime state
```

It also permits fallback serialized Slot and Actor text when declarations are absent. That is acceptable for a passive diagnostic declaration, but it is not acceptable for a runtime admission token.

P3K therefore requires a new immutable runtime relation:

```text
PlayerGameplayOccupancySnapshot
  SessionContextId
  RuntimeContentOwner
  PlayerSlotId
  ActorId
  ActorProfileId
  PlayerActorPreparationToken
  RuntimeContentIdentity
  MaterializationRevision
  OccupancyRevision
  State
```

This relation is created only after the exact current P3J preparation has been validated.

### 4.2 P3J remains the materialization authority

P3J already owns:

```text
selected Profile resolution
stable host registration
Logical Actor creation
ActorId generation
RuntimeContent registration
activation/release
preparation token and stale-operation rejection
```

P3K must not duplicate these records or retain an independent physical Actor reference as a second lifetime authority.

The P3K module may retain a scoped admission record containing the current preparation token and typed binding handles. The Actor instance remains owned by the P3J materialization handle and contextual RuntimeContent owner.

### 4.3 P3K requires a narrow gameplay-input binding authority

The code-level inspection confirmed that the accepted package does not contain `PlayerControlRuntimeContext`. The older F52 PlayerBinding chain must not be revived as a hidden dependency.

P3K.3 therefore introduces:

```text
PlayerGameplayInputBindingRuntimeContext
  validates live P3K.2 occupancy
  validates exact stable-host PlayerInput
  validates generated PlayerActorDeclaration identity
  activates/restores the configured gameplay action map
  derives Allowed / BlockedByGate from UnityPlayerInputGateAdapter
  guards release with a typed binding token
```

For Activity-owned Logical Actors, this binding is Activity-sensitive even though the Local Player Host persists:

```text
stable host persists
PlayerInput persists
Slot selection persists

Activity Actor changes
ActorId changes
occupancy token changes
input binding token changes
preparation token changes
```

Activity Restart must release the old gameplay-input binding before occupancy and Actor release, then bind the newly prepared Actor.

### 4.4 Existing Gate adapter is reused; legacy F52 targets are not

`UnityPlayerInputGateAdapter` remains the accepted adapter for Pause/Transition availability. P3K.3 validates that it targets the exact stable-host `PlayerInput` and uses its configured gameplay action map.

The contextual runtime path supplies typed identity through preparation and occupancy tokens. Serialized Slot or Actor strings, legacy bridge targets and `PlayerComposer` runtime execution are not authorities.

### 4.5 P3 logical prefabs need a narrow explicit endpoint

The P3 materialized Logical Actor Host must not require a full `PlayerComposer`.

Add one small authoring endpoint on the Logical Actor prefab, provisionally:

```text
PlayerGameplayBindingAuthoring
  PlayerControlBindingTargetBehaviour
  explicit control target
  optional camera target
  optional look-at target
  optional camera rig/profile reference
  camera requiredness
```

Rules:

```text
references are explicit
references must belong to the materialized Logical Actor hierarchy
no PlayerInput reference
no PlayerSlotId or ActorId string
no runtime selection policy
no movement implementation
no camera winner logic
```

The stable Local Player Host remains the source of `PlayerInput` and joined Slot evidence.

### 4.6 `LocalPlayerCameraRequestBinding` is not reusable for P3

The current component requires:

```text
PlayerComposer
PlayerComposer PlayerSlotId and ActorId
PlayerComposer camera targets
serialized eligibility scope id
serialized request id
CameraRigComposer
```

It may also publish from `OnEnable`.

This is incompatible with a contextual Actor whose identity and owner are generated at runtime. P3K should reuse lower-level camera primitives, not the component.

Add a typed runtime adapter, provisionally:

```text
PreparedPlayerCameraBindingAdapter
```

It creates a request from:

```text
CameraOutputSession
PlayerSlotId
ActorId
RuntimeContentOwner
PlayerActorPreparationToken
explicit target references
explicit rig reference
explicit precedence/tie-break policy
```

Request and lifetime identities must derive deterministically from the current preparation/owner evidence, not from arbitrary serialized strings.

### 4.7 Camera remains optional unless policy requires it

`GameplayReady` must distinguish required and optional bindings.

Default baseline:

```text
occupancy: required
control binding: required for gameplay-capable Activity
PlayerInput bridge: required for local gameplay
gameplay action-map activation: required for local gameplay
camera: optional unless the Actor/Activity policy marks it required
movement implementation: never a framework readiness requirement
```

A shared-camera Activity can become GameplayReady without a per-Player camera request.

---

## 5. Closed decision gate DG-P3-07

### Question

Which existing input and camera bindings can be reconfigured after provisioning without requiring a scene-authored `PlayerComposer`?

### Decision

```text
Reuse directly:
  stable LocalPlayerHostAuthoring PlayerInput ownership
  PlayerActorDeclaration typed Actor / PlayerInput evidence
  UnityPlayerInputGateAdapter
  CameraOutputSessionBinding
  CameraRequest / CameraOutputContext / publisher primitives
  P3J preparation token and materialization snapshot
  P3K.2 live occupancy authority

Add for the contextual runtime path:
  PlayerGameplayInputBindingRuntimeContext

Do not use in the P3 runtime path:
  PlayerComposer execution
  PlayerSlotOccupancy as mutable occupancy
  legacy F52 PlayerBinding target chain
  LocalPlayerCameraRequestBinding
  serialized Slot/Actor strings as authority
```

---

## 6. Recommended runtime authority

Add one sibling module on the existing `FrameworkRuntimeHost`:

```text
PlayerGameplayAdmissionRuntimeHostModule
```

Responsibilities:

```text
validate a current P3J preparation
coordinate effective occupancy
coordinate live occupancy-backed gameplay-input bind/release
coordinate stable-host PlayerInput action-map activation and Gate availability
coordinate optional camera request publication/release
publish immutable admission/readiness snapshots
retain rollback diagnostics
```

It is not:

```text
a global Player manager
a spawner
an Actor selection authority
a camera winner
a movement controller
an Activity transition gate
```

The Activity lifecycle authority explicitly invokes it.

P3L later decides whether an Activity may become Active. P3K only produces truthful readiness evidence and performs explicit bind/release transactions.

---

## 7. Canonical identity guard

Every P3K operation must match the complete current tuple:

```text
SessionContextId
RuntimeContentOwner
PlayerSlotId
ActorProfileId
ActorId
PlayerActorPreparationToken
RuntimeContentIdentity
MaterializationRevision
LocalPlayerHost
PlayerInput
```

Reject when any element is:

```text
invalid
foreign
stale
released
owned by another scope
bound to a different Slot
bound to a different Actor
bound to a different PlayerInput
```

Names, tags, hierarchy paths, Unity `playerIndex`, device IDs and display metadata are diagnostic only.

---

## 8. Admission state model

Recommended state:

```text
None
Admitting
Admitted
Releasing
Released
Failed
ReleaseFailed
```

Recommended immutable snapshot:

```text
PlayerGameplayAdmissionSnapshot
  SessionContextId
  PlayerSlotId
  ActorId
  ActorProfileId
  RuntimeContentOwner
  RuntimeContentIdentity
  PreparationToken
  AdmissionRevision
  OccupancyState
  ControlBindingState
  InputBridgeState
  InputActivationState
  CameraBindingState
  GameplayReady
  LastStatus
  LastDiagnostic
```

`GameplayReady` is derived, never directly authored or manually toggled.

---

## 9. Transaction order

### Admit

```text
1. Validate P3J preparation token and Active materialization.
2. Resolve the registered stable Local Player Host.
3. Validate exact Slot, Actor and PlayerInput coherence.
4. Resolve explicit logical Actor gameplay endpoints.
5. Confirm effective runtime Slot -> Actor occupancy.
6. Bind the exact generated Actor identity to the stable-host PlayerInput.
7. Activate the configured gameplay action map.
8. Apply current Gate availability without replacing binding identity.
9. Publish optional/required camera request.
10. Publish GameplayReady snapshot.
```

### Rollback after a failed admit

```text
camera release if published
gameplay-input binding release and previous-map restore
occupancy release
retain original failure plus rollback evidence
```

### Release

```text
1. Validate current admission and preparation token.
2. Remove GameplayReady.
3. Release camera request.
4. Clear Gate-owned temporary state required by control release.
5. Clear gameplay action-map activation and restore previous map.
6. Clear PlayerInput bridge.
7. Clear PlayerControl binding.
8. Release effective occupancy.
9. Mark admission Released.
10. Permit P3J Logical Actor release.
```

A stale release request must not disturb the current restarted Actor.

---

## 10. Required package changes by cut

### P3K.2 — Effective Runtime Occupancy

Create:

```text
PlayerGameplayOccupancyState
PlayerGameplayOccupancyToken
PlayerGameplayOccupancySnapshot
PlayerGameplayOccupancyResult
PlayerGameplayOccupancyRuntimeContext
```

Acceptance:

```text
only a current Active P3J preparation can be occupied
one Slot has at most one effective occupancy
one preparation has at most one occupancy
same request is idempotent
foreign/stale owner, Actor and token are rejected
release is token-guarded
PlayerSlotOccupancy remains passive and unchanged
```

### P3K.3 — Typed Control and Input Binding

Create or extend:

```text
PlayerGameplayBindingAuthoring
typed runtime configuration for bridge/activation/Gate targets
PlayerGameplayControlBindingAdapter
PlayerGameplayAdmissionRuntimeHostModule foundation
```

Acceptance:

```text
no PlayerComposer runtime execution
exact host PlayerInput is used
exact generated ActorId is used
configured gameplay map activates
Gate blocks availability without releasing identity
rollback clears partial bindings
Activity restart rebinds the new Actor
```

### P3K.4 — Prepared Player Camera Eligibility

Create:

```text
PlayerGameplayCameraAuthoring
PlayerGameplayCameraEligibilityRuntimeContext
PlayerGameplayCameraEligibilityToken / summary / snapshot / result
```

Acceptance:

```text
no dependency on PlayerComposer
eligibility requires current occupancy and current gameplay input binding
request, lifetime and tie-break identities are owner/preparation-derived
optional camera may explicitly skip
required invalid camera remains rejected
shared-camera Activity may omit a per-Player camera request
release is idempotent and stale-token guarded
no CameraRequest is published in this cut
```

### P3K.5 — Camera Publication, GameplayReady Aggregation and Reverse Release

Create:

```text
PlayerGameplayAdmissionRequest/result
PlayerGameplayAdmissionSnapshot
PlayerGameplayReadinessSnapshot
full admission transaction and rollback
```

Acceptance:

```text
GameplayReady requires all policy-required evidence
partial admission never reports Ready
release order is deterministic
release failure is retained diagnostically
P3J release cannot run before P3K release is attempted
```

### P3K.6 — Activity-Owned End-to-End QA

QA must prove:

```text
real local join
Activity prepares Actor
occupancy confirms exact Slot -> Actor
control binds exact generated Actor
gameplay map activates
Gate blocks/restores availability
camera optional and required cases
GameplayReady evidence
restart releases old bindings before old Actor
new Actor receives new binding/readiness identity
stale admission/release token rejected
clear leaves no occupancy/control/input/camera leaks
stable host and PlayerInput survive
```

This smoke still does not implement the P3L activation gate.

---

## 11. Documentation corrections

Update the P3 master plan:

```text
P3C: Completed
P3D: Completed
P3E: completed through the P3F runtime decision
P3F: Completed
P3G: Completed
P3H: Completed
P3I: Completed
P3J: Completed — P3J.6 QA PASS 20/20
P3K: Active — P3K.1 audit completed
```

The P3J section should record that Activity-owned preparation, restart identity replacement, stale-token rejection and negative readiness are proven.

---

## 12. Final acceptance of P3K.1

```text
DG-P3-07 is closed.

PlayerSlotOccupancy is not the runtime occupancy authority.

PlayerGameplayInputBindingRuntimeContext is the narrow contextual gameplay-input bind/release authority.

PlayerComposer remains authoring-only and is not required at runtime.

The P3 logical Actor prefab receives a narrow explicit gameplay binding endpoint.

The stable Local Player Host remains the sole PlayerInput owner.

LocalPlayerCameraRequestBinding is not used for P3 materialized Players.

CameraOutputSession and lower-level camera request primitives are reused.

GameplayReady is a derived transactional snapshot guarded by the current
P3J preparation identity.

P3K.2 and P3K.3 implement occupancy and typed gameplay-input binding; P3K.4 is the next implementation cut.
```
