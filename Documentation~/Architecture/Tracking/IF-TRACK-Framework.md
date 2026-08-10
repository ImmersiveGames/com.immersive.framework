# IF-TRACK — Immersive Framework

Status: **Active**  
Last updated: 2026-08-09  
Package version: `1.0.0-preview.17`

## Current source baselines

```text
com.immersive.framework
  43b96a4b100b8273da1190520536007ba82dc081
  ADR-010B

QAFramework
  b6a45728285ddb2ce08269fc1f88ae3f1a4235e4
  P0 — Serialized Player Migration Integrity

FIRSTGAME / planet-devourer
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
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

UX does not enter functional completion arithmetic.

## Progress estimate

The tracker keeps a percentage view as a **planning and attention tool**. It is
not release certification and it does not replace the explicit status of each
evidence dimension.

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
| IF-ADR-001 | 20/20 | 28/30 | 17/20 | 15/15 | 13/15 | **93%** | exceptional lifecycle cleanup / future Session-Persistent contract |
| IF-ADR-002 | 20/20 | 23/30 | 16/20 | 10/15 | 10/15 | **79%** | portfolio-wide consistency of official authoring surfaces |
| IF-ADR-003 | 20/20 | 29/30 | 18/20 | 15/15 | 5/15 | **87%** | current-model Player integration in FIRSTGAME |
| IF-ADR-004 | 20/20 | 26/30 | 18/20 | 10/15 | 8/15 | **82%** | Camera negative QA + broader real integration |
| IF-ADR-005 | 20/20 | 27/30 | 18/20 | 11/15 | 9/15 | **85%** | focused Pause/Input/Reset negative contracts |
| IF-ADR-006 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | exceptional post-commit paths only |
| IF-ADR-007 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | focused uncovered readiness variants |
| IF-ADR-008 | 20/20 | 27/30 | 18/20 | 10/15 | 10/15 | **85%** | current Scene Template integration/technical hardening evidence |
| IF-ADR-009 | 20/20 | 26/30 | 17/20 | 9/15 | 10/15 | **82%** | visibility negative regression coverage |
| IF-ADR-010 | 20/20 | 28/30 | 20/20 | N/A | N/A | **97%*** | per-feature adoption only; no generic UX QA |
| IF-ADR-011 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | focused public waiting/joining integration evidence |
| IF-ADR-012 | 20/20 | 28/30 | 18/20 | 15/15 | 4/15 | **85%** | current-model Player participation in FIRSTGAME |
| IF-ADR-013 | 20/20 | 18/30 | 12/20 | 7/15 | 0/15 | **57%** | optional BGM promotion requires technical + real-game evidence |
| IF-ADR-014 | 20/20 | 30/30 | 20/20 | 15/15 | 15/15 | **100%** | closed for current accepted boundary |
| IF-ADR-015 | 20/20 | 29/30 | 18/20 | 15/15 | 4/15 | **86%** | current public command/status integration in FIRSTGAME |
| IF-ADR-016 | 20/20 | 30/30 | 19/20 | 15/15 | 4/15 | **88%** | current Scene-/Manager-Provisioned FIRSTGAME integration |

`* IF-ADR-010` is normalized over the 70 applicable points because a generic QA
or FIRSTGAME program for Inspector UX is intentionally not part of that ADR.
Objective Editor contracts continue to be scored under the feature that owns
them.

Portfolio planning view:

```text
Current mean estimate across ADRs: 86.9%

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
  -> package/QA may be strong, but real-product integration is not yet proven

UX friction
  -> record separately; do not subtract completion points
```

A high percentage never overrides a missing required gate. Example: a feature can
reach 85% with `FIRSTGAME = 0/15`, but its functional status remains
`INTEGRATION PROOF PENDING` whenever real-product integration is applicable.

## Functional completion rule

A feature boundary that requires real-game application is closed by evidence from:

```text
Package implementation
  +
QA technical proof
  +
FIRSTGAME real-product integration
```

FIRSTGAME is therefore part of functional proof when applicable. Ease of use,
Inspector polish and other UX observations discovered during the same work are
qualitative and do not independently open/close the technical feature.

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

## Current Player real integration

The committed FIRSTGAME Player content still contains superseded authoring data
from the former Capacity/separate-provisioning-Profile shape and is not current
accepted-model proof.

Current status:

```text
Scene-Provided current-model integration       NOT PROVEN
Manager-Provisioned current-model integration  NOT PROVEN
Player Participation current-model integration NOT PROVEN
```

The next FIRSTGAME Player work should rebuild/reauthor against the official
current package model rather than add compatibility behavior for the historical
serialized shape.

UX observations collected during that rebuild may drive optional package
improvements, but are not a separate completion score.

## Track board

| Track | Package | QA | FIRSTGAME integration | Current disposition |
|---|---|---|---|---|
| Runtime authority / lifecycle | Implemented | Certified for current transaction/readiness boundary | Proven for core Route/Activity flows | preserve scoped typed authority; future Session-Persistent work is separate |
| Player serialized migration integrity | Implemented | **Certified** | Not applicable to technical P0 | closed; retired value 30 must never be reused |
| Player Session | Implemented | **Certified** | **Not Proven on current model** | rebuild real consumer integration using accepted ADR-016 model |
| Player Scene-Provided | Implemented | **Certified** | **Not Proven on current model** | FIRSTGAME real integration required |
| Player Manager-Provisioned | Implemented | **Certified** | **Not Proven on current model** | FIRSTGAME real integration required |
| Player Actor lifecycle | Implemented | **Certified** | Partial/historical only | prove current model through real Player consumer flow |
| Player public surface / ADR-015 | Implemented | **Certified** | **Not Proven on current model** | real command/status integration required; extra tooling optional |
| Player Activity participation / ADR-012 | Implemented | **Certified** | **Not Proven on current model** | layer after underlying Player mode integration |
| Activity readiness + reveal | Implemented | Certified for core supported policies | Proven in existing real readiness/loading scenario | preserve readiness authority; add QA only for concrete gaps |
| Participant-aware Loading progress | Implemented | Certified for core supported boundary | Proven in existing readiness/loading scenario | preserve monotonic/terminal semantics |
| Loading / Transition | Implemented | Certified for current transaction/readiness boundary | Proven for core flows | exceptional paths only when concrete evidence requires them |
| Camera — current single output | Implemented | Partial/current regressions | Partial real integration | focused technical hardening only for demonstrated gaps |
| Pause / Input / Gate | Implemented | Partial/current regressions | Partial real integration | harden concrete negatives; no generic gate manager |
| Reset / Activity Restart | Implemented | Partial/current regressions | Partial real integration | harden concrete lifecycle negatives |
| Persistent Content Scene Template | Implemented | Validation exists; broader QA partial | Partial real integration | Scene Template is canonical product model; no Composer requirement |
| Activity local visibility | Implemented | Partial | Partial real integration | preserve explicit occurrence-scoped visibility |
| Authored identity / ADR-014 | Implemented | **Certified** | **Proven** | closed for current accepted boundary; IF-ID-07 deferred by design |
| Optional BGM / ADR-013 | Experimental implementation | Partial | Not Proven | defer promotion until a real game requires and proves it |
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
2. focused non-Player hardening only for concrete technical gaps
3. rebuild FIRSTGAME Player integration against the accepted current model
4. record UX friction separately and improve only where real use justifies it
```

Do not reopen removed Capacity/provisioning-Profile semantics or add tooling only
to improve a documentation score.

## Future contracts kept separate

```text
Session-Persistent Player
Player Leave / disconnect / reconnect
heterogeneous per-Slot Host Provisioning
split-screen / multiple Camera outputs
application-scoped stable-ID resolver (IF-ID-07)
```

These require separate approved cuts when a concrete product need exists.
