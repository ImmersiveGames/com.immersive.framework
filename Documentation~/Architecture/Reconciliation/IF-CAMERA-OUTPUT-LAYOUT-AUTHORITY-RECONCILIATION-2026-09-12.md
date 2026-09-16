# Camera Output Participation and Physical Presentation Ownership Reconciliation — 2026-09-12 / corrected 2026-09-15 / updated 2026-09-16

Status: **Architecture reconciled; CUT 1/2/3 implemented and technically certified; CUT 4 code reconciliation and CUT 5 PlayerInput integration implemented; focused physical-presentation / automatic split-screen certification and final consumer closure remain pending**

Affected decisions: IF-ADR-026, IF-ADR-027 and IF-ADR-028
Historical evidence: [Camera Full Technical Certification — 2026-09-12](IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
Current certification: [Camera Full Technical Certification — 2026-09-16](IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-16.md)
Execution mapping clarified: **2026-09-13**
CUT 1 certification: **2026-09-13**
CUT 2 certification: **2026-09-14**
Physical-presentation ownership corrected: **2026-09-15**
CUT 3 certification / CUT 4-5 implementation reconciliation: **2026-09-16**

## Audit finding

The post-certification audit found two incorrect couplings in the first multi-output implementation:

```text
registered physical Output == Output that must have a View binding
View→Output binding == owner of physical screen layout
```

Both are superseded.

A later implementation audit on 2026-09-15 found a third boundary error in the planned replacement: moving viewport ownership out of View→Output topology did **not** mean the Framework should introduce a new generic `Camera.rect` layout authority.

The corrected boundary is:

```text
available Framework Outputs       -> 1..N
current View→Output associations  -> 0..N subset
Camera binding                    -> View identity + Output identity
physical Camera presentation      -> external owner
```

External physical-presentation owners include:

```text
Unity serialized / gameplay-authored Camera state
PlayerInputManager for Player split-screen
gameplay-owned custom presentation systems
```

An available Output without a current View association is valid. A binding to an unavailable Output remains invalid. Two Views targeting the same Output in one association snapshot remain invalid.

## Preserved architecture

The reconciliation does not reopen Camera Subject / Assignment / View / Rig separation, explicit physical Output identity, per-output request arbitration, output-owned Default Rig, force-default ownership, deterministic request arbitration or `CameraRigComposer` materialization authority.

Ordinary Player gameplay continues to publish zero Camera requests.

Scope and precedence remain separate:

```text
scope      -> ownership / lifetime
precedence -> arbitration policy
```

## Player integration boundary

Player Actors may contribute Camera Subjects, but PlayerParticipation core must not become Camera topology authority.

Current productive direction:

```text
Player Actor / Presentation occurrence evidence
      ↓
PlayerActorCameraSubjectIntegrationRuntime
      ↓
Camera Subject availability
```

CAMERA-026-I was technically certified by the 2026-09-16 Full Camera run.

For split-screen Player Cameras, physical viewport ownership remains with Unity Input System:

```text
explicit Player Slot -> Camera Output policy
      ↓
PlayerCameraOutputIntegrationRuntime
      ↓
PlayerInput.camera
      ↓
PlayerInputManager owns Camera.rect layout
```

Framework Camera must not rewrite that rect.

The Player Camera integration path is implemented as of 2026-09-16. Focused certification with automatic `PlayerInputManager.splitScreen` enabled remains pending.

## Certification disposition

The 2026-09-12 Full Camera QA remains valid dated evidence for the contract it executed:

```text
39/39 PASS
ADR-026 phases 2/2
9/9 dimensions
```

It remains historical evidence for the former viewport-bearing boundary.

The corrected 2026-09-14 certification established CUT 2 without relabeling that historical run:

```text
Persistent Camera Presentation Composition regression   12/12 PASS
retired viewport-only case                              invalid-viewport:SupersededByCAMERA028B
CAMERA-028-A Partial revalidation                       8/8 PASS
outputParticipation                                     PASS
Full Camera established cases                           39/39 PASS
ADR-026 phases                                          2/2 PASS
current Full Camera dimensions                          8/8 PASS
viewOutputAssociation                                   PASS
viewportSplitTopology                                   removed from active certification
canonical Shared baseline restore                       PASS
```

The 2026-09-16 Full Camera certification adds:

```text
Full Camera established cases                           39/39 PASS
ADR-026 phases                                          2/2 PASS
current Full Camera dimensions                          8/8 PASS
CAMERA-026-I SceneProvided                              PASS
Split runtime-host integration regression               8/8 PASS
canonical baseline restore                              PASS
```

No `physicalPresentationNonOwnership='PASS'` or `playerInputLayoutIntegration='PASS'` is claimed by that run. The canonical Full Camera topology builder still disables automatic PlayerInput split-screen, so focused CUT 4/5 proof remains distinct.

## Required implementation cuts

The ADR numbers below describe ownership of the decisions. They do **not** imply duplicate implementation work where two ADRs describe the same runtime correction.

Canonical execution mapping:

```text
CUT 1
CAMERA-028-A — available Output vs active participation
  satisfies CAMERA-026-H2 — partial Output participation
  one implementation cut, not two
  status: IMPLEMENTED / TECHNICALLY CERTIFIED — 2026-09-13
  revalidated: 2026-09-14

CUT 2
CAMERA-028-B — remove viewport from Camera runtime topology
CAMERA-027-D2 — reconcile View→Output authoring without viewport
  coordinated runtime + authoring migration
  status: IMPLEMENTED / TECHNICALLY CERTIFIED — 2026-09-14
  obsolete viewportSplitTopology removed, not renamed

CUT 3
CAMERA-026-I — Player→Camera Subject integration boundary
  Camera publication responsibility lives on the Camera/integration side
  consumes typed Player prepared-Actor occurrence evidence
  preserves Player-domain evidence and Player lifetime authority
  status: IMPLEMENTED / TECHNICALLY CERTIFIED — 2026-09-16

CUT 4
CAMERA-028-C — external physical-presentation ownership boundary
  Framework must have zero productive Camera.rect/pixelRect/targetDisplay/targetTexture layout writers
  preserve gameplay/Unity supplied physical Camera presentation
  superseded CameraOutputPresentationRuntime writer removed/reconciled
  status: IMPLEMENTATION RECONCILED — 2026-09-16; focused runtime preservation certification PENDING

CUT 5
CAMERA-028-D — PlayerInputManager layout integration
  explicit Player Slot -> Camera Output policy implemented
  integrate Player Camera with PlayerInput/PlayerInputManager
  PlayerInputManager remains sole split-screen Camera.rect writer
  Framework remains Camera topology/rig authority only
  status: IMPLEMENTED / EXPERIMENTAL — 2026-09-16; focused automatic split-screen certification PENDING

CUT 6
Camera QA physical-presentation recertification
  prove physicalPresentationNonOwnership and playerInputLayoutIntegration with focused CUT 4/5 fixtures
  status: PENDING

CUT 7
CAMERA-027-F — official Samples/FIRSTGAME consumer closure
  only after the corrected physical-presentation / PlayerInput runtime boundary is focused-certified
  status: PENDING
```

`CAMERA-026-H2` remains in IF-ADR-026 because it states the corrected topology obligation owned by that ADR. `CAMERA-028-A` is the implementation vehicle for that obligation.

`CAMERA-027-D2` is not an alias for `CAMERA-028-B`: D2 owns the product-authoring change while 028-B owns the runtime/topology change. They were implemented and certified together so no viewport-bearing dual authority survives between authoring and runtime.

CAMERA-026-I is similarly distinct from CAMERA-028-D: 026-I publishes Player-backed Camera Subjects; 028-D associates explicit Player Slots with explicit Camera Outputs for PlayerInput integration. Neither responsibility grants physical-layout ownership to Framework Camera.

## QA replacement status

The corrected Camera QA currently proves:

```text
viewOutputAssociation                   PASS
outputParticipation                     PASS
CAMERA-026-I Player→Camera integration  PASS
```

The obsolete `viewportSplitTopology` dimension remains removed.

Focused corrected proof still must add:

```text
physicalPresentationNonOwnership   PENDING CAMERA-028-C focused runtime proof
playerInputLayoutIntegration       PENDING CAMERA-028-D focused runtime proof
```

Current proven boundary includes:

```text
strict-subset Output association                               PASS
unassociated available Output                                  PASS
binding references unavailable Output                          explicit rejection PASS
conflicting Views target one Output                            explicit rejection PASS
Camera topology contains no viewport                           PASS
generic Camera arbitration regression                          PASS
Player count 0 -> 1 -> 0 creates no implicit association       PASS
Output A/B arbitration isolation                               PASS
Player-backed Camera Subject SceneProvided integration         PASS
canonical Shared restore after certification                   PASS
```

Source/implementation reconciliation now includes:

```text
superseded CameraOutputPresentationRuntime removed              COMPLETE
explicit Player Slot -> Camera Output policy                    IMPLEMENTED
PlayerCameraOutputIntegrationRuntime                             IMPLEMENTED
```

Still pending focused runtime proof:

```text
Framework preserves external authored rect                     CAMERA-028-C
PlayerInputManager is sole split-screen Camera.rect writer      CAMERA-028-D
PlayerInput layout does not select Subjects/requests            CAMERA-028-D
```

## Consumer proof

The Getting Started migration remains useful evidence for typed View, Output and Rig Behavior definitions and explicit Camera Subject authoring. Final CAMERA-027-F closure waits for focused QA of the corrected physical-presentation boundary and a representative consumer integration.

For Player-driven split-screen, consumer proof must demonstrate:

```text
Framework Camera
  supplies/uses the explicit Camera required by Framework
  supplies logical View→Output association where applicable
  uses explicit Player Slot -> Camera Output identity
  does not partition the screen
  does not rewrite Camera.rect

PlayerInputManager integration
  receives the appropriate Camera for the participating Player
  owns split viewport layout
```

Shared-camera multiplayer must remain valid with multiple Players and one active Output.

Gameplay-only Cameras such as PiP, spectator, replay, RenderTexture or other custom presentation Cameras remain consumer/gameplay responsibilities unless they explicitly participate in a Framework Camera contract.

## Closure condition

Current state:

```text
CAMERA-028-A / CAMERA-026-H2                    COMPLETE / TECHNICALLY CERTIFIED
CAMERA-028-B + CAMERA-027-D2                    COMPLETE / TECHNICALLY CERTIFIED
CAMERA-026-I                                    COMPLETE / TECHNICALLY CERTIFIED — 2026-09-16
CAMERA-028-C                                    CODE RECONCILIATION COMPLETE; FOCUSED RUNTIME CERTIFICATION PENDING
CAMERA-028-D                                    IMPLEMENTED / EXPERIMENTAL; FOCUSED SPLIT-SCREEN CERTIFICATION PENDING
corrected Camera logical QA recertification     COMPLETE, including CAMERA-026-I
physical-presentation QA recertification        PENDING focused CUT 4/5
CAMERA-027-F official consumer proof            PENDING
```

This reconciliation remains open only for focused physical-presentation / PlayerInput split-screen certification and final consumer closure. Current documentation must not describe either the old viewport-bearing Camera topology or a generic Framework-owned physical layout system as normative, and it must not describe CAMERA-026-I, CAMERA-028-C code reconciliation or CAMERA-028-D implementation as still absent.
