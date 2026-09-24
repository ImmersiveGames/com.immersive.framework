# Immersive Framework — ADR Completion Summary

> Historical record.
>
> This completion summary captures the framework state at the time it was
> written. It is not current tracking or normative authority. For current
> status, see the Tracker.

> Historical planning document updated with the **2026-08-10 Camera closure**.
> Older percentage snapshots remain historical planning evidence; current mutable
> status authority is `Tracking/IF-TRACK-Framework.md`.

**Original summary date:** 2026-08-08 / 2026-08-09 rebaseline  
**Camera closure update:** 2026-08-10  
**Status:** current operational summary

## Current source baselines

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

## Completion model

Current status is tracked by explicit evidence dimensions:

```text
Architecture / contracts
Package implementation
Product surface / diagnostics
Technical QA
FIRSTGAME integration when applicable
```

UX friction is separate evidence and does not rewrite technical verdicts.

## Player serialization integrity

P0 remains technically closed. Serialized provisioning command identities remain:

```text
10 OpenJoining
20 CloseJoining
30 retired / unsupported
40 RequestJoin
50 RequestDefaultActorSelection
```

Do not restore Capacity, `SetCapacity`, separate `PlayerProvisioningProfile` or
per-Slot Host Provisioning override semantics.

## ADR-010 disposition

```text
IF-ADR-010   ACCEPTED
IF-ADR-010A  CLOSED
IF-ADR-010B  CLOSED
IF-ADR-010C  CANCELLED / NOT REQUIRED
```

Manual explicit authoring remains the default. Additional Composer/Wizard layers
are conditional on a real product lifecycle/materialization need.

## IF-ADR-004 Camera closure — 2026-08-10

The former summary described Camera as deferred and suggested a larger redesign.
That is no longer current.

The actual closure sequence was:

```text
IF-ADR-004A
  normative reconciliation
  no broad redesign justified
        ↓
IF-ADR-004B first execution
  17/18
  abnormal Route-owner orphan reproduced
        ↓
IF-ADR-004C
  narrow scoped owner/publication lifetime package fix
  10/10 certified
        ↓
C9R
  remains 11/11
        ↓
IF-ADR-004B rerun
  18/18 certified
```

Current Camera status:

```text
Architecture
  ACCEPTED

Package current single-output boundary
  IMPLEMENTED

Product surface
  CameraRigComposer + explicit persistent output + scoped overrides
  IMPLEMENTED / CONFORMANT

Technical QA
  CERTIFIED
  C9R 11/11
  ADR004C 10/10
  ADR004B 18/18

FIRSTGAME
  broader real-consumer Camera proof remains PARTIAL / SEPARATE

Large Camera redesign
  NOT REQUIRED by current evidence

Split-screen / multi-output
  FUTURE CONTRACT
```

### QA teardown hygiene

A redundant synthetic Local Player `release-not-found` occurred during scene
teardown after the functional Camera gates had already passed. The v10 QA-only
cleanup patch reconciles that local publisher state with the output context.
Clean-log retest of that hygiene patch was still pending at this documentation
update and does not reopen the Camera technical certification.

## Current ADR matrix — qualitative disposition

| ADR | Normative status | Technical/package interpretation | Current action |
|---|---|---|---|
| IF-ADR-001 | Accepted | mature core lifecycle/runtime authority | focused hardening only when a concrete gap exists |
| IF-ADR-002 | Accepted | mature product authoring model | no cross-cutting implementation |
| IF-ADR-003 | Accepted | strong Player participation/Actor technical state | consumer integration separate |
| **IF-ADR-004** | **Accepted** | **single-output Camera implemented; C9R/004B/004C technically certified** | **preserve certification; broader FIRSTGAME consumer proof only** |
| IF-ADR-005 | Accepted | current Pause/Input/Gate/Reset solution exists | justified hardening only |
| IF-ADR-006 | Accepted | mature Transition/Loading runtime | focused exceptional paths only |
| IF-ADR-007 | Accepted | mature readiness/reveal contract | focused uncovered variants only |
| IF-ADR-008 | Accepted | current Scene Template model implemented | no generic Composer requirement |
| IF-ADR-009 | Accepted | visibility contract handled by its own current evidence | preserve current owner boundary |
| IF-ADR-010 | Accepted | standard + package audit closed | per-feature adoption only |
| IF-ADR-011 | Accepted | strong readiness/loading progress | consumer presentation may evolve separately |
| IF-ADR-012 | Accepted | Player participation technically strong | consumer integration separate |
| IF-ADR-013 | Accepted / Experimental | optional narrow adapter | demand-driven only |
| IF-ADR-014 | Accepted | current identity boundary complete | no active work |
| IF-ADR-015 | Accepted | public Player command/observation surface implemented | current consumer integration separate |
| IF-ADR-016 | Accepted | current no-Capacity Player Session model implemented | current consumer integration separate |

## Current priority interpretation

Camera is no longer a deferred redesign program or an unresolved 004B gate.

```text
1. preserve current contracts and certification records
2. apply/retest isolated QA teardown hygiene where useful
3. continue other feature hardening only from concrete evidence
4. use FIRSTGAME separately for real-consumer integration/UX observations
5. treat split-screen/multi-output Camera as a future approved contract
```

## What should not be done to improve status

Do not create global managers/service locators, revive removed Player semantics,
add generic Composer/Wizard layers, add synthetic UX smokes or reopen certified
Camera behavior solely to increase a score.

## Current Camera conclusion

```text
IF-ADR-004 current single-output technical boundary
  CLOSED / CERTIFIED

Open Camera technical blocker
  NONE

Remaining real-product evidence
  FIRSTGAME broader Camera integration/UX, separate from technical certification
```
