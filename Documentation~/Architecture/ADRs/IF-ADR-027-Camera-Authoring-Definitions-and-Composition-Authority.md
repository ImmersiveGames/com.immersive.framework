# IF-ADR-027 — Camera Authoring Definitions and Composition Authority

Status: **Reopened for authoring reconciliation — typed View / Output / Rig Behavior authority remains accepted; CAMERA-027-D2 is implemented and technically certified; viewport/layout authoring is superseded by IF-ADR-028**  
Proposed: **2026-09-11**  
Accepted: **2026-09-12**  
Reopened: **2026-09-12**  
Type: architecture / product authoring / Camera composition  
Extends: IF-ADR-002, IF-ADR-010, IF-ADR-014  
Preserves: IF-ADR-004 request arbitration; IF-ADR-022 rig materialization; corrected IF-ADR-026 Subject / Assignment / View / Rig / Output separation  
Implementation state: **CAMERA-027-A/B/C retained; CAMERA-027-D2 implemented/certified 2026-09-14; CAMERA-027-E deferred; CAMERA-027-F final closure pending corrected layout/integration implementation**  
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

`CameraViewDefinition` remains authored authority for exact View definition identity, human-readable intent and stable `CameraViewId` projection.

Rules:

- exact asset reference is authored authority;
- duplicate stable IDs across distinct definitions block;
- rename/move preserves identity;
- no name/hierarchy fallback;
- View Definition does not own screen layout.

## 4. Camera Output Definition

`CameraOutputDefinition` remains authored authority for exact Output definition identity, human-readable intent and stable `CameraOutputId` projection.

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

The accepted presentation family remains Fixed, Follow, Mounted and Third Person.

Reusable behavior/tuning is authored through typed Camera Rig Behavior definitions and materialized through `CameraRigComposer`.

The Behavior Definition must not select Subjects, Players or Outputs; arbitrate requests; discover scene objects; or own screen layout.

## 6. Camera Subject authoring

`ActorCameraSubjectAuthoring` remains valid direct authoring for an exact observation Transform belonging to the Actor Presentation.

No Camera Subject definition asset is required merely for symmetry. Publication of Player-backed Subject availability follows the integration-boundary rule in reopened IF-ADR-026.

## 7. View→Output association authoring

The logical Camera relation remains explicit:

```text
View Definition -> Output Definition
```

The Framework may deterministically project that relation into runtime binding evidence.

> **Binding is runtime topology; association is authoring intent.**

Derived runtime binding:

```text
CameraViewOutputBinding
  ViewId
  OutputId
```

`Viewport` is no longer part of the Camera association.

## 8. Advanced multi-binding authoring

Advanced topology authoring remains valid for explicit logical associations such as multiple Views and Outputs.

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

The final integration rule is owned by CAMERA-028-D. Until that cut exists, the current implementation may continue to report automatic split-screen as unsupported; this is no longer justified by Camera viewport ownership.

## 12. Camera Composition Definition remains optional

A reusable Camera Composition definition remains deferred until real consumer evidence justifies it.

If introduced later, it may group Camera-domain intent such as View Definition, Output Definition, Rig Behavior Definition and Subject selection policy. It must not absorb screen-layout authority into a mega Camera asset.

## 13. Physical Output remains concrete scene authority

A definition asset does not replace the physical Output boundary. The concrete Session composition still requires explicit physical evidence: Unity Camera, CinemachineBrain and Default Camera Rig.

No Output definition may create implicit Camera or scene-discovery authority.

## 14. No silent inference

The Framework must not infer Camera topology from Player count, Player index, first Player, object naming, hierarchy or scene traversal order.

Accepted:

```text
Authored: View = Gameplay View, Output = Main Output
Derived: exact ViewId -> OutputId runtime binding
```

Rejected: silently choosing the first available Output when no Output is authored.

## 15. Normal versus Advanced / Debug

Normal Camera authoring prioritizes typed definition references, Subject policy, Rig behavior, logical Output association, required physical scene references and validation.

Advanced / Debug may expose stable IDs, Assignment owner/context identities, technical bindings, materialization provenance, revisions, resolved Subjects and current request winner.

Layout diagnostics belong to the selected IF-ADR-028 presentation authority.

## 16. Migration policy

No indefinite dual authority is accepted between raw identity strings and typed definition references.

No indefinite dual authority is accepted between Camera-owned viewport state and an external layout authority.

CAMERA-027-D2 removed viewport from the current Camera View→Output authoring surface and did not retain a compatibility overload as a second authority.

## 17. Implementation cuts

### CAMERA-027-A — Definition identity foundation
Status: **implemented / retained**.

### CAMERA-027-B — Definition-backed View / Output authoring
Status: **implemented / retained**.

### CAMERA-027-C — Camera Rig Behavior definitions
Status: **implemented / retained**.

### CAMERA-027-D2 — Logical association authoring reconciliation
Status: **implemented / technically certified — 2026-09-14**.

Certified result:

```text
normal Camera composition authors View Definition + Output Definition
runtime binding contains View identity + Output identity
no viewport in CameraViewOutputBinding / CameraViewOutputTopology
advanced multi-binding uses typed definitions only
available unassociated Outputs remain valid
```

Current Unity evidence:

```text
structural regression               12/12 PASS
partial participation               8/8 PASS
Full Camera established cases       39/39 PASS
ADR-026 phases                      2/2 PASS
corrected Full Camera dimensions    8/8 PASS
viewOutputAssociation               PASS
Shared baseline restore             PASS
```

The former viewport-only negative case is retired as `invalid-viewport:SupersededByCAMERA028B`; the obsolete `viewportSplitTopology` dimension is removed from active certification rather than renamed.

### CAMERA-027-E — Optional reusable Camera Composition definition
Status: **deferred / optional**.

### CAMERA-027-F — Consumer migration and stale surface removal
Status: **partially exercised; final closure pending remaining corrected layout/integration implementation**.

Final consumer proof must demonstrate typed Camera authoring, no copied View/Output IDs, no Camera-authored viewport, one explicit layout authority when layout is required, explicit Camera Subject, explicit Rig behavior, explicit physical Output and correct Play Mode behavior.

## 18. QA obligations

Current corrected coverage includes logical View→Output association projection, no viewport in the Camera binding contract, partial Output association acceptance, no Player-count inference and no implicit Output discovery.

The 2026-09-12 39/39 certification remains historical evidence for the previous viewport-bearing boundary. The 2026-09-14 corrected run certifies CAMERA-027-D2.

## 19. Preserved decisions

Preserved: typed View Definition authority, typed Output Definition authority, stable ID projection and collision blocking, typed Camera Rig Behavior definitions, `CameraRigComposer` materialization/provenance authority, exact Camera Subject observation Transform, concrete physical Output references, Assignment context/owner identities as technical infrastructure, direct modular authoring, and no global Camera manager or silent lookup.

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

CAMERA-027-D2 is complete. IF-ADR-027 remains reopened because CAMERA-027-F still waits for remaining layout/integration implementation and final consumer proof.
