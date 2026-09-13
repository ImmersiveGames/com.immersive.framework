# Camera Output Participation and Layout Authority Reconciliation — 2026-09-12

Status: **Architecture reconciled; Cut 1 implemented/certified; Cuts 2–7 pending**

Affected decisions: IF-ADR-026, IF-ADR-027 and IF-ADR-028  
Historical evidence: [Camera Full Technical Certification — 2026-09-12](IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)  
Execution mapping clarified: **2026-09-13**
Certification updated: **2026-09-13**

## Audit finding

The post-certification audit found two incorrect couplings in the first multi-output implementation:

```text
registered physical Output == Output that must have a View binding
View→Output binding == owner of physical screen layout
```

Both are superseded.

The corrected boundary is:

```text
available physical Outputs       -> 1..N
current View→Output associations -> 0..N subset
Camera binding                   -> View identity + Output identity
Output Presentation / Layout     -> viewport / display / RenderTexture / PiP
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

The current Player-backed Subject projection requires implementation reconciliation without singleton, service locator, global registry or silent lookup.

## Certification disposition

The earlier 2026-09-12 Full Camera QA remains valid dated evidence for the contract it executed:

```text
39/39 PASS
ADR-026 phases 2/2
9/9 dimensions
```

It is not certification of the corrected contract because the corrected contract changes Output participation, viewport ownership, PlayerInput layout integration and Player→Camera projection placement.

## Required implementation cuts

The ADR numbers below describe ownership of the decisions. They do **not** imply duplicate implementation work where two ADRs describe the same runtime correction.

Canonical execution mapping:

```text
CUT 1
CAMERA-028-A — available Output vs active participation
  satisfies CAMERA-026-H2 — partial Output participation
  one implementation cut, not two
  status: IMPLEMENTED / TECHNICALLY CERTIFIED — 2026-09-13
  evidence: 2 available Outputs / 1 participating association / 8/8 PASS
  cleanup: focused orchestrator restored canonical Shared baseline

CUT 2
CAMERA-028-B — remove viewport from Camera runtime topology
CAMERA-027-D2 — reconcile View→Output authoring without viewport
  coordinated runtime + authoring migration

CUT 3
CAMERA-026-I — Player→Camera Subject integration boundary
  move Camera publication responsibility to the Camera/integration side
  preserve Player-domain evidence and Player lifetime authority

CUT 4
CAMERA-028-C — explicit Output Presentation / Layout authority
  introduce the minimum typed single-writer layout boundary

CUT 5
CAMERA-028-D — PlayerInputManager layout integration
  allow PlayerInput-managed split layout without making PlayerInput Camera topology authority

CUT 6
Camera QA recertification
  replace the superseded viewportSplitTopology dimension

CUT 7
CAMERA-027-F — official Samples/FIRSTGAME consumer closure
  only after the corrected runtime/authoring boundary is certified
```

`CAMERA-026-H2` remains in IF-ADR-026 because it states the corrected topology obligation owned by that ADR. `CAMERA-028-A` is the implementation vehicle for that obligation.

`CAMERA-027-D2` is not an alias for `CAMERA-028-B`: D2 owns the product-authoring change while 028-B owns the runtime/topology change. They should be implemented together so no viewport-bearing dual authority survives between authoring and runtime.

## Required QA replacement

The next aggregate must retain unaffected regressions and distinguish:

```text
viewOutputAssociation
outputParticipation
layoutAuthority
playerInputLayoutIntegration
```

Minimum new proof includes:

```text
strict-subset Output association                               PASS
unassociated available Output                                  PASS
binding references unavailable Output                          explicit FAIL
conflicting Views target one Output                            explicit FAIL
Camera topology contains no viewport                           PASS
one selected layout writer                                     PASS
conflicting layout writers                                     explicit FAIL
custom layout applies/releases only owned state                PASS
PlayerInputManager selected as layout authority                PASS
PlayerInput layout does not select Subjects/requests           PASS
force-default changes Rig without taking layout ownership      PASS
generic Camera arbitration regression                          PASS
```

The historical `viewportSplitTopology` dimension must not be renamed and reused as if it proved the corrected contract. The new aggregate must test the new boundaries directly.

## Consumer proof

The Getting Started migration remains useful evidence for typed View, Output and Rig Behavior definitions and explicit Camera Subject authoring. Final CAMERA-027-F closure waits for implementation and QA of the corrected boundary.

For Player-driven split-screen, consumer proof must demonstrate the intended responsibility split:

```text
Framework Camera
  supplies explicit physical Output / Unity Camera evidence
  supplies logical View→Output association
  does not partition the screen

PlayerInputManager integration
  receives the appropriate Camera for the participating Player
  owns split viewport layout when explicitly selected as layout authority
```

Shared-camera multiplayer must remain valid with multiple Players and one active Output.

## Closure condition

This reconciliation closes only after:

```text
CAMERA-028-A / CAMERA-026-H2                 COMPLETE / TECHNICALLY CERTIFIED
CAMERA-028-B + CAMERA-027-D2                 PENDING
CAMERA-026-I                                 PENDING
CAMERA-028-C                                 PENDING
CAMERA-028-D                                 PENDING
corrected Camera QA recertification          PARTIAL — 028-A focused proof only
CAMERA-027-F official consumer proof         PENDING
```

are complete and current documentation no longer describes the superseded viewport-bearing Camera topology as normative.
