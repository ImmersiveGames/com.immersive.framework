# Immersive Framework — ADR Completion Summary

> Current reconciliation of the historical completion summary, the independent
> 2026-08-09 rebaseline and the Player serialized-migration integrity audit.
>
> Historical percentages are preserved for traceability. The current stricter
> portfolio number is reported as **evidence-backed maturity**, not as proof that
> implementation regressed.

**Date:** 2026-08-09  
**Status:** current reconciliation / planning baseline  
**Mode:** read-only evidence review; no implementation contained in this document

---

## 1. Current Git baselines

```text
com.immersive.framework
  current HEAD:
    434e73f5aa09377679acc092246c76fa3275dd43
    Add Player command serialization identity regression

QAFramework
  current HEAD before the local full-certification integration patch:
    ba06f257f19b7556ca9fe7899f77193a3bcab0d1
    Add Player command serialization identity regression

FIRSTGAME / planet-devourer
  current HEAD inspected only:
    796618243c3ca76f70d582f38475320c6461420b
    Demo02 Reajuste
```

The package HEAD contains the serialized command identity correction. The QA HEAD contains the focused serialization regression; the full-certification integration is delivered as a local patch on top of that HEAD and therefore has no invented post-patch commit SHA.

The QA project consumes the framework through a local `file:` package path.
Therefore:

```text
Player QA execution verdict
  valid current technical evidence for the state exercised

exact package Git SHA exercised by Unity
  not independently pinned by the QA manifest
```

Do not convert the documentation inspection SHA into stronger provenance than
the captured evidence supports.

---

## 2. Metric terminology

The former summary reported:

```text
Historical planning completion average
  84.6%
```

The independent rebaseline applied the same nominal weights more strictly to
**current evidence**:

```text
Normative contract and architectural clarity   20%
Runtime implementation                         30%
Authoring, diagnostics and documentation        20%
Current QA evidence                             15%
Current FIRSTGAME consumer evidence             15%
```

Its result is:

```text
Current evidence-backed maturity
  72.1%
```

These numbers should not be narrated as:

```text
"the framework regressed from 84.6% to 72.1%"
```

The correct interpretation is:

```text
84.6%
  historical planning estimate using the evidence accepted at that time

72.1%
  stricter rebaseline of how much maturity is currently backed by
  architecture + runtime + product + QA + real-consumer evidence
```

The current rebaseline is especially stricter where historical QA or FIRSTGAME
evidence refers to a superseded contract.

Percentages remain planning/evidence estimates, not release certification.

---

## 3. Serialized Player migration integrity — technical closure

### P0 — Serialized Player Migration Integrity

Status:

```text
TECHNICALLY CLOSED
```

The reconciliation discovered that the ADR-016 consolidation had reused serialized enum identities. The package correction and focused QA now close that technical defect without restoring Capacity or adding a compatibility rail.

Pre-R1:

```text
OpenJoining                   = 10
CloseJoining                  = 20
SetCapacity                   = 30
RequestJoin                   = 40
RequestDefaultActorSelection  = 50
```

Corrected current runtime:

```text
OpenJoining                   = 10
CloseJoining                  = 20
30                            = retired / unsupported
RequestJoin                   = 40
RequestDefaultActorSelection  = 50
```

The former R1 mapping created two silent semantic collisions. The corrected mapping no longer reuses those identities:

```text
30
  old = SetCapacity
  new = RequestJoin

40
  old = RequestJoin
  new = RequestDefaultActorSelection
```

FIRSTGAME contains historical evidence of the defect in pre-redesign Player authoring:

```yaml
operation: 40
requestedCapacity: 1
```

The obsolete `requestedCapacity` field proves the component payload was authored
under the old schema, where `40` meant `RequestJoin`.

The corrected package again interprets `40` as `RequestJoin`; `30` is unsupported. FIRSTGAME is not promoted by this fix: its current Player product evidence remains absent/deferred and will be redesigned separately.

Closure evidence:

```text
package serialized identities corrected
IF-PLAYER-SERIALIZATION-01 PASS — 5/5
canonical Player QA includes the serialization gate
```

See:

```text
IMMERSIVE-FRAMEWORK-PLAYER-SERIALIZATION-MIGRATION-INTEGRITY-2026-08-09.md
```

---

## 4. Current Player Session model

The accepted IF-ADR-016 model remains:

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

There is no:

```text
Initial Capacity
Current Capacity
Dynamic Capacity
SetCapacity
SetDynamicCapacity
separate PlayerProvisioningProfile
per-Slot Host Provisioning override
```

The public IF-ADR-015 vocabulary remains:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

The P0 does not reopen Capacity. It identifies serialized identity reuse during
the migration away from Capacity.

---

## 5. Current Player technical certification

The canonical full-certification contract after this integration is expected to emit:

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

The focused serialization regression already has 5/5 Unity evidence. The exact combined one-button summary requires the manual retest after applying the QA patch; it is not claimed as executed by this documentation update.

Representative evidence:

```text
Serialized Command Identity             PASS — 5 cases
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

This is strong technical evidence for the current no-Capacity model. Serialized migration integrity is now closed by the corrected numeric identities plus `IF-PLAYER-SERIALIZATION-01`; the one-button integration makes that focused proof a required gate of future canonical Player certifications.

---

## 6. FIRSTGAME current state

Current FIRSTGAME is no longer treated as merely "proof pending".

For the Player authoring cut, current Git contains superseded serialized fields:

```text
Demo02_Session_ManagerProvided.asset
Demo02_Session_SceneProvided.asset

  initialCapacity
  playerProvisioningProfile
```

while the current package Profile owns:

```text
supportedSlots
initialJoiningOpen
hostProvisioning
actorResolutionPolicy
```

Current FIRSTGAME also contains the confirmed `operation: 40` command collision.

Therefore:

```text
FIRSTGAME historical Player evidence
  useful as historical integration evidence

FIRSTGAME current accepted Player-model evidence
  NOT CERTIFIED

FIRSTGAME current Player authoring integrity
  OPEN / DEFERRED — separate consumer redesign/rebuild
```

This directly affects the real-consumer dimension of:

```text
IF-ADR-003
IF-ADR-012
IF-ADR-015
IF-ADR-016
```

It does not invalidate their current package/QA technical evidence.

---

## 7. Reconciled ADR matrix

The historical column preserves the previous completion summary.

The rebaseline column preserves the independent audit as the current
**evidence-backed maturity** estimate. The technical P0 is closed. The evidence-backed percentages are intentionally left unchanged in this reconciliation so the still-absent FIRSTGAME consumer evidence is not silently promoted and no ad-hoc rebaseline is introduced.

| ADR | Normative status | Historical planning | Evidence-backed maturity | Delta | Current interpretation |
|---|---|---:|---:|---:|---|
| IF-ADR-001 | Accepted | 92% | **87%** | -5 | Core lifecycle strong; exceptional/session-persistent boundaries remain |
| IF-ADR-002 | Accepted | 65% | **55%** | -10 | Product authoring model is real but inconsistent across portfolio |
| IF-ADR-003 | Accepted | 84% | **77%** | -7 | Runtime + canonical QA strong; serialized migration integrity closed; current FIRSTGAME product evidence absent/deferred |
| IF-ADR-004 | Accepted | 78% | **66%** | -12 | Camera runtime exists; isolated negative QA/product proof incomplete |
| IF-ADR-005 | Accepted | 78% | **72%** | -6 | Gate/readiness authority strong; product/negative matrix incomplete |
| IF-ADR-006 | Accepted | 94% | **86%** | -8 | Transition/loading technically mature; focused product/exception gaps remain |
| IF-ADR-007 | Accepted | 96% | **85%** | -11 | Readiness contract mature; focused control-plane/product regressions remain |
| IF-ADR-008 | Accepted | 90% | **73%** | -17 | Strong product example; current idempotency/product QA evidence needs renewal |
| IF-ADR-009 | Accepted | 88% | **75%** | -13 | Runtime integrated; post-discovery negative QA/product polish remains |
| IF-ADR-010 | Proposed | 70% | **55%** | -15 | Broad Editor foundation; mandatory product-surface standard not closed |
| IF-ADR-011 | Accepted | 94% | **82%** | -12 | Runtime/QA strong; presentation and real consumer waiting/join proof remain |
| IF-ADR-012 | Accepted | 90% | **70%** | -20 | Participation technically certified; serialized migration integrity closed; current FIRSTGAME product evidence absent/deferred |
| IF-ADR-013 | Accepted / Experimental | 65% | **46%** | -19 | Narrow optional adapter; promotion/consumer evidence intentionally incomplete |
| IF-ADR-014 | Accepted | 100%* | **97%** | -3 | Essentially complete for current accepted scope |
| IF-ADR-015 | Proposed | 80% | **64%** | -16 | Current surface technically certified; serialized migration integrity closed; current FIRSTGAME product evidence absent/deferred |
| IF-ADR-016 | Accepted | 90% | **64%** | -26 | Current model implemented + QA-certified; serialized migration integrity closed; current FIRSTGAME product evidence absent/deferred |

`*` Historical 100% for IF-ADR-014 means `Complete for current accepted scope`.

Portfolio arithmetic from the independent rebaseline:

```text
(87+55+77+66+72+86+85+73+75+55+82+70+46+97+64+64) / 16
= 72.1%
```

Historical arithmetic:

```text
(92+65+84+78+78+94+96+90+88+70+94+90+65+100+80+90) / 16
= 84.6%
```

For context only, excluding the optional/experimental IF-ADR-013:

```text
evidence-backed maturity excluding ADR-013
  = 73.9%
```

Do not use the experimental exclusion to hide the ADR. It is only a second
portfolio view showing how the optional promotion program affects the aggregate.

---

## 8. Classification

### Essentially complete — >= 95%

```text
IF-ADR-014  97
```

### Mature / focused remaining gaps — 85–94%

```text
IF-ADR-001  87
IF-ADR-006  86
IF-ADR-007  85
```

### Material gaps remain — 70–84%

```text
IF-ADR-003  77  [FIRSTGAME evidence deferred]
IF-ADR-005  72
IF-ADR-008  73
IF-ADR-009  75
IF-ADR-011  82
IF-ADR-012  70  [FIRSTGAME evidence deferred]
```

### Incomplete evidence/product program — < 70%

```text
IF-ADR-002  55
IF-ADR-004  66
IF-ADR-010  55
IF-ADR-013  46
IF-ADR-015  64  [FIRSTGAME evidence deferred]
IF-ADR-016  64  [FIRSTGAME evidence deferred]
```

`< 70` does not mean the runtime is necessarily incomplete. ADR-015/016 are the
clearest examples: their package/runtime + QA state is substantially stronger
than their current real-consumer evidence.

---

## 9. How to read the Player scores

The P0 must not be used to double-penalize the same defect.

Current interpretation:

```text
IF-ADR-003
  architecture/runtime/QA = strong
  serialized integrity     = closed
  real-consumer proof      = absent/deferred

IF-ADR-012
  participation runtime/QA = strong
  serialized integrity     = closed
  real-consumer proof      = absent/deferred

IF-ADR-015
  public surface runtime   = implemented
  Q1/Q2                    = green
  serialized integrity     = closed
  final product proof      = absent/deferred
  normative status         = Proposed

IF-ADR-016
  normative status         = Accepted
  runtime                  = implemented
  technical QA             = green
  serialized integrity     = closed
  current FIRSTGAME assets = not valid current-model proof
```

Therefore the independent 72.1% portfolio number remains a useful conservative
evidence baseline, but it should not be renamed "implementation percentage".

---

## 10. Current priority order

The technical P0 is closed and is removed from the active priority queue. No ADR-010 implementation is part of this cut.

```text
1. IF-ADR-010 minimum product-surface standard
2. IF-ADR-010 package product-surface audit
3. canonical Editor QA
4. focused non-Player hardening
5. redesigned FIRSTGAME consumer proof
```

Focused non-Player hardening candidates remain:

```text
IF-ADR-004 Camera negative matrix
IF-ADR-008 Apply/Rebuild idempotency/preservation
IF-ADR-009 post-discovery negative regression
IF-ADR-005 reset/restart/pause negative matrix
```

### P5 — optional/experimental promotion

```text
IF-ADR-013 BGM
```

Do not prioritize solely because its numeric score is lowest. Promote only when
a real game needs the Route/Activity BGM behavior.

---

## 11. ADRs below 85% — current limiting dimension

| ADR | Primary limiter | Smallest legitimate next step |
|---|---|---|
| IF-ADR-002 | product consistency + QA | reassess after Player real-use and ADR-010 standard; do not pre-commit to Composer |
| IF-ADR-003 | FIRSTGAME | redesigned current Scene/Manager consumer proof when that separate consumer cut begins |
| IF-ADR-004 | QA + product proof | focused Camera negative matrix / isolated product proof |
| IF-ADR-005 | product + negative QA | one focused Pause/Restart/Reset product cut |
| IF-ADR-008 | current QA evidence | certify Apply/Rebuild idempotency and user-content preservation |
| IF-ADR-009 | current QA evidence | negative QA after scene-discovery unification |
| IF-ADR-010 | normative + product QA | freeze canonical product Inspector standard + editor QA |
| IF-ADR-011 | consumer/presentation | current waiting/joining real-consumer proof |
| IF-ADR-012 | FIRSTGAME + product | redesigned current participation authoring proof |
| IF-ADR-013 | experimental consumer proof | defer until real BGM demand |
| IF-ADR-015 | FIRSTGAME + final product disposition | redesigned consumer proof, then P5 disposition from real usage |
| IF-ADR-016 | FIRSTGAME | redesigned/rebuilt current Session consumer proof |

---

## 12. What should NOT be done to improve the numbers

Do not:

```text
reintroduce Capacity
restore a separate PlayerProvisioningProfile
create per-Slot Host Provisioning overrides
map legacy SetCapacity=30 to RequestJoin
silently migrate invalid content into a convenient supported operation
create a global Player manager/service locator
add reflection-based runtime migration
create Wizard/Composer before real friction is observed
add validators/smokes as substitutes for missing product/runtime behavior
promote BGM only to improve the portfolio average
implement Session-Persistent Player without a real requirement/ADR
```

The objective is not to maximize the percentage.

The objective is to make the percentage reflect the current product honestly.

---

## 13. Current portfolio interpretation

```text
ARCHITECTURE
  strong
  authority/lifetime boundaries mostly coherent
  Player Session consolidation is a real simplification

RUNTIME
  strong
  Player, transition, readiness/loading and identity are the strongest areas

QA
  Player canonical QA is strong and green
  several non-Player product/negative matrices remain uneven

PRODUCT / AUTHORING
  weakest transversal dimension
  good examples exist, but the standard is not portfolio-wide

FIRSTGAME
  valuable historical integration evidence
  current Player evidence is not current-model certified and is deferred to a separate redesign/rebuild

SERIALIZATION / MIGRATION
  technical integrity closed
  package identities corrected; focused QA 5/5; canonical Player QA now gates on serialization
```

---

## 14. Score governance from this point

After technical P0 closure and before any later rebaseline:

```text
historical planning average
  84.6% — preserved for traceability

current evidence-backed maturity
  72.1% — provisional current baseline

P0 technical migration integrity
  CLOSED

FIRSTGAME current Player evidence
  OPEN / DEFERRED — not current-model certified
```

At a later evidence rebaseline:

```text
re-score only the dimensions for which new evidence exists
do not mechanically restore historical percentages
do not change runtime points because a consumer proof was added
do not change FIRSTGAME points because QA passed
```

This preserves independence between the five dimensions.

---

## 15. Completion-summary consequence

```text
P0 — Serialized Player Migration Integrity
  TECHNICALLY CLOSED

FIRSTGAME product/consumer proof
  OPEN / DEFERRED
  separate redesigned consumer cut
```

Closing the technical P0 does not promote FIRSTGAME evidence and does not change the evidence-backed maturity percentages in this reconciliation.

---

## 16. Suggested next implementation sequence

```text
1. IF-ADR-010 minimum product-surface standard
2. IF-ADR-010 package product-surface audit
3. canonical Editor QA
4. focused non-Player hardening
5. redesigned FIRSTGAME consumer proof
```

ADR-010 implementation is not started by this documentation cut. FIRSTGAME remains a later, separate consumer redesign/rebuild effort.

---

## 17. Suggested commit messages for this closure cut

QA:

```text
Integrate Player serialization identity into full certification
```

Package documentation:

```text
Close Player serialized migration integrity P0
```

No FIRSTGAME commit belongs to this cut.
