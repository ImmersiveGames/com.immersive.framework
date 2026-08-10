# IF-TRACK — Immersive Framework

Status: **Active**  
Last updated: **2026-08-10**  
Package version: `1.0.0-preview.17`

## Current source baselines

```text
com.immersive.framework
  7b53b47814ddf59159972f56db171d60d421b14f
  Camera-Docs

QAFramework
  d000303c6409338888c8abe21e83c70759171df6
  Cam-Pass

FIRSTGAME / planet-devourer
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
```

The QA project consumes the framework through a local `file:` package path.
Captured Unity verdicts remain execution evidence for the exercised workspace;
QA source metadata does not independently pin the package Git SHA.

## Status model

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

UX remains separate from technical completion.

FIRSTGAME is Stage B consumer evidence. It does not reopen Stage A technical
conformity after package/contracts/QA for an ADR boundary are certified.

## Progress estimate

The percentage view is a planning aid, not release certification.

| ADR | Arch. | Package | Surface | QA | FIRSTGAME | Estimate | Primary limiter |
|---|---:|---:|---:|---:|---:|---:|---|
| IF-ADR-001 | 20/20 | 30/30 | 20/20 | 15/15 | 15/15 | **100%** | closed for current accepted boundary |
| IF-ADR-002 | 20/20 | 30/30 | 20/20 | N/A | N/A | **100%** | closed for current cross-cutting boundary |
| IF-ADR-003 | 20/20 | 29/30 | 18/20 | 15/15 | 5/15 | **87%** | current-model Player integration in FIRSTGAME |
| IF-ADR-004 | 20/20 | 30/30 | 18/20 | 15/15 | 8/15 | **91%** | broader FIRSTGAME Camera consumer proof / product UX only |
| IF-ADR-005 | 20/20 | 30/30 | 20/20 | 13/15 | 9/15 | **92%** | focused Pause QA; FIRSTGAME is Stage B only |
| IF-ADR-006 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | exceptional post-commit paths only |
| IF-ADR-007 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | focused uncovered readiness variants |
| IF-ADR-008 | 20/20 | 27/30 | 18/20 | 10/15 | 10/15 | **85%** | current Scene Template integration/hardening evidence |
| IF-ADR-009 | 20/20 | 26/30 | 17/20 | 9/15 | 10/15 | **82%** | visibility negative regression coverage |
| IF-ADR-010 | 20/20 | 28/30 | 20/20 | N/A | N/A | **97%*** | per-feature adoption only; no generic UX QA |
| IF-ADR-011 | 20/20 | 29/30 | 18/20 | 15/15 | 13/15 | **95%** | focused public waiting/joining integration evidence |
| IF-ADR-012 | 20/20 | 28/30 | 18/20 | 15/15 | 4/15 | **85%** | current-model Player participation in FIRSTGAME |
| IF-ADR-013 | 20/20 | 18/30 | 12/20 | 7/15 | 0/15 | **57%** | optional BGM promotion proof |
| IF-ADR-014 | 20/20 | 30/30 | 20/20 | 15/15 | 15/15 | **100%** | closed for current accepted boundary |
| IF-ADR-015 | 20/20 | 29/30 | 18/20 | 15/15 | 4/15 | **86%** | current public command/status integration in FIRSTGAME |
| IF-ADR-016 | 20/20 | 30/30 | 19/20 | 15/15 | 4/15 | **88%** | current Scene-/Manager-Provisioned FIRSTGAME integration |

`IF-ADR-002` and `IF-ADR-010` remain normalized over their applicable
Architecture/Package/Product Surface dimensions.

With the current Camera reconciliation and the ADR-005 documentation correction,
the portfolio planning mean is approximately **89.7%**. The explicit ADR/evidence
status remains more important than this planning average.

## ADR-005 technical reconciliation — 2026-08-10

The current ADR-005 package boundary is implemented and its direct product
surfaces are sufficient under ADR-010. No missing Composer, Wizard, Recipe or
generic Gate Manager is identified.

Current evidence disposition:

```text
Architecture
  ACCEPTED / RECONCILED

Package
  IMPLEMENTED

Product Surface / Diagnostics
  IMPLEMENTED

Input Gate
  QA PROVEN for current focused regression

Reset
  QA PROVEN for current focused regression

Activity Restart
  QA PROVEN for current focused regression

Transition / Readiness gates
  existing focused certifications remain valid

Pause
  IMPLEMENTED
  direct focused QA proof pending

Pause + Activity Restart
  direct focused QA proof pending

Divergent package behavior
  NONE IDENTIFIED

Missing package contract
  NONE IDENTIFIED

FIRSTGAME
  STAGE B / separate consumer evidence
```

The remaining Stage A cut is QA-only unless the focused Pause regression reveals
a real package defect. It should prove `PauseRuntime`, `PausePlayerInputBinding`,
`PauseRequestTrigger`, cleanup/restoration semantics and deterministic behavior
when Activity Restart occurs while Pause is active.

Do not alter package behavior simply to satisfy the synthetic test. Any real
contract defect discovered by that QA cut must become a separate minimal package
cut with explicit evidence.

## Camera technical closure — 2026-08-10

```text
IF-ADR-004A
  CLOSED — normative reconciliation

C9R Camera Override Authority
  PASS 11/11

IF-ADR-004B
  first valid run: 17/18
  case 16 reproduced abnormal Route-owner orphan
  final re-certification after 004C: PASS 18/18

IF-ADR-004C
  package owner-lifetime correction implemented
  PASS 10/10
```

The package HEAD now contains the 004C scoped component-lifetime correction.
Camera is no longer waiting on 004B or a conditional 004C opening.

Current Camera technical interpretation:

```text
Architecture
  ACCEPTED

Package — single-output boundary
  IMPLEMENTED

Technical QA
  CERTIFIED

FIRSTGAME
  PARTIAL / separate real-consumer proof

Split-screen / multi-output
  future contract, not a current deficit
```

### Residual QA teardown hygiene

A QA-only synthetic Local Player teardown path emitted a redundant
`release-not-found` after all functional Camera gates had already passed. The v10
QA patch reconciles its local publisher state with the output context before a
second release.

That clean-log hygiene retest was still pending when this Tracker was updated; it
is not a package defect and does not reopen C9R/004B/004C certification.

## Canonical Player model

```text
PlayerSessionProfile
├── Supported Slots
├── Initial Joining
├── Host Provisioning
│   ├── Scene Provided
│   └── Manager Provisioned
└── Actor Resolution
```

Removed from the current model:

```text
PlayerProvisioningProfile
PlayerSlotProvisioningOverride
Initial / Current / Dynamic Capacity
SetCapacity / SetDynamicCapacity
per-Slot Host Provisioning override
```

Accepted serialized command identity remains:

```text
10 OpenJoining
20 CloseJoining
30 retired / unsupported
40 RequestJoin
50 RequestDefaultActorSelection
```

## Track board

| Track | Package | QA | FIRSTGAME integration | Current disposition |
|---|---|---|---|---|
| Runtime authority / lifecycle | Implemented | Certified for current transaction/readiness boundary | Proven for core Route/Activity flows | closed for current ADR-001 boundary |
| Player serialized migration integrity | Implemented | Certified | N/A | closed; retired value 30 must never be reused |
| Player Session | Implemented | Certified | Not Proven on current model | rebuild real consumer integration using ADR-016 |
| Player Scene-Provided | Implemented | Certified | Not Proven on current model | FIRSTGAME proof remains |
| Player Manager-Provisioned | Implemented | Certified | Not Proven on current model | FIRSTGAME proof remains |
| Player Actor lifecycle | Implemented | Certified | Partial/historical | current real consumer proof remains |
| Player public surface / ADR-015 | Implemented | Certified | Not Proven on current model | real command/status integration remains |
| Player Activity participation / ADR-012 | Implemented | Certified | Not Proven on current model | layer after underlying Player integration |
| Activity readiness + reveal | Implemented | Certified for core policies | Proven in existing scenario | focused gaps only |
| Participant-aware Loading progress | Implemented | Certified | Proven in existing scenario | preserve semantics |
| Loading / Transition | Implemented | Certified for current boundary | Proven for core flows | exceptional paths only |
| **Camera — current single output** | **Implemented; 004C owner-lifetime fix present** | **Certified: C9R 11/11 + 004C 10/10 + 004B 18/18** | **Partial** | **technical boundary closed; broader real-consumer Camera proof remains separate** |
| Pause / Input / Gate | Implemented | Partial — Input/Transition/Readiness proven; direct Pause proof pending | Partial | focused Pause QA is the only current Stage A gap |
| Reset / Activity Restart | Implemented | Proven for current focused boundary | Partial | technical regressions present; FIRSTGAME remains separate Stage B evidence |
| Persistent Content Scene Template | Implemented | Validation exists; broader QA partial | Partial | Scene Template remains canonical |
| Activity local visibility | Implemented | Partial | Partial | preserve occurrence-scoped visibility |
| Authored identity / ADR-014 | Implemented | Certified | Proven | closed current boundary |
| Optional BGM / ADR-013 | Experimental | Partial | Not Proven | demand-driven promotion only |
| Application frame rate | Implemented / Experimental surface | focused proof per feature | Not Proven | validate when promoted |
| Editor product surface / ADR-010 | Implemented across package | no generic UX QA required | per-feature | accepted; package audit closed |

## Active priority

```text
1. keep documentation aligned with current contracts and certification evidence
2. preserve Camera C9R / 004B / 004C certification; do not reopen it for unrelated UX work
3. apply/retest the QA-only Camera teardown hygiene patch for a clean console when convenient
4. close ADR-005 Stage A with focused Pause and Pause + Activity Restart QA
5. continue other non-Camera hardening only for concrete technical gaps
6. rebuild FIRSTGAME Player integration against the accepted current model
7. use FIRSTGAME as separate consumer integration/UX evidence where desired
8. promote experimental systems only from real product demand
```

## Future contracts kept separate

```text
Session-Persistent Player
exceptional post-commit compensation
Player Leave / disconnect / reconnect
heterogeneous per-Slot Host Provisioning
split-screen / multiple Camera outputs
application-scoped stable-ID resolver (IF-ID-07)
```

These require separate approved cuts and must not be treated as missing pieces of
a certified current boundary.
