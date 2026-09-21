# IF-ADR-029 — Camera Composition, Group Presentation and Camera View Removal

Status: **Accepted architecture — CAMERA-029-A through F committed**
Proposed: **2026-09-18**
Accepted: **2026-09-18**
Type: architecture / Camera composition / presentation / output participation
Reconciles: IF-ADR-004, IF-ADR-022, IF-ADR-026, IF-ADR-027 and IF-ADR-028
Preserves: IF-ADR-004 request arbitration and output-owned Default semantics; IF-ADR-028 external physical-presentation ownership
Supersedes: Camera View as an independent runtime/authoring authority, View→Output topology as the normal participation path, and SharedFollow as an implicit cardinality-dependent Follow mode

Implementation: **A implemented; B implemented; C implemented; D implemented; E implemented; F implemented; all committed**
Unity tested: **NO**
Technically validated: **NO**
Certified: **NO**

## 1. Context

The Camera architecture currently documents the following conceptual chain:

```text
Camera Subject
    ↓
Assignment
    ↓
Camera View
    ↓
Rig / Presentation
    ↓
Camera Output
```

A focused architecture audit of the productive implementation found that `CameraView`
does not currently own independent state, lifetime or behavior.

The productive Shared Camera path is effectively:

```text
Camera Subject availability
    ↓
CameraSharedComposition
    ↓
CameraView identity / assignment bookkeeping
    ↓
CameraOutputAuthoring.DefaultCameraRig
    ↓
Camera Output
```

while Route, Activity and Session camera behavior already uses the independent request path:

```text
Camera Rig
    ↓
CameraRequest
    ↓
CameraOutputContext
    ↓
CameraOutputSession
    ↓
Camera Output
```

This creates two parallel presentation authorities.

The Shared path also uses the Output Default Rig as the normal Subject-driven gameplay rig.
That conflicts with the established IF-ADR-004 contract in which the Default Rig is the
persistent fallback presentation of an Output when no normal request wins or force-default
is active.

The audit also found that:

- `CameraViewDefinition` contains only stable identity and description;
- runtime `CameraView` contains only identity and description;
- the only productive `CameraViewAssignmentContext` owner is `CameraSharedComposition`;
- View→Output topology validates identity relationships but does not select or apply a View-owned rig;
- `CameraRequest` already carries explicit Output identity, rig, ownership, lifetime and arbitration policy without a View;
- the current Shared multi-Subject behavior is implemented as an implicit special case of Follow rather than a first-class presentation intent.

The architecture therefore contains an abstraction whose independent authority is not
demonstrated by the implementation, while the concrete Composition, Rig, Request and Output
authorities already cover the productive responsibilities.

## 2. Decision

> **Camera View is removed as an independent Framework Camera authority. Camera Composition owns Subject membership and presentation input; Camera Rig owns local presentation behavior; Camera Request owns participation in an Output; Camera Output owns physical Camera capacity and persistent Default fallback.**

The canonical Camera chain becomes:

```text
Camera Subject(s)
        ↓
Camera Composition
        ↓
CameraRigComposer
        ↓
CameraRequest
        ↓
CameraOutputSession
        ↓
Camera Output
```

The physical Output selection rule remains:

```text
force-default active
    -> Default Camera Rig

otherwise normal request winner exists
    -> winning request Rig

otherwise
    -> Default Camera Rig
```

No separate "View participant" arbitration tier is introduced.

## 3. Camera Composition authority

Camera Composition is the owner of the observable set used by one Camera presentation
occurrence.

It owns:

```text
Subject selection policy
current ordered Subject membership
membership ownership/tokens
availability revisions
stale-evidence rejection
composition reconciliation state
presentation input derived from the current Subject set
publication lifetime for its current Camera intent
diagnostics for that composition occurrence
```

Composition does not own:

```text
physical Unity Camera
CinemachineBrain
Output identity
Default Camera Rig
request winner arbitration
request precedence semantics
screen viewport/layout
PlayerInputManager split layout
global Player or Actor discovery
```

The existing `CameraSharedComposition` is therefore a concrete precursor to the accepted
Composition authority, but its View identity dependency and direct mutation of the Output
Default Rig are superseded.

A generic reusable `CameraCompositionDefinition` asset is **not** required by this ADR.
Introduce one only if later consumer evidence demonstrates reusable authored configuration
that cannot remain coherently on the concrete composition boundary.

## 4. Camera View removal

The following concepts no longer represent independent product/runtime authority:

```text
CameraView
CameraViewId
CameraViewDefinition
View-owned assignment identity
View→Output association as a normal presentation path
View participation state
```

Their useful responsibilities must be preserved under their actual owners:

```text
View Subject membership/revisions
    -> Camera Composition

View presentation input
    -> Camera Composition snapshot/input

View target projection
    -> Camera Rig presentation behavior

View→Output identity
    -> explicit CameraRequest.OutputId

one winner per Output
    -> CameraOutputContext
```

Historical View-based certification remains valid only as evidence for the architecture that
existed when it was executed. It must not be relabeled as certification of this ADR.

## 5. Camera Output authority remains explicit

`CameraOutputAuthoring` remains the concrete physical Framework Output authority:

```text
CameraOutputAuthoring
  Camera Output identity
  Unity Camera
  CinemachineBrain
  Default Camera Rig
  CameraOutputSession
  CameraOutputContext
```

`CameraOutputDefinition` remains accepted for this migration as the typed shared authoring
identity used across scene/prefab boundaries and Camera consumers.

Its current behavior is closer to an identity/key asset than a broad behavioral definition,
but renaming or replacing it is outside this ADR because it does not block the corrected
runtime ownership model.

The exact Output asset reference continues to prevent copied raw IDs, hierarchy discovery,
`Camera.main`, implicit first-Output behavior and scene-object coupling across authoring
surfaces.

## 6. Default Camera Rig is fallback only

The Output Default Camera Rig has one semantic responsibility:

> **persistent fallback presentation for its physical Output.**

It is selected when:

- force-default is active; or
- no normal Camera request currently wins.

It must not also be the mutable Subject-driven rig of a normal gameplay Composition.

Therefore:

```text
Output Default Rig != normal Subject-driven Composition Rig
```

A Composition may use Fixed, Follow, Mounted, Third Person, Group or a future accepted
presentation intent, but that rig participates through normal request selection rather than
by mutating the Output Default.

The Default remains outside the normal request precedence ladder and must not be represented
as a synthetic CameraRequest.

## 7. Composition participation uses the existing Camera request path

A presentable Composition participates in a Camera Output through the existing
`CameraRequest` / `CameraOutputSession` path.

The request supplies:

```text
exact CameraOutputId
exact Camera Rig
owner/lifetime evidence
deterministic arbitration policy
```

The Composition owns the publication/release token for the request that represents its
current presentation intent.

The semantic Camera request owner/lifetime remains explicit. This ADR does **not** add a
`CameraRequestOwnerKind.View` or `CameraRequestLifetimeKind.View`.

A new `Composition` owner/lifetime enum value is also not accepted by default. Existing
Session, Route, Activity or explicit-operation semantics should be used when they accurately
represent the consumer lifetime. A new owner kind requires separate concrete evidence that
Composition lifetime is semantically independent from all existing scopes.

CAMERA-029-C established that evidence: the request exists while one Composition is enabled
and its current presentation is presentable, independently of Session, Route, Activity and
explicit-operation lifetimes. The accepted minimum extension is therefore
`CameraRequestOwnerKind.Composition`, `CameraRequestLifetimeKind.Composition` and the logical,
diagnostic-only `CameraTargetSourceKind.Composition`. All three use the Composition's explicit
runtime context identity; none infer scope from Player, hierarchy, scene order or naming.

No scope may be inferred from:

```text
GameObject name
hierarchy
scene order
first matching component
Player index
timing
```

## 8. Composition presentability and request lifetime

A Composition request exists only while its presentation contract is currently satisfiable.

Examples:

```text
Fixed
  may be presentable with zero Subjects when its target contract requires none

Follow
  requires its accepted single Subject/target evidence

Mounted
  requires its accepted single mount/target evidence

Third Person
  requires its accepted single tracking Subject/target evidence

Group
  requires its accepted Subject set cardinality
```

When required Subject evidence becomes unavailable:

```text
Composition loses presentability
    ↓
Composition releases its Camera request
    ↓
CameraOutputSession recomputes normal winner
    ↓
another request wins, or Output Default is restored
```

When valid evidence becomes available again, the Composition may publish a new current
request using current occurrence/revision evidence.

Stale Subject occurrences must never reactivate a Composition.

## 9. Group becomes a first-class presentation intent

The current SharedFollow behavior is superseded by an explicit presentation model:

```text
CameraRigPresentationIntent
  Fixed
  Follow
  Mounted
  ThirdPerson
  Group
```

Product semantics:

```text
Follow
  -> one observable target
  -> single-target follow behavior

Group
  -> 1..N observable Subjects
  -> CinemachineTargetGroup
  -> CinemachineGroupFraming
```

Group owns group-specific presentation configuration such as:

```text
member weight
default member radius
framing size
damping
FOV range
dolly range
orthographic size range
```

These settings no longer belong to Follow merely because a Follow presentation happened to
receive more than one Subject.

The Composition decides **which** Subjects participate.

The Group presentation decides **how** the resolved set is presented.

IF-ADR-030 narrows the member-radius rule: a Camera Subject may publish an optional
presentation-space framing radius centered on its Observation. Group uses that radius for
the corresponding member when present; `GroupCameraRigBehaviorDefinition.memberRadius`
remains the explicit fallback when the Subject does not publish one.

Neither Group nor `CameraRigComposer` discovers Players, Actors or scene objects.

## 10. Camera Rig authority

`CameraRigComposer` remains the canonical local rig materialization authority.

It owns:

```text
one local CinemachineCamera
presentation intent
presentation-specific materialized Cinemachine controls
materialization provenance
local presentation application/clear behavior
```

It does not own:

```text
Subject selection
request winner arbitration
Output lifetime
Player participation
screen layout
```

Removing Camera View must not move Subject-selection policy into the Composer.

## 11. Transactional presentation requirement

Moving Subject-driven Composition into the request path must not introduce a partially
applied state.

The implementation must preserve a transactionally coherent result between:

```text
current Composition Subject snapshot
resolved presentation input
Rig target/member application
Camera request admission/release
physical Output selection
```

Required invariant:

> **A failed request/output application must not leave the selected Rig mutated to new Composition targets while the Output/request state rolls back to older evidence.**

Likewise, failed target/member application must not leave a newly admitted request active.

The implementation may satisfy this through staged validation, reversible presentation
application, an explicit apply transaction or another small mechanism consistent with
IF-ADR-004 rollback guarantees.

This ADR does not prescribe a new global transaction service.

## 12. Multi-output semantics

Multiple explicit Camera Outputs remain supported.

Each Output retains an independent:

```text
CameraOutputId
CameraOutputSession
CameraOutputContext
Default Rig
request arbitration state
```

A Composition participates in an Output through an explicit request targeting that Output.

The former View→multiple-Outputs topology is not preserved merely as an abstract modeling
capability. The audited implementation did not execute that fan-out as physical
presentation.

If one future Composition must drive multiple Outputs, the consumer must provide explicit
per-Output request/rig compatibility. No implicit one-View-to-N-Outputs fan-out is inferred
by Camera core.

## 13. Player and Actor boundary

Ordinary Player gameplay continues to contribute observable Camera Subject evidence only.

```text
Player / Actor occurrence
    ↓
Camera integration boundary
    ↓
Camera Subject availability
```

PlayerParticipation does not become Camera request/output authority.

An ordinary Player does not own a CameraRequest merely because it is joined.

A gameplay/shared Composition may consume Player-backed Subjects and own the request
publication that presents its resolved rig, subject to explicit lifetime/owner semantics.

## 14. Split-screen and physical presentation remain external

IF-ADR-028 physical presentation ownership is preserved.

When Unity `PlayerInputManager` automatic split-screen is used:

```text
Framework Player->Camera integration
  associates the exact Unity Camera with PlayerInput.camera

PlayerInputManager
  owns split count/recomposition
  owns Camera.rect
```

Removing Camera View does not transfer any layout authority to Camera Composition,
CameraRequest, CameraRigComposer or CameraOutputSession.

Framework Camera must not:

```text
write Camera.rect
infer viewport from Player count
create Output identity from Player index
treat Group presentation as split-screen
```

Group means one Camera presenting multiple Subjects. Split-screen means multiple physical
Player Cameras whose layout is owned by `PlayerInputManager`. These are independent
features.

## 15. Request arbitration remains the single normal selection authority

No second normal presentation arbitration system is introduced.

Normal Output selection remains:

```text
CameraOutputContext
  admits/releases normal CameraRequests
  selects one deterministic winner
```

Request owner scope and precedence remain separate concepts.

Existing Route, Activity, Session, Cutscene, Modal, Spectator and Debug request behavior
remain valid.

Composition request publication must coexist with those requests through the same
deterministic Output context rather than bypassing it.

## 16. Rejected alternatives

Rejected:

```text
retain Camera View only to preserve existing type structure
add View Rig as a fourth Output selection tier
keep Shared Composition mutating Output.DefaultCameraRig
make Default Camera Rig a synthetic low-precedence request
make Player own ordinary gameplay Camera requests
make PlayerInputManager Subject/Composition/Rig authority
make Group choose Players or Subjects
keep multi-Subject behavior hidden inside Follow
introduce a global Camera manager/service/registry
infer request scope from scene/hierarchy
introduce Camera.main, FindObjectOfType or name/tag lookup
restore Framework Camera.rect/layout ownership
create a generic mega Camera definition asset to replace View
```

## 17. Superseded decisions

This ADR supersedes the following architecture where it conflicts:

### IF-ADR-026

Superseded:

```text
Camera View as an independent runtime lifetime/authority
Camera View -> 0..N Camera Subjects as the canonical assignment boundary
Camera Subject -> 0..N Camera Views as a required core relation
View→Output topology as the normal participation model
```

Preserved:

```text
Camera Subject occurrence safety
explicit Subject availability
stale token rejection
Player→Camera Subject integration boundary
multi-Output identity/isolation
Player count does not determine Output count
```

### IF-ADR-027

Superseded:

```text
CameraViewDefinition as a required normal authoring definition
CameraViewId as normal Camera composition authority
normal View→Output association authoring
advanced CameraViewOutputPolicyAuthoring as the normal multi-binding topology
```

Preserved:

```text
typed authored references over copied raw stable IDs
CameraOutputDefinition exact-reference authority
Camera Rig Behavior definitions
explicit physical Output authoring
no silent inference
```

### IF-ADR-022

Superseded:

```text
multi-Subject SharedFollow hidden inside Follow
Group Framing configuration owned by Follow behavior
```

Preserved:

```text
CameraRigComposer materialization authority
Fixed / Follow / Mounted / Third Person models
one local CinemachineCamera per Composer
materialization provenance
safe model switching and preflight rules
```

### IF-ADR-004

Preserved as the primary Output/request authority.

The existing Shared path that bypasses request selection and mutates the Output Default is
reconciled by this ADR. Default and force-default semantics remain unchanged.

### IF-ADR-028

Its main decision is preserved:

```text
physical Camera presentation/layout remains external
PlayerInputManager remains split-screen rect owner
Framework Camera owns no generic viewport policy
```

References to View→Output topology become historical/superseded and must be reconciled in a
later documentation cut.

## 18. Implementation cuts

Implementation must proceed in small contract-preserving cuts.

### CAMERA-029-A — First-class Group presentation

Required result:

```text
Group added to CameraRigPresentationIntent
Group behavior definition owns Group settings
Group materialization owns CinemachineTargetGroup / GroupFraming
Follow becomes single-target presentation
no View dependency
```

### CAMERA-029-B — Composition-owned Subject membership

Local implementation status: **implemented; tests authored; static Runtime/test compilation passed; Unity tests pending**

Required result:

```text
Composition owns ordered 0..N Subject membership
Composition owns revision/stale protection
View naming/identity removed from membership runtime
existing occurrence safety preserved
```

### CAMERA-029-C — Composition request participation

Local implementation status: **implemented; tests authored; static Runtime/test compilation passed; Unity tests pending**

Required result:

```text
presentable Composition -> normal CameraRequest
unpresentable Composition -> request released
request uses exact Output identity and exact Rig
Output Default remains independent
no View participation tier
```

### CAMERA-029-D — Transactional presentation reconciliation

Local implementation status: **implemented; tests authored; static Runtime/test compilation passed; Unity tests pending**

Required result:

```text
target/member application and request/output selection cannot diverge
failed apply restores prior coherent state
no stale target/member residue
IF-ADR-004 rollback guarantees preserved
```

### CAMERA-029-E — Camera View removal

Local implementation status: **implemented; tests migrated/authored; static Runtime/Editor/test compilation passed; Unity tests pending**

Required result:

```text
remove obsolete CameraView/ViewId/ViewDefinition surfaces
remove obsolete View→Output topology/policy surfaces
remove or rename View-specific assignment/presentation types to Composition semantics
remove FrameworkRuntimeHost View runtime lifetime
remove View injection/validation gates
```

### CAMERA-029-F — Consumer/documentation migration

Required result:

```text
Framework consumers migrated
legacy View authoring removed
Camera usage documentation reconciled
IF-ADR-022/026/027/028 carry explicit supersession notices
historical certifications remain historical
```

## 19. Validation obligations

At minimum the new architecture must prove:

### Default / gameplay lifecycle

```text
0 required Subjects
  -> no gameplay Composition request
  -> Output Default active

Subject becomes available
  -> current Composition input
  -> gameplay request admitted
  -> gameplay Rig active

last required Subject disappears
  -> gameplay request released
  -> another winner or Output Default active

new Subject occurrence appears
  -> new current evidence
  -> gameplay request admitted again
  -> stale old Subject absent
```

### Group lifecycle

```text
P1
  -> Group {P1}

P1 + P2
  -> Group {P1,P2}

P1 leaves
  -> Group {P2}

P1 rejoins as a new occurrence
  -> Group {P1-new,P2}
  -> P1-old absent
```

The presentation intent remains Group throughout valid 1..N membership.

### Request arbitration

```text
gameplay Composition request active
  -> higher winning scoped request
  -> gameplay request remains logically admitted where policy permits
  -> scoped winner releases
  -> gameplay request is restored

force-default active
  -> Default active regardless of normal winner
  -> force-default releases
  -> current normal winner restored
```

### Transactional failure

Prove that failed presentation/request application does not leave:

```text
new targets on an old active rig
old targets on a new active rig
admitted request with rejected presentation input
released request with partially mutated active presentation
stale Group members
```

### Split-screen non-regression

Prove:

```text
PlayerInput.camera association remains exact
PlayerInputManager remains Camera.rect writer
Framework does not infer split count/layout
Group presentation does not alter split-screen geometry
```

## 20. Migration and compatibility

The migration is intentionally breaking for Experimental View authoring surfaces.

Do not preserve dual authority indefinitely.

Rejected compatibility approach:

```text
new Composition path
+
legacy View path
+
silent fallback between them
```

A short explicit migration utility or serialized migration step is acceptable when required
for existing package consumers, but the resulting authoring must have one authority.

`CameraOutputDefinition` and `CameraOutputId` remain stable through this migration.

Existing Default/force-default and scoped request behavior must remain source-compatible
where not directly coupled to removed View APIs.

## 21. Consequences

Positive:

- removes an unproven domain abstraction;
- restores one normal Output selection path;
- restores Default Rig as an independent fallback;
- makes shared multi-Subject presentation explicit through Group;
- gives Subject membership one clear owner: Composition;
- preserves deterministic Camera request arbitration;
- preserves PlayerInputManager split-screen ownership;
- reduces identity-only Camera assets and topology infrastructure;
- aligns runtime structure with actual productive responsibility.

Costs:

- substantial API and serialized-authoring migration for View-based Experimental surfaces;
- Group settings must migrate out of Follow;
- Composition request publication requires explicit owner/lifetime semantics;
- presentation/request rollback needs focused implementation and tests;
- historical View-based Camera certification cannot certify the new boundary.

## 22. Architecture invariants after migration

The following must hold:

```text
Camera Subject != Camera Composition
Camera Composition != Camera Rig
Camera Rig != Camera Request
Camera Request != Camera Output
Camera Output != physical screen layout

Default Rig != normal Subject-driven Composition Rig

Player != Camera Request owner by default
Player count != Camera Output count
Group != split-screen

Camera Composition selects Subjects
Camera Rig presents resolved input
CameraOutputContext selects normal request winner
CameraOutputSession selects winner/default
PlayerInputManager owns automatic split-screen layout
```

No global Camera manager, service locator, mutable registry, hierarchy lookup or implicit
first-Player/first-Output behavior is introduced.

## 23. Disposition

```text
CameraView independent authority          REMOVED by decision
Camera Composition authority              ACCEPTED
Composition -> CameraRequest participation ACCEPTED
Output Default as fallback only           RETAINED / clarified
Camera request arbitration                RETAINED
CameraOutputDefinition / CameraOutputId    RETAINED
Group presentation intent                 ACCEPTED
SharedFollow implicit multi-target mode   SUPERSEDED
PlayerInputManager split layout authority RETAINED
physical Camera layout outside Framework  RETAINED
implementation                            A/B/C/D/E/F committed
Unity tested                              NO
technically validated                     NO
certified                                 NO
consumer/documentation migration          COMMITTED
```

The next gate is Unity import, focused Framework test execution and consumer lifecycle
validation. Static checks do not promote this ADR to technically validated or certified.
