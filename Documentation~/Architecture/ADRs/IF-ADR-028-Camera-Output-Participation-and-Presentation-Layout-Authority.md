# IF-ADR-028 — Camera Output Participation and Physical Presentation Ownership

Status: **Accepted architecture — CAMERA-028-A/B implemented and technically certified; physical presentation ownership corrected on 2026-09-15**

Proposed: **2026-09-12**  
Accepted: **2026-09-12**  
Corrected: **2026-09-15**  
Type: architecture / Camera output / physical presentation / integration  
Extends: corrected IF-ADR-026 and IF-ADR-027  
Preserves: IF-ADR-004 request arbitration and output-owned Default semantics; IF-ADR-022 rig materialization  
Supersedes: viewport-bearing Camera View→Output topology and any Framework-owned screen-rectangle authority  
Implementation: **partial — CAMERA-028-A/B implemented; the 2026-09-15 CUT 4C Framework `Camera.rect` writer is architecturally superseded and must be removed/reconciled; CAMERA-028-D pending**

Technical certification: **partial — CAMERA-028-A certified 2026-09-13 and revalidated 2026-09-14; CAMERA-028-B certified 2026-09-14; corrected physical-presentation boundary not yet certified**

Historical evidence: [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)

## 1. Context

The first multi-output implementation correctly introduced explicit physical Outputs and explicit View→Output bindings, but coupled three different concerns:

```text
physical Output exists
      ==
Output must participate in current View topology
      ==
Camera owns the Output screen rectangle
```

That equivalence is rejected.

A later 2026-09-15 audit clarified an additional ownership boundary:

> The Framework operates only the Camera infrastructure it needs for Default and Player Camera behavior. It is not a general physical Camera compositor.

Physical presentation properties such as viewport rectangle, display and render target belong to the system that presents that Camera: Unity serialized/gameplay state, `PlayerInputManager` for Player split-screen, or another explicit gameplay-owned presentation system.

The Framework must support configurations such as:

```text
one shared Framework Camera for multiple Players
Player Cameras whose split-screen layout is owned by PlayerInputManager
Framework Camera using a rect already authored by gameplay
custom gameplay-owned PiP / spectator / RenderTexture cameras outside Framework Camera ownership
```

## 2. Decision

> **Camera Output availability, Camera View participation and physical presentation are independent concerns, and physical presentation layout is external to Framework Camera authority.**

The architecture separates:

```text
FRAMEWORK CAMERA DOMAIN
  Subject
  Assignment
  View
  Rig / presentation behavior
  Output identity required by Framework
  request arbitration
  logical View→Output association

FRAMEWORK INTEGRATION
  Player→Camera Subject adapter
  Player Camera→PlayerInput integration
  Transition→force-default integration

EXTERNAL PHYSICAL PRESENTATION
  Unity serialized / gameplay Camera.rect
  PlayerInputManager split-screen Camera.rect
  gameplay-owned display / RenderTexture / PiP / spectator composition
```

The Framework may reference and operate an explicit Unity Camera as a Camera Output, but that does not grant the Framework ownership of the Camera's physical presentation properties.

## 3. Camera Output availability

A physical Camera Output is explicit Camera capacity required by the Framework runtime.

```text
Session available Framework Outputs -> 1..N
```

Each Output retains exact Framework Camera authority:

```text
CameraOutputId
Unity Camera reference
CinemachineBrain
Default Camera Rig
CameraOutputContext
CameraOutputSession
```

Registering an Output means only that this Camera is available for Framework Camera operation. It does not mean a View must currently feed it, that the Framework owns its viewport/display/render target, or that it corresponds to a Player.

Gameplay-only Cameras that do not participate in Default or Player Camera behavior do not need to become Framework Camera Outputs merely because they exist in the scene.

Duplicate physical Output identity remains blocking.

## 4. Active View→Output participation

The logical Camera topology associates Views only with Outputs that currently participate.

```text
available Outputs       = {A, B, C, D}
current Camera bindings = {Gameplay -> A}
```

This is valid.

Normative cardinality:

```text
available physical Outputs       -> 1..N
current View→Output associations  -> 0..N subset of available Outputs
one Output                        -> at most one View per snapshot
one View                          -> 0..N explicit Outputs
```

The rule below is explicitly rejected:

```text
binding count == available physical Output count
```

A binding to an unavailable Output must still fail. Two conflicting Views targeting the same Output must still fail.

## 5. Camera View→Output binding

The Camera binding contract contains only Camera-domain identity:

```text
CameraViewId
CameraOutputId
```

It does not contain physical layout.

Outside Camera View→Output topology:

```text
Rect viewport
screen partition index
safe-area region
RenderTexture
target display
PiP rectangle
spectator window placement
PlayerInput split-screen rectangle
```

## 6. Physical presentation ownership

The Framework does not own the physical presentation layout of a Camera Output.

The effective presentation may come from:

```text
Unity / authored Camera state
        OR
gameplay-owned presentation code
        OR
PlayerInputManager automatic split-screen for Player Cameras
```

Normative rule:

```text
Framework resolves/operates the Camera
External presentation authority decides where/how that Camera renders
```

The Framework must preserve externally supplied physical presentation state unless an accepted integration contract explicitly delegates a non-layout Camera responsibility to the Framework.

The Framework must not introduce a generic `Camera.rect`, `pixelRect`, display or RenderTexture policy merely because a Camera is registered as an Output.

## 7. Single-writer rule

Physical presentation state must have exactly one external active authority for a given property/Camera lifetime.

For `UnityEngine.Camera.rect`:

```text
non-Player/default authored Camera -> Unity serialized state or gameplay owner
Player split-screen Camera          -> PlayerInputManager when split-screen is active
custom gameplay Camera              -> gameplay-owned system
Framework Camera                    -> never the rect writer
```

> **The Framework must not become a competing physical-presentation writer.**

Silent last-writer-wins behavior is rejected.

The Framework may validate an integration configuration when a supported external authority requires it, but validation does not transfer property ownership to the Framework.

## 8. PlayerInputManager integration

`PlayerInputManager` owns split-screen viewport layout when its automatic split-screen feature is used.

In that mode:

```text
PlayerInputManager
  owns Camera.rect partitioning and recomposition

Framework Camera
  owns explicit Camera Output identity needed by Framework
  owns logical View→Output association
  owns request arbitration
  owns Rig presentation behavior
  may integrate the appropriate Player Camera with PlayerInput
  does not rewrite the rect produced by PlayerInputManager
```

`PlayerInputManager` must not implicitly create Framework Output identities or decide Camera Subject assignment.

The previous global rejection of automatic PlayerInput split-screen was a temporary incompatibility caused by the old Camera-owned viewport model. CAMERA-028-D must replace that rejection with an explicit supported integration path, without adding a second rect writer.

## 9. Player count independence

Player count does not define Camera Output count or Framework Camera topology by itself.

Valid examples:

```text
1 Player, 1 shared Output
2 Players, 1 shared Output
2 Players, 2 Player Cameras whose rects are owned by PlayerInputManager
4 Players, 2 gameplay-defined team Cameras
0 Players, 1 fixed Default Camera
```

A higher-level Player integration may react to join/leave. Camera core never infers Output topology from Player count, and Framework Camera never computes split-screen rectangles from Player count.

## 10. Transition / force-default boundary

IF-ADR-004 Default and force-default semantics remain valid.

Force-default answers which Rig presentation an Output temporarily uses. It does not decide where that Camera appears on screen.

```text
Transition -> force Default Rig
```

remains independent from:

```text
external Camera viewport/display/render-target state
```

Transition never acquires physical-layout ownership merely because it forces Default presentation.

## 11. Request arbitration boundary

`CameraOutputContext` continues to arbitrate normal Camera requests for an Output.

```text
scope       -> ownership / lifetime context
precedence  -> deterministic arbitration policy
```

Scope is not precedence. Activity/Route/Session default values may remain authoring conventions; they are not hard-coded Camera-domain semantic hierarchy.

Physical presentation state is not request precedence evidence.

## 12. Authoring model

Normal Camera authoring under IF-ADR-027 remains:

```text
Camera Subject
Camera View Definition
Camera Rig Behavior Definition
Camera Output Definition
logical View→Output association
```

Framework Camera authoring must not add viewport/display/render-target policy to those contracts.

Physical layout is authored/managed by the external owner:

```text
Unity Camera serialized state
Gameplay presentation code/data
PlayerInputManager split-screen configuration
```

No reusable Framework Output Layout asset is required or accepted by this ADR.

## 13. Runtime restrictions

Framework Camera must not:

```text
write Camera.rect
write Camera.pixelRect
write Camera.targetDisplay
write Camera.targetTexture
write screen layout merely because an Output exists
require every available Output to have a View binding
infer split layout from Player count
use Player index as Output identity
use Camera.main
search by name/tag/hierarchy
silently overwrite externally authored Camera presentation state
```

PlayerInput integration must not select Subjects, select Camera request winners, materialize Rigs, create implicit Output identity or become Player Session authority.

## 14. Implementation cuts

### CAMERA-028-A — Output availability versus participation
Status: **implemented / technically certified — 2026-09-13; revalidated 2026-09-14**.

Required result:

```text
CameraOutputSessionTopology registers 1..N available Outputs
CameraViewOutputTopology may bind a strict subset
no binding-count equality requirement
unassociated Output remains valid
missing referenced Output still blocks
conflicting duplicate Output binding still blocks
```

Certification evidence:

```text
available physical Outputs                              2
participating View→Output associations                  1
Partial runtime proof                                   8/8 PASS
available unassociated Output                           PASS
Player count 0 → 1 → 0 creates no implicit association PASS
unavailable Output rejection                            PASS
Output A/B arbitration isolation                        PASS
CAMERA-028-A focused orchestrator                       PASS
outputParticipation                                     PASS
canonical Shared baseline Built / Verified              PASS / PASS
canonical Shared baseline Restored / RestoredAfterRun   PASS / PASS
```

This certification covers Output participation. It does not certify PlayerInput layout integration.

### CAMERA-028-B — Remove viewport from Camera topology
Status: **implemented / technically certified — 2026-09-14**.

Implemented result:

```text
CameraViewOutputBinding = View identity + Output identity
CameraViewOutputTopology contains no viewport authority
Camera association authoring contains no viewport authority
Camera runtime no longer captures, applies or restores Camera.rect through binding lifetime
CameraViewport removed from the productive Camera contract
```

Technical certification evidence:

```text
Persistent Camera Presentation Composition regression   12/12 PASS
retired invalid viewport case                            invalid-viewport:SupersededByCAMERA028B
Partial Output participation revalidation               8/8 PASS
viewOutputAssociation                                    PASS
Full Camera established cases                            39/39 PASS
ADR-026 runtime phases                                   2/2 PASS
current Full Camera dimensions                           8/8 PASS
viewportSplitTopology                                    absent from active certification
canonical Shared baseline restore                        PASS
```

The `8/8` Full Camera dimension count is intentional: the obsolete `viewportSplitTopology` dimension was removed rather than renamed.

### CAMERA-028-C — External physical-presentation ownership boundary
Status: **architecture corrected 2026-09-15; implementation reconciliation pending**.

Required result:

```text
Framework has zero productive writers of Camera.rect
Framework has zero productive writers of Camera.pixelRect / targetDisplay / targetTexture
registered Output preserves externally supplied physical Camera presentation state
no Framework Output Presentation/Layout policy or authoring asset is required
no global lookup or implicit Camera creation
```

The 2026-09-15 CUT 4C implementation that introduced `CameraOutputPresentationRuntime` and a Framework `Camera.rect` writer is technically coherent with its superseded design, but violates this corrected ownership boundary. It must not be certified as CAMERA-028-C and must be removed or reconciled before this cut can close.

### CAMERA-028-D — PlayerInput split-layout integration
Status: **pending**.

Provide an explicit path where Player Cameras are integrated with `PlayerInput` / `PlayerInputManager` while `PlayerInputManager` remains the sole writer of split-screen `Camera.rect`.

Join/leave may change layout through Unity's PlayerInput integration without implicitly changing Framework Output identity, Camera Subject assignment or request arbitration.

## 15. QA obligations

The corrected boundary must prove at least:

```text
strict-subset Output association                          PASS — CAMERA-028-A
unassociated available Output                             PASS — CAMERA-028-A
binding references unavailable Output                     explicit FAIL proven
two Views target same Output                              explicit FAIL proven
Camera topology snapshot contains no screen rectangle     PASS — CAMERA-028-B
Framework productive Camera.rect writers                  0 — PENDING CAMERA-028-C reconciliation
external authored rect preserved by Framework             PENDING CAMERA-028-C
PlayerInputManager is sole split-screen rect writer       PENDING CAMERA-028-D
PlayerInput layout does not select Subjects/requests      PENDING CAMERA-028-D
generic Camera arbitration regression                     PASS
force-default changes Rig without layout ownership        retained regression evidence
```

The historical QA dimension `viewportSplitTopology` remains removed from the active corrected Camera aggregate.

Future proof should use:

```text
physicalPresentationNonOwnership
playerInputLayoutIntegration
```

rather than reviving or renaming the obsolete Camera-owned viewport contract.

## 16. Historical and current certification disposition

The 2026-09-12 Full Camera QA `39/39` run remains valid evidence that the previous viewport-bearing implementation behaved according to its then-current contract.

The 2026-09-14 corrected Camera run establishes the post-CAMERA-028-B logical association boundary:

```text
Persistent structural regression   12/12 PASS
CAMERA-028-A Partial                8/8 PASS
Full Camera established cases       39/39 PASS
ADR-026 phases                      2/2 PASS
current dimensions                  8/8 PASS
viewOutputAssociation               PASS
outputParticipation                 PASS
viewportSplitTopology               REMOVED
Shared baseline restore             PASS
```

The 2026-09-15 CUT 4C static implementation evidence is not certification because its Framework-owned `Camera.rect` authority is superseded by this correction.

```text
architecture decision     ACCEPTED / CORRECTED 2026-09-15
implementation            PARTIAL — CAMERA-028-A/B current; CAMERA-028-C code reconciliation + CAMERA-028-D pending
technical certification   PARTIAL — CAMERA-028-A/B certified through 2026-09-14
consumer proof             PENDING Player Camera integration and final consumer closure
```

IF-ADR-028 is not fully implemented or fully certified.

## 17. Rejected alternatives

Rejected:

```text
keep viewport inside CameraViewOutputBinding
force every physical Output to participate
make Framework a general Camera.rect/layout authority
let Framework and PlayerInputManager both write Camera.rect
make Player count automatically define Camera topology
make PlayerInputManager Camera Subject/Assignment authority
register every gameplay Camera as a Framework Output
create a global Camera/Layout singleton
silently overwrite gameplay-authored physical Camera state
```

## 18. Consequences

The Framework keeps explicit Camera Output identity and deterministic Camera behavior only for Cameras it needs to operate, while physical presentation remains owned by the game/Unity integration that presents those Cameras.

The corrected architecture supports shared multiplayer Camera and PlayerInput-managed split-screen without turning Framework Camera into a generic screen compositor. Gameplay-specific PiP, spectator, replay, RenderTexture, secondary-display and similar Cameras remain gameplay responsibilities unless a future accepted requirement explicitly brings a narrowly defined integration into Framework scope.