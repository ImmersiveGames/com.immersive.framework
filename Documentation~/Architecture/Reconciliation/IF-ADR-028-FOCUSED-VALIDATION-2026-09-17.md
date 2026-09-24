# IF-ADR-028 Focused Physical Presentation / PlayerInput Validation — 2026-09-17

> Historical validation only. Current normative Camera authority: [IF-ADR-032](../ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md).

Status: **IMPLEMENTED = YES / TESTED = YES / INTEGRATED = YES / VALIDATED = NO**

Scope: IF-ADR-028 CAMERA-028-C / CAMERA-028-D closure audit after the focused PlayerInput split-screen implementation.

Related:
- IF-ADR-028 — Camera Output Participation and Physical Presentation Ownership (historical boundary)
- [Camera Output Participation and Physical Presentation Ownership Reconciliation](IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)
- [Camera Full Technical Certification — 2026-09-16](IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-16.md)

## 1. Purpose

This record preserves the 2026-09-17 focused validation result without relabeling earlier Camera certification runs.

The current implementation has functional evidence for the PlayerInput split-screen path, but the certification harness does not yet prove a clean read-only starting state, cleanup participation in the terminal verdict, or a second run from the post-cleanup state without reparative preparation.

Therefore the correct disposition is:

```text
IMPLEMENTED = YES
TESTED      = YES
INTEGRATED  = YES
VALIDATED   = NO
```

## 2. Focused runtime evidence

The supplied focused CAMERA-028-D run completed:

```text
RUN 1
33/33 PASS
```

The run proves the intended functional cycle and current semantic preservation, including the explicit Player Slot -> Camera Output policy and PlayerInput-owned split-screen behavior.

Adjacent regression evidence supplied with the same validation session:

```text
CAMERA-028-C  PASS
C9R           39/39 PASS
Player Q1     39/39 PASS
Player Q2     36/36 PASS
ADR020-H      26/26 PASS
```

These results are supporting regression evidence. They do not remove the focused CAMERA-028-D harness-cleanliness requirements below.

## 3. Read-only preflight

Result: **FAIL / NOT IMPLEMENTED**

The certification coordinator calls preparation before entering Play Mode:

```text
Prepare
  -> QaCameraPersistentBaselineGuard.PrepareAndVerify(...)
  -> ConfigurePlayerInputLayoutIntegration(...)
```

There is no local read-only preflight that proves the repository/runtime QA state is already clean before any Prepare / Build / Repair mutation.

This means a successful run cannot distinguish:

```text
already clean
```

from:

```text
made clean by preparation
```

That distinction is required for reentrancy certification.

## 4. Player cleanup

Result: **PARTIAL**

Runtime evidence proves cleanup after Leave:

```text
playerCount     == 0
PlayerInput.all == 0
JoinedCount     == 0
```

This is useful runtime evidence, but there is no independent read-only preflight before the next certification cycle proving that the previous run left the environment clean.

No confirmed Player runtime residue was observed.

## 5. QA-owned device cleanup

Result: **PARTIAL / BLOCKING FOR VALIDATION**

The focused regression creates two QA-owned `Keyboard` devices and removes them idempotently from `finally`.

The remaining certification problem is ordering and observability:

```text
CompletePassed(...)
  -> terminal PASS is emitted

finally
  -> QA-owned Keyboard cleanup runs
```

Consequences:

- there is no post-cleanup device inventory before the PASS verdict;
- device removal does not participate in the terminal result;
- a cleanup failure after `CompletePassed` cannot invalidate that already-emitted PASS;
- the next run has no read-only device-residue preflight.

Required correction:

```text
execute cleanup
-> verify QA-owned device inventory is clean
-> preserve the first failure if one already exists
-> only then emit terminal PASS
```

## 6. Subscription cleanup

Result: **PASS — static ownership audit**

Current productive integration subscribes to:

```text
PlayerParticipationRuntimeContext.Changed
PlayerActorPreparationRuntimeHostModule.SessionPhysicalHostChanged
```

and removes both subscriptions in `Dispose`.

The CAMERA-028-D QA regression uses the application log listener through the normal:

```text
OnEnable  -> subscribe
OnDisable -> unsubscribe
```

pair.

No temporary `PlayerInputManager` event subscription is introduced by the focused regression.

## 7. Split-screen transaction cleanup

Result: **PARTIAL**

The runtime brackets Manager-Provisioned split-screen joins by temporarily suspending `PlayerInputManager.splitScreen`, then completing the transaction when:

```text
physical host evidence arrives
reservation is cancelled / leaves Reserved or Joined
runtime integration is disposed
```

The completion path restores `splitScreen`.

The transaction state is internal. The focused QA does not directly expose or prove:

```text
rejected overlapping transaction
rollback after intermediate failure
transaction state after every refusal path
```

No confirmed split-screen transaction residue was observed, but the current QA observability is incomplete.

## 8. Policy / topology restore

Result: **PARTIAL**

The certification coordinator restores the canonical Shared baseline at the end of the two-phase run.

The focused runtime also proves that PlayerInput layout changes do not mutate the Camera-domain semantic baseline for:

```text
Camera Output identity
exact Unity Camera identity
View -> Output association
Subject availability / assignment contexts
request winner / admitted request state
selected/applied Rig
```

What remains missing is a read-only post-cleanup confirmation that proves the restored baseline without invoking reparative preparation.

## 9. Static ownership audit

Result: **PASS**

Current productive Camera / PlayerInput integration preserves IF-ADR-028 ownership rules:

```text
zero productive Framework writes to Camera.rect
zero productive Framework writes to Camera.pixelRect
zero Framework-owned viewport geometry/grid policy
zero use of playerIndex as Camera Output authority
zero global Camera lookup
zero reflection-based integration
```

The only physical PlayerInput association write is:

```text
PlayerInput.camera
```

`PlayerInputManager.splitScreen` is suspended/restored only as a join transaction boundary so Unity can perform its own split-screen recomposition after the exact Camera association exists.

The canonical typed topology remains:

```text
Dictionary<PlayerSlotId, PlayerCameraOutputBinding>
```

This is the explicit Player Slot -> Camera Output integration topology, not a parallel physical-layout table.

## 10. Reentrancy evidence

Current state:

```text
RUN 1                  PASS 33/33
PREFLIGHT AFTER RUN 1  NOT EXECUTED / NOT PROVEN
RUN 2                  NOT PROVEN without Prepare / Build / Repair
PREFLIGHT AFTER RUN 2  NOT EXECUTED / NOT PROVEN
```

A second successful run that begins only after reparative preparation is not sufficient evidence of cleanup/reentrancy.

The certification must prove a clean read-only state between runs.

## 11. Residue disposition

No runtime residue was confirmed by the supplied run.

Observed gaps are evidence/observability gaps:

```text
QA-owned InputSystem devices after finally
internal split-screen join transaction state
post-restore baseline without preparation
post-run clean starting state for the next run
```

These gaps prevent `VALIDATED = YES`.

## 12. Required change before closure

**CHANGE REQUIRED = YES**

Minimum correction:

1. add a local read-only preflight before any `Prepare / Build / Repair`;
2. move QA-owned device cleanup before the terminal PASS;
3. verify post-cleanup device inventory before terminal success;
4. preserve the first failure if cleanup adds a later failure;
5. execute a post-run read-only preflight;
6. prove a second run can start from that clean state without reparative preparation.

Additional transaction observability is desirable for refusal/rollback diagnostics but is not a reason to move physical-layout ownership into Framework Camera.

## 13. Closure gate

IF-ADR-028 focused PlayerInput closure remains open until the harness can produce evidence equivalent to:

```text
READ-ONLY PREFLIGHT BEFORE RUN 1   PASS
RUN 1                              PASS
POST-CLEANUP READ-ONLY PREFLIGHT   PASS
RUN 2 WITHOUT REPAIR/PREPARE       PASS
POST-CLEANUP READ-ONLY PREFLIGHT   PASS
QA-OWNED DEVICE INVENTORY          CLEAN BEFORE TERMINAL PASS
FIRST FAILURE                      PRESERVED
CANONICAL SHARED BASELINE          CONFIRMED READ-ONLY
```

Until that evidence exists:

```text
CAMERA-028-C focused regression evidence  PASS
CAMERA-028-D functional focused run       PASS 33/33
IF-ADR-028 implementation                 IMPLEMENTED
IF-ADR-028 integration                    INTEGRATED
IF-ADR-028 final validation               OPEN
```
