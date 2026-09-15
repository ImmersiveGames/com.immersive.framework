# IF-ADR-027 — Camera Authoring Definitions and Composition Authority

Status: **Reopened for authoring reconciliation — typed View / Output / Rig Behavior authority remains accepted; CAMERA-027-D2 is implemented and technically certified; physical layout authoring is external to Framework Camera under corrected IF-ADR-028**  
Proposed: **2026-09-11**  
Accepted: **2026-09-12**  
Reopened: **2026-09-12**  
Corrected: **2026-09-15**  
Type: architecture / product authoring / Camera composition  
Extends: IF-ADR-002, IF-ADR-010, IF-ADR-014  
Preserves: IF-ADR-004 request arbitration; IF-ADR-022 rig materialization; corrected IF-ADR-026 Subject / Assignment / View / Rig / Output separation  
Implementation state: **CAMERA-027-A/B/C retained; CAMERA-027-D2 implemented/certified 2026-09-14; CAMERA-027-E deferred; CAMERA-027-F final closure pending corrected physical-presentation and PlayerInput integration reconciliation**  
Historical technical evidence: [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)  
Current reconciliation: [Camera Output Participation and Physical Presentation Ownership Reconciliation — 2026-09-12 / corrected 2026-09-15](../Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)

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

A later audit found one authoring overreach: `Viewport` was modeled as part of View→Output association. That responsibility is not Camera authoring. Corrected IF-ADR-028 establishes that physical Camera presentation is externally owned by Unity/gameplay or, for Player split-screen, `PlayerInputManager`.

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

The explicit Unity Camera reference is the Camera that the Framework may operate for Camera-domain behavior. Referencing that Camera does not transfer ownership of `Camera.rect`, `pixelRect`, `targetDisplay` or `targetTexture` to the Framework.

An Output may be registered/available without a current View association under corrected IF-ADR-026.

Gameplay-only Cameras that do not participate in Framework Default/Player Camera behavior do not need a `CameraOutputDefinition` merely because they exist.

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

## 10. Physical presentation is external to Camera authoring

Corrected IF-ADR-028 owns the architectural boundary, but the physical presentation properties themselves are not authored by Framework Camera.

Normative separation:

```text
Framework Camera authoring
  View -> Output
  explicit Unity Camera reference

External physical presentation
  Unity/gameplay -> viewport / display / RenderTexture / custom composition
  PlayerInputManager -> split-screen Camera.rect for Player Cameras
```

Framework Camera authoring must not add a replacement layout asset or viewport field after removing viewport from View→Output topology.

## 11. PlayerInputManager integration

`PlayerInputManager` may participate through an explicit integration adapter for Player Cameras.

When automatic split-screen is active, `PlayerInputManager` remains the sole writer of the participating Player Camera `rect` values. The Framework may provide/integrate the correct Camera reference but must not recalculate, copy, restore or overwrite the split-screen rectangle.

`PlayerInputManager` must not become Camera Subject, Assignment, View, Output identity or request-arbitration authority.

The final integration rule is owned by CAMERA-028-D. Until that cut exists, the current implementation may continue to report automatic split-screen as unsupported; that rejection is temporary integration debt, not evidence that Framework Camera owns viewport state.

## 12. Camera Composition Definition remains optional

A reusable Camera Composition definition remains deferred until real consumer evidence justifies it.

If introduced later, it may group Camera-domain intent such as View Definition, Output Definition, Rig Behavior Definition and Subject selection policy. It must not absorb physical screen-layout authority into a mega Camera asset.

## 13. Physical Output remains concrete scene authority

A definition asset does not replace the physical Output boundary. The concrete Session composition still requires explicit physical evidence: Unity Camera, CinemachineBrain and Default Camera Rig.

No Output definition may create implicit Camera or scene-discovery authority.

Explicit physical binding does not mean Framework ownership of arbitrary Camera presentation properties.

## 14. No silent inference

The Framework must not infer Camera topology from Player count, Player index, first Player, object naming, hierarchy or scene traversal order.

Accepted:

```text
Authored: View = Gameplay View, Output = Main Output
Derived: exact ViewId -> OutputId runtime binding
```

Rejected: silently choosing the first available Output when no Output is authored.

Also rejected: introducing a Framework viewport/layout definition merely because a Camera Output exists.

## 15. Normal versus Advanced / Debug

Normal Camera authoring prioritizes typed definition references, Subject policy, Rig behavior, logical Output association, required physical scene references and validation.

Advanced / Debug may expose stable IDs, Assignment owner/context identities, technical bindings, materialization provenance, revisions, resolved Subjects and current request winner.

Physical layout diagnostics belong to the external owner of that state. Framework diagnostics may report integration compatibility but must not imply layout ownership.

## 16. Migration policy

No indefinite dual authority is accepted between raw identity strings and typed definition references.

No Framework Camera-owned viewport authority is accepted alongside an external layout authority.

CAMERA-027-D2 removed viewport from the current Camera View→Output authoring surface and did not retain a compatibility overload as a second authority.

The 2026-09-15 experimental Framework `Camera.rect` authority introduced during CAMERA-028-C work is not a new authoring direction; corrected IF-ADR-028 requires it to be removed/reconciled rather than exposed through authoring.

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
Status: **partially exercised; final closure pending corrected physical-presentation ownership and PlayerInput integration**.

Final consumer proof must demonstrate typed Camera authoring, no copied View/Output IDs, no Framework Camera-authored viewport, explicit Camera Subject, explicit Rig behavior, explicit physical Output where Framework Camera operation is required, preserved externally owned physical presentation state, correct Player Camera integration where applicable, and correct Play Mode behavior.

## 18. QA obligations

Current corrected coverage includes logical View→Output association projection, no viewport in the Camera binding contract, partial Output association acceptance, no Player-count inference and no implicit Output discovery.

The 2026-09-12 39/39 certification remains historical evidence for the previous viewport-bearing boundary. The 2026-09-14 corrected run certifies CAMERA-027-D2.

Future certification must not require a Framework layout authoring surface. Instead it must prove physical-presentation non-ownership and the PlayerInput integration boundary defined by corrected IF-ADR-028.

## 19. Preserved decisions

Preserved: typed View Definition authority, typed Output Definition authority, stable ID projection and collision blocking, typed Camera Rig Behavior definitions, `CameraRigComposer` materialization/provenance authority, exact Camera Subject observation Transform, concrete physical Output references, Assignment context/owner identities as technical infrastructure, direct modular authoring, and no global Camera manager or silent lookup.

## 20. Superseded authoring decisions

Superseded:

```text
Viewport as a field of normal Camera View→Output association
Viewport as a field of advanced Camera topology binding
Camera composition authoring as owner of split-screen rectangle
Framework Output Layout authoring as replacement viewport authority
Global PlayerInputManager split-screen rejection caused by Camera viewport ownership
```

Corrected IF-ADR-028 is the replacement boundary.

## 21. Consequences

The corrected model is:

```text
Designer authors Framework Camera:
  Subject
  View Definition
  Rig Behavior Definition
  Output Definition
  logical View→Output association
  explicit Unity Camera reference where Framework operation is required

Framework derives Camera:
  stable identity projections
  Assignment infrastructure identities
  logical runtime bindings
  rig/request behavior

External owner manages physical presentation:
  Unity/gameplay -> Camera rect/display/RenderTexture/custom composition
  PlayerInputManager -> Player split-screen rects
```

CAMERA-027-D2 is complete. IF-ADR-027 remains reopened because CAMERA-027-F still waits for physical-presentation reconciliation, PlayerInput integration and final consumer proof.