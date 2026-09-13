# Camera Output Participation and Layout Authority Reconciliation — 2026-09-12

Status: **Architecture reconciled; implementation and recertification pending**  
Affected decisions: IF-ADR-026, IF-ADR-027 and IF-ADR-028  
Historical evidence: [Camera Full Technical Certification — 2026-09-12](IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)

## Audit finding

The post-certification audit found two incorrect couplings in the first multi-output implementation:

```text
registered physical Output == Output that must have a View binding
View→Output binding == owner of physical screen layout
```

Both are superseded.

The corrected boundary is:

```text
available physical Outputs      -> 1..N
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

```text
CAMERA-026-H2  partial Output participation
CAMERA-026-I   Player→Camera Subject integration boundary
CAMERA-027-D2  View→Output authoring without viewport
CAMERA-028-A   available Output vs active participation
CAMERA-028-B   remove viewport from Camera topology
CAMERA-028-C   explicit Output Presentation / Layout authority
CAMERA-028-D   PlayerInputManager layout integration
```

## Required QA replacement

The next aggregate must retain unaffected regressions and distinguish:

```text
viewOutputAssociation
outputParticipation
layoutAuthority
playerInputLayoutIntegration
```

Minimum new proof includes: strict-subset Output association PASS; unassociated Output PASS; unavailable Output reference FAIL; conflicting Views to one Output FAIL; no viewport in Camera topology PASS; one selected layout writer PASS; conflicting layout writers FAIL; PlayerInput layout isolation PASS; force-default without layout ownership PASS.

## Consumer proof

The Getting Started migration remains useful evidence for typed View, Output and Rig Behavior definitions and explicit Camera Subject authoring. Final CAMERA-027-F closure waits for implementation and QA of the corrected boundary.

## Closure condition

This reconciliation closes only after CAMERA-026-H2, CAMERA-026-I, CAMERA-027-D2 and CAMERA-028-A/B/C/D are implemented, recertified and reflected in official Samples and current documentation.