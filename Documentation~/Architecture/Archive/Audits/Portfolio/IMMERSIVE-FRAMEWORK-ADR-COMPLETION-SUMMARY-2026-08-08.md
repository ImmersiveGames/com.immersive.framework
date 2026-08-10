> **Archived historical snapshot.**
> This file is preserved for traceability and is not current product/status authority.
> Use `Architecture/ADRs/` for decisions and `Architecture/Tracking/IF-TRACK-Framework.md` for current status.

# Immersive Framework — ADR Completion Summary

> Historical execution summary with current-status reconciliation on 2026-08-09.
> Earlier Player certification text that referenced Capacity, a separate
> provisioning Profile or per-Slot Host Provisioning overrides is superseded by
> the accepted IF-ADR-016 model and the current Player QA certification below.

Date: 2026-08-09  
Package Git baseline inspected: `4662fade4e27e2c06b6daf4485d2829e4fb24096` (`R1 — Consolidar Player Session Authoring`)  
QA Git baseline / certification record: `219cc22e2267d8222da7665807f1175edb64042c` (`Player QA`); canonical Unity Player QA executed 2026-08-09  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Portfolio average: **84.6% equivalent**

> Git baselines identify repository states inspected for documentation. The
> Player QA verdict is Unity execution evidence for the package/runtime state
> exercised by that run; it is not a claim that package commit `4662fade` alone
> contains every local R2–R4 edit used during certification. Percentages are
> planning estimates, not release scores.

## Important baseline changes

1. IF-TXN-01/02/03A remain closed for their accepted transition/gate authority boundaries.
2. Readiness/Loading compatibility remains green across terminal failure, WaitVisible/WaitCovered, participant-aware progress and startup parity.
3. No generic transaction manager, rollback manager, release token or silent fallback was introduced.
4. **IF-ADR-016 is Accepted and its current no-Capacity implementation is technically QA-certified.**
5. The canonical Session initialization model is now only:

```text
PlayerSessionProfile
├── Supported Slots
├── Initial Joining
├── Host Provisioning
│   ├── Scene Provided
│   └── Manager Provisioned
└── Actor Resolution
    ├── Resolve Configured Default
    └── Leave Unresolved
```

6. `Supported Slots` is the structural Session maximum. Joining Open/Closed is runtime admission intent. There is no Initial/Current/Dynamic Capacity and no runtime capacity command.
7. Host Provisioning is Session-wide. Per-Slot Host Provisioning overrides and mixed Scene/Manager Sessions are not part of the accepted model.
8. **The canonical one-button Player QA completed with `PLAYER QA CERTIFIED`.**
9. Scene-Provided, Manager-Provisioned, Actor lifecycle, Public Surface and Activity Participation all passed in isolated owned phases.
10. **IF-ADR-015 current consumer surface is technically revalidated against IF-ADR-016.** Its previous Capacity-era certification is historical only.
11. Public Surface Q1 is now **28/28 PASS** and Q2 remains **36/36 PASS** under Supported-Slots semantics.
12. IF-ADR-015 remains **Proposed** because FIRSTGAME real-consumer proof, P5 tooling disposition and final ADR acceptance remain product gates; technical QA is no longer the blocker.
13. IF-ADR-016 remains **Accepted**; FIRSTGAME product/usability proof remains the next evidence gate, not another technical QA redesign.
14. IF-ADR-003 and IF-ADR-012 now have current canonical Player integration evidence rather than only historical/reorganized smoke coverage.

## Method

Each percentage considers:

```text
Normative contract and architectural clarity   20%
Runtime implementation                         30%
Authoring, diagnostics and documentation        20%
Current QA evidence                             15%
Current FIRSTGAME consumer evidence             15%
```

The current Player certification strengthens the evidence classification. This
reconciliation does not arbitrarily raise planning percentages merely because a
new QA run passed; remaining product/FIRSTGAME gaps still contribute to the
existing estimates.

## ADR matrix

| ADR | Decision | Normative status | Completion | Current classification |
|---|---|---:|---:|---|
| IF-ADR-001 | Core Lifecycle and Runtime Authority | Accepted | **92%** | IF-TXN-01/02/03A implemented and QA-certified; Session-Persistent Player and concrete exceptional post-commit paths remain |
| IF-ADR-002 | Product Authoring Model | Accepted | **65%** | Partially implemented across the product portfolio |
| IF-ADR-003 | Player Participation and Actor Lifecycle | Accepted | **84%** | Runtime substantially implemented and canonical Player lifecycle QA green; product hardening, Leave/disconnect and Session-Persistent work remain |
| IF-ADR-004 | Camera Requests and Output Authority | Accepted | **78%** | Core runtime implemented; isolated product proof incomplete |
| IF-ADR-005 | Input, Pause, Gate and Reset | Accepted | **78%** | Gate authority taxonomy clarified; IF-TXN-03A certified; broader product/negative matrix remains |
| IF-ADR-006 | Loading, Transition, Persistence and Diagnostics | Accepted | **94%** | IF-TXN-01/02/03A certified; concrete exceptional cleanup/compensation diagnostics and product templates remain |
| IF-ADR-007 | Activity Entry Readiness and Reveal Gating | Accepted | **96%** | Runtime contract complete; WaitVisible/WaitCovered, terminal recovery, startup parity and Player pending→terminal integration certified |
| IF-ADR-008 | Persistent Application Content Composition | Accepted | **90%** | Product model implemented; portfolio expansion and QA remain |
| IF-ADR-009 | Activity Local Visibility Rules | Accepted | **88%** | Runtime integrated; authoring and regression polish remain |
| IF-ADR-010 | Editor and Inspector Product Surface Authority | Proposed | **70%** | Broad foundation exists; decision not fully accepted or consistently applied |
| IF-ADR-011 | Participant-Aware Activity Readiness Loading Progress | Accepted | **94%** | Runtime complete; progress/terminal/startup parity plus Player WaitingForJoin/WaitCovered path certified; presentation guidance remains |
| IF-ADR-012 | Activity Player Participation Profile and Readiness Compatibility | Accepted | **90%** | Contract/runtime implemented and current Player Participation integration QA green; product consolidation remains |
| IF-ADR-013 | Optional Audio BGM Adapter | Accepted / Experimental | **65%** | Technical adapter exists; product promotion incomplete |
| IF-ADR-014 | Authored Definition and Stable Identity Authority | **Accepted** | **100%*** | **Complete for current accepted scope; IF-ID closed; IF-ID-07 deferred by design** |
| IF-ADR-015 | Player Provisioning Commands and Consumer Observation Surface | Proposed | **80%** | Official no-Capacity P1–P4 surface implemented and technically QA-certified; FIRSTGAME, P5 and final ADR disposition remain |
| IF-ADR-016 | Player Session Initial Configuration | Accepted | **90%** | Consolidated Profile, Supported Slots, uniform provisioning, Actor Resolution and runtime behavior implemented and technically QA-certified; FIRSTGAME product proof remains |

`*` IF-ADR-014 uses 100% only for portfolio arithmetic. Its official ADR wording remains `Complete for current accepted scope`.

Portfolio arithmetic: `(92+65+84+78+78+94+96+90+88+70+94+90+65+100+80+90) / 16 = 84.6%` rounded to one decimal.

## Current Player technical certification — 2026-08-09

Canonical QA entrypoint:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Final verdict:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
session='PASS'
sceneProvided='PASS'
managerProvisioned='PASS'
actor='PASS'
publicSurface='PASS'
participation='PASS'
```

Representative certified evidence:

```text
Player Participation Authoring        PASS — 7 cases
Scene-Provided route/negative matrix  PASS — 25 cases
Manager public contract               PASS — 9 cases
Manager waiting projection            PASS — 14 cases
Actor selection runtime binding       PASS — 13 cases
Player gameplay admission             PASS — 114 cases
Public Surface Q1                     PASS — 28 cases
Public Surface Q2                     PASS — 36 cases
Activity Session Projection           PASS — 30 cases
```

The phases are intentionally isolated. Scene-Provided runs with Session Host
Provisioning = `SceneProvided`; Manager-Provisioned is prepared separately with
Host Provisioning = `ManagerProvisioned`. The Manager Input System bridge is a
derived materialized constraint from `SupportedSlotCount`; it is not Session
Capacity or runtime authority.

Q2 intentionally emits framework error diagnostics for rejected/stale/wrong-
scope/destroyed/unbound negative operations. Those logs are expected evidence;
the certification authority is the typed case evidence plus the final PASS.

See `IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md`.

## IF-ADR-015 current certification

The accepted public consumer vocabulary is:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

Public access is typed and Route/Activity scoped. Observation is immutable.
Internal Slot reservation, Actor preparation/materialization, gameplay admission
and Activity reconcile remain internal authorities.

Current certification:

```text
Public Surface Q1
  PASS — 28/28

Public Surface Q2
  PASS — 36/36

Master phase
  publicSurface='PASS'

Joint technical verdict
  PLAYER QA CERTIFIED
```

This supersedes the old 29/29 Capacity-era Q1 record as the current certification
for the accepted IF-ADR-016 model.

## IF-ADR-016 current certification

The current technical QA proves the accepted initialization semantics in the
same canonical Player run:

```text
Supported Slots are the Session structural universe
Join uses first vacant Supported Slot in authored order
no vacant Slot rejects explicitly
Initial Joining resolves into runtime Joining state
Host Provisioning is Session-wide
Scene Provided and Manager Provisioned are distinct
Actor Resolution remains independent
runtime effective configuration is frozen at Session creation
no provisioning fallback is applied
```

The previous certification language around Capacity bounds, mixed per-Slot
provisioning and a separate provisioning Profile is historical and must not be
used as current IF-ADR-016 evidence.

## IF-TXN-03A closure incorporated

### Certified operational model

```text
Transition Gate
  internal GameFlow operation state
  no external resource acquire/release protocol
  no release refusal/ownership-token failure contract

TransitionGateSnapshot
  pure Transition Gate

CurrentTransitionGateMode
  pure Transition Gate mode

ActivityEntryReadinessGateSnapshot
  Transition Gate + Activity Entry Readiness Recovery Gate

CurrentGateSnapshot
  broader host operational composition
```

A committed-target readiness failure may validly have Transition Gate released
while Readiness Recovery remains active. This is intentional recovery protection,
not Transition Gate leakage.

Existing transaction/readiness certification remains:

```text
IF-TXN-03A Transition Gate Terminal Integrity       PASS — 16/16
IF-TXN-02 Clear/Restart Transition Authority        PASS — 16/16
IF-TXN-01 Transition Failure Authority              PASS — 22/22
Participant-Aware Readiness Loading Terminal        PASS — 34/34
Direct Activity Readiness Policies                  PASS — 42/42
Participant-Aware Readiness Loading Progress        PASS — 32/32
Participant-Aware Startup Parity — Route            PASS — 25/25
Participant-Aware Startup Parity — Game Application PASS — 20/20
```

## Priority order

### P0 — Player real-consumer proof

Technical Player QA is no longer the blocker.

```text
FIRSTGAME
  prove Scene-Provided manual composition
  prove Manager-Provisioned manual composition
  prove current Supported-Slots Session model
  prove scoped commands/status without internal knowledge
```

The purpose is usability/product evidence, not another technical certification
suite.

### P1 — IF-ADR-015 final product disposition

After FIRSTGAME:

```text
P5 creation-workflow/tooling disposition
  NO ADDITIONAL TOOLING REQUIRED
  or smallest justified Create/Inspector/template/Composer improvement
```

Do not introduce a Wizard/Composer solely because the architecture can support
one.

### P2 — remaining Player hardening

- Session Player Leave and device disconnect/reconnect require separate contracts.
- Session-Persistent Player remains blocked/not productized.
- Heterogeneous per-Slot Host Provisioning remains outside IF-ADR-016 and requires a future concrete requirement/ADR.

### P3 — other framework programs

- Continue concrete exceptional transaction/cleanup investigations only when evidence exists.
- Continue IF-ADR-010 product authoring consistency.
- Preserve current readiness/loading authority separation and diagnostic semantics.

## Portfolio interpretation

```text
Core runtime architecture
  strong and mostly implemented

Transaction authority / gate integrity
  IF-TXN-01 CLOSED
  IF-TXN-02 CLOSED
  IF-TXN-03A CLOSED / CERTIFIED

Readiness / Loading
  WaitVisible + WaitCovered green
  terminal/recovery green
  startup parity green
  Player WaitingForJoin + WaitCovered path green

Player Session
  IF-ADR-016 Accepted
  consolidated no-Capacity model implemented
  technical QA certified
  FIRSTGAME product proof pending

Player consumer surface
  IF-ADR-015 P1–P4 implemented
  current no-Capacity Q1 28/28 + Q2 36/36
  technical QA certified
  FIRSTGAME + P5 + final ADR disposition pending

Player lifecycle
  Scene-Provided PASS
  Manager-Provisioned PASS
  Actor PASS
  Public Surface PASS
  Activity Participation PASS

QA
  canonical one-button Player flow is green
  focused menus remain advanced diagnostics

FIRSTGAME
  next gate is real-consumer usability/product proof
```
