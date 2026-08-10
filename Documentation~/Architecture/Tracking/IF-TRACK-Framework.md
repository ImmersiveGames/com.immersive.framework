# IF-TRACK — Immersive Framework

Status: **Active**  
Last updated: 2026-08-10  
Package version: `1.0.0-preview.17`

## Current source baselines

```text
com.immersive.framework
  18a6c5079f7436cd86ffa1158cabfe12278855da
  Adr13A-Audio

QAFramework
  dcabd982fee949a571ced53394066ecee9cd313f
  clear

FIRSTGAME / planet-devourer
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
  consumer-stage baseline only; not consulted for ADR-003/012 technical reconciliation
```

The QA project consumes the framework through a local `file:` package path.
Captured Unity verdicts are valid execution evidence for the exercised workspace,
but the QA manifest does not independently pin an exact package Git SHA.

## Status model

This tracker separates four dimensions:

```text
Architecture
  Proposed / Accepted / Superseded

Package
  Absent / Partial / Implemented

QA
  Not Proven / Partial / Certified

FIRSTGAME integration
  Not Proven / Partial / Proven / Not Applicable
```

UX is recorded separately when useful:

```text
Not Evaluated / Acceptable / Friction Observed / Improvement Proposed
```

UX does not enter technical reconciliation arithmetic.

## Reconciliation sequence

ADR reconciliation and real-consumer validation are deliberately separate stages:

```text
Stage A — technical reconciliation
  normative ADR
    -> current package code / official product surface
    -> QAFramework technical proof
    -> classify IMPLEMENTED / DIVERGENT / ABSENT / QA GAP / DOC-TRACKING GAP / DEFERRED

Stage B — real consumer proof
  FIRSTGAME
    -> integration in a real game
    -> usability / authoring observations
    -> route any proven package defect back to a new package cut
```

FIRSTGAME is therefore **not an input to the Stage A conformance verdict**. A
missing FIRSTGAME proof can keep the later consumer stage open, but it cannot turn
an ADR-aligned and QA-certified package contract into a technical gap.

`Product Surface / Diagnostics` remains part of Stage A only when the owning ADR
or the accepted product-authoring standards require an official authoring or
diagnostic surface. Ergonomic preference is still recorded separately as UX.

## Progress estimate

The tracker keeps a percentage view as a **delivery-planning and attention tool**.
It spans both Stage A technical work and Stage B consumer work. It is not a
conformance score, release certification, or a substitute for the explicit
technical reconciliation status. In particular, FIRSTGAME points must never be
used to lower or reopen a Stage A ADR/package/QA verdict.

Scoring dimensions:

| Dimension | Weight | Meaning |
|---|---:|---|
| Architecture / Contract | 20 | normative decision, authority and supported boundary are clear |
| Package Implementation | 30 | official package implementation exists and operates for the accepted boundary |
| Product Surface / Diagnostics | 20 | the feature can be configured and diagnosed through official surfaces; required documentation exists |
| Technical QA | 15 | objective technical contracts are proven in QAFramework |
| FIRSTGAME Integration | 15 | the accepted package boundary is proven in a real product composition |

`Product Surface / Diagnostics` is **not a UX score**. It measures whether the
feature is functionally authorable and diagnosable. Inspector polish, number of
clicks, discoverability preference and other ergonomic observations belong to the
separate UX state and never add or remove completion points.

When a dimension is genuinely not applicable to an ADR, the percentage is
normalized over the applicable dimensions instead of assigning artificial
missing points.

Current evidence-based planning estimate:

| ADR | Arch. | Package | Surface | QA | FIRSTGAME | Estimate | Primary limiter |
|---|---:|---:|---:|---:|---:|---:|---|
| IF-ADR-001 | 20/20 | 30/30 | 20/20 | 15/15 | 15/15 | **100%** | closed for current accepted boundary; deferred extensions are separate contracts |
| IF-ADR-002 | 20/20 | 23/30 | 16/20 | 10/15 | 10/15 | **79%** | portfolio-wide consistency of official authoring surfaces |
| IF-ADR-003 | 20/20 | 29/30 | 18/20 | 15/15 | 5/15 | **87%** | Stage A CLOSED; current-model Player integration is Stage B work |
| IF-ADR-004 | 20/20 | 26/30 | 18/20 | 10/15 | 8/15 | **82%** | IF-ADR-004B negative integrity certification + broader FIRSTGAME Camera proof |
| IF-ADR-005 | 20/20 | 27/30 | 18/20 | 11/15 | 9/15 | **85%** | focused Pause/Input/Reset negative contracts |
| IF-ADR-006 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | exceptional post-commit paths only |
| IF-ADR-007 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | focused uncovered readiness variants |
| IF-ADR-008 | 20/20 | 27/30 | 18/20 | 10/15 | 10/15 | **85%** | current Scene Template integration/technical hardening evidence |
| IF-ADR-009 | 20/20 | 26/30 | 17/20 | 9/15 | 10/15 | **82%** | visibility negative regression coverage |
| IF-ADR-010 | 20/20 | 28/30 | 20/20 | N/A | N/A | **97%*** | per-feature adoption only; no generic UX QA |
| IF-ADR-011 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | focused public waiting/joining integration evidence |
| IF-ADR-012 | 20/20 | 28/30 | 18/20 | 15/15 | 4/15 | **85%** | Stage A CLOSED; current-model participation integration is Stage B work |
| IF-ADR-013 | 20/20 | 18/30 | 12/20 | 7/15 | 0/15 | **57%** | typed execution evidence + negative QA, then real-game proof |
| IF-ADR-014 | 20/20 | 30/30 | 20/20 | 15/15 | 15/15 | **100%** | closed for current accepted boundary |
| IF-ADR-015 | 20/20 | 29/30 | 18/20 | 15/15 | 4/15 | **86%** | current public command/status integration in FIRSTGAME |
| IF-ADR-016 | 20/20 | 30/30 | 19/20 | 15/15 | 4/15 | **88%** | current Scene-/Manager-Provisioned FIRSTGAME integration |

`* IF-ADR-010` is normalized over the 70 applicable points because a generic QA
or FIRSTGAME program for Inspector UX is intentionally not part of that ADR.
Objective Editor contracts continue to be scored under the feature that owns
them.

Portfolio planning view:

```text
Current mean estimate across ADRs: 87.3%

Lowest current estimates:
  IF-ADR-013  57%  Optional BGM / Experimental
  IF-ADR-002  79%  Product Authoring Model portfolio consistency
  IF-ADR-004  82%  Camera
  IF-ADR-009  82%  Activity Local Visibility
  IF-ADR-005  85%  Input / Pause / Gate / Reset
  IF-ADR-008  85%  Persistent Content
  IF-ADR-012  85%  Activity Player Participation
```

Interpretation rules:

```text
low Package score
  -> implementation is the main problem

low QA score
  -> implementation exists but technical proof/hardening is weak

low FIRSTGAME score
  -> Stage B real-product integration is not yet proven; this is not a Stage A technical gap

UX friction
  -> record separately; do not subtract completion points
```

A delivery-planning percentage never overrides the explicit stage status. A feature
may have low FIRSTGAME delivery points while its accepted technical boundary is
already `CLOSED / QA CERTIFIED`.

## Closure rules

Technical reconciliation closes when the accepted boundary is aligned and proven:

```text
ADR normative contract
  +
official package implementation / required product surface
  +
QA technical proof
=
Stage A CLOSED / RECONCILED
```

Only after Stage A does FIRSTGAME prove real-product integration:

```text
Stage A CLOSED
  -> FIRSTGAME real-product composition
  -> UX/integration findings
  -> Stage B PROVEN
```

A FIRSTGAME failure may expose a new package or QA defect. When that happens, the
defect is classified explicitly and routed back to Stage A; FIRSTGAME itself does
not become technical authority.

## ADR-001 reconciliation

```text
IF-ADR-001 — Core Lifecycle and Runtime Authority

Normative status
  ACCEPTED

Current accepted boundary
  CLOSED / RECONCILED

Package implementation
  IMPLEMENTED

Technical QA
  CERTIFIED for the current transaction/readiness boundary

FIRSTGAME
  PROVEN for core Route/Activity lifecycle flows

Current-scope blockers
  NONE IDENTIFIED

Deferred — not blockers
  Session-Persistent Player
  exceptional post-commit compensation
```

The ADR-001 score no longer reserves missing points for deferred contracts. A
future Session-Persistent Player contract or exceptional post-commit compensation
must be opened as a separate approved cut and must not be used to reinterpret the
current lifecycle authority as incomplete.

## ADR-003 + ADR-012 technical reconciliation — 2026-08-10

This reconciliation compares the accepted Player lifecycle/participation decisions
with the current official package line and the canonical Player QA certification.
FIRSTGAME is intentionally outside this Stage A verdict.

| ADR | Normative | Package | QA | Stage A verdict | Stage B |
|---|---|---|---|---|---|
| IF-ADR-003 | Accepted | Implemented for current accepted Player lifecycle boundary | Player QA Certified | **CLOSED / RECONCILED** | FIRSTGAME current-model proof pending |
| IF-ADR-012 | Accepted | Implemented for current accepted Activity participation boundary | Participation PASS / 30-case Activity Session Projection | **CLOSED / RECONCILED** | FIRSTGAME current-model proof pending |

Current Stage A gap classification:

```text
IF-ADR-003
  DIVERGENT         none identified
  ABSENT            none identified
  QA GAP            none identified for the current accepted boundary
  DOC/TRACKING GAP  previous FIRSTGAME-based limiter incorrectly represented Stage B as Stage A debt
  DEFERRED          Player Leave; disconnect/reconnect; Session-Persistent Player

IF-ADR-012
  DIVERGENT         none identified
  ABSENT            none identified
  QA GAP            none identified for the current accepted boundary
  DOC/TRACKING GAP  previous FIRSTGAME-based limiter incorrectly represented Stage B as Stage A debt
  DEFERRED          broader future participation UX/dynamic extensions only when separately opened
```

The Player certification baseline is older than the repository HEADs, but the
repository comparisons from that baseline to the current HEADs contain no Player
runtime or Player QA file changes. Subsequent package changes are documentation,
Activity/Content and Audio work; subsequent QA changes are Audio, Camera, Activity
Local Visibility and shared scene/hub work. No Player regression is inferred from
those later commits.

FIRSTGAME remains the next consumer proof for these Player boundaries, not a
missing implementation contract.

## Canonical Player model

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

Removed/rejected from the current model:

```text
PlayerProvisioningProfile
PlayerSlotProvisioningOverride
Initial / Current / Dynamic Capacity
SetCapacity / SetDynamicCapacity
per-Slot Host Provisioning override
```

Accepted public commands:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

Serialized command identity:

```text
10 OpenJoining
20 CloseJoining
30 retired / unsupported
40 RequestJoin
50 RequestDefaultActorSelection
```

## Current Player technical certification

Canonical QA entrypoint:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Executed verdict:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
serialization='PASS'
session='PASS'
sceneProvided='PASS'
managerProvisioned='PASS'
actor='PASS'
publicSurface='PASS'
participation='PASS'
```

Technical Player certification is therefore closed for the current accepted
boundary.

## Current Player real integration — Stage B

The committed FIRSTGAME Player content still contains superseded authoring data
from the former Capacity/separate-provisioning-Profile shape and is not current
accepted-model proof.

Current status:

```text
Scene-Provided current-model integration       NOT PROVEN
Manager-Provisioned current-model integration  NOT PROVEN
Player Participation current-model integration NOT PROVEN
```

When Stage B is opened, FIRSTGAME Player work should rebuild/reauthor against the
official current package model rather than add compatibility behavior for the
historical serialized shape. Its absence does not reopen the closed Stage A
technical verdict.

UX observations collected during that rebuild may drive optional package
improvements, but are not a separate completion score.

## Track board

| Track | Package | QA | FIRSTGAME integration | Current disposition |
|---|---|---|---|---|
| Runtime authority / lifecycle | Implemented | Certified for current transaction/readiness boundary | Proven for core Route/Activity flows | **closed for current accepted ADR-001 boundary**; Session-Persistent Player and exceptional post-commit compensation remain separate future contracts |
| Player serialized migration integrity | Implemented | **Certified** | Not applicable to technical P0 | closed; retired value 30 must never be reused |
| Player Session | Implemented | **Certified** | **Not Proven on current model** | technical boundary certified; FIRSTGAME is later Stage B proof |
| Player Scene-Provided | Implemented | **Certified** | **Not Proven on current model** | technical boundary certified; FIRSTGAME is later Stage B proof |
| Player Manager-Provisioned | Implemented | **Certified** | **Not Proven on current model** | technical boundary certified; FIRSTGAME is later Stage B proof |
| Player Actor lifecycle | Implemented | **Certified** | Partial/historical only | ADR-003 Stage A closed; real consumer proof belongs to Stage B |
| Player public surface / ADR-015 | Implemented | **Certified** | **Not Proven on current model** | preserve technical certification; consumer command/status proof is Stage B |
| Player Activity participation / ADR-012 | Implemented | **Certified** | **Not Proven on current model** | ADR-012 Stage A closed; layer real participation proof in Stage B |
| Activity readiness + reveal | Implemented | Certified for core supported policies | Proven in existing real readiness/loading scenario | preserve readiness authority; add QA only for concrete gaps |
| Participant-aware Loading progress | Implemented | Certified for core supported boundary | Proven in existing readiness/loading scenario | preserve monotonic/terminal semantics |
| Loading / Transition | Implemented | Certified for current transaction/readiness boundary | Proven for core flows | exceptional paths only when concrete evidence requires them |
| Camera — current single output | Implemented; ADR-004 normatively reconciled by IF-ADR-004A | Partial/current regressions; IF-ADR-004B pending | Partial real integration | execute 004B before package changes; open 004C only if abnormal owner-lifetime orphan is proven |
| Pause / Input / Gate | Implemented | Partial/current regressions | Partial real integration | harden concrete negatives; no generic gate manager |
| Reset / Activity Restart | Implemented | Partial/current regressions | Partial real integration | harden concrete lifecycle negatives |
| Persistent Content Scene Template | Implemented | Validation exists; broader QA partial | Partial real integration | Scene Template is canonical product model; no Composer requirement |
| Activity local visibility | Implemented | Partial | Partial real integration | preserve explicit occurrence-scoped visibility |
| Authored identity / ADR-014 | Implemented | **Certified** | **Proven** | closed for current accepted boundary; IF-ID-07 deferred by design |
| Optional BGM / ADR-013 | Experimental implementation | Partial | Not Proven | close typed execution evidence + negative QA before FIRSTGAME promotion proof |
| Application frame rate | Implemented / Experimental surface | focused proof required per feature | Not Proven | validate in real consumer when promoted |
| Editor product surface / ADR-010 | Implemented across package | **No generic UX QA required** | integration evaluated per feature | ADR-010 accepted; package audit closed |

## ADR-010 closure

```text
IF-ADR-010  Accepted
010A        Product-surface standard absorbed into ADR-010
010B        Package surface audit closed and archived
010C        Not required / cancelled
```

Objective technical Editor contracts continue to belong to their owning feature's
QA backlog. There is no generic UX smoke program.

## Active priority

```text
1. keep documentation aligned with current contracts
2. continue ADR -> package -> QA reconciliation and close only concrete technical gaps
3. do not create FIRSTGAME work to compensate for an unresolved package/QA contract
4. after the selected technical boundaries are reconciled, run FIRSTGAME as the final real-consumer stage
5. route any real UX/integration defect back to the smallest owning package/QA cut
```

Do not reopen removed Capacity/provisioning-Profile semantics or add tooling only
to improve a documentation score.

## Future contracts kept separate

```text
Session-Persistent Player
exceptional post-commit compensation
Player Leave / disconnect / reconnect
heterogeneous per-Slot Host Provisioning
split-screen / multiple Camera outputs
application-scoped stable-ID resolver (IF-ID-07)
```

These require separate approved cuts when a concrete product need exists.
