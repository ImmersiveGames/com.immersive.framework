# IF-ADR-004A — Camera Authority Normative Reconciliation

Status: **CLOSED — reconciled into IF-ADR-004**  
Date: **2026-08-10**  
Type: **Architecture / Documentation**  
Primary system: **Camera**  
Normative target: **IF-ADR-004 — Camera Requests and Output Authority**

## Purpose

004A reconciled the normative Camera ADR with the package architecture that was
already present on 2026-08-10. It established the accepted single-output model:

```text
CameraRigComposer
  local rig intent/materialization

Scoped Camera request publishers
  explicit owner/lifetime publication

CameraOutputContext
  deterministic logical arbitration

CameraOutputSession
  transactional logical/physical synchronization

CameraOutputSessionBinding
  explicit persistent physical output
```

It deliberately did not invent a Camera redesign or convert an unproven
abnormal-lifetime concern into a defect before QA evidence existed.

## Decisions recorded by 004A

- deterministic precedence + tie-break evidence replaces timing/newest-request
  semantics;
- physical apply is transactional with logical state;
- Persistent Content owns the physical Camera output;
- `CameraRigComposer` owns only local rig materialization;
- Route/Activity Camera ownership follows exact authored-definition references;
- the accepted presentation capability remains Follow;
- single-output is the current Stable product boundary;
- abnormal owner loss required focused negative QA before any package change.

## Historical gate opened by 004A

At closure time the correct next step was:

```text
IF-ADR-004B
  negative integrity certification

IF-ADR-004C
  conditional only if 004B proved an owner-lifetime defect
```

That classification was correct for the evidence available at the time.

## Downstream resolution — 2026-08-10

The conditional branch was later exercised by evidence:

1. C9R preserved the supported positive Camera lifecycle.
2. IF-ADR-004B first execution reproduced an orphaned Route Camera request when
   the active `RouteCameraOverrideBinding` was disabled abnormally.
3. IF-ADR-004C applied the narrow owner-lifetime correction at the existing
   scoped publication owner rather than adding a global manager or parallel
   lifecycle.
4. IF-ADR-004C certified owner lifetime integrity at `10/10`.
5. IF-ADR-004B was rerun and certified at `18/18`.
6. C9R remained `11/11`.

Therefore 004A remains a **closed historical reconciliation cut**, while its
conditional follow-on is now resolved rather than pending.

## Current result

```text
IF-ADR-004A
  CLOSED
  documentation-only reconciliation

IF-ADR-004B
  CERTIFIED 18/18

IF-ADR-004C
  ACCEPTED / IMPLEMENTED / CERTIFIED 10/10

IF-ADR-004 current single-output boundary
  IMPLEMENTED + TECHNICALLY CERTIFIED
```

No retroactive change is made to the original 004A methodology: its purpose was
to describe the architecture accurately and require proof before package
hardening. The later 004B/004C sequence validated that decision process.
