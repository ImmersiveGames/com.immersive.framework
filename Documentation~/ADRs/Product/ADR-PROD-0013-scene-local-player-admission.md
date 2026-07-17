# ADR-PROD-0013 — Scene Local Player Admission

Status: Accepted  
Date: 2026-07-16  
Package: `com.immersive.framework`  
Area: Player Product Surface / Player Participation / Existing Scene Host Admission  
Extends: `P3-ADR-Canonical-Player-Lane`, `ADR-PROD-0007`, `ADR-PROD-0008`, `ADR-PROD-0010`, `ADR-PROD-0011`, `ADR-PROD-0012`

## Context

The canonical P3 Player lane currently proves local Player provisioning through an explicit manual join:

```text
PlayerSlotProfile
-> Slot reservation
-> PlayerInputManager.JoinPlayer
-> Local Player Host admission
-> Actor selection and preparation
-> occupancy, input, camera and Activity admission
```

Some products instead author one local Player directly in an Activity scene. In that construction, the physical `GameObject`, `PlayerInput` and Logical Actor already exist before runtime admission.

Forcing that object through `PlayerInputManager.JoinPlayer` would be false provisioning and could create a duplicate Player. Reusing `PreAuthoredPlayerComposer` would also be incorrect: that surface authors fixed runtime identity, is editor-first, and is not the P3 participation authority.

The framework therefore needs one official admission path for a scene-existing Player without introducing a second local Player spawner or a parallel participation architecture.

## Decision

Introduce the product surface:

```text
Scene Local Player Admission
```

It admits one explicitly referenced scene-existing local Player Host into the existing P3 Player Participation domain.

The canonical local Player model has two physical sources:

```text
Provisioned Local Player
  PlayerInputManager creates the Local Player Host after an authorized manual join.

Scene Local Player
  the Local Player Host already exists in the Activity scene and is externally owned.
```

Both sources share:

```text
PlayerSlotProfile and PlayerSlotId semantics
Slot reservation and Joined state
host admission contracts
ActorProfile selection evidence
Actor preparation/readiness stages
occupancy, input and camera eligibility
Activity admission and release diagnostics
```

They do not share physical creation or destruction.

## Authority boundary

### Player Participation runtime context

Owns:

```text
Slot allocation state
reservation and admission transactions
admitted host registry
release tokens
readiness and preparation handoff
explicit rollback and diagnostics
```

It remains domain-scoped, typed and explicitly injected. No singleton, service locator or scene-wide discovery is introduced.

### PlayerInputManager provisioning adapter

Owns only the runtime-created path:

```text
PlayerInput and InputUser provisioning
device pairing
control-scheme assignment
Unity playerIndex
technical local-player limits
```

### Scene Local Player Admission adapter

Owns no physical creation. It:

```text
receives explicit serialized references
reserves one explicitly configured PlayerSlotProfile
stages and commits host admission
registers the existing Logical Actor for preparation
releases contextual participation without destroying the physical objects
```

## Product surface

The initial designer-facing authoring component is named:

```text
Scene Local Player Admission
```

Avoid `PreAuthored`, `Adopt` and `ExistingExternallyOwned` in the default Inspector. Those terms describe implementation mechanics and may appear only in Advanced/Debug evidence.

Minimum authoring:

```text
Player Slot Profile
Local Player Host
Actor Profile
Scene Logical Player Actor
Admission Timing
  On Activity Enter
  Manual
```

Advanced/Debug exposes read-only evidence:

```text
PlayerSlotId
Host admission state
ActorProfile and runtime Actor identity
physical ownership
reservation/admission token
preparation/readiness state
release state and last diagnostic
```

## Apply / Validate boundary

An Editor Apply/Validate operation may:

```text
validate explicit references
validate exactly one PlayerInput on the Host
validate Actor Mount ownership
validate the scene Logical Actor is under the exact Actor Mount
validate ActorProfile compatibility
materialize explicit serialized authoring evidence
```

It must not:

```text
assign runtime PlayerSlotId
assign functional runtime ActorId
reserve a Slot
start admission
activate input, camera or gameplay
use Awake, OnEnable or another Unity callback as admission authority
```

Runtime validation must not depend on `AssetDatabase`, `PrefabUtility`, prefab paths, object names, tags or hierarchy search. Any prefab/profile compatibility proof needed at runtime must be materialized as explicit typed serialized evidence by Editor authoring.

## Initial scope

The first supported shape is deliberately narrow:

```text
scope: Activity
one scene-existing local Player Host per configured Slot
Host ownership: ExistingExternallyOwned
Logical Actor ownership: ExistingExternallyOwned
Slot selection: explicit PlayerSlotProfile
Actor selection: explicit ActorProfile
```

The Activity pipeline requests admission on enter when configured for `On Activity Enter`, or an authorized typed caller requests it when configured as `Manual`.

## Admission transaction

Canonical flow:

```text
1. Validate authoring and runtime dependencies.
2. Resolve the explicitly configured PlayerSlotProfile.
3. Reject unavailable, Reserved or Joined Slot state.
4. Reserve the Slot with a typed pending operation.
5. Stage admission on the explicit Local Player Host.
6. Commit Slot admission.
7. Commit Host admission.
8. Register the existing Logical Actor for preparation.
9. Continue through the canonical P3 readiness stages.
10. Publish input and camera eligibility only after the required readiness level.
```

A failure before completion compensates the transaction and returns the Slot to its previous valid state. It never destroys or permanently disables the scene-owned Host or Actor.

## Release transaction

The required release order is:

```text
1. pending Actor candidate, when present
2. contextual gameplay: camera, input, occupancy and admission
3. Logical Actor preparation/materialization evidence
4. Local Player Host admission and Slot
5. Activity binding
```

The Host and Slot remain admitted while dependent gameplay or Actor preparation still exists.

For `ExistingExternallyOwned` objects, release removes framework-owned contextual evidence only. It does not destroy the Host or Logical Actor.

### Release failure compensation

If release temporarily changes an external object's active state and a later logical release step fails:

```text
restore the original active state before returning failure
preserve the logical handle when it was not released
preserve Host and Slot admission
return explicit primary and compensation diagnostics
allow deterministic retry
```

Failures before and after logical handle release are distinct states and must not be collapsed into one ambiguous boolean.

Minimum release states:

```text
Admitted
ReleaseStaged
ReleaseFailed
Released
```

A `ReleaseFailed` Activity exit blocks scene unload. `OnDestroy` may perform best-effort cleanup during shutdown, but it is not the normal release policy and must log failures.

## Camera and input boundary

Scene admission does not publish camera or gameplay input directly.

```text
admission
-> Actor preparation
-> Activity requirement evaluation
-> gameplay readiness
-> input/camera eligibility
```

Camera targets come from a typed target source independent of `PreAuthoredPlayerComposer`, such as a prepared Actor target provider or explicit Transform source.

## Guardrails

```text
Do not call PlayerInputManager.JoinPlayer for a scene-existing Host.
Do not create a second local Player spawner.
Do not reuse PreAuthoredPlayerComposer as admission authority.
Do not author runtime ActorId or PlayerSlotId on the scene Host.
Do not allocate a fallback Slot.
Do not mutate Profile assets at runtime.
Do not discover Players by name, tag, hierarchy search or singleton.
Do not destroy or silently deactivate externally owned objects on failure or release.
Do not enable camera or gameplay input before readiness.
Do not depend on OnDestroy for normal Activity release.
Do not add compatibility aliases or silent migration bridges for PreAuthored.
```

## Out of scope

```text
Route-persistent scene-owned Player Hosts
Session-persistent GameObject references
mixed physical ownership combinations
framework-instantiated Actor under a scene-owned Host
network or remote Player admission
leave, disconnect and reconnect state machines
automatic local join
character-selection workflow
split-screen output assignment
forced scene unload after ReleaseFailed
```

## Implementation sequence

```text
P3M0 — Freeze the read-only source baseline and retain H5 as a manual Unity gate.
P3M1 — Accept this ADR and reconcile current Player/Camera documentation.
P3M2 — Decouple Camera, shared Editors and QA from PreAuthoredPlayerComposer.
P3M3 — Remove the PreAuthored Player surface destructively.
P3M4 — Promote Scene Local Player Admission atomically into the package.
P3M5 — Prove admission, rollback, release and manual-join regression in QAFramework.
P3M6 — Prove real usability in FIRSTGAME.
P3M7 — Add the official sample and concise usage documentation.
```

## Technical acceptance criteria

```text
The package and QA compile after every implementation cut.
Manual join remains functional and continues using PlayerInputManager.
Scene admission never calls PlayerInputManager.JoinPlayer.
The scene Host and Actor are never created or destroyed by the admission runtime.
Slot reservation, stage, commit, rollback and release are typed and diagnostic.
Stale and foreign tokens are rejected explicitly.
Release is idempotent and retryable.
External active state is restored on release failure.
Activity exit blocks scene unload while release remains failed.
Runtime has no Editor dependency, global lookup or silent fallback.
Camera and input become eligible only through P3 readiness.
```

## Product acceptance criteria

```text
A designer can place one Player in an Activity scene.
A designer can configure Slot, Host, ActorProfile and Logical Actor explicitly.
The default Inspector communicates product intent rather than ownership mechanics.
Apply/Validate explains invalid authoring before Play Mode.
On Activity Enter and Manual timing are explicit.
Advanced/Debug explains admission, readiness, ownership, release and failures.
FIRSTGAME proves the same official package surface without local facades.
```

## Consequences

### Positive

```text
Single-player scene authoring no longer requires a fake join.
Manual join and scene-existing Player use one participation model.
PlayerInputManager remains the sole runtime provisioner for local Players it creates.
Physical ownership and contextual admission are separated.
The PreAuthored experimental surface can be removed without losing the product use case.
```

### Cost

```text
The admission runtime needs explicit transactional release and compensation.
LocalPlayerHostAuthoring needs a typed scene-admission validation path.
Camera and QA must be decoupled before PreAuthored removal.
The initial ownership matrix is intentionally narrow.
```

## Suggested commit message

```text
Docs: define Scene Local Player Admission
```

## Implementation checkpoint — P3M4A

The product surface and serialized evidence boundary are implemented first:

```text
SceneLocalPlayerAdmissionAuthoring
SceneLogicalPlayerActorEvidence
LocalPlayerHostAuthoring scene-admission validation/release contract
Designer-first Inspector
Apply / Rebuild and Validate
Create menu
QA authoring smoke
```

This checkpoint deliberately does not start runtime admission. Runtime reservation, Activity enter/exit integration and canonical readiness handoff remain the next P3M4B checkpoint and must consume this exact authoring contract.

## P3M4B1 implementation checkpoint

P3M4B1 promotes the Host/Slot transaction only.

Frozen checkpoint decisions:

```text
Scene admission is explicitly authorized and may execute while public JoiningOpen is false.
Configured Slot order still applies.
The requested Slot must equal the current first Available configured Slot.
No fallback Slot is selected.
Release uses Joined -> Leaving -> Available.
Host and Actor physical ownership remain external.
Actor preparation and gameplay remain outside this checkpoint.
Scene admission composes directly from FrameworkRuntimeHost + Session participation.
The provisioned PlayerInputManager path is not a dependency.
```

The temporary manual API is a technical product gate, not the final Activity UX.
`OnActivityEnter` is still serialized intent and must not execute automatically until the
P3M4B2 Activity participant and reverse-order release have passed QA.
