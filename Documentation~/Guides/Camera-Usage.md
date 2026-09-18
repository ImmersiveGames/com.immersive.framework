# Camera Usage

Status: **Current guide — CAMERA-026-I and CAMERA-028-A/B are technically certified; CAMERA-028-C focused behavior PASS; CAMERA-028-D implemented/tested/integrated with 33/33 focused functional PASS; final ADR-028 validation remains open**  
Last updated: **2026-09-17**

This guide describes the current implemented Camera boundary after the viewport-bearing View→Output contract was removed, including the external physical-presentation ownership rule and the explicit PlayerInput split-screen integration.

Do not restore viewport ownership to Camera View→Output topology. Physical layout remains externally owned; Framework Camera does not become a `Camera.rect` writer.

See:

- [IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology](../Architecture/ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md)
- [IF-ADR-027 — Camera Authoring Definitions and Composition Authority](../Architecture/ADRs/IF-ADR-027-Camera-Authoring-Definitions-and-Composition-Authority.md)
- [IF-ADR-028 — Camera Output Participation and Presentation Layout Authority](../Architecture/ADRs/IF-ADR-028-Camera-Output-Participation-and-Presentation-Layout-Authority.md)
- [Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](../Architecture/Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)
- [IF-ADR-028 Focused Physical Presentation / PlayerInput Validation — 2026-09-17](../Architecture/Reconciliation/IF-ADR-028-FOCUSED-VALIDATION-2026-09-17.md)
- [Camera Full Technical Certification — 2026-09-12](../Architecture/Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
- [IF-ADR-004D — Camera Default Output Presentation Authority](../Architecture/Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)
- [IF-ADR-022 — Camera Rig Presentation Models](../Architecture/ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md)

---

## 1. Normative Camera model

Keep these authorities separate:

```text
Camera Subject
  what may be observed

Camera Assignment
  which Subject(s) feed a logical Camera View

Camera View
  logical composed view

Camera Rig / Presentation
  how resolved Subject(s) are observed

Camera Output
  explicit physical Camera capacity

View→Output association
  which logical View feeds which available Output

Output Presentation / Layout
  where/how an Output is presented physically

Camera Request
  normal scoped arbitration for rig presentation on an Output

Output Default / force-default
  persistent fallback/system rig presentation for an Output
```

Critical invariants:

```text
Player != Camera
Player != Camera View
Player != Camera Output
Player count != Output count
available Output != active View→Output association
Camera View→Output topology != screen layout
scope != request precedence
force-default != layout authority
```

---

## 2. Current implementation versus remaining target

The repository now implements the corrected logical Camera topology through CAMERA-028-A/B and CAMERA-027-D2.

Current implemented boundary:

```text
Session available Outputs        -> 1..N
current View→Output associations -> 0..N subset

CameraViewOutputBinding
  View identity
  Output identity

Camera View→Output runtime
  validates/resolves logical participation only
  does not own Camera.rect
```

The old behavior is removed from the current Camera contract:

```text
viewport inside CameraViewOutputBinding        REMOVED
binding-count == physical Output count rule    REMOVED
Camera View→Output runtime Camera.rect writer  REMOVED
viewport validation in Camera topology         REMOVED
```

The current physical-presentation boundary is implemented as non-ownership:

```text
CAMERA-028-C
  Framework owns no Camera.rect / pixelRect / targetDisplay / targetTexture layout policy
  externally supplied Camera presentation is preserved

CAMERA-028-D
  explicit Player Slot -> Camera Output integration
  PlayerInput.camera receives the exact participating Camera
  PlayerInputManager owns automatic split-screen rect recomposition
```

The 2026-09-17 focused run provides 33/33 functional evidence for the PlayerInput path. Final validation is still open because the certification harness lacks a read-only clean-state preflight and emits runtime PASS before QA-owned InputSystem device cleanup has been verified.

---

## 3. Preserved definition-backed authoring

The typed-definition work from IF-ADR-027 remains current and should be preserved.

Normal Camera-domain concepts are:

```text
Camera Subject
Camera View Definition
Camera Rig Behavior Definition
Camera Output Definition
logical View→Output association
```

Stable `CameraViewId`, `CameraOutputId`, Assignment Context IDs, Assignment Owner IDs and runtime bindings remain diagnostic/runtime evidence. They are not the normal Inspector linking mechanism.

> **Binding is runtime topology; association is authoring intent.**

Do not restore workflows based on copying stable IDs between Inspectors.

---

## 4. Camera View Definition

`CameraViewDefinition` represents reusable authored View identity and intent. The exact asset reference is authoring authority and stable ID is runtime/diagnostic evidence. Duplicate stable IDs across different definitions must block.

A View Definition does not own Unity Camera, CinemachineBrain, viewport, RenderTexture, target display or Player occurrence.

---

## 5. Camera Output Definition and physical Output

`CameraOutputDefinition` represents reusable authored Output identity.

The physical scene/prefab authority remains `CameraOutputAuthoring`:

```text
CameraOutputAuthoring
  Output Definition
  Unity Camera
  CinemachineBrain
  Default Camera Rig
```

The Unity Camera and `CinemachineBrain` are explicit physical references. No implicit Camera or hierarchy guessing is accepted.

A registered Output means physical capacity is available. It does not mean the Output must currently have a View association or occupy screen space.

---

## 6. Available Outputs versus active participation

Corrected cardinality:

```text
available physical Outputs       -> 1..N
current View→Output associations  -> 0..N subset of available Outputs
```

Example:

```text
Available:
  Main
  Player2
  Spectator

Current Camera topology:
  Gameplay View -> Main

Player2 and Spectator remain available and unassociated.
```

This boundary is implemented and certified by CAMERA-026-H2 / CAMERA-028-A. The focused 2026-09-14 revalidation proved two available Outputs with one participating association and `8/8` runtime cases.

Still invalid:

```text
binding references an Output that is not registered
View A -> Output X
View B -> Output X in the same association snapshot
```

One View may feed multiple explicit Outputs where the supported topology requires it.

---

## 7. View→Output association

The current Camera relation is only:

```text
Camera View -> Camera Output
```

The runtime binding contains Camera-domain identity only:

```text
CameraViewId
CameraOutputId
```

The following do not belong to Camera View→Output topology:

```text
viewport rectangle
screen partition
safe area
RenderTexture destination
target display
PiP placement
spectator window placement
PlayerInput split-screen rectangle
```

CAMERA-027-D2 / CAMERA-028-B removed the old serialized viewport fields, viewport-bearing authoring overloads and Camera runtime viewport ownership. Do not reintroduce them as compatibility surfaces.

---

## 8. Output Presentation / Layout

IF-ADR-028 defines a separate authority for physical Output presentation.

```text
Camera Output
      ↓
Output Presentation / Layout policy
      ↓
physical presentation target
```

Possible targets include fullscreen viewport, split-screen viewport, RenderTexture, target display, picture-in-picture, spectator display or no current visible region.

No reusable Framework Output Layout asset is required by the current ADR-028 boundary. CAMERA-028-C establishes external physical-presentation ownership instead of introducing a Framework layout writer.

### Single-writer rule

One physical presentation property has one active writer. Conflicting writers must block explicitly; silent last-writer-wins behavior is rejected.

---

## 9. PlayerInputManager split-screen

The previous rule that Framework Camera ownership itself globally invalidates PlayerInput automatic split-screen is superseded.

Current rule:

```text
PlayerInputManager automatic split-screen enabled
  -> explicit Player Slot -> Camera Output policy is required
  -> Framework associates the exact Camera through PlayerInput.camera
  -> PlayerInputManager owns viewport partitioning

Framework/game custom physical presentation
  -> external gameplay/Unity owner controls presentation
  -> Framework remains non-writer for Camera.rect
```

`PlayerInputManager` never becomes authority for Camera Subject, Camera Assignment, Camera View, Camera Output identity or Camera request arbitration.

CAMERA-028-D implements this integration. The focused 2026-09-17 run passes 33/33 functional cases, including exact inverted Slot→Output binding, 0→1→2→1 Player lifecycle, coherent two-player viewport behavior, semantic Camera preservation and explicit incomplete-coverage rejection. Final validation remains open only on harness clean-state/cleanup/reentrancy evidence.

---

## 10. Camera Rig Behavior and CameraRigComposer

The accepted Presentation family remains Fixed, Follow, Mounted and Third Person.

`CameraRigComposer` materializes local Cinemachine presentation from the selected typed Camera Rig Behavior Definition. It does not select Players, discover Outputs or own screen layout.

Apply/Rebuild materializes the local Cinemachine rig only. It does not create persistent physical Camera authority.

---

## 11. Fixed

Use Fixed for authored static/local presentation: menu camera, room camera, static Activity/Route camera or establishing shot.

The authored Cinemachine Camera Transform owns pose. Optional/required Look At remains a presentation-specific target contract.

---

## 12. Follow and Group

Follow presents exactly one resolved Subject through the supported Follow materialization.

Group presents an explicitly assigned set of one or more Subjects:

```text
Gameplay View
  Subjects = {P1, P2, P3}
        ↓
Group presentation
        ↓
one Camera Rig
```

Another Player joining changes Subject availability/assignment. It does not imply another Camera Rig or Output.

`Follow + Many Subjects` is rejected and never becomes Group implicitly. Cinemachine
Target Group / Group Framing are Group projection mechanisms, never assignment or
composition authority.

---

## 13. Mounted

Mounted consumes one explicit observation/mount Transform. Gameplay owns movement/rotation of the mount; Camera does not read Player gameplay input merely because the presentation is Mounted.

---

## 14. Third Person

Third Person remains the accepted over-the-shoulder base presentation using its typed behavior definition and local Cinemachine materialization.

Its existence does not change Camera Output count, request precedence or physical layout.

---

## 15. Actor Camera Subject authoring

`ActorCameraSubjectAuthoring` remains the direct authoring surface when an Actor Presentation needs to expose an exact observation Transform.

```text
Actor Presentation
  Camera Subject
    Observation Transform = exact authored child/pivot
```

Once explicit authoring exists, missing/foreign/invalid observation evidence must fail. Do not silently fall back to Actor root.

The desired dependency direction is:

```text
Player Actor / Presentation occurrence evidence
      ↓
Camera integration boundary
      ↓
Camera Subject availability
```

PlayerParticipation core must not become Camera topology/lifecycle authority. CAMERA-026-I reconciles the current implementation location of the Player-backed Subject projection.

---

## 16. Ordinary Player Camera participation

Ordinary Player gameplay contributes observable Subject evidence. It does not own a normal Camera request, Camera View, Camera Rig or Camera Output.

The old ordinary per-Player Camera request path remains non-canonical. Camera does not own Player Join, Actor creation, initial placement or Player Leave.

---

## 17. Camera requests and arbitration

Built-in scoped request publishers remain meaningful for Session, Route, Activity and other explicit specialized owners.

`CameraOutputContext` retains deterministic normal request arbitration.

```text
scope      -> request ownership / lifetime context
precedence -> arbitration policy
```

Scope is not precedence. Presentation Model and physical layout are not precedence evidence.

---

## 18. Default Camera Rig and force-default

Every physical Output retains one explicit persistent Default Camera Rig.

```text
force-default owner active
  -> Default Camera Rig
otherwise normal request winner exists
  -> winner Rig
otherwise
  -> Default Camera Rig
```

The Default is not a Camera Request and has no request precedence.

Force-default changes which Rig presentation the Output uses. It does not acquire viewport/display layout ownership.

---

## 19. Failure and no-fallback rules

Mandatory Camera evidence fails explicitly. Examples include invalid/missing View or Output Definition, stable identity collision, required Subject/target missing, stale Actor occurrence evidence, missing physical Output references, unavailable Output binding, conflicting two Views for one Output, conflicting layout writers, request identity/tie-break conflict and physical apply/rollback failure.

Do not introduce fallback through implicit Camera discovery, object naming, hierarchy guessing, first Player, nearest Actor, global registries, silent first Output or silent first layout writer.

---

## 20. Certification status

Historical 2026-09-12 Full Camera evidence remains immutable:

```text
[QA_CAMERA_FULL]
39/39 PASS
ADR-026 phases = 2/2
9/9 dimensions
```

It certified the former viewport-bearing contract and is not relabeled.

The corrected 2026-09-14 certification proves the viewport-free logical topology boundary:

```text
Persistent Camera Presentation Composition regression  12/12 PASS
invalid-viewport                                       SupersededByCAMERA028B
CAMERA-028-A Partial                                   8/8 PASS
outputParticipation                                    PASS
Full Camera established cases                          39/39 PASS
ADR-026 phases                                         2/2 PASS
active Full Camera dimensions                          8/8 PASS
viewOutputAssociation                                  PASS
viewportSplitTopology                                  REMOVED
Shared baseline restore                                PASS
```

This certifies CAMERA-028-A/B and CAMERA-027-D2. CAMERA-026-I was subsequently certified on 2026-09-16.

The focused 2026-09-17 validation session adds:

```text
CAMERA-028-C physical-presentation behavior        PASS
CAMERA-028-D focused functional run                33/33 PASS
C9R adjacent regression                            39/39 PASS
Player Q1 adjacent regression                      39/39 PASS
Player Q2 adjacent regression                      36/36 PASS
ADR020-H adjacent regression                       26/26 PASS
```

This is sufficient to mark ADR-028 implemented, tested and integrated, but not validated. The focused harness still needs read-only preflight, cleanup before terminal verdict and a second no-repair run from the post-cleanup state.

---

## 21. Implementation sequence and current status

```text
CAMERA-026-H2 — IMPLEMENTED / TECHNICALLY CERTIFIED
  revalidated 2026-09-14

CAMERA-026-I — IMPLEMENTED / TECHNICALLY CERTIFIED 2026-09-16
  Player→Camera Subject integration boundary

CAMERA-027-D2 — IMPLEMENTED / TECHNICALLY CERTIFIED 2026-09-14
  logical View→Output authoring without viewport

CAMERA-028-A — IMPLEMENTED / TECHNICALLY CERTIFIED
  Output availability separated from participation

CAMERA-028-B — IMPLEMENTED / TECHNICALLY CERTIFIED 2026-09-14
  viewport/layout ownership removed from Camera topology/runtime

CAMERA-028-C — IMPLEMENTED / FOCUSED BEHAVIOR PASS 2026-09-17
  external physical-presentation ownership; Framework has no rect/layout writer

CAMERA-028-D — IMPLEMENTED / TESTED / INTEGRATED
  focused functional run 33/33 PASS 2026-09-17
  PlayerInputManager owns automatic split-screen layout

ADR-028 VALIDATION — OPEN
  read-only preflight + QA-device cleanup-before-verdict + reentrant second run pending
```

After the ADR-028 validation harness proves clean-state/reentrancy and cleanup participates in the terminal verdict, close remaining official Samples/FIRSTGAME consumer work under CAMERA-027-F.

---

## 22. Corrected QA obligations

Already proven:

```text
N available Outputs + strict subset associated          PASS
unassociated available Output                           PASS
binding references unavailable Output                   explicit rejection PASS
conflicting Views target same Output                    explicit rejection PASS
Camera View→Output topology carries no viewport         PASS
viewOutputAssociation                                   PASS
outputParticipation                                     PASS
generic request arbitration regression                  PASS
```

Focused physical-presentation / PlayerInput evidence:

```text
Framework preserves external Camera.rect                PASS — CAMERA-028-C
Framework productive rect writer                        ABSENT — static audit
PlayerInputManager automatic split path                 PASS — CAMERA-028-D 33/33
exact Player Slot -> Camera Output association           PASS
PlayerInput layout preserves Camera identities           PASS
PlayerInput layout preserves Subject/request/rig state   PASS
```

Still pending final validation evidence:

```text
read-only preflight before Prepare / Build / Repair      PENDING
QA-owned device inventory clean before terminal PASS     PENDING
first failure preserved through cleanup                  PENDING
post-cleanup read-only baseline confirmation             PENDING
second run without reparative preparation                PENDING
```

The old `viewportSplitTopology` dimension is retired. Do not rename it and reuse its old semantics.

---

## 23. Consumer/Sample guidance during reconciliation

The Getting Started migration remains useful evidence for typed Camera View Definition, typed Camera Output Definition, typed Camera Rig Behavior Definition, explicit Actor Camera Subject and physical Camera Output binding.

Current Camera composition authoring is logical View→Output association only; do not add viewport data back to `CameraSharedComposition` or `CameraViewOutputPolicyAuthoring`.

Final CAMERA-027-F consumer closure occurs only after the remaining ADR-028 validation-harness gate is closed.

Until then, preserve existing working sample behavior where compatible, avoid dependencies on Camera-owned viewport, avoid using Camera View→Output policy as a layout authority and avoid treating every available Output as necessarily active.

---

## 24. Next-cut planning checklist

Before final ADR-028 validation and CAMERA-027-F consumer closure, confirm the plan preserves:

```text
[ ] Subject / Assignment / View / Rig / Output separation
[ ] explicit CameraOutputId and physical Output references
[ ] typed View / Output / Rig Behavior definitions
[ ] CameraRigComposer materialization authority
[ ] deterministic request arbitration
[ ] Output-owned Default / force-default semantics
[ ] no ordinary per-Player Camera request
[ ] no global manager / singleton / service locator
[ ] available Output != active Output
[ ] View→Output binding contains no screen layout
[ ] one physical layout property has one writer
[ ] PlayerInput layout integration remains outside Camera topology authority
[ ] Player→Camera Subject bridge does not make PlayerParticipation Camera authority
[ ] historical QA remains historical rather than being relabeled
```

Use IF-ADR-026 through IF-ADR-029 together as the normative baseline for the next implementation work.
