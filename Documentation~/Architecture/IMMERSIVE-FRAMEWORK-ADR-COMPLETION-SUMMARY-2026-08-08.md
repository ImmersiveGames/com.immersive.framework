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
  documentation HEAD:
    bc851304347df0b8460affaa2695fdba5a32fbe6
    Docs

  Player runtime migration baseline:
    4662fade4e27e2c06b6daf4485d2829e4fb24096
    R1 — Consolidar Player Session Authoring

QAFramework
  219cc22e2267d8222da7665807f1175edb64042c
  Player QA

FIRSTGAME / planet-devourer
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
```

The package documentation HEAD is later than R1 but does not change the Player
runtime schema relevant to this reconciliation.

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

## 3. Critical P0 discovered during reconciliation

### P0 — Serialized Player Migration Integrity

Status:

```text
CONFIRMED / OPEN
```

The current package changed serialized enum meaning across the ADR-016
consolidation.

Pre-R1:

```text
OpenJoining                   = 10
CloseJoining                  = 20
SetCapacity                   = 30
RequestJoin                   = 40
RequestDefaultActorSelection  = 50
```

R1/current runtime:

```text
OpenJoining                   = 10
CloseJoining                  = 20
RequestJoin                   = 30
RequestDefaultActorSelection  = 40
```

This creates two silent semantic collisions:

```text
30
  old = SetCapacity
  new = RequestJoin

40
  old = RequestJoin
  new = RequestDefaultActorSelection
```

Current FIRSTGAME contains a real pre-R1-schema component with:

```yaml
operation: 40
requestedCapacity: 1
```

The obsolete `requestedCapacity` field proves the component payload was authored
under the old schema, where `40` meant `RequestJoin`.

The current code interprets `40` as `RequestDefaultActorSelection`.

Therefore the current FIRSTGAME contains a confirmed serialized semantic remap.

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

The canonical QA verdict remains:

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

Representative evidence:

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

This is strong technical evidence for the current no-Capacity model.

It does not prove migration safety for pre-R1 serialized consumer content. That
is the separate P0 identified above.

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
  P0 BLOCKED
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
**evidence-backed maturity** estimate. The P0 is recorded as a gate rather than
subtracting the same consumer defect a second time.

| ADR | Normative status | Historical planning | Evidence-backed maturity | Delta | Current interpretation |
|---|---|---:|---:|---:|---|
| IF-ADR-001 | Accepted | 92% | **87%** | -5 | Core lifecycle strong; exceptional/session-persistent boundaries remain |
| IF-ADR-002 | Accepted | 65% | **55%** | -10 | Product authoring model is real but inconsistent across portfolio |
| IF-ADR-003 | Accepted | 84% | **77%** | -7 | Runtime + canonical QA strong; **P0 blocks current FIRSTGAME proof** |
| IF-ADR-004 | Accepted | 78% | **66%** | -12 | Camera runtime exists; isolated negative QA/product proof incomplete |
| IF-ADR-005 | Accepted | 78% | **72%** | -6 | Gate/readiness authority strong; product/negative matrix incomplete |
| IF-ADR-006 | Accepted | 94% | **86%** | -8 | Transition/loading technically mature; focused product/exception gaps remain |
| IF-ADR-007 | Accepted | 96% | **85%** | -11 | Readiness contract mature; focused control-plane/product regressions remain |
| IF-ADR-008 | Accepted | 90% | **73%** | -17 | Strong product example; current idempotency/product QA evidence needs renewal |
| IF-ADR-009 | Accepted | 88% | **75%** | -13 | Runtime integrated; post-discovery negative QA/product polish remains |
| IF-ADR-010 | Proposed | 70% | **55%** | -15 | Broad Editor foundation; mandatory product-surface standard not closed |
| IF-ADR-011 | Accepted | 94% | **82%** | -12 | Runtime/QA strong; presentation and real consumer waiting/join proof remain |
| IF-ADR-012 | Accepted | 90% | **70%** | -20 | Participation technically certified; **P0 blocks current FIRSTGAME proof** |
| IF-ADR-013 | Accepted / Experimental | 65% | **46%** | -19 | Narrow optional adapter; promotion/consumer evidence intentionally incomplete |
| IF-ADR-014 | Accepted | 100%* | **97%** | -3 | Essentially complete for current accepted scope |
| IF-ADR-015 | Proposed | 80% | **64%** | -16 | Current surface technically certified; **P0 blocks consumer/product closure** |
| IF-ADR-016 | Accepted | 90% | **64%** | -26 | Current model implemented + QA-certified; **P0 blocks current FIRSTGAME proof** |

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
IF-ADR-003  77  [P0 FIRSTGAME gate]
IF-ADR-005  72
IF-ADR-008  73
IF-ADR-009  75
IF-ADR-011  82
IF-ADR-012  70  [P0 FIRSTGAME gate]
```

### Incomplete evidence/product program — < 70%

```text
IF-ADR-002  55
IF-ADR-004  66
IF-ADR-010  55
IF-ADR-013  46
IF-ADR-015  64  [P0 FIRSTGAME gate]
IF-ADR-016  64  [P0 FIRSTGAME gate]
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
  real-consumer proof      = blocked

IF-ADR-012
  participation runtime/QA = strong
  real-consumer proof      = blocked

IF-ADR-015
  public surface runtime   = implemented
  Q1/Q2                    = green
  final product proof      = blocked
  normative status         = Proposed

IF-ADR-016
  normative status         = Accepted
  runtime                  = implemented
  technical QA             = green
  current FIRSTGAME assets = not valid current-model proof
```

Therefore the independent 72.1% portfolio number remains a useful conservative
evidence baseline, but it should not be renamed "implementation percentage".

---

## 10. Current priority order

### P0 — Serialized Player Migration Integrity

Package first:

```text
preserve serialized IDs of still-supported commands
retire former Capacity ID without reuse
unsupported legacy value must fail explicitly
```

QA second:

```text
add serialized migration-integrity regression
prove 30 does not execute Join
prove legacy 40 remains Join
prove legacy 50 remains Default Actor Selection
```

FIRSTGAME third:

```text
reauthor current PlayerSessionProfile assets
verify command triggers intentionally
run Scene-Provided
run Manager-Provisioned
record real-consumer proof
```

This is a technical integrity cut. It precedes usability scoring because a real
consumer should not be evaluated on semantically stale serialized content.

### P1 — Player real-consumer proof

After P0:

```text
prove current Supported-Slots model manually
prove Scene-Provided
prove Manager-Provisioned
prove scoped commands/status without internal knowledge
capture actual authoring friction
```

### P2 — IF-ADR-015 product disposition / P5

Only after real use:

```text
NO ADDITIONAL TOOLING REQUIRED

or

smallest justified:
  Create action
  template
  Inspector remediation
  Composer
  other focused authoring aid
```

Do not create Wizard/Composer merely to satisfy an abstract pattern.

### P3 — IF-ADR-010 canonical product-surface standard

Freeze a minimum mandatory product standard and exceptions:

```text
designer intent first
explicit validation
readable status
safe explicit remediation
Advanced / Debug technical evidence
runtime state read-only
Apply/Rebuild only where technical materialization exists
Undo/prefab-stage/non-destructive expectations
```

Then reassess IF-ADR-002 based on actual portfolio consistency.

### P4 — focused QA/product hardening outside Player

Highest-value existing candidates:

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
| IF-ADR-003 | FIRSTGAME | close P0, then current Scene/Manager consumer proof |
| IF-ADR-004 | QA + product proof | focused Camera negative matrix / isolated product proof |
| IF-ADR-005 | product + negative QA | one focused Pause/Restart/Reset product cut |
| IF-ADR-008 | current QA evidence | certify Apply/Rebuild idempotency and user-content preservation |
| IF-ADR-009 | current QA evidence | negative QA after scene-discovery unification |
| IF-ADR-010 | normative + product QA | freeze canonical product Inspector standard + editor QA |
| IF-ADR-011 | consumer/presentation | current waiting/joining real-consumer proof |
| IF-ADR-012 | FIRSTGAME + product | close P0, then current participation authoring proof |
| IF-ADR-013 | experimental consumer proof | defer until real BGM demand |
| IF-ADR-015 | FIRSTGAME + final product disposition | close P0, prove current surface, then P5 |
| IF-ADR-016 | FIRSTGAME | close P0 and reauthor current Session Profiles |

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
  current Player evidence is blocked by P0 serialized migration integrity

SERIALIZATION / MIGRATION
  newly identified product-contract risk
  must be fixed before current Player real-consumer certification
```

---

## 14. Score governance from this point

Until P0 closes:

```text
historical planning average
  84.6% — preserved for traceability

current evidence-backed maturity
  72.1% — provisional current baseline

Player FIRSTGAME promotion
  frozen for ADR-003 / 012 / 015 / 016
```

After P0 + FIRSTGAME current-model proof:

```text
re-score only the dimensions for which new evidence exists
do not mechanically restore historical percentages
do not change runtime points because a consumer proof was added
do not change FIRSTGAME points because QA passed
```

This preserves independence between the five dimensions.

---

## 15. Completion-summary consequence

The previous priority:

```text
P0 — Player real-consumer proof
```

is replaced by:

```text
P0 — Serialized Player Migration Integrity
P1 — Player real-consumer proof
P2 — IF-ADR-015 final product disposition
```

because real-consumer usability cannot be evaluated reliably while committed
consumer authoring data has a confirmed semantic serialization collision.

---

## 16. Suggested next implementation sequence

```text
PACKAGE
  PlayerProvisioningCommandOperation serialized ID correction

QA
  IF-PLAYER-SERIALIZATION-01

FIRSTGAME
  Demo02 current PlayerSessionProfile reauthoring
  command trigger verification
  Scene-Provided proof
  Manager-Provisioned proof

DOCUMENTATION
  mark P0 closed
  attach FIRSTGAME evidence
  re-score affected dimensions only
```

---

## 17. Suggested commit messages

Package:

```text
Fix Player command serialized operation identities
```

QA:

```text
Add Player command serialization migration regression
```

FIRSTGAME:

```text
Migrate Demo02 Player authoring to current Session model
```

Documentation:

```text
Reconcile ADR completion rebaseline after Player migration audit
```
