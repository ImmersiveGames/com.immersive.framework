# IF-ADR-004 — Camera Requests and Output Authority

Status: **Accepted**  
Last updated: 2026-08-10  
Package implementation: **Substantially implemented for the accepted single-output boundary**  
Technical QA: **Partial — IF-ADR-004B negative integrity certification pending**  
FIRSTGAME integration: **Partial — broader real-consumer proof pending**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-008, IF-ADR-010, IF-ADR-014

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.
>
> The 2026-08-10 normative reconciliation is recorded by IF-ADR-004A. It aligns
> this ADR with the already-existing package architecture; it does not claim that
> IF-ADR-004B negative integrity QA or broader FIRSTGAME proof has been completed.

## Context

Camera presentation requires one explicit physical output authority while
allowing Session, Route, Activity and eligible Local Player scopes to request
presentation without directly mutating shared output or discovering authority
through scene hierarchy, global registries or timing.

The accepted implementation has matured beyond the earlier shorthand of
"request -> camera output". The framework now separates:

```text
product authoring
  -> typed request publication
  -> logical arbitration
  -> transactional output synchronization
  -> physical Unity/Cinemachine projection
```

Those boundaries are normative because they prevent logical winner state from
silently diverging from the physical Camera presentation.

The current accepted product supports one persistent Camera output per Session.
Multi-output, split-screen and concurrent per-player outputs require a separate
accepted architectural extension.

## Decision

The canonical Camera authority chain is:

```text
Camera request source
  Session / Route / Activity / eligible Local Player
        ↓
typed CameraRequest + explicit ownership/lifetime evidence
        ↓
ScopedCameraRequestPublisher
        ↓
CameraOutputSession
        ↓
CameraOutputContext
  logical admission + deterministic winner selection
        ↓
CameraOutputRigApplicator
  physical projection
        ↓
CameraOutputSessionBinding
        ↓
explicit Unity Camera + CinemachineBrain
```

The responsibilities are intentionally separate:

- `CameraOutputContext` owns logical request admission, release, deterministic
  arbitration, winner state and restoration of the next valid request;
- `CameraOutputRigApplicator` owns projection of the logical winner to the
  concrete Camera/Cinemachine output;
- `CameraOutputSession` owns transactional synchronization between those two
  states and is the mutation boundary used by publishers;
- `CameraOutputSessionBinding` owns one scene-authored scoped output session and
  the explicit physical Unity Camera/CinemachineBrain references;
- request publishers translate one already-owned scope into explicit
  publish/release operations and do not discover Camera authority;
- `CameraRigComposer` owns local rig intent and local Cinemachine materialization,
  not the persistent physical output.

Normal consumers should use the product-facing authoring and integration
surfaces. `CameraOutputContext`, `CameraOutputSession`, the applicator and
publisher implementation types are technical runtime machinery rather than the
normal game-facing configuration path.

## 1. Physical Output Authority

For the current single-output boundary:

- exactly one persistent `CameraOutputSessionBinding` is authored for the
  application Session composition;
- the binding references one explicit Unity `Camera` and one explicit
  `CinemachineBrain`;
- the Unity Camera and CinemachineBrain belong to the same physical output
  GameObject;
- the output has one explicit `CameraOutputId`;
- one scoped `CameraOutputContext` arbitrates requests for that output;
- consumers receive the output/session through explicit typed composition or
  injection;
- no static/global Camera registry is authoritative;
- no singleton or service locator is authoritative;
- no `Camera.main` lookup is authoritative;
- no hierarchy/name/tag search is authoritative for Camera output ownership.

Persistent Content composition validation is the correct boundary for proving
that the accepted application composition contains exactly one physical Camera
output. The individual binding is responsible for validating its own references;
it is not responsible for globally discovering competing outputs.

## 2. Camera Rig Authoring Boundary

`CameraRigComposer` is the official designer-facing surface for one concrete
local Camera rig.

It owns authored intent such as:

- current presentation intent;
- target source mode;
- Follow and Look At target requirements;
- explicit targets or a typed `ICameraTargetSource`;
- local framing values;
- local Cinemachine technical materialization.

Its Editor surface may validate and idempotently Apply/Rebuild the local rig.
Apply/Rebuild may create or repair the local `CinemachineCamera` and required
local Cinemachine technical state.

It must not create or claim:

- the persistent Unity Camera;
- the persistent CinemachineBrain;
- the persistent `CameraOutputSessionBinding`;
- a global Camera authority;
- AudioListener ownership.

The current official presentation capability is **Follow**. The existence of a
Composer does not imply support for arbitrary Cinemachine presentation models.
Orbit, rail, fixed cinematic modes or other presentation models are future
product extensions unless separately implemented and accepted.

Reusable CameraRigComposer values may use Unity Presets where appropriate. A
separate Recipe/Profile layer is not required by this ADR.

## 3. Typed Request Contract

A Camera request must carry enough explicit evidence to be validated and
arbitrated without hidden discovery. The accepted contract includes:

- valid request identity;
- matching output identity;
- owner kind and owner scope evidence;
- lifetime kind and lifetime scope evidence;
- valid rig reference;
- explicit target-source evidence;
- explicit arbitration policy;
- explicit release semantics;
- diagnostic source/description evidence where applicable.

Invalid requests block explicitly. The output authority must not silently repair
or guess mandatory runtime request identity, ownership, target or output data.

## 4. Deterministic Arbitration

Arbitration is deterministic and does not use publication timing as an implicit
policy.

The accepted model is:

```text
higher precedence
  -> wins

equal precedence
  -> both requests must carry deterministic tie-break evidence
  -> deterministic tie-break ordering selects the winner
```

For equal-precedence requests:

- missing deterministic tie-break evidence blocks the conflicting admission;
- colliding deterministic tie-break evidence blocks the conflicting admission;
- distinct deterministic tie-break evidence allows deterministic ordering.

A duplicate admitted `CameraRequestId` also blocks explicitly.

The older interpretation "equal priority -> newest request wins" is not part of
the accepted contract. Runtime timing, dictionary enumeration or callback order
must not become hidden Camera policy.

The currently demonstrated product-level precedence convention is:

```text
Local Player  50
Activity     100
Route        200
Session      300
```

These values describe the current supported producer convention. The normative
rule is explicit deterministic precedence and tie-break evidence, not magical
knowledge of those four numbers inside the output context.

## 5. Transactional Logical / Physical Integrity

Logical Camera state must not be reported as successfully changed when physical
output application failed.

For admission:

```text
CameraOutputContext.Admit(request)
        ↓
CameraOutputRigApplicator.Apply(context)
        ↓
physical apply succeeds
  -> admission succeeds

physical apply fails
  -> remove the admitted request
  -> re-apply the previous logical state
  -> report RolledBack when restoration succeeds
  -> report RollbackFailed when consistency cannot be restored
```

Release follows the same principle:

```text
CameraOutputContext.Release(request)
        ↓
apply replacement / cleared winner
        ↓
physical apply succeeds
  -> release succeeds

physical apply fails
  -> re-admit the released request
  -> re-apply the previous state
  -> report RolledBack or RollbackFailed explicitly
```

Therefore:

- logical request admission is not equivalent to confirmed physical presentation;
- a failed physical apply cannot silently commit a new winner;
- a failed replacement after release cannot silently discard the previous winner;
- rollback failure is terminal diagnostic evidence and must never be hidden as a
  normal success.

## 6. Scope Ownership and Lifecycle

Camera request ownership must remain aligned with the scope that published the
request.

Current producer boundaries include:

- Session-owned override;
- Route-owned override;
- Activity-owned override;
- eligible Local Player-owned request.

Route and Activity bindings use the exact authored `RouteAsset` or
`ActivityAsset` reference as lifecycle owner identity. Stable/textual identities
remain technical evidence and must not silently replace authored-definition
reference authority. This follows IF-ADR-014.

Normal lifecycle cleanup is explicit:

- Activity exit releases the Activity-owned request;
- Route exit releases the Route-owned request;
- output detachment ends the attached owner scope and releases its active request;
- explicit release is idempotent at the publisher/binding surface.

### Abnormal owner-loss boundary

The package currently proves normal lifecycle-driven release, but the 2026-08-10
audit did **not** certify every abnormal Unity lifetime path such as an owner
component/GameObject being disabled or destroyed before its expected lifecycle
exit/detach callback.

This is an unproven hardening boundary, not a confirmed package defect.

IF-ADR-004B must determine whether:

1. a higher-level lifecycle invariant guarantees release/detach before such owner
   loss and QA can prove that invariant; or
2. an admitted orphan request can survive its valid owner lifetime.

Only the second result justifies opening the conditional package cut
`IF-ADR-004C — Camera Owner Lifetime Integrity`.

No global Camera cleanup manager, static liveness registry or service locator is
authorized as a speculative solution.

## 7. Target Resolution

Camera target resolution is explicit and typed.

Accepted sources may include:

- explicit Transform references;
- typed framework target-source bindings;
- typed Local Player Camera target sources;
- future dedicated providers accepted by a later product cut.

Required target failures block explicitly.

Runtime target resolution must not rely on:

- GameObject name search;
- tags as ownership authority;
- hierarchy guessing;
- `Camera.main`;
- global singleton/service-locator lookups.

## 8. Runtime / Editor Boundary

Editor tooling may:

- create or repair local Cinemachine rig structure owned by
  `CameraRigComposer`;
- validate Camera authoring;
- expose explicit Apply/Rebuild;
- record materialization evidence;
- validate Persistent Content Camera composition.

Editor tooling must not become runtime Camera authority.

Runtime code must not depend on Editor assemblies or use Editor-only state to
resolve active Camera ownership.

Authoring components must not execute gameplay merely because they exist in a
prefab or scene. Runtime override behavior remains in explicit lifecycle/runtime
bindings and publishers.

## 9. Product Surface and Diagnostics

The supported product-facing surface includes:

- `CameraRigComposer` for local rig intent and materialization;
- `CameraOutputSessionBinding` for persistent physical output authoring;
- Session, Route and Activity Camera override bindings;
- typed Local Player Camera publication through Player integration;
- feature validation through the appropriate Editor/composition surface;
- Advanced / Diagnostics for technical evidence.

Designer-facing configuration should lead with intent and operational state.
Technical evidence may expose, as applicable:

- output identity;
- request identity;
- owner/lifetime evidence;
- precedence and deterministic tie-break identity;
- admitted request count/identities;
- current logical winner;
- requested and resulting output state;
- physical apply result;
- rollback attempt/result;
- explicit blocking issue codes/messages;
- last operation status.

Diagnostics must make invalid mandatory state actionable. Failures must not be
relabelled as a normal fallback.

## 10. Relationship to Other ADRs

### IF-ADR-001 — Core Lifecycle and Runtime Authority

Defines the scoped-authority model. Camera uses explicit scoped runtime objects
rather than a global manager or discoverable current Camera authority.

### IF-ADR-002 — Product Authoring Model

`CameraRigComposer` is a justified materializing Composer because authored rig
intent deterministically produces local Cinemachine technical state. This does
not create a generic requirement for Recipes/Composers in other systems.

### IF-ADR-003 — Player Participation and Actor Lifecycle

Defines the eligibility/ownership context from which Local Player Camera
publication may occur. Camera does not discover Players on its own.

### IF-ADR-005 — Input, Pause, Gate and Reset

Contextual interaction may affect when Camera requests are useful or available,
but ADR-005 does not become Camera output authority.

### IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

Defines lifecycle/diagnostic expectations around transitions and persistent
presentation. Camera must preserve explicit failure and restoration evidence
through those flows.

### IF-ADR-008 — Persistent Application Content Composition

Persistent Content owns the application-persistent physical Camera composition.
The local rig Composer does not replace that ownership boundary.

### IF-ADR-010 — Editor and Inspector Product Surface Authority

Defines designer-first authoring, explicit validation/materialization where
justified and Advanced/Diagnostics separation. CameraRigComposer is a Class C
materialized-composition example.

### IF-ADR-014 — Authored Definition and Stable Identity Authority

Route/Activity Camera ownership follows exact authored definition references.
Stable IDs remain persistence/diagnostic evidence rather than definition
identity authority.

## 11. Non-Goals

This ADR does not authorize:

- a global `CameraManager`;
- a Camera service locator;
- static request registries;
- `Camera.main` as runtime authority;
- scene/name/tag discovery as output ownership authority;
- hidden timing-based request priority;
- multiple simultaneous physical outputs;
- split-screen;
- concurrent per-player output ownership;
- a generic Camera request broker outside the scoped output model;
- a new Recipe/Profile layer solely for symmetry with other features;
- a second Composer around the same local rig intent;
- automatic creation of the persistent physical output from a local rig;
- Camera ownership of AudioListener behavior;
- broad automatic Camera gameplay authored by the framework;
- speculative lifetime managers before IF-ADR-004B demonstrates a concrete gap.

## 12. Validation Requirements

Technical validation for the accepted boundary must cover both positive behavior
and negative integrity.

### Existing positive/current evidence

The current QA surface already exercises the main authority ladder and normal
release/restoration behavior, including:

- Local Player, Activity, Route and Session precedence flow;
- explicit request/release;
- restoration of the next valid request;
- repeated request/release behavior;
- Activity/Route lifecycle cleanup;
- persistent output authoring/composition validation.

### Required IF-ADR-004B negative certification

The next technical gate must explicitly certify:

1. higher precedence wins;
2. equal precedence with distinct deterministic tie-breakers is deterministic;
3. equal precedence with missing tie-break evidence blocks;
4. equal precedence with colliding tie-break evidence blocks;
5. duplicate RequestId blocks;
6. wrong OutputId blocks;
7. repeated Publish preserves state without duplicate mutation;
8. repeated Release preserves released state without mutation;
9. releasing the current winner restores the next valid request;
10. out-of-order release preserves the correct winner;
11. physical apply failure during admission rolls logical admission back;
12. physical apply failure during release restores the previous request/state;
13. rollback failure produces explicit `RollbackFailed` evidence;
14. Activity exit cleans only its owned request;
15. Route exit cleans only its owned request;
16. abnormal owner disable/destruction proves the accepted lifetime invariant or
    exposes an orphan request;
17. duplicate Persistent Camera Output authoring blocks in composition
    validation;
18. missing/invalid output binding references fail explicitly.

Package changes are not a prerequisite for this QA cut. QA should first test the
current official package and identify whether a package defect actually exists.

## 13. Current Certification State

As of 2026-08-10:

```text
Architecture
  ACCEPTED
  IF-ADR-004A normative reconciliation recorded

Package
  SUBSTANTIALLY IMPLEMENTED
  current single-output architecture is coherent
  no broad Camera redesign identified

Product Surface
  STRONG / CONFORMANT
  CameraRigComposer + output binding + scoped override surfaces are established

Technical QA
  PARTIAL
  positive/current regressions exist
  IF-ADR-004B negative integrity certification is pending

FIRSTGAME
  PARTIAL
  broader real-consumer Camera proof is still required

Confirmed package defect requiring IF-ADR-004C
  NO — not currently proven
```

The planning score remains tracked separately in IF-TRACK and must not be
changed merely because the normative documentation was reconciled.

## 14. Follow-on Cuts

### IF-ADR-004A — Camera Authority Normative Reconciliation

Status: **Closed by this documentation revision**.

Purpose: align the normative ADR with the already-existing scoped output,
deterministic arbitration, transactional application and product-authoring
architecture.

No runtime, Editor, QA or FIRSTGAME code change is implied by closing 004A.

### IF-ADR-004B — Camera Negative Integrity Certification

Status: **Next technical gate**.

Purpose: attempt to break the current implementation across arbitration,
transactional rollback, invalid authoring and owner-lifetime boundaries before
changing package architecture.

### IF-ADR-004C — Camera Owner Lifetime Integrity

Status: **Conditional / not opened**.

Open only if IF-ADR-004B proves that an admitted request can outlive its valid
owner because the accepted lifecycle cannot guarantee cleanup.

If 004B instead proves a sufficient higher-level lifecycle invariant, document
that invariant and do not create 004C.

## 15. Exit Criteria

The current ADR-004 boundary is functionally complete when:

- the package continues to expose the accepted authoring/runtime surface;
- technical QA certifies the positive and negative request/arbitration/output
  integrity matrix;
- persistent output authoring validation remains enforced;
- mandatory failure and rollback evidence remain explicit and diagnostic;
- no hidden Camera authority/discovery path is introduced;
- FIRSTGAME demonstrates the same official package surfaces in real gameplay,
  including request replacement/restoration, without local duplicate Camera
  authority.

## 16. Reopen / Extension Triggers

Reopen or extend this ADR if the framework needs:

- multi-output;
- split-screen;
- concurrent local-player Camera outputs;
- a different arbitration model;
- a different physical output ownership model;
- a new runtime target-discovery authority;
- a package lifetime fix proven necessary by IF-ADR-004B.
