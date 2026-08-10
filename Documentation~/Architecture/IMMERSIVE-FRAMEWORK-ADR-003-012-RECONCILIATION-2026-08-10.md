# Immersive Framework — IF-ADR-003 + IF-ADR-012 Technical Reconciliation

**Date:** 2026-08-10  
**Type:** Technical architecture / package / QA reconciliation  
**ADRs:** IF-ADR-003, IF-ADR-012  
**Status:** **CLOSED / RECONCILED for the current accepted technical boundaries**

## 1. Objective

Align the accepted Player lifecycle and Activity participation decisions with the
current official package line and the canonical QA evidence, then classify only
real current gaps.

This reconciliation deliberately separates technical conformance from later
real-game integration.

```text
ADR
  -> package code / official product surface
  -> QA technical proof
  -> technical gap classification
  -> only then FIRSTGAME real-consumer proof
```

## 2. Source baselines

Current repository heads inspected for this reconciliation:

```text
com.immersive.framework
  18a6c5079f7436cd86ffa1158cabfe12278855da
  Adr13A-Audio

QAFramework
  dcabd982fee949a571ced53394066ecee9cd313f
  clear
```

Canonical Player certification record:

```text
Documentation~/Architecture/IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md

certification documentation/package baseline
  43b96a4b100b8273da1190520536007ba82dc081

certification QA source baseline
  b6a45728285ddb2ce08269fc1f88ae3f1a4235e4
```

Repository comparison from those certification baselines to the current heads was
also inspected.

Result:

```text
Package changes after Player certification baseline
  documentation / ADR reconciliation
  Activity / Content
  Audio
  no Player runtime files changed

QA changes after Player certification baseline
  Audio
  Camera
  Activity Local Visibility / shared QA scenes
  no Player QA files changed
```

Therefore the later commits do not provide evidence that the certified Player
technical boundary regressed.

## 3. Scope

### In scope

```text
IF-ADR-003
  Session-scoped Player participation
  Scene-Provided / Manager-Provisioned separation
  Host vs Actor lifecycle separation
  Joining / admission boundary
  Actor selection / preparation / materialization / gameplay admission
  readiness contribution and contextual reconcile
  rejected Capacity / separate provisioning Profile semantics

IF-ADR-012
  Activity projection over Session Player state
  one normalized effective participation policy
  requested/effective/provenance diagnostics
  explicit compatibility failures
  participation/readiness projection without Session mutation

QA
  canonical full Player certification
  current Player Session / Actor / public-surface / participation evidence
```

### Out of scope

```text
FIRSTGAME construction or repair
FIRSTGAME score as technical conformance evidence
new Player runtime features
Player Leave
connect/disconnect/reconnect
Session-Persistent Player
new dynamic participation UX not already accepted
```

FIRSTGAME is the final real-consumer stage. It may reveal a new package defect,
but it is not part of this technical reconciliation verdict.

## 4. Reconciliation classifications

| Classification | Meaning |
|---|---|
| **IMPLEMENTED** | accepted ADR requirement is represented by the current official package boundary |
| **DIVERGENT** | package behavior exists but contradicts the accepted ADR |
| **ABSENT** | accepted ADR requires a package capability that is missing |
| **QA GAP** | package implementation exists but the required technical contract is not currently proven |
| **DOC / TRACKING GAP** | technical boundary is aligned but mutable documentation describes it incorrectly |
| **DEFERRED** | separate future contract; not current completion debt |

## 5. IF-ADR-003 result

### Normative boundary

IF-ADR-003 currently requires the Player lifecycle to preserve these distinctions:

```text
Session Slot configuration
Joining / admission
Local Player Host provisioning or adoption
Logical Player participation
Actor selection
Logical Actor preparation
physical Actor materialization
input / camera / gameplay admission
Activity readiness contribution
contextual release / reconcile
```

The accepted Session dependency is the current IF-ADR-016 model:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
    Scene Provided
    Manager Provisioned
  Actor Resolution
```

Rejected current-model behavior includes Capacity as a second admission limit,
a separate `PlayerProvisioningProfile`, per-Slot Host Provisioning override,
consumer Slot reservation and consumer preparation/materialization authority.

### Package / QA confrontation

The canonical Player certification proves:

```text
serialization       PASS
session             PASS
sceneProvided       PASS
managerProvisioned  PASS
actor               PASS
publicSurface       PASS
participation       PASS
```

Representative certified coverage includes:

```text
Player Participation Authoring        7 cases
Scene-Provided route / negative        25 cases
Manager public contract                9 cases
Manager waiting projection            14 cases
Actor selection runtime binding       13 cases
Player gameplay admission            114 cases
Public Surface Q1                     28 cases
Public Surface Q2                     36 cases
Activity Session Projection           30 cases
```

### Classification

```text
IMPLEMENTED
  current Session / Slot / Host / Actor authority shape
  Scene-Provided and Manager-Provisioned peer modes
  typed public command / observation boundary used by normal consumers
  no-Capacity current model
  explicit failure / negative-state diagnostics

DIVERGENT
  none identified in the current accepted boundary

ABSENT
  none identified in the current accepted boundary

QA GAP
  none identified for the current accepted boundary

DOC / TRACKING GAP
  FIRSTGAME was still being used as the primary limiter of ADR-003 completion
  even though the technical Player boundary is already certified

DEFERRED
  Player Leave
  device disconnect / reconnect
  Session-Persistent Player
```

### ADR-003 verdict

```text
Normative status          ACCEPTED
Package                    IMPLEMENTED for current accepted boundary
Technical QA               CERTIFIED
Stage A technical status   CLOSED / RECONCILED
Current technical blockers NONE IDENTIFIED
FIRSTGAME                  NEXT STAGE; not a technical blocker
```

## 6. IF-ADR-012 result

### Normative boundary

IF-ADR-012 requires Activity Player policy to project current Session Player/Slot
state without creating another Session authority.

```text
PlayerSessionProfile
  owns Supported Slots
  owns Initial Joining
  owns Session Host Provisioning
  owns Actor Resolution

Activity Player policy
  projects / qualifies current Session Slots
  defines participation / readiness intent
  resolves to one effective policy
  preserves provenance and requested/effective diagnostics
  does not replace Session provisioning
  does not create Capacity
```

Invalid or incompatible required state must fail explicitly; no silent fallback is
allowed.

### Package / QA confrontation

The canonical Player certification includes:

```text
participation='PASS'
Activity Session Projection — 30 cases PASS
```

The 30-case projection regression proves, among other things:

```text
ExplicitActivitySubset
SessionExpansionWithoutProjectionExpansion
ActivityExitPreservesSession
OccurrenceProjectionReplacement
PlayerContributionNotAggregateGate
ReadOnlyIdempotency
```

The recorded run also proves that a second Session Player may join while the
current Activity projection remains stable and that an excluded Slot is not
materialized by that Activity projection.

### Classification

```text
IMPLEMENTED
  Activity projection remains separate from Session authority
  effective participation is occurrence-aware
  excluded Session Players do not silently expand current projection
  public read is idempotent
  participation contributes to readiness without becoming aggregate authority
  diagnostics/evidence are available through the certified Player surface

DIVERGENT
  none identified in the current accepted boundary

ABSENT
  none identified in the current accepted boundary

QA GAP
  none identified for the current accepted boundary

DOC / TRACKING GAP
  FIRSTGAME was still being used as the primary limiter of ADR-012 completion
  even though the current Activity participation technical integration is certified

DEFERRED
  broader dynamic participation/product UX only when separately proposed
  future Player persistence / reconnect behavior owned by separate contracts
```

### ADR-012 verdict

```text
Normative status          ACCEPTED
Package                    IMPLEMENTED for current accepted boundary
Technical QA               CERTIFIED
Stage A technical status   CLOSED / RECONCILED
Current technical blockers NONE IDENTIFIED
FIRSTGAME                  NEXT STAGE; not a technical blocker
```

## 7. Product-surface disposition

This reconciliation does **not** infer that every Player feature needs another
Composer, Wizard or Apply/Rebuild layer.

The current accepted product model already treats direct authoring and reusable
Profile intent as valid shapes when they are explicit and diagnosable.

For ADR-003 / ADR-012, no additional generic product-authoring layer is opened by
this cut merely to increase a score.

If later FIRSTGAME use exposes a concrete repeated authoring problem, that finding
must be classified and returned to the smallest owning package surface.

## 8. FIRSTGAME disposition

FIRSTGAME remains pending for current-model Player real-consumer proof, but that
is a separate Stage B status:

```text
Stage A
  ADR-003  CLOSED / QA CERTIFIED
  ADR-012  CLOSED / QA CERTIFIED

Stage B
  current-model Player integration       PENDING
  current-model participation integration PENDING
```

Do not create compatibility behavior for the historical Capacity / separate
provisioning-Profile model just to make old FIRSTGAME serialized content work.
When Stage B is executed, reauthor the consumer against the official current model.

## 9. Files affected by this documentation cut

Edited:

```text
Documentation~/Architecture/Tracking/IF-TRACK-Framework.md
```

Created:

```text
Documentation~/Architecture/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md
```

Removed:

```text
none
```

The normative ADR files are not changed by this cut because their current accepted
shape already matches the certified Player model. Historical dated audits and
completion summaries are not rewritten.

## 10. Acceptance criteria

### Technical

```text
PASS  current ADR-003 contract is not contradicted by certified Player behavior
PASS  current ADR-012 contract is not contradicted by certified participation behavior
PASS  canonical Player QA is certified for the current accepted model
PASS  no later Player runtime / Player QA change invalidates that certification evidence
PASS  deferred Player contracts are not counted as current gaps
PASS  FIRSTGAME is removed from the Stage A technical verdict
```

### Product / process

```text
PASS  tracker now distinguishes technical reconciliation from consumer proof
PASS  current Player FIRSTGAME work remains visible as later Stage B evidence
PASS  UX findings remain qualitative until they identify a concrete owning package defect
PASS  no generic Composer / Wizard / Apply-Rebuild requirement is invented
```

## 11. Architectural gain

```text
technical truth comes from ADR + package + QA
consumer truth comes later from FIRSTGAME
future contracts remain future contracts
historical consumer data cannot redefine current runtime authority
```

## 12. Usability gain

The team can now continue the ADR-by-ADR reconciliation without spending time
rebuilding FIRSTGAME between technical audits. Real-game integration is preserved
as the final proof stage, where it can focus on actual authoring/usability rather
than compensating for unresolved framework contracts.

## 13. Suggested commit message

```text
Reconcile ADR-003 and ADR-012 technical Player status
```
