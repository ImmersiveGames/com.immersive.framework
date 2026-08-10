# Immersive Framework — Camera QA Certification Record

Date: **2026-08-10**  
System: **Camera**  
Scope: **current Stable single-output Camera authority**  
Result: **TECHNICALLY CERTIFIED**

## Source state

Current repository heads at documentation reconciliation:

```text
com.immersive.framework
  baecd612c79fe4dabfde5be8d7cf17f3b6b4a3ea
  Adr004

QAFramework
  c7f3443df9a95011220db5d584de7afb94e331ec
  Cam-Pass

FIRSTGAME / planet-devourer
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
```

The executed ADR004B runner retains its own historical base evidence
(`bbaf05d...` + `packagePatch='IF-ADR-004C'`). The package HEAD above confirms
that the 004C correction is now present in the official repository state.

## Certification results

### C9R — supported positive lifecycle

```text
[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]
status='Passed'
phase='canonical-override-fixture'
cases='11'
```

### IF-ADR-004C — owner lifetime integrity

```text
[QA_CAMERA_ADR004C]
status='Passed'
cases='10/10'
failed='0'
verdict='ADR-004C CAMERA OWNER LIFETIME INTEGRITY CERTIFIED'
```

### IF-ADR-004B — negative integrity

```text
[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
failed='0'
blocked='0'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

## Evidence sequence

The certification was not a one-pass green result:

```text
004B initial run
  17/18
  abnormal Route owner disable -> orphan=True
      ↓
004C opened from evidence
      ↓
narrow scoped publication lifetime fix
      ↓
004C 10/10
C9R 11/11
004B 18/18
```

This sequence is important: QA first proved the package defect, then the package
was fixed at the narrow owner-lifetime boundary, then both positive and negative
contracts were rerun.

## Certified technical boundaries

- Local Player / Activity / Route / Session precedence ladder;
- deterministic equal-precedence tie-break behavior;
- duplicate request and output mismatch blocking;
- publication/release idempotence;
- winner restoration and out-of-order release;
- physical apply rollback and explicit rollback failure;
- normal Activity/Route lifecycle cleanup;
- abnormal Route/Activity disable/destruction cleanup;
- Session component lifetime cleanup;
- winner/non-winner owner loss behavior;
- no silent re-publish on re-enable;
- persistent single-output composition validation;
- invalid physical output reference blocking.

## Residual QA teardown hygiene

After the functional gates had already passed, the synthetic QA Local Player
binding emitted a redundant `release-not-found` during scene teardown because its
local publisher state had not yet reconciled with a request already absent from
the output context.

A QA-only cleanup patch was prepared so teardown treats that already-absent
synthetic request as `Preserved` while keeping real release failures blocking.

This item is **not** a Camera package defect and does not invalidate the executed
C9R/004C/004B certification. A clean-log retest of that QA-only hygiene patch had
not yet been supplied when this record was authored.

## Product interpretation

```text
Architecture
  ACCEPTED

Package current single-output boundary
  IMPLEMENTED

Technical QA
  CERTIFIED

FIRSTGAME broader Camera consumer proof
  PARTIAL / SEPARATE

Split-screen / multi-output
  OUT OF SCOPE / FUTURE CONTRACT
```
