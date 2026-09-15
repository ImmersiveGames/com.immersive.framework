# Camera Output Participation and Physical Presentation Ownership Reconciliation — 2026-09-12 / corrected 2026-09-15

Status: **Architecture reconciled; CUT 1 and CUT 2 implemented/certified; physical presentation ownership corrected; remaining code reconciliation and PlayerInput integration pending**

Affected decisions: IF-ADR-026, IF-ADR-027 and IF-ADR-028  
Historical evidence: [Camera Full Technical Certification — 2026-09-12](IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)  
Execution mapping clarified: **2026-09-13**  
CUT 1 certification: **2026-09-13**  
CUT 2 certification: **2026-09-14**  
Physical-presentation ownership corrected: **2026-09-15**

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

Target direction:

```text
Player Actor / Presentation evidence
      ↓
Camera integration adapter
      ↓
Camera Subject availability
```

For split-screen Player Cameras, physical viewport ownership remains with Unity Input System:

```text
Framework identifies/integrates the Player Camera
      ↓
PlayerInput.camera
      ↓
PlayerInputManager owns Camera.rect layout
```

Framework Camera must not rewrite that rect.

## Certification disposition

The 2026-09-12 Full Camera QA remains valid dated evidence for the contract it executed:

```text
39/39 PASS
ADR-026 phases 2/2
9/9 dimensions
```

It remains historical evidence for the former viewport-bearing boundary.

The corrected 2026-09-14 certification establishes CUT 2 without relabeling that historical run:

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

No physical-presentation ownership PASS or `playerInputLayoutIntegration` PASS is claimed by those runs.

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
  move Camera publication responsibility to the Camera/integration side
  preserve Player-domain evidence and Player lifetime authority
  implementation/static work completed separately; runtime certification remains independent

CUT 4
CAMERA-028-C — external physical-presentation ownership boundary
  Framework must have zero productive Camera.rect/pixelRect/targetDisplay/targetTexture layout writers
  preserve gameplay/Unity supplied physical Camera presentation
  remove/reconcile the superseded 2026-09-15 CameraOutputPresentationRuntime writer before certification
  status: PENDING RECONCILIATION

CUT 5
CAMERA-028-D — PlayerInputManager layout integration
  integrate Player Camera with PlayerInput/PlayerInputManager
  PlayerInputManager remains sole split-screen Camera.rect writer
  Framework remains Camera topology/rig authority only
  status: PENDING

CUT 6
Camera QA physical-presentation recertification
  prove physicalPresentationNonOwnership and playerInputLayoutIntegration after CUT 4/5
  status: PENDING

CUT 7
CAMERA-027-F — official Samples/FIRSTGAME consumer closure
  only after the corrected runtime/integration boundary is certified
  status: PENDING
```

`CAMERA-026-H2` remains in IF-ADR-026 because it states the corrected topology obligation owned by that ADR. `CAMERA-028-A` is the implementation vehicle for that obligation.

`CAMERA-027-D2` is not an alias for `CAMERA-028-B`: D2 owns the product-authoring change while 028-B owns the runtime/topology change. They were implemented and certified together so no viewport-bearing dual authority survives between authoring and runtime.

## QA replacement status

The corrected Camera QA already distinguishes:

```text
viewOutputAssociation         PASS
outputParticipation           PASS
```

The obsolete `viewportSplitTopology` dimension remains removed.

Future corrected proof must add:

```text
physicalPresentationNonOwnership   PENDING CAMERA-028-C
playerInputLayoutIntegration       PENDING CAMERA-028-D
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
canonical Shared restore after certification                   PASS
```

Still pending:

```text
Framework productive Camera.rect writer count == 0             CAMERA-028-C
Framework preserves external authored rect                     CAMERA-028-C
PlayerInputManager is sole split-screen Camera.rect writer      CAMERA-028-D
PlayerInput layout does not select Subjects/requests            CAMERA-028-D
```

## Consumer proof

The Getting Started migration remains useful evidence for typed View, Output and Rig Behavior definitions and explicit Camera Subject authoring. Final CAMERA-027-F closure waits for implementation and QA of the corrected boundary.

For Player-driven split-screen, consumer proof must demonstrate:

```text
Framework Camera
  supplies/uses the explicit Camera required by Framework
  supplies logical View→Output association where applicable
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
CAMERA-026-I                                    IMPLEMENTATION/STATIC CLOSED; RUNTIME CERTIFICATION SEPARATE
CAMERA-028-C                                    ARCHITECTURE CORRECTED; CODE RECONCILIATION PENDING
CAMERA-028-D                                    PENDING
corrected Camera logical QA recertification     COMPLETE for CUT 1/2
physical-presentation QA recertification        PENDING CUT 4/5
CAMERA-027-F official consumer proof            PENDING
```

This reconciliation remains open until the superseded Framework layout writer is reconciled, PlayerInput integration is certified and consumer closure is complete. Current documentation must not describe either the old viewport-bearing Camera topology or a generic Framework-owned physical layout system as normative.