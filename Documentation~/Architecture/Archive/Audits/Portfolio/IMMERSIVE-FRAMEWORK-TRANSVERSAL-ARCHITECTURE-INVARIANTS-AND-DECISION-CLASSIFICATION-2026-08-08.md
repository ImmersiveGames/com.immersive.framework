> **Archived historical snapshot.**
> This file is preserved for traceability and is not current product/status authority.
> Use `Architecture/ADRs/` for decisions and `Architecture/Tracking/IF-TRACK-Framework.md` for current status.

# Immersive Framework — Transversal Architecture Invariants and Decision Classification

> Historical conceptual baseline. Any Current Capacity invariant in this record
> is superseded by `IF-ADR-016` dated 2026-08-09 and is not normative.

**Date:** 2026-08-08  
**Status:** Conceptual baseline for normalization of the Player / Session / Activity decision matrix  
**Project:** Immersive Framework 1.1 / Unity 6.5

> This document defines the transversal/global architectural invariants that sit above domain-specific Player decisions.
>
> Its purpose is to reduce duplicated decisions in the Player / Session / Activity matrix by separating:
>
> - global architectural principles;
> - genuine Player-domain decisions;
> - derived validation/runtime rules;
> - rejected historical shapes;
> - items that still require technical reconciliation.
>
> This document is **not yet an ADR reconciliation**. Existing ADRs and current source/runtime must still be confronted after normalization.

---

# 1. Purpose

The current Player / Session / Activity decision matrix contains decisions from different abstraction levels mixed together.

The normalization target is:

```text
GLOBAL INVARIANT
    architectural rule that applies across framework systems

DOMAIN DECISION
    actual Player / Session / Activity model decision

DERIVED RULE
    logical consequence of global invariants + domain decisions

HISTORICAL / REJECTED
    previously considered shape that must not return without evidence

TECHNICAL REVIEW
    concept that still needs reconciliation with current ADR/source/runtime
```

The main objective is to avoid repeatedly treating consequences of already-settled global rules as new architectural questions.

---

# 2. Transversal invariants — Layer 0

| ID | Invariant | Rule | Status |
|---|---|---|---|
| X01 | Single runtime authority | The runtime owner of a piece of mutable truth is the only authority that can actually change it. Other systems request operations. | **Global candidate — closed conceptually** |
| X02 | Intent is not runtime state | Profile/Recipe/authoring declares intent. Once runtime authority exists, its current state is not a continuously synchronized copy of the asset. | **Global candidate — closed conceptually** |
| X03 | Explicit mutation | Relevant runtime state changes happen through explicit capabilities/commands. Entering a Route/Activity or resolving a Profile must not cause hidden lateral mutations unless that behavior is itself an explicit contract. | **Global candidate — closed conceptually** |
| X04 | Observation is not command | Read-only observation/evidence never grants mutation authority. Observation and Commands remain separate public surfaces. | **Global candidate — closed conceptually** |
| X05 | Explicit scope and lifetime | Contexts, sessions, inhibits, snapshots, services and temporary authorities require explicit scope/lifetime. No implicit global lookup should define ownership. | **Global candidate — closed conceptually** |
| X06 | No silent fallback | Missing, contradictory or rejected configuration/operation must produce explicit error/evidence. Absence must not silently acquire a convenient semantic. | **Global candidate — closed conceptually** |
| X07 | Structural invalidity differs from runtime condition | Authoring rejects only contradictions the framework can prove structurally. Valid configurations may still become temporarily or occurrence-specifically unsatisfiable at runtime. | **Global candidate — closed conceptually** |
| X08 | Proven impossibility fails explicitly | Runtime must not remain in indefinite waiting once it can prove that the current occurrence can no longer satisfy a required condition. | **Global candidate — closed conceptually** |
| X09 | Snapshot differs from live state | Historical evidence for a specific occurrence is frozen separately from runtime state that continues evolving. | **Global candidate — closed conceptually** |
| X10 | Framework responsibility differs from gameplay responsibility | Framework owns technical truth, contracts, evidence, capabilities and lifecycle it genuinely owns. Game owns UI, interaction timing, game rules and orchestration policy. | **Global candidate — closed conceptually** |
| X11 | Distinct identities/layers are not collapsed | Logical identity, runtime host, logical domain object, physical representation and gameplay-state concepts stay separate when they have different ownership/lifetime. | **Global candidate — closed conceptually** |
| X12 | Context declares need; authority executes | A Route/Activity/context may declare a requirement or restriction, but execution remains with the runtime authority that owns the affected state. | **Global candidate — closed conceptually** |
| X13 | Ensure/Reconcile over blind reconstruction | If a valid instance/state already exists, the framework reuses or reconciles it instead of reconstructing it merely because context changed. | **Global candidate — closed conceptually** |
| X14 | Extension does not create a second authority | Game extensions may observe public evidence and invoke public capabilities, but may not mutate private truth, force readiness, bypass scope or become a parallel authority. | **Global candidate — closed conceptually** |
| X15 | Typed contextual reactions | Reusable reaction flow should prefer typed Fact → Condition → Action → Result/Evidence, with context-specific authoring rather than string-driven buses or an unbounded universal mini visual-scripting system. | **Global candidate — closed conceptually** |

---

# 3. Candidate invariant intentionally not promoted yet

## X16 — Inherit by default + deliberate Override

Current Player Activity design strongly benefits from:

```text
Default source
    ↓
Inherit by default

Only when design intentionally differs:
    Override
```

However, this should **not yet** be promoted to a universal framework invariant.

Reason:

```text
some systems may benefit from inheritance
some systems may require explicit local configuration
some systems may have no stable parent/default concept
```

Therefore:

| ID | Candidate | Status |
|---|---|---|
| X16 | Prefer `Inherit` + deliberate `Override` when successive units normally share stable intent. | **Reserved for final cross-system review** |

---

# 4. Consequences for the existing Player matrix

The following sections identify which current Player decisions are likely applications of the transversal invariants rather than independent architectural decisions.

The original decision IDs remain useful for traceability, but their role changes.

---

# 5. X01 + X03 — authority and explicit mutation

These global rules explain the architectural basis of:

```text
S04
Runtime becomes authority after Session creation.

S05
Capacity / Joining and equivalent mutable Session state change through explicit capabilities.

S06
Route / Activity do not reapply Session Profile automatically.

S09
Activity accepting Slots does not raise Session Capacity.

S10
Capacity changes require an explicit request.

PH07
Player runtime owns the actual physical-presence operation.

C04
Commands are requests to existing authorities, not raw setters.
```

Normalization target:

```text
X01 + X03
    ↓
domain-specific applications
```

These domain lines may remain as concrete rules, but no longer need to be treated as separate architectural principles.

---

# 6. X02 — Profile/authoring intent is not live runtime state

This global invariant explains the basis of:

```text
S03
Player Session Profile initializes Session once.

PR06
Effective Player Provisioning Profile stays stable for the Session.

PR07
Route / Activity do not replace the Provisioning Profile automatically.

A09
Effective Player Activity Profile is resolved for an occurrence and stays stable during it.
```

Important distinction:

```text
X02 answers:
"Does the asset continuously drive mutable runtime truth?"
    → No.

Domain decision answers:
"What is the lifetime of the resolved effective configuration?"
    → Session / Activity occurrence / other explicit scope.
```

Therefore, lifetime-specific decisions remain domain decisions, but continuous asset-driven mutation is already eliminated globally.

---

# 7. X04 — observation vs command

This invariant is the architectural root of:

```text
C01
Gameplay can observe without mutation authority.

C02
Activity has a Slot-centered observation projection.

C03
Session has its own observation surface.

C04
Commands request operations from authorities.
```

Normalization target:

```text
X04
    establishes the global boundary

Player-domain matrix
    only needs to define:
        what is observable
        at what scope
        through which typed projection
        which commands are legitimate
```

The Player matrix does not need to repeatedly justify why reading state must not mutate it.

---

# 8. X05 — explicit scope and lifetime

This invariant explains the architectural basis of:

```text
J05
Multiple Join Inhibits need identity.

J06
An owner releases only its own inhibit.

J07
Inhibit carries Owner + Scope/Lifetime + Reason/Evidence.

J08
Consumer-created inhibit must use a typed/scoped public capability.
```

The remaining Player-specific questions are:

```text
Which inhibit types/scopes exist?
When are they acquired?
What does RequestJoin report?
```

The ownership model itself is transversal.

---

# 9. X06 + X07 — explicit validation

These invariants explain why the following are validation rules rather than independent architectural principles:

```text
A08
Activity Inherit without a resolvable Route default is invalid.

W07
Explicit Who Participates cannot reference unsupported Slots.

E10
At Least N cannot exceed eligible Slot count.

W08
No Slots cannot require Player readiness or physical presence.

PH11
Route Suppress + Activity Require is invalid.

PH12
Route Suppress + readiness requiring physical presence is invalid.
```

Normalization target:

```text
Derived Validation Rules
    V01
    V02
    V03
    ...
```

Each validation rule should reference the global invariant(s) and domain decision(s) from which it derives.

Example:

```text
V03
At Least N > Eligible Slots
    → authoring error

Derived from:
    X06 No silent fallback
    X07 Structural invalidity vs runtime condition
    E07 Coverage semantics
    W05 Who Participates modes
```

---

# 10. X07 + X08 — runtime satisfiability

These invariants explain:

```text
E11
Insufficient current Capacity does not automatically invalidate a structurally valid Profile.

E12
While satisfiable → Preparing.
When provably unsatisfiable → Failed.
```

Normalization target:

```text
Global rule:
    structural validity != runtime satisfiability

Player-specific rule:
    define how satisfiability is calculated for:
        At Least N
        All Occupied
        All Eligible
        zero-player policy
```

The algorithm/criteria remain Player-domain work; the wait-vs-fail principle is transversal.

---

# 11. X09 — snapshot vs live state

This invariant explains the common basis of:

```text
E03
Late join does not reopen committed Entry Readiness.

T03
Entry Participation Snapshot captures the question/cohort/rule.

T04
Player lifecycle evidence remains live during Preparing.

T05
Committed Entry snapshot becomes immutable historical evidence.

T06
Current Activity Participation remains a live projection after Commit.

A09
Effective Activity configuration is stable for the occurrence.
```

Normalization target:

```text
X09
    snapshot and live truth are distinct

Player domain
    defines:
        what gets frozen
        when it gets frozen
        what remains live
        how historical evidence is exposed
```

---

# 12. X10 — framework vs gameplay

This invariant is the root of much of the existing `G` section:

```text
P04
Gameplay chooses when/why Actor selection/change occurs.

G01
Framework owns technical truth/contracts/capabilities.

G02
Game owns UI/interaction/game rules.

G03
Framework does not own generic character-select flow.

G04
Actor unresolved does not imply a global Selection Pending flow.

G07
Framework does not validate that the game has an accessible UI path.

G08
Framework exposes unresolved dependency clearly.

G09
Framework hard-errors only intrinsic contradictions.

R01
Runtime reports typed failure; it does not know Lobby/Menu/etc.

R02
Game chooses recovery.
```

This invariant also explains why several earlier proposals were rejected:

```text
Allow Reselect
Require Reselect
Activity Reentry Behavior
global Selection Pending
framework-owned UI-path validation
```

Normalization target:

```text
keep X10 as the principle

keep only Player-specific applications where they define actual contracts
```

---

# 13. X11 — distinct identities and layers

This invariant is the common basis of:

```text
P01–P06
PH01
```

Player-specific identity model still needs to remain explicit:

```text
PlayerSlotId
Player Host
Actor
Physical Representation
Gameplay state
```

But repeated statements of:

```text
X is not Y
```

should be consolidated into a single domain model section instead of appearing as many independent decisions.

---

# 14. X12 + X13 — contextual requirement and materialization

These invariants explain:

```text
PH05
Physical Presence Require triggers ensure/reconcile at structural points.

PH07
Player runtime owns the operation.

PH08
Existing valid physical representation can be reused.
```

They also explain why these shapes were rejected:

```text
Activity rematerializes every Player on every Activity entry.

Route change automatically destroys/dematerializes Player.
```

Player-domain work that remains:

```text
Which contexts can declare physical presence intent?

Which structural lifecycle points request ensure/reconcile?

What counts as valid physical availability?

How is Suppress represented publicly?
```

---

# 15. X14 + X15 — extension and reactions

These invariants explain most of the general reaction architecture:

```text
R03
Official authoring should exist for Entry-failure recovery.

R04
Typed facts / conditions / actions / results.

R05
Context-specific authoring instead of universal visual scripting.

R06
Game can extend through public contracts.

R07
Extensions cannot bypass authority or scope.
```

Normalization target:

```text
Global reaction/extension contract
    X14 + X15

Player-specific application
    Activity Entry Failure Recovery
```

The Player matrix should not carry the entire reaction architecture as though it were unique to Player.

---

# 16. Genuine Player-domain decisions that must remain

The following are not trivial consequences of global invariants and should remain as first-class Player model decisions.

## Session model

```text
PlayerSlotId as stable Player identity
Supported Slots vs Current Capacity
first-available Slot allocation policy
Slot identity remains stable after other Players leave
```

## Provisioning model

```text
Player Provisioning Profile
Scene Provided vs Manager Provisioned Host
Host provisioning separate from Actor resolution
Default Actor vs unresolved/external Actor resolution
Provisioning intent stable for Session / late joins
```

## Player Activity model

```text
Route provides Default Player Activity Profile
Activity uses Inherit or complete Override
Override is local to that Activity
effective Profile is occurrence-scoped
```

## Participation

```text
Who Participates:
    All Supported Slots
    Explicit Slots
    No Slots

vacant eligible Slots remain relevant
Activity scope is Slot-based
```

## Entry readiness

```text
Ready When stages
Physical Actor Available stage
Required Coverage:
    At Least N
    All Occupied
    All Eligible

If No Players Are Available:
    Allow Empty Entry
    Require Player
```

## Joining

```text
Joining Intent
Join Inhibits
RequestJoin admission semantics
first-free Slot assignment
```

## Physical Presence

```text
Activity:
    No Requirement
    Require

Route:
    Preserve Existing
    Suppress

Require applies to current participants / late joins
Require is not respawn
```

These remain genuine model decisions even after transversal normalization.

---

# 17. Proposed normalized document structure

The Player architecture document should eventually be reorganized into:

```text
1. Global / Transversal Invariants
   X01–X15
   X16 candidate

2. Player Domain Model
   identities
   Session
   provisioning
   Activity participation
   physical presence

3. Domain Policies
   Slot allocation
   coverage
   zero-player behavior
   inheritance
   Join admission

4. Derived Validation Rules
   V01...
   V02...
   V03...

5. Derived Runtime Behaviors
   B01...
   B02...
   B03...

6. Explicitly Rejected / Historical Shapes

7. Technical Review / ADR Reconciliation
```

This separates:

```text
principle
    from
domain decision
    from
derived consequence
```

---

# 18. Classification scheme for the existing decision matrix

Every current decision ID should be reclassified using exactly one primary category:

| Category | Meaning |
|---|---|
| `GLOBAL` | General architectural invariant that applies beyond Player. |
| `DOMAIN DECISION` | Genuine Player/Session/Activity model decision. |
| `DERIVED RULE` | Consequence of global invariant(s) + domain decision(s). |
| `HISTORICAL / REJECTED` | Previously considered shape explicitly rejected or deferred. |
| `TECHNICAL REVIEW` | Concept requires reconciliation with ADR/source/runtime before being considered official. |

A decision may retain traceability to multiple global invariants, but should have one primary classification.

Example:

```text
S10
"Capacity increases or decreases only through explicit request."

Primary classification:
    DERIVED RULE

Derived from:
    X01 Single runtime authority
    X03 Explicit mutation

Domain anchor:
    Session owns Current Capacity
```

Example:

```text
E07
"Coverage = At Least N / All Occupied / All Eligible."

Primary classification:
    DOMAIN DECISION
```

Example:

```text
PH11
"Route Suppress + Activity Require is invalid."

Primary classification:
    DERIVED RULE

Derived from:
    X06 No silent fallback
    X07 Structural invalidity vs runtime condition

Domain anchors:
    Route Suppress
    Activity Require
```

---

# 19. Normalization rule

When revising the Player decision matrix:

> A line should not remain a first-class architectural decision if it is fully determined by an already accepted transversal invariant plus an existing domain decision.

Instead, move it to:

```text
Derived Validation Rule
or
Derived Runtime Behavior
```

This prevents the architecture discussion from reopening already-settled principles through domain-specific examples.

---

# 20. Reopening rule

A transversal invariant should only be reopened when concrete evidence shows one of these:

```text
an existing ADR defines incompatible semantics

current runtime architecture makes the invariant invalid

another framework domain exposes a contradiction

FIRSTGAME demonstrates a real product failure

QA demonstrates the rule cannot be implemented/tested consistently
```

A domain-specific example alone is not sufficient reason to reopen the global rule unless it demonstrates an actual contradiction.

---

# 21. Next work order

Before Player gap analysis:

```text
1. Keep the existing Player decision matrix as traceability source.
2. Apply the classification:
      GLOBAL
      DOMAIN DECISION
      DERIVED RULE
      HISTORICAL / REJECTED
      TECHNICAL REVIEW
3. Remove duplicated architectural weight from derived rules.
4. Produce a normalized Player model.
5. Only then perform the genuine gap scan.
6. After gap correction, confront:
      existing ADRs
      current package source/runtime
      QA contracts
      FIRSTGAME integration needs
7. Classify reconciled decisions as:
      already defined
      compatible refinement
      new ADR / ADR update required
      conflict
```

---

# 22. Baseline principle

The goal of this transversal layer is not to reduce documentation by deleting useful details.

The goal is to ensure that every detail appears at the correct abstraction level:

```text
Global invariant
    explains architecture

Domain decision
    defines Player behavior/model

Derived rule
    proves consequence

Historical/rejected record
    prevents regression

Technical review
    prevents conceptual decisions from being mistaken for existing official contracts
```

This document should therefore be used together with the Player / Session / Activity decision matrix during normalization and later ADR reconciliation.
