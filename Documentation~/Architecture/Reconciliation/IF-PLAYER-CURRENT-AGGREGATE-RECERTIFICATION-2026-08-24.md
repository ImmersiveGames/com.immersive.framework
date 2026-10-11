# Immersive Framework — Player Aggregate Recertification (Historical)

> **Historical evidence only.** The legacy aggregate runner is not part of current QAFramework/QA-001. The `27/27` result remains tied to the contracts and execution recorded here; current Player coverage and pending gates are maintained in [the 2026-10-11 recertification report](IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md).

Status: **Historical certification record — superseded as current QA authority by QA-001 Player Slices**
Date of recorded run: **2026-08-24**
Related decisions: IF-ADR-003, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021

## Purpose

Record the Player technical certification reported on 2026-08-24 after the IF-ADR-021 Model B replacement runtime, Player/Game Flow bootstrap-order correction and QA Play Mode startup-synchronization sweep.

This record does not rewrite the historical Full Player `25/25` certification or the historical ADR-021 Initial Placement `9/9` result. Those remain dated evidence for the boundaries they executed.

## Terminal aggregate recorded on 2026-08-24

The 2026-08-24 Full Player QA run reported:

```text
[QA_PLAYER_FULL]
status='Completed'
verdict='PLAYER CURRENT AGGREGATE COMPLETE'
historicalFullPlayer='25/25'
mandatoryContracts='27'
executedContracts='27'
passedContracts='27'
```

Aggregate phases recorded in this run:

```text
Serialization                         PASS
Session                               PASS
Route Spatial Entry                   PASS
Activity Relocation                   PASS
Historical Placement                  NOT RUN
Scene Provided                        PASS
Scene Provided Leave                  PASS
Scene Provided Leave Without Activity PASS
Scene Provided Session Termination    PASS
Manager Provisioned                   PASS
Manager Join Without Activity         PASS
Manager Session Termination           PASS
Actor Lifecycle                       PASS
Public Surface                        PASS
Session Player Leave                  PASS
Failed First Scene Adoption           PASS
Failed Contextual Reprojection        PASS
No Physical Handoff                   PASS
```

## IF-ADR-021 Model B focused evidence

The replacement spatial model is independently covered by:

```text
Route Spatial Entry
  18/18 PASS
  verdict='ADR-021 MODEL B ROUTE SPATIAL ENTRY VERIFIED'

Activity Explicit Relocation
  23/23 PASS
  verdict='ADR-021 MODEL B ACTIVITY RELOCATION VERIFIED'
```

The focused evidence proves the separation between Route baseline spatial entry and optional Activity contextual relocation, including no-Activity and null-`ActivityContentProfile` paths, Scene-Provided and Manager-Provisioned flows, exact binding cardinality, occurrence idempotence, ineligible-scene exclusion, and physical-representation replacement handling.

## Bootstrap integration correction

The Model B integration exposed an initialization-order defect: Player Actor Preparation attempted to consume canonical Route lifecycle authority before `GameFlowRuntime` existed.

The corrected composition order is:

```text
Player Session core
  -> Game Flow / Route lifecycle authority
  -> Player Actor Preparation lifecycle attachment
  -> Route startup / admission consumption
```

The correction is a split of initialization timing, not a new authority or fallback. Missing canonical Game Flow authority remains an explicit failure after composition should be ready.

## QA startup synchronization correction

The bootstrap split made a previous QA timing assumption visible: some Play Mode regressions treated `FrameworkRuntimeHost` existence as equivalent to completed `StartAsync`.

All 14 Play Phases in `QaPlayerFullCertificationOrchestrator` were audited. Route-scoped regressions now wait on the already-resolved canonical Host for:

```text
host.State.GameFlowStarted
&& host.State.CurrentRoute != null
```

Scene-Provided lifecycle/contextualization keeps the stronger readiness it actually requires, including a ready current Activity. Manager-without-Activity keeps its Route-scoped public-access readiness and does not fabricate an Activity requirement.

These QA changes synchronize observation with the accepted runtime lifecycle. They do not alter Player, Route, Activity, Join, Leave, Actor Selection, provisioning, spatial-entry or relocation semantics.

## Historical evidence preserved

```text
Historical Full Player
  25/25
  remains valid for the 2026-08-15 boundary it executed

Historical ADR-021 Initial Placement
  9/9
  remains evidence for the superseded Activity-owned discovery model
  is not current Model B certification
```

At the time of the 2026-08-24 run, this `27/27` aggregate was the recorded certification reference. It is preserved as dated evidence and is not the current QA-001 certification route; it does not prove later contracts or the current package SHA.

## Disposition

```text
IF-ADR-019 Session physical lifetime       CLOSED / CURRENT AGGREGATE PASS
IF-ADR-020 Leave/resource release           CLOSED / CURRENT AGGREGATE PASS
IF-ADR-021 Route Spatial Entry              CLOSED / 18/18 + CURRENT AGGREGATE PASS
IF-ADR-021 Activity Explicit Relocation     CLOSED / 23/23 + CURRENT AGGREGATE PASS
Full Player aggregate recorded in this run  CLOSED / 27/27 (historical)
Historical Full Player                      PRESERVED / 25/25
Historical ADR-021 Initial Placement        PRESERVED / 9/9 / SUPERSEDED BOUNDARY
```

Stage B consumer/sample proof remains separate from this technical certification.
