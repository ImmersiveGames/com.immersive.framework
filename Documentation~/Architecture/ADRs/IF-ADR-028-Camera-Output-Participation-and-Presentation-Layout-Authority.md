# IF-ADR-028 — Camera Output Participation and Presentation Layout Authority

Status: **Accepted architecture — CAMERA-028-A implemented and technically certified; corrected layout implementation incomplete**

Proposed: **2026-09-12**  
Accepted: **2026-09-12**  
Type: architecture / Camera output / presentation layout / integration  
Extends: corrected IF-ADR-026 and IF-ADR-027  
Preserves: IF-ADR-004 request arbitration and output-owned Default semantics; IF-ADR-022 rig materialization  
Supersedes: viewport-bearing Camera View→Output topology and Camera-owned screen-rectangle authority introduced by the original CAMERA-026-H / CAMERA-027-D boundary  
Implementation: **partial — CAMERA-028-A implemented; CAMERA-028-B/C/D pending**

Technical certification: **partial — CAMERA-028-A certified 2026-09-13; overall ADR certification pending**

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

The Framework must support configurations such as:

```text
4 physical Outputs available, only Main currently used
2 Outputs used for split-screen
Spectator Output available but inactive
Output rendered to RenderTexture rather than a screen viewport
PlayerInputManager selected as split-layout authority
custom game layout selected instead of PlayerInputManager
```

## 2. Decision

> **Camera Output availability, Camera View participation and physical presentation layout are independent authorities.**

The architecture separates:

```text
CAMERA DOMAIN
  Subject
  Assignment
  View
  Rig / presentation behavior
  Output
  request arbitration
  logical View→Output association

INTEGRATION
  Player→Camera Subject adapter
  PlayerInput→layout adapter
  Transition→force-default integration

PRESENTATION / LAYOUT
  Output→screen region
  Output→RenderTexture
  Output→display
  split-screen policy
  PiP / spectator layout
```

## 3. Camera Output availability

A physical Camera Output is explicit capacity available to the Session.

```text
Session available Outputs -> 1..N
```

Each Output retains exact physical authority:

```text
CameraOutputId
Unity Camera
CinemachineBrain
Default Camera Rig
CameraOutputContext
CameraOutputSession
```

Registering an Output means only that the physical destination is available. It does not mean a View must currently feed it, that it must occupy screen space, or that it corresponds to a Player.

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
Unity display index
PiP rectangle
spectator window placement
PlayerInput split-screen rectangle
```

## 6. Output Presentation / Layout authority

A separate presentation authority decides how an Output is exposed to a physical presentation surface.

```text
available Camera Output
        ↓
Output Presentation / Layout policy
        ↓
physical presentation target
```

Possible targets include fullscreen viewport, split-screen viewport, RenderTexture, target display, PiP region, spectator display or no current visible screen region.

A layout policy may reference an exact Camera Output identity, but it must not become Camera Subject, Assignment, View or request-arbitration authority.

## 7. Single-writer rule

Physical layout state must have exactly one active authority for a given presentation target.

For example, `UnityEngine.Camera.rect` must not be simultaneously owned by Framework Camera topology and `PlayerInputManager` automatic split-screen.

> **One physical presentation property has one active writer.**

Authority selection must be explicit and inspectable. Silent last-writer-wins behavior is rejected.

## 8. PlayerInputManager integration

`PlayerInputManager` may manage split-screen layout when explicitly selected as the layout authority.

In that mode:

```text
PlayerInputManager
  owns viewport partitioning

Framework Camera
  owns explicit Camera Outputs
  owns logical View→Output association
  owns request arbitration
  owns Rig presentation behavior
```

`PlayerInputManager` must not implicitly create Framework Output identities or decide Camera Subject assignment.

A custom Framework/game layout policy may instead own screen rectangles while PlayerInput automatic split-screen is disabled.

Therefore the old global rule that automatic PlayerInput split-screen is always invalid while Framework Camera topology exists is superseded.

Correct rule:

```text
selected layout authority = PlayerInputManager
  -> PlayerInputManager writes layout

selected layout authority = Framework/game layout policy
  -> that policy writes layout

multiple selected writers
  -> block explicitly
```

## 9. Player count independence

Player count does not define Camera Output count or layout by itself.

Valid examples:

```text
1 Player, 1 shared Output
2 Players, 1 shared Output
2 Players, 2 split Outputs
4 Players, 2 team Outputs
0 Players, 1 fixed cinematic Output
2 Players, Main + unused Spectator Output
```

A higher-level integration policy may react to join/leave, but Camera core never infers topology from Player count.

## 10. Transition / force-default boundary

IF-ADR-004 Default and force-default semantics remain valid.

Force-default answers which Rig presentation an Output temporarily uses. It does not decide where that Output appears on screen.

```text
Transition -> force Default Rig
```

remains independent from:

```text
Output -> viewport/display layout
```

Transition does not acquire screen-layout ownership merely because it forces Default presentation.

## 11. Request arbitration boundary

`CameraOutputContext` continues to arbitrate normal Camera requests for an Output.

```text
scope       -> ownership / lifetime context
precedence  -> deterministic arbitration policy
```

Scope is not precedence. Activity/Route/Session default values may remain authoring conventions; they are not hard-coded Camera-domain semantic hierarchy.

Layout is not request precedence evidence.

## 12. Authoring model

Normal Camera authoring under IF-ADR-027 remains:

```text
Camera Subject
Camera View Definition
Camera Rig Behavior Definition
Camera Output Definition
logical View→Output association
```

Layout authoring is separate:

```text
Output Presentation / Layout policy
  references explicit Output(s)
  owns presentation surface/region intent
```

The exact reusable layout-definition product type is not frozen by this ADR. Runtime authority must be proven before adding unnecessary authoring assets.

## 13. Runtime restrictions

Camera core must not:

```text
write screen layout merely because an Output exists
require every available Output to have a View binding
infer split layout from Player count
use Player index as Output identity
use Camera.main
search by name/tag/hierarchy
silently disable another layout writer
silently accept multiple layout writers
```

Layout integration must not select Subjects, select request winners, materialize Rigs, create implicit Output identity or become Player Session authority.

## 14. Implementation cuts

### CAMERA-028-A — Output availability versus participation
Status: **implemented / technically certified — 2026-09-13**.

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
canonical Shared baseline Built / Verified              PASS / PASS
canonical Shared baseline Restored / RestoredAfterRun   PASS / PASS
```

This certification covers only Output participation. It does not certify viewport removal,
layout authority or PlayerInput layout integration.

### CAMERA-028-B — Remove viewport from Camera topology
Status: **pending**.

Required result:

```text
CameraViewOutputBinding = View identity + Output identity
CameraViewOutputTopology contains no viewport authority
Camera association projection contains no viewport authority
Camera runtime stops owning layout restoration through binding lifetime
```

Stale serialized viewport fields must be removed without silent dual authority.

### CAMERA-028-C — Explicit Output Presentation / Layout authority
Status: **pending**.

Introduce the minimum typed runtime boundary needed to express one selected layout authority and deterministic application to explicit Outputs.

Requirements:

```text
explicit owner
explicit Output references
single-writer validation
deterministic application
cleanup releases/restores only owned state
no global lookup
no implicit Output creation
```

No generic singleton/layout manager is accepted.

### CAMERA-028-D — PlayerInput split-layout integration
Status: **pending**.

Provide an explicit path where `PlayerInputManager` automatic split-screen may be selected as layout authority without becoming Camera topology authority.

Join/leave may change layout through that integration without implicitly changing Framework Output identity.

## 15. QA obligations

The corrected boundary must prove at least:

```text
4 available Outputs, 1 associated                         PASS
unassociated available Output                             PASS
binding references unavailable Output                     explicit FAIL
two Views target same Output                              explicit FAIL
Camera topology snapshot contains no screen rectangle     PASS
two layout writers target same property                   explicit FAIL
custom layout applies/releases only owned state           PASS
PlayerInputManager selected as layout authority            PASS
PlayerInput layout does not select Subjects/requests      PASS
generic Camera arbitration regression                     PASS
force-default changes Rig without taking layout ownership PASS
```

The historical QA dimension `viewportSplitTopology` must be replaced by separate dimensions for:

```text
viewOutputAssociation
outputParticipation
layoutAuthority
playerInputLayoutIntegration
```

## 16. Historical certification disposition

The 2026-09-12 Full Camera QA `39/39` run remains valid evidence that the previous viewport-bearing implementation behaved according to its then-current contract.

It does not certify this ADR because this ADR changes that contract.

```text
architecture decision     ACCEPTED
implementation            PARTIAL — CAMERA-028-A implemented; CAMERA-028-B/C/D pending
technical certification   PARTIAL — CAMERA-028-A certified 2026-09-13
consumer proof             PENDING final corrected layout migration
```

IF-ADR-028 is not fully implemented or fully certified. The overall corrected layout
implementation remains incomplete until CAMERA-028-B, CAMERA-028-C and CAMERA-028-D are
implemented and certified.

## 17. Rejected alternatives

Rejected:

```text
keep viewport inside CameraViewOutputBinding
force every physical Output to participate
let Camera and PlayerInputManager both write Camera.rect
make Player count automatically define Camera topology
make PlayerInputManager Camera authority
create a global Camera/Layout singleton
silently pick the first available layout writer
```

## 18. Consequences

The Framework keeps explicit Camera Output identity and deterministic Camera behavior while allowing multiple presentation strategies.

The corrected architecture supports shared multiplayer Camera, split-screen, spectator capacity, PiP, RenderTexture feeds, multiple displays, replay/debug Outputs, PlayerInput-managed layout and custom game-managed layout without coupling Camera topology to Player count or screen rectangle ownership.
