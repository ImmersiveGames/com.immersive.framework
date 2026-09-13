# Camera Usage

Status: **Transitional current guide — IF-ADR-026/027 reopened; IF-ADR-028 accepted; corrected implementation and recertification pending**  
Last updated: **2026-09-12**

This guide distinguishes the **currently implemented Camera surface on `master`** from the
**current normative architecture** that must drive the next implementation cuts.

Do not use the viewport-bearing implementation as architectural authority merely because it
still exists in code or serialized content.

See:

- [IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology](../Architecture/ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md)
- [IF-ADR-027 — Camera Authoring Definitions and Composition Authority](../Architecture/ADRs/IF-ADR-027-Camera-Authoring-Definitions-and-Composition-Authority.md)
- [IF-ADR-028 — Camera Output Participation and Presentation Layout Authority](../Architecture/ADRs/IF-ADR-028-Camera-Output-Participation-and-Presentation-Layout-Authority.md)
- [Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](../Architecture/Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)
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

## 2. Current implementation versus normative target

The repository has not yet implemented the complete IF-ADR-028 correction.

Current implementation still contains the previous CAMERA-026-H / CAMERA-027-D behavior:

```text
CameraViewOutputBinding includes viewport
Camera topology projection expects every registered physical Output to be bound
Camera runtime applies/restores Unity Camera viewport state
Framework validation rejects PlayerInputManager automatic split-screen while Camera owns viewport
```

Those behaviors are **implementation debt**, not current normative architecture.

The target implementation is:

```text
Session available Outputs       -> 1..N
current View→Output associations -> 0..N subset

CameraViewOutputBinding
  View identity
  Output identity

separate Output Presentation / Layout authority
  viewport / display / RenderTexture / PiP
```

Until the pending cuts are implemented, do not expand official Samples around the old
viewport ownership model.

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

Stable `CameraViewId`, `CameraOutputId`, Assignment Context IDs, Assignment Owner IDs and
runtime bindings remain diagnostic/runtime evidence. They are not the normal Inspector
linking mechanism.

Normative shorthand:

> **Binding is runtime topology; association is authoring intent.**

Do not restore workflows based on copying stable IDs between Inspectors.

---

## 4. Camera View Definition

`CameraViewDefinition` represents reusable authored View identity and intent.

```text
exact View definition asset reference
        ↓
stable CameraViewId projection
```

The exact asset reference is authoring authority. Stable ID is runtime/diagnostic evidence.
Duplicate stable IDs across different definitions must block.

A View Definition does not own:

```text
Unity Camera
CinemachineBrain
viewport
RenderTexture
target display
Player occurrence
```

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

The Unity Camera and `CinemachineBrain` are explicit physical references. No
`Camera.main`, object-name lookup, hierarchy guessing or implicit global Camera is accepted.

A registered Output means **physical capacity is available**. It does not mean the Output
must currently have a View association or occupy screen space.

---

## 6. Available Outputs versus active participation

Corrected IF-ADR-026 / IF-ADR-028 cardinality:

```text
available physical Outputs       -> 1..N
current View→Output associations  -> 0..N subset of available Outputs
```

Valid example:

```text
Available:
  Main
  Player2
  Spectator

Current Camera topology:
  Gameplay View -> Main

Player2 and Spectator remain available and unassociated.
```

This must be valid after CAMERA-026-H2 / CAMERA-028-A.

Still invalid:

```text
binding references an Output that is not registered
View A -> Output X
View B -> Output X in the same association snapshot
```

One View may feed multiple explicit Outputs where the supported topology requires it.

---

## 7. View→Output association

The normative Camera relation is only:

```text
Camera View -> Camera Output
```

The runtime binding must contain Camera-domain identity only:

```text
CameraViewId
CameraOutputId
```

The following do **not** belong to Camera View→Output topology:

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

The current serialized `Viewport` fields remain migration debt until CAMERA-027-D2 /
CAMERA-028-B remove them.

---

## 8. Output Presentation / Layout

IF-ADR-028 introduces a separate authority for physical Output presentation.

Conceptually:

```text
Camera Output
      ↓
Output Presentation / Layout policy
      ↓
physical presentation target
```

Possible targets include:

```text
fullscreen viewport
split-screen viewport
RenderTexture
target display
picture-in-picture region
spectator display
no current visible region
```

The exact reusable authoring type is intentionally not frozen yet. CAMERA-028-C must first
establish the smallest explicit runtime authority with ownership-safe cleanup and validation.

### Single-writer rule

One physical presentation property has one active writer.

For example, `UnityEngine.Camera.rect` must not be written concurrently by both a Framework
layout policy and `PlayerInputManager` automatic split-screen.

Conflicting writers must block explicitly. Silent last-writer-wins behavior is rejected.

---

## 9. PlayerInputManager split-screen

The previous rule:

```text
Framework Camera active -> PlayerInputManager automatic split-screen always rejected
```

is superseded.

The corrected rule is:

```text
selected layout authority = PlayerInputManager
  -> PlayerInputManager owns viewport partitioning

selected layout authority = Framework/game layout policy
  -> that policy owns viewport partitioning

multiple selected writers
  -> explicit failure
```

`PlayerInputManager` never becomes authority for:

```text
Camera Subject
Camera Assignment
Camera View
Camera Output identity
Camera request arbitration
```

CAMERA-028-D implements and proves this integration.

---

## 10. Camera Rig Behavior and CameraRigComposer

The accepted Presentation family remains:

```text
Fixed
Follow
Mounted
Third Person
```

Create reusable behavior assets from the Camera Rig Behavior definition family and assign
the intended Definition to `CameraRigComposer`.

The authority chain remains:

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

`CameraRigComposer` does not select Players, discover Outputs or own screen layout.

Apply/Rebuild materializes the local Cinemachine rig only. It does not create a persistent
Unity Camera, `CinemachineBrain`, AudioListener or Camera Output authority.

---

## 11. Fixed

Use Fixed for authored static/local presentation.

Typical uses:

```text
menu camera
room camera
static Activity camera
static Route camera
establishing shot
```

The authored Cinemachine Camera Transform owns pose. Optional/required Look At remains a
presentation-specific target contract.

---

## 12. Follow and shared multi-target framing

Follow observes resolved target evidence through the supported Follow materialization.

Shared Follow may frame an explicitly assigned Subject set:

```text
Gameplay View
  Subjects = {P1, P2, P3}
        ↓
Follow / group projection
        ↓
one Camera Rig
```

Another Player joining changes Subject availability/assignment. It does not imply another
Camera Rig or Output.

Cinemachine Target Group / Group Framing are projection mechanisms, never assignment or
composition authority.

---

## 13. Mounted

Mounted consumes one explicit observation/mount Transform.

Typical uses:

```text
first-person mount
cockpit
helmet camera
vehicle camera socket
gameplay-controlled observation mount
```

Materialization remains based on hard lock / follow-target rotation behavior according to
the selected Mounted Behavior Definition.

Gameplay owns movement/rotation of the mount. Camera does not read Player gameplay input
merely because the presentation is Mounted.

---

## 14. Third Person

Third Person remains the accepted over-the-shoulder base presentation using its typed
behavior definition and local Cinemachine materialization.

Its existence does not change Camera Output count, request precedence or physical layout.

---

## 15. Actor Camera Subject authoring

`ActorCameraSubjectAuthoring` remains the direct authoring surface when an Actor Presentation
needs to expose an exact observation Transform.

```text
Actor Presentation
  Camera Subject
    Observation Transform = exact authored child/pivot
```

Once explicit authoring exists, missing/foreign/invalid observation evidence must fail. Do
not silently fall back to Actor root.

The desired dependency direction is:

```text
Player Actor / Presentation occurrence evidence
      ↓
Camera integration boundary
      ↓
Camera Subject availability
```

PlayerParticipation core must not become Camera topology/lifecycle authority. CAMERA-026-I
reconciles the current implementation location of the Player-backed Subject projection.

---

## 16. Ordinary Player Camera participation

Ordinary Player gameplay contributes observable Subject evidence. It does not own a normal
Camera request, Camera View, Camera Rig or Camera Output.

```text
Prepared Player Actor
  -> Camera Subject availability

independently

Camera composition
  -> View / Assignment
  -> Rig
  -> Output
```

The old ordinary per-Player Camera request path remains non-canonical.

Camera does not own Player Join, Actor creation, initial placement or Player Leave.

---

## 17. Camera requests and arbitration

Built-in scoped request publishers remain meaningful for Session, Route, Activity and other
explicit specialized owners.

`CameraOutputContext` retains deterministic normal request arbitration.

Normative distinction:

```text
scope
  -> request ownership / lifetime context

precedence
  -> arbitration policy
```

Scope is not precedence.

Existing values such as Activity `100`, Route `200` and Session `300` may remain normal
authoring defaults/conventions. Camera core must not treat those values as an intrinsic
semantic hierarchy.

Presentation Model and physical layout are not precedence evidence.

---

## 18. Default Camera Rig and force-default

Every physical Output retains one explicit persistent Default Camera Rig.

Selection remains:

```text
force-default owner active
  -> Default Camera Rig

otherwise normal request winner exists
  -> winner Rig

otherwise
  -> Default Camera Rig
```

The Default is not a Camera Request and has no request precedence.

Force-default changes **which Rig presentation the Output uses**. It does not acquire
viewport/display layout ownership.

Transition integration may continue to force/release Default without becoming Output Layout
authority.

---

## 19. Failure and no-fallback rules

Mandatory Camera evidence fails explicitly.

Examples include:

```text
invalid/missing View or Output Definition
stable identity collision
required Subject/target missing
stale Actor occurrence evidence
ambiguous local rig materialization
missing Unity Camera or CinemachineBrain
missing Default Camera Rig
binding references unavailable Output
conflicting two Views for one Output
conflicting layout writers
request identity/tie-break conflict
physical apply/rollback failure
```

Do not introduce fallback through:

```text
Camera.main
GameObject.Find
object names
tags
hierarchy guessing
first Player
nearest Actor
global registries
silent first Output
silent first layout writer
```

---

## 20. Historical certification status

The 2026-09-12 Full Camera run remains valid dated evidence:

```text
[QA_CAMERA_FULL]
39/39 PASS
ADR-026 phases = 2/2
9/9 dimensions
```

It certified the contract that existed at execution time, including the old viewport-bearing
split topology and global PlayerInput split-screen rejection while Camera owned viewport.

After IF-ADR-026/027 reopening and IF-ADR-028 acceptance, that run is **historical evidence**,
not current certification of the corrected boundary.

Unchanged evidence such as Subject occurrence safety, shared Camera behavior, ordinary
per-Player request removal, multi-output isolation, generic arbitration and negative request
integrity remains useful regression evidence, but the corrected aggregate must execute again.

---

## 21. Implementation sequence and current status

Do not treat the Camera architecture as closed until every remaining cut is implemented and tested:

```text
CAMERA-026-H2 — IMPLEMENTED / TECHNICALLY CERTIFIED 2026-09-13
  allow a strict subset of available Outputs to participate

CAMERA-026-I
  reconcile Player→Camera Subject integration boundary

CAMERA-027-D2
  author logical View→Output association without viewport

CAMERA-028-A — IMPLEMENTED / TECHNICALLY CERTIFIED 2026-09-13
  separate Output availability from active participation

CAMERA-028-B
  remove viewport/layout ownership from Camera topology/runtime

CAMERA-028-C
  introduce explicit Output Presentation / Layout authority

CAMERA-028-D
  integrate PlayerInputManager as one selectable layout authority
```

Then update package tests, integrated QA and official Samples/FIRSTGAME.

---

## 22. Corrected QA obligations

The replacement Camera aggregate must prove at least:

```text
N available Outputs + strict subset associated          PASS
unassociated available Output                           PASS
binding references unavailable Output                   explicit FAIL
conflicting Views target same Output                    explicit FAIL
Camera View→Output topology carries no viewport         PASS
one selected layout writer                              PASS
conflicting layout writers                              explicit FAIL
custom layout ownership cleanup                         PASS
PlayerInputManager selected as layout authority          PASS
PlayerInput layout does not alter Camera identities     PASS
force-default does not take layout ownership            PASS
generic request arbitration regression                  PASS
Subject occurrence/replacement/leave regression         PASS
```

Replace the old single `viewportSplitTopology` dimension with distinct proof for:

```text
viewOutputAssociation
outputParticipation
layoutAuthority
playerInputLayoutIntegration
```

---

## 23. Consumer/Sample guidance during reconciliation

The Getting Started migration remains useful evidence for:

```text
typed Camera View Definition
typed Camera Output Definition
typed Camera Rig Behavior Definition
explicit Actor Camera Subject
physical Camera Output binding
```

Do not use its current viewport-bearing `CameraSharedComposition` serialization as the
future contract.

Final CAMERA-027-F consumer closure occurs only after the corrected runtime/authoring/layout
cuts are implemented and recertified.

Until then:

```text
preserve existing working sample behavior
avoid adding new dependencies on Camera-owned viewport
avoid proliferating CameraViewOutputPolicyAuthoring for layout
avoid treating every available Output as necessarily active
```

---

## 24. Implementation-planning checklist

Before starting the code cuts, confirm the plan preserves:

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

Use IF-ADR-026, IF-ADR-027 and IF-ADR-028 together as the normative baseline for the next
implementation work.
