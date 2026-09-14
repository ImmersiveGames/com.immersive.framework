# Camera Output Participation and Layout Authority Reconciliation — 2026-09-12

Status: **Architecture reconciled; CUT 1 and CUT 2 implemented/certified; remaining layout/integration cuts pending**

Affected decisions: IF-ADR-026, IF-ADR-027 and IF-ADR-028  
Historical evidence: [Camera Full Technical Certification — 2026-09-12](IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)  
Execution mapping clarified: **2026-09-13**  
CUT 1 certification: **2026-09-13**  
CUT 2 certification: **2026-09-14**

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

The current Player-backed Subject projection still requires CAMERA-026-I reconciliation without singleton, service locator, global registry or silent lookup.

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

No `layoutAuthority` or `playerInputLayoutIntegration` PASS is claimed. Those dimensions remain future work under CAMERA-028-C and CAMERA-028-D.

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
  evidence: 2 available Outputs / 1 participating association / 8/8 PASS
  cleanup: focused orchestrator restored canonical Shared baseline

CUT 2
CAMERA-028-B — remove viewport from Camera runtime topology
CAMERA-027-D2 — reconcile View→Output authoring without viewport
  coordinated runtime + authoring migration
  status: IMPLEMENTED / TECHNICALLY CERTIFIED — 2026-09-14
  evidence: structural 12/12; Full Camera 39/39; ADR-026 2/2; dimensions 8/8
  active dimensions: viewOutputAssociation + outputParticipation
  obsolete viewportSplitTopology removed, not renamed

CUT 3
CAMERA-026-I — Player→Camera Subject integration boundary
  move Camera publication responsibility to the Camera/integration side
  preserve Player-domain evidence and Player lifetime authority
  status: PENDING

CUT 4
CAMERA-028-C — explicit Output Presentation / Layout authority
  introduce the minimum typed single-writer layout boundary
  status: PENDING

CUT 5
CAMERA-028-D — PlayerInputManager layout integration
  allow PlayerInput-managed split layout without making PlayerInput Camera topology authority
  status: PENDING

CUT 6
Camera QA layout recertification
  add direct layoutAuthority and playerInputLayoutIntegration proof after CUT 4/5
  status: PENDING

CUT 7
CAMERA-027-F — official Samples/FIRSTGAME consumer closure
  only after the remaining corrected runtime/authoring/layout boundary is certified
  status: PENDING
```

`CAMERA-026-H2` remains in IF-ADR-026 because it states the corrected topology obligation owned by that ADR. `CAMERA-028-A` is the implementation vehicle for that obligation.

`CAMERA-027-D2` is not an alias for `CAMERA-028-B`: D2 owns the product-authoring change while 028-B owns the runtime/topology change. They were implemented and certified together so no viewport-bearing dual authority survives between authoring and runtime.

## QA replacement status

The corrected Camera QA now directly distinguishes:

```text
viewOutputAssociation   PASS
outputParticipation     PASS
layoutAuthority         PENDING CAMERA-028-C
playerInputLayoutIntegration PENDING CAMERA-028-D
```

The active Full Camera aggregate no longer contains `viewportSplitTopology`. Its corrected dimension count is `8/8`, while the 39 established generic/arbitration cases remain `39/39`.

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
one selected layout writer                                     CAMERA-028-C
conflicting layout writers                                     CAMERA-028-C
custom layout applies/releases only owned state                CAMERA-028-C
PlayerInputManager selected as layout authority                CAMERA-028-D
PlayerInput layout does not select Subjects/requests           CAMERA-028-D
```

## Consumer proof

The Getting Started migration remains useful evidence for typed View, Output and Rig Behavior definitions and explicit Camera Subject authoring. Final CAMERA-027-F closure waits for implementation and QA of the remaining corrected boundary.

For Player-driven split-screen, future consumer proof must demonstrate the intended responsibility split:

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

Current state:

```text
CAMERA-028-A / CAMERA-026-H2                 COMPLETE / TECHNICALLY CERTIFIED
CAMERA-028-B + CAMERA-027-D2                 COMPLETE / TECHNICALLY CERTIFIED
CAMERA-026-I                                 PENDING
CAMERA-028-C                                 PENDING
CAMERA-028-D                                 PENDING
corrected Camera logical QA recertification  COMPLETE for CUT 1/2
layout-authority QA recertification          PENDING CUT 4/5
CAMERA-027-F official consumer proof         PENDING
```

This reconciliation remains open until the remaining integration/layout cuts and consumer closure are complete. Current documentation must not describe the superseded viewport-bearing Camera topology as normative.
