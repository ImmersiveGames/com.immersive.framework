# IF-ADR-027 — Camera Authoring Definitions and Composition Authority

Status: **Reopened for authoring reconciliation — typed View / Output / Rig Behavior authority remains accepted; viewport/layout authoring is superseded by IF-ADR-028**  
Proposed: **2026-09-11**  
Accepted: **2026-09-12**  
Reopened: **2026-09-12**  
Type: architecture / product authoring / Camera composition  
Extends: IF-ADR-002, IF-ADR-010, IF-ADR-014  
Preserves: IF-ADR-004 request arbitration; IF-ADR-022 rig materialization; corrected IF-ADR-026 Subject / Assignment / View / Rig / Output separation  
Implementation state: **CAMERA-027-A/B/C retained; CAMERA-027-D requires D2 reconciliation; CAMERA-027-E deferred; CAMERA-027-F final closure pending corrected topology/layout implementation**  
Historical technical evidence: [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)  
Current reconciliation: [Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](../Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)

## 1. Context

IF-ADR-027 correctly replaced normal Camera authoring based on copied technical identities with typed definition references. The accepted logical Camera chain remains:

```text
Camera Subject
      ↓
Camera Assignment
      ↓
Camera View
      ↓
Camera Rig / Presentation
      ↓
Camera Output
```

A later audit found one authoring overreach: `Viewport` was modeled as part of View→Output association. That responsibility belongs to physical Output Presentation / Layout and is moved to IF-ADR-028.

## 2. Normative authoring rule

> When one authored Camera concept is shared by multiple official authoring surfaces, consumers share a typed definition reference, not copied stable-ID text.

```text
typed authored definition
        ↓
stable identity projection
        ↓
runtime topology / diagnostics
```

Stable IDs remain strong runtime and diagnostic evidence. They are not the normal Inspector linking mechanism.

## 3. Camera View Definition

`CameraViewDefinition` remains authored authority for:

```text
exact View definition identity
human-readable intent
stable CameraViewId projection
```

Rules:

- exact asset reference is authored authority;
- duplicate stable IDs across distinct definitions block;
- rename/move preserves identity;
- no name/hierarchy fallback;
- View Definition does not own screen layout.

## 4. Camera Output Definition

`CameraOutputDefinition` remains authored authority for:

```text
exact Output definition identity
human-readable intent
stable CameraOutputId projection
```

Concrete physical binding remains:

```text
CameraOutputAuthoring
  Output Definition
  Unity Camera
  CinemachineBrain
  Default Camera Rig
```

The Output Definition must not own scene instances, Player/Actor occurrences, viewport rectangles, RenderTextures or display layout state.

An Output may be registered/available without a current View association under corrected IF-ADR-026.

## 5. Camera Rig Behavior Definition

The accepted presentation family remains:

```text
Fixed
Follow
Mounted
Third Person
```

Reusable behavior/tuning is authored through typed Camera Rig Behavior definitions.

```text
Camera Rig Behavior Definition
        ↓
CameraRigComposer
  validation
  Apply / Rebuild
  materialization provenance
        ↓
Cinemachine materialization
```

The Behavior Definition must not select Subjects, Players or Outputs; arbitrate requests; discover scene objects; or own screen layout.

## 6. Camera Subject authoring

`ActorCameraSubjectAuthoring` remains valid direct authoring for an exact observation Transform belonging to the Actor Presentation.

```text
Actor Presentation
  Camera Subject
    Observation Transform = exact authored Transform
```

No Camera Subject definition asset is required merely for symmetry. Publication of Player-backed Subject availability follows the integration-boundary rule in reopened IF-ADR-026.

## 7. View→Output association authoring

The logical Camera relation remains explicit:

```text
View Definition -> Output Definition
```

The Framework may deterministically project that relation into runtime binding evidence.

> **Binding is runtime topology; association is authoring intent.**

The corrected normal association is:

```text
Gameplay View
  Output = Main Output
```

Derived:

```text
CameraViewOutputBinding
  ViewId
  OutputId
```

`Viewport` is no longer part of the Camera association.

## 8. Advanced multi-binding authoring

Advanced topology authoring remains valid for explicit logical associations such as:

```text
Gameplay View A -> Output A
Gameplay View B -> Output B
Spectator View  -> Output C
```

Rules:

- typed View/Output definitions only;
- no copied stable-ID strings as normal authority;
- no viewport/screen partition in Camera topology binding;
- one Output may have at most one View in one association snapshot;
- one View may feed multiple explicit Outputs where supported;
- available unassociated Outputs are valid.

## 9. Assignment infrastructure identities

`ViewAssignmentContextId` and `CameraSubjectAssignmentOwnerId` remain runtime ownership evidence, not normal gameplay authoring.

They may be derived from exact authored composition authority or owned by the concrete composition authoring instance when derivation is explicitly scoped, deterministic, collision-safe and inspectable in Advanced / Debug.

They must never be derived from object names, hierarchy, scene order or timing.

## 10. Output Presentation / Layout is separate

IF-ADR-028 owns how an available Camera Output is physically presented.

Examples:

```text
fullscreen
split-screen region
picture-in-picture
RenderTexture
target display
spectator display
safe-area constrained presentation
```

Normative separation:

```text
Camera authoring
  View -> Output

Presentation/Layout authoring
  Output -> presentation surface / region
```

Only one active layout authority may write the same physical presentation state.

## 11. PlayerInputManager integration

`PlayerInputManager` may participate through an explicit integration adapter when its automatic split-screen feature is selected as layout authority.

It must not become Camera Subject, Assignment, View, Output identity or request-arbitration authority.

The Framework must not globally reject automatic split-screen merely because Framework Camera topology exists. Conflict prevention belongs to explicit layout-authority selection under IF-ADR-028.

## 12. Camera Composition Definition remains optional

A reusable Camera Composition definition remains deferred until real consumer evidence justifies it.

If introduced later, it may group Camera-domain intent such as View Definition, Output Definition, Rig Behavior Definition and Subject selection policy. It must not absorb screen-layout authority into a mega Camera asset.

## 13. Physical Output remains concrete scene authority

A definition asset does not replace the physical Output boundary.

The concrete Session composition still requires explicit physical evidence:

```text
Unity Camera
CinemachineBrain
Default Camera Rig
```

No Output definition may create an implicit global Camera, use `Camera.main`, perform name/tag lookup or create hidden persistent runtime objects.

## 14. No silent inference

The Framework must not infer Camera topology from:

```text
Player count
Player index
first Player
nearest Actor
Camera.main
GameObject.Find
tag
object name
hierarchy name
scene traversal order
```

Accepted:

```text
Authored: View = Gameplay View, Output = Main Output
Derived: exact ViewId -> OutputId runtime binding
```

Rejected:

```text
No Output selected -> silently use first CameraOutputAuthoring
```

## 15. Normal versus Advanced / Debug

Normal Camera authoring prioritizes typed definition references, Subject policy, Rig behavior, logical Output association, required physical scene references and validation.

Advanced / Debug may expose stable IDs, Assignment owner/context identities, technical bindings, materialization provenance, revisions, resolved Subjects and current request winner.

Layout diagnostics belong to the selected IF-ADR-028 presentation authority.

## 16. Migration policy

No indefinite dual authority is accepted between raw identity strings and typed definition references.

No indefinite dual authority is accepted between Camera-owned viewport state and an external layout authority.

Migration tooling may convert existing content explicitly, but runtime must end with one canonical authority per concern.

## 17. Implementation cuts

### CAMERA-027-A — Definition identity foundation
Status: **implemented / retained**.

### CAMERA-027-B — Definition-backed View / Output authoring
Status: **implemented / retained**.

### CAMERA-027-C — Camera Rig Behavior definitions
Status: **implemented / retained**.

### CAMERA-027-D2 — Logical association authoring reconciliation
Status: **pending**.

Required result:

```text
normal Camera composition authors View Definition + Output Definition
runtime binding contains View identity + Output identity
no viewport in CameraViewOutputBinding / CameraViewOutputTopology
advanced multi-binding uses typed definitions only
available unassociated Outputs remain valid
```

### CAMERA-027-E — Optional reusable Camera Composition definition
Status: **deferred / optional**.

### CAMERA-027-F — Consumer migration and stale surface removal
Status: **partially exercised; final closure pending corrected topology/layout implementation**.

Final consumer proof must demonstrate:

```text
no copied View ID
no copied Output ID
no manually invented Assignment Context/Owner IDs
no hand-authored technical binding for normal one View -> one Output
no Camera-authored viewport
one explicit layout authority when layout is required
Camera Subject explicit
Rig behavior explicit
physical Output explicit
Play Mode behavior correct
```

## 18. QA obligations

Required coverage after reconciliation includes:

```text
View definition identity
Output definition identity
exact definition-reference authority
ID collision rejection
missing definition rejection
no raw-ID fallback
behavior validation/materialization equivalence
logical View→Output association projection
no viewport in Camera binding contract
partial Output association accepted
no Player-count inference
no implicit Output discovery
layout ownership delegated to IF-ADR-028
```

The 2026-09-12 39/39 certification remains historical evidence for the previous integrated boundary and must not be relabeled as certification of CAMERA-027-D2 or IF-ADR-028.

## 19. Preserved decisions

Preserved:

- typed View Definition authority;
- typed Output Definition authority;
- stable ID projection and collision blocking;
- typed Camera Rig Behavior definitions;
- `CameraRigComposer` materialization/provenance authority;
- exact Camera Subject observation Transform;
- concrete `CameraOutputAuthoring` physical references;
- Assignment context/owner identities as technical infrastructure;
- direct modular authoring;
- no global Camera manager or silent lookup.

## 20. Superseded authoring decisions

Superseded:

```text
Viewport as a field of normal Camera View→Output association
Viewport as a field of advanced Camera topology binding
Camera composition authoring as owner of split-screen rectangle
Global PlayerInputManager split-screen rejection caused by Camera viewport ownership
```

IF-ADR-028 is the replacement authority.

## 21. Consequences

The corrected model is:

```text
Designer authors Camera:
  Subject
  View Definition
  Rig Behavior Definition
  Output Definition
  logical View→Output association

Framework derives Camera:
  stable identity projections
  Assignment infrastructure identities
  logical runtime bindings

Separate presentation policy authors:
  viewport / display / RenderTexture / layout
```

This preserves strong Camera identities while removing screen-layout authority from the Camera domain.