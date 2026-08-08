# Immersive Framework — Normalized Player Decision Classification

**Date:** 2026-08-08  
**Status:** Mechanical normalization completed; gap analysis not started  
**Project:** Immersive Framework 1.1 / Unity 6.5

## Source baseline

This normalization is based only on these two conceptual documents:

1. `IMMERSIVE-FRAMEWORK-PLAYER-SESSION-ACTIVITY-DECISION-MATRIX-2026-08-08.md`
2. `IMMERSIVE-FRAMEWORK-TRANSVERSAL-ARCHITECTURE-INVARIANTS-AND-DECISION-CLASSIFICATION-2026-08-08.md`

No ADR/source/runtime reconciliation was performed in this pass.

The purpose of this document is to classify every decision from the original Player matrix into one primary category:

```text
GLOBAL
DOMAIN DECISION
DERIVED RULE
HISTORICAL / REJECTED
TECHNICAL REVIEW
```

A `DOMAIN DECISION` may still require later source/ADR reconciliation. The classification answers **what kind of decision it is**, not whether the current package already implements it.

---

# 1. Result summary

Total original decision IDs classified: **113**

| Classification | Count |
|---|---:|
| GLOBAL | 11 |
| DOMAIN DECISION | 62 |
| DERIVED RULE | 31 |
| HISTORICAL / REJECTED | 2 |
| TECHNICAL REVIEW | 7 |

The main normalization result is:

```text
Original matrix
    113 individually weighted decisions

After classification
    62 genuine Player-domain decisions
    31 derived rules/behaviors
    11 global rules folded into X01–X15
    2 historical/rejected IDs
    7 explicit technical-review IDs
```

This does **not** mean only 62 items matter. It means the architecture should stop treating global principles and their consequences as independent domain decisions.

---

# 2. Complete mechanical classification

| ID | Primary classification | Basis / rationale | Normalized destination | Reconciliation note |
|---|---|---|---|---|
| P01 | **DOMAIN DECISION** | Player-specific stable logical identity. | Keep: Player Domain Model |  |
| P02 | **DOMAIN DECISION** | Player ownership belongs to Session. | Keep: Player Domain Model |  |
| P03 | **DOMAIN DECISION** | Player Slot may exist without a resolved Actor. | Keep: Player Domain Model |  |
| P04 | **DERIVED RULE** | Derived from X10 plus P03: game chooses when/why Actor selection/change is requested. | Move: Derived Runtime/Responsibility Rule |  |
| P05 | **DOMAIN DECISION** | Defines what an accepted Join establishes in Session. | Keep: Player Domain Model |  |
| P06 | **DOMAIN DECISION** | Player-specific identity/layer separation under X11. | Keep: Player Domain Model |  |
| S01 | **TECHNICAL REVIEW** | Conceptual placement of Default Player Session Profile in GameApplication must be reconciled with existing GameApplication responsibility. | Review queue | GameApplication ownership/source |
| S02 | **DOMAIN DECISION** | Defines Session-creation default vs explicit override semantics. | Keep: Session Configuration | Depends on final S01 owner |
| S03 | **DERIVED RULE** | Derived from X02: Profile initializes intent; it does not continuously drive runtime state. | Move: Derived Runtime Behavior |  |
| S04 | **DERIVED RULE** | Derived from X01: Session runtime becomes mutable authority after creation. | Move: Derived Runtime Behavior |  |
| S05 | **DERIVED RULE** | Derived from X01 + X03: mutable Session state changes through explicit capabilities. | Move: Derived Runtime Behavior |  |
| S06 | **DERIVED RULE** | Derived from X02 + X03: Route/Activity do not reapply Session Profile automatically. | Move: Derived Runtime Behavior |  |
| S07 | **DOMAIN DECISION** | Defines structural Slot universe for a Session. | Keep: Session Model |  |
| S08 | **DOMAIN DECISION** | Defines Current Capacity as runtime-variable within Supported Slots. | Keep: Session Model |  |
| S09 | **DERIVED RULE** | Derived from X03 plus S07/S08: Activity eligibility does not mutate Capacity. | Move: Derived Runtime Behavior |  |
| S10 | **DERIVED RULE** | Derived from X01 + X03 plus S08: Capacity changes require explicit request. | Move: Derived Runtime Behavior |  |
| S11 | **DOMAIN DECISION** | Defines Join Slot allocation policy. | Keep: Session/Join Policy |  |
| S12 | **DOMAIN DECISION** | Defines stability of Slot identity after assignment. | Keep: Session Model |  |
| PR01 | **DOMAIN DECISION** | Dedicated Player Provisioning Profile is a genuine product/domain shape. | Keep: Provisioning Model | Reconcile with existing package types/assets |
| PR02 | **DOMAIN DECISION** | Defines composition of Session Profile with Provisioning Profile. | Keep: Provisioning Model |  |
| PR03 | **DOMAIN DECISION** | Defines Scene Provided vs Manager Provisioned Host modes. | Keep: Provisioning Model |  |
| PR04 | **DOMAIN DECISION** | Separates Host provisioning from Actor resolution. | Keep: Provisioning Model |  |
| PR05 | **DOMAIN DECISION** | Defines default Actor resolution vs unresolved/external resolution. | Keep: Provisioning Model | Reconcile names/contracts with current actor-selection/provisioning APIs |
| PR06 | **DOMAIN DECISION** | Defines Session lifetime of the effective Provisioning Profile, including late joins. | Keep: Provisioning Lifetime |  |
| PR07 | **DERIVED RULE** | Derived from X02 + X03 plus PR06: Route/Activity do not replace Session provisioning implicitly. | Move: Derived Runtime Behavior |  |
| A01 | **DOMAIN DECISION** | Complete Player Activity Profile is a genuine authoring/domain shape. | Keep: Activity Authoring Model | Reconcile with existing Activity player policy/profile types |
| A02 | **DOMAIN DECISION** | Defines composition: participation + physical-presence intent. | Keep: Activity Authoring Model |  |
| A03 | **DOMAIN DECISION** | Defines Route as stable default source for Player Activity Profile. | Keep: Activity Authoring Model | Reconcile with current Route authoring/contracts |
| A04 | **DOMAIN DECISION** | Defines Activity Inherit vs Override choice. | Keep: Activity Authoring Policy |  |
| A05 | **DOMAIN DECISION** | Defines designer-first default behavior: Inherit. | Keep: Activity Authoring UX |  |
| A06 | **DOMAIN DECISION** | Defines complete Profile replacement instead of field-level override. | Keep: Activity Authoring Policy |  |
| A07 | **DOMAIN DECISION** | Defines Override locality: only the current Activity. | Keep: Activity Authoring Policy |  |
| A08 | **DERIVED RULE** | Derived validation from X06 + X07 plus A03/A04: Inherit without a resolvable Route default is invalid. | Move: Validation V01 |  |
| A09 | **DOMAIN DECISION** | Defines lifetime of the resolved Effective Player Activity Profile: one Activity occurrence. | Keep: Activity Lifetime |  |
| A10 | **TECHNICAL REVIEW** | Candidate X16 generalization must be reviewed across other framework domains before promotion. | Review queue | Cross-system review |
| W01 | **DOMAIN DECISION** | Defines Slot as Activity participation unit. | Keep: Participation Model |  |
| W02 | **DOMAIN DECISION** | Defines eligibility independently of current occupancy. | Keep: Participation Model |  |
| W03 | **DOMAIN DECISION** | Defines Who Participates stability for the Activity occurrence. | Keep: Participation Lifetime |  |
| W04 | **DOMAIN DECISION** | Defines Who Participates as scope/permission, not lifecycle authority. | Keep: Participation Model |  |
| W05 | **DOMAIN DECISION** | Defines participation modes: All Supported / Explicit / No Slots. | Keep: Participation Policy |  |
| W06 | **DOMAIN DECISION** | Defines All Supported Slots as normal/default choice. | Keep: Participation UX |  |
| W07 | **DERIVED RULE** | Derived validation from X06 + X07 plus S07/W05: unsupported Slot reference is invalid. | Move: Validation V02 |  |
| W08 | **DERIVED RULE** | Derived validation from X06 + X07 plus W05: No Slots cannot carry Player readiness/physical requirements. | Move: Validation V03 |  |
| W09 | **DOMAIN DECISION** | Session Join may assign a Slot outside current Activity scope. | Keep: Session/Activity Boundary |  |
| W10 | **DERIVED RULE** | Derived from W04 + W09: outside-scope Slot stays Session-valid but is excluded from current Activity participation/readiness/physical requirements. | Move: Derived Runtime Behavior |  |
| E01 | **DOMAIN DECISION** | Defines independent Activity entry dimensions: scope, stage and coverage. | Keep: Entry Readiness Model |  |
| E02 | **DOMAIN DECISION** | Defines Ready When as Entry-only semantics. | Keep: Entry Readiness Lifetime |  |
| E03 | **DERIVED RULE** | Derived from X09 + E02: post-Commit late join cannot reopen Entry Readiness. | Move: Derived Runtime Behavior |  |
| E04 | **DOMAIN DECISION** | Defines Activity observation to include all relevant Slots, not only ready ones. | Keep: Observation Model |  |
| E05 | **DOMAIN DECISION** | Defines conceptual Player lifecycle evidence chain used by readiness/observation. | Keep: Player Evidence Model | Reconcile exact existing terminology |
| E06 | **TECHNICAL REVIEW** | Need for explicit Physical Actor Available readiness stage must be checked against current contracts/source. | Review queue | Existing readiness stages/contracts |
| E07 | **DOMAIN DECISION** | Defines coverage modes: At Least N / All Occupied / All Eligible. | Keep: Entry Coverage Policy |  |
| E08 | **DOMAIN DECISION** | Defines All Occupied cohort capture semantics. | Keep: Entry Coverage Policy |  |
| E09 | **DOMAIN DECISION** | Defines All Eligible semantics over eligible Slots, including vacant Slots. | Keep: Entry Coverage Policy |  |
| E10 | **DERIVED RULE** | Derived validation from X06 + X07 plus E07/W05: At Least N cannot exceed eligible Slot count. | Move: Validation V04 |  |
| E11 | **DERIVED RULE** | Derived from X07 plus S08/E07: insufficient current Capacity does not by itself invalidate a structurally valid Profile. | Move: Runtime Satisfiability Rule |  |
| E12 | **DERIVED RULE** | Derived from X07 + X08: continue Preparing while satisfiable; fail once provably unsatisfiable. | Move: Runtime Satisfiability Rule |  |
| E13 | **DOMAIN DECISION** | Defines explicit zero-player policy as part of Player Activity entry semantics. | Keep: Entry Coverage Policy |  |
| E14 | **DOMAIN DECISION** | Defines Allow Empty Entry vs Require Player. | Keep: Entry Coverage Policy |  |
| E15 | **DOMAIN DECISION** | Defines Ready When=None semantics without removing Activity scope/observation. | Keep: Entry Readiness Policy |  |
| T01 | **TECHNICAL REVIEW** | Meaning of Current Activity as last committed valid context must be reconciled with current transition ADR/runtime semantics. | Review queue | Transition ADR/runtime |
| T02 | **TECHNICAL REVIEW** | Preparing → Ready → Commit / Failed attempt shape may duplicate or conflict with existing transition semantics. | Review queue | Transition ADR/runtime |
| T03 | **DOMAIN DECISION** | Defines the Player-specific contents of an Entry Participation Snapshot. | Keep: Entry Evidence Model | Must fit existing transition occurrence/revision model |
| T04 | **DERIVED RULE** | Derived from X09 plus T03: lifecycle evidence remains live while the question/snapshot is stable. | Move: Derived Snapshot Behavior |  |
| T05 | **DERIVED RULE** | Derived from X09 plus T03: committed Entry snapshot remains immutable historical evidence. | Move: Derived Snapshot Behavior |  |
| T06 | **DOMAIN DECISION** | Defines separate live Current Activity Participation projection after Commit. | Keep: Activity Observation Model |  |
| T07 | **TECHNICAL REVIEW** | Explicit guard against creating a second transaction/transition concept. | Review queue | Transition ADR/runtime |
| J01 | **DOMAIN DECISION** | Defines Joining Intent separately from temporary blocking. | Keep: Join Model |  |
| J02 | **DOMAIN DECISION** | Defines game-controlled Open/Closed intent. | Keep: Join Model |  |
| J03 | **DOMAIN DECISION** | Defines temporary Join Inhibits without rewriting Joining Intent. | Keep: Join Model | Reconcile with existing transition/player gates |
| J04 | **DOMAIN DECISION** | Defines effective Join admission composition. | Keep: Join Admission Policy |  |
| J05 | **DERIVED RULE** | Derived from X05 plus J03: multiple inhibits require distinct identity. | Move: Derived Inhibit Contract |  |
| J06 | **DERIVED RULE** | Derived from X05: an inhibit owner can release only its own inhibit. | Move: Derived Inhibit Contract |  |
| J07 | **DERIVED RULE** | Derived from X05: inhibit has explicit owner, scope/lifetime and evidence, and cannot outlive scope. | Move: Derived Inhibit Contract |  |
| J08 | **DOMAIN DECISION** | Defines a public typed/scoped extension point for game-owned temporary Join Inhibits. | Keep: Join Public Capability | Reconcile exact consumer API with ADR-015/current source |
| J09 | **DOMAIN DECISION** | Defines transition as a temporary Join-inhibited context without Close/Open restoration. | Keep: Join/Transition Policy | Reconcile with current transition gate behavior |
| J10 | **DOMAIN DECISION** | Defines typed RequestJoin acceptance/rejection evidence. | Keep: Join Public Contract | Reconcile exact existing result types |
| PH01 | **DOMAIN DECISION** | Defines physical representation separately from gameplay states such as alive/visible/controllable. | Keep: Physical Presence Model |  |
| PH02 | **DOMAIN DECISION** | Defines Activity Physical Presence modes: No Requirement / Require. | Keep: Physical Presence Policy |  |
| PH03 | **DOMAIN DECISION** | Places Activity Physical Presence inside the Route-defaulted Player Activity Profile. | Keep: Activity Authoring Model |  |
| PH04 | **DOMAIN DECISION** | Defines Require lifetime over the current Activity occurrence and late joins. | Keep: Physical Presence Lifetime |  |
| PH05 | **DOMAIN DECISION** | Defines structural ensure/reconcile trigger points. | Keep: Physical Presence Runtime Policy | Reconcile with current preparation/materialization contracts |
| PH06 | **DERIVED RULE** | Derived from X10 + PH02/PH05: Require is not a gameplay respawn policy. | Move: Derived Runtime Boundary |  |
| PH07 | **DERIVED RULE** | Derived from X12: Activity declares need; Player runtime executes physical presence operations. | Move: Derived Runtime Boundary |  |
| PH08 | **DERIVED RULE** | Derived from X13: reuse/reconcile valid existing representation instead of rematerializing on every Activity. | Move: Derived Runtime Behavior |  |
| PH09 | **DOMAIN DECISION** | Defines Route physical-presence intent: Preserve Existing / Suppress. | Keep: Route Physical Presence Policy | Reconcile against existing Route/player preparation contracts |
| PH10 | **DOMAIN DECISION** | Defines that Route change itself does not imply dematerialization; Suppress is explicit desired absence. | Keep: Route Physical Presence Policy |  |
| PH11 | **DERIVED RULE** | Derived validation from X06 + X07 plus PH02/PH09: Suppress + Require is invalid. | Move: Validation V05 |  |
| PH12 | **DERIVED RULE** | Derived validation from X06 + X07 plus PH09/E05: Suppress conflicts with entry stages that necessarily require physical presence. | Move: Validation V06 |  |
| PH13 | **DERIVED RULE** | Derived from X11/X12: public intent/evidence does not prescribe Destroy vs pool/hide implementation. | Move: Derived Implementation Boundary |  |
| G01 | **GLOBAL** | Fold into X01/X10: framework ownership of technical truth/contracts/capabilities. | Remove from Player-specific matrix |  |
| G02 | **GLOBAL** | Fold into X10: game owns UI/interaction/game rules/orchestration. | Remove from Player-specific matrix |  |
| G03 | **DERIVED RULE** | Derived from X10 plus PR05/P03: framework does not own generic character-select flow. | Move: Derived Gameplay Boundary |  |
| G04 | **DERIVED RULE** | Derived from X10 plus P03: unresolved Actor is factual state, not global Selection Pending flow. | Move: Derived Gameplay Boundary |  |
| G05 | **HISTORICAL / REJECTED** | Explicitly withdrawn: Allow/Require Reselect and Activity re-entry selection behavior. | Keep only in rejected-history section |  |
| G06 | **HISTORICAL / REJECTED** | Explicitly withdrawn: generic Runtime Participation Requirement derived from Ready When. | Keep only in rejected-history section |  |
| G07 | **GLOBAL** | Fold into X07/X10: framework does not validate existence/accessibility of game UI path for an external dependency. | Remove from Player-specific matrix |  |
| G08 | **GLOBAL** | Fold into X06/X10: unresolved external dependencies must remain diagnosable/evident. | Remove from Player-specific matrix |  |
| G09 | **GLOBAL** | Fold into X07: hard errors only for intrinsically provable contradictions. | Remove from Player-specific matrix |  |
| R01 | **DERIVED RULE** | Derived from X10 + X15: runtime/readiness reports typed failure without choosing game destination. | Move: Derived Failure Boundary |  |
| R02 | **DERIVED RULE** | Derived from X10: game chooses recovery policy. | Move: Derived Failure Boundary |  |
| R03 | **DOMAIN DECISION** | Defines official Activity Entry Failure Recovery authoring as a product surface. | Keep: Activity Failure Recovery | Reconcile with existing failure/reaction contracts |
| R04 | **GLOBAL** | Fold into X15: reactions use typed facts/conditions/actions/results. | Remove from Player-specific matrix |  |
| R05 | **GLOBAL** | Fold into X15: prefer context-specific authoring over universal visual scripting. | Remove from Player-specific matrix |  |
| R06 | **GLOBAL** | Fold into X14/X15: extensions use public capabilities without becoming authority. | Remove from Player-specific matrix |  |
| R07 | **GLOBAL** | Fold into X05/X14: reaction extensions cannot bypass authority/scope/private state. | Remove from Player-specific matrix |  |
| C01 | **GLOBAL** | Fold into X04: observation does not grant mutation authority. | Remove from Player-specific matrix |  |
| C02 | **DOMAIN DECISION** | Defines Slot-centered Activity observation projection combining Activity scope with live Session truth. | Keep: Observation Model |  |
| C03 | **DOMAIN DECISION** | Defines independent Session observation surface. | Keep: Observation Model |  |
| C04 | **GLOBAL** | Fold into X01/X03/X04: Commands request operations from authorities rather than directly setting internal truth. | Remove from Player-specific matrix |  |
| C05 | **DOMAIN DECISION** | Defines Activity-contextual capability scope validation against Who Participates. | Keep: Public Capability Scope |  |
| C06 | **DOMAIN DECISION** | Defines Session capabilities/observation as independent of current Activity participation scope. | Keep: Public Capability Scope |  |
| C07 | **TECHNICAL REVIEW** | Concrete scoped consumer reachability must be reconciled with ADR-015 and current package APIs. | Review queue | ADR-015 / current consumer surface |

---

# 3. GLOBAL items — remove from the Player-specific decision layer

These original IDs should no longer carry independent architectural weight in the Player model:

```text
G01 G02 G07 G08 G09 R04 R05 R06 R07 C01 C04
```

They are absorbed by the transversal layer, principally:

```text
X01  Single runtime authority
X03  Explicit mutation
X04  Observation is not command
X05  Explicit scope and lifetime
X06  No silent fallback
X07  Structural invalidity differs from runtime condition
X10  Framework responsibility differs from gameplay responsibility
X14  Extension does not create a second authority
X15  Typed contextual reactions
```

Their content may still appear as explanatory text, but the canonical architectural authority should be the `X` invariant rather than a duplicate Player-specific decision.

---

# 4. Genuine Player DOMAIN DECISIONS

These remain first-class Player architecture decisions after transversal normalization:

```text
P01 P02 P03 P05 P06 S02 S07 S08 S11 S12 PR01 PR02 PR03 PR04 PR05 PR06 A01 A02 A03 A04 A05 A06 A07 A09 W01 W02 W03 W04 W05 W06 W09 E01 E02 E04 E05 E07 E08 E09 E13 E14 E15 T03 T06 J01 J02 J03 J04 J08 J09 J10 PH01 PH02 PH03 PH04 PH05 PH09 PH10 R03 C02 C03 C05 C06
```

They should be reorganized by model instead of by conversation chronology.

## 4.1 Player identity and Session model

Retain:

```text
P01  PlayerSlotId is the stable logical Player identity/seat.
P02  Player/Slot belongs to Session.
P03  Slot may exist with Current Actor = none.
P05  Accepted Join establishes Slot + Player Host in Session.
P06  Physical representation is distinct from Player/Actor identity.

S07  Supported Slots define the structural Slot universe.
S08  Current Capacity is runtime-variable within Supported Slots.
S11  Join uses defined first-available Slot allocation.
S12  Assigned Slot identity does not renumber.
```

## 4.2 Session configuration

Retain as domain policy, subject to the owner review of S01:

```text
S02
Session creation may consume the default Player Session Profile
or an explicit creation-time override.
```

## 4.3 Provisioning

Retain:

```text
PR01  Player Provisioning Profile
PR02  Session Profile composes Provisioning Profile
PR03  Scene Provided / Manager Provisioned Host modes
PR04  Host provisioning separate from Actor resolution
PR05  Default Actor resolution vs unresolved/external
PR06  Effective Provisioning Profile is Session-scoped, including late joins
```

## 4.4 Player Activity authoring

Retain:

```text
A01  Complete Player Activity Profile
A02  Participation + Physical Presence composition
A03  Route default source
A04  Inherit / Override
A05  Inherit as normal authoring default
A06  Complete Profile override
A07  Override local to one Activity
A09  Effective Profile is occurrence-scoped
```

## 4.5 Activity participation

Retain:

```text
W01  Participation unit is Slot
W02  Eligibility is independent from current occupancy
W03  Who Participates is stable for occurrence
W04  Who Participates is scope/permission, not lifecycle driver
W05  All Supported / Explicit / No Slots
W06  All Supported as normal default
W09  Session Join may assign a Slot outside current Activity scope
```

## 4.6 Entry readiness

Retain:

```text
E01  Who Participates / Ready When / Coverage are independent
E02  Ready When is Entry-only
E04  Observation includes all relevant Slots
E05  Player lifecycle evidence stages
E07  At Least N / All Occupied / All Eligible
E08  All Occupied captured cohort semantics
E09  All Eligible semantics
E13  explicit zero-player policy
E14  Allow Empty Entry / Require Player
E15  Ready When=None does not remove Activity scope
```

`E06` remains in technical review because the source must establish whether `Physical Actor Available` is a new contract or an existing one under another name.

## 4.7 Entry evidence

Retain:

```text
T03  Player-specific Entry Participation Snapshot contents
T06  separate live Current Activity Participation projection
```

The exact transition state machine remains outside this normalized Player decision layer until T01/T02/T07 are reconciled.

## 4.8 Joining

Retain:

```text
J01  Joining Intent != temporary blockers
J02  Open/Closed game intent
J03  Join Inhibits
J04  Effective Join admission composition
J08  public typed/scoped game-owned inhibit capability
J09  transition creates temporary Join inhibition without rewriting intent
J10  typed RequestJoin results
```

## 4.9 Physical Presence

Retain:

```text
PH01  physical representation != gameplay-state concepts
PH02  Activity: No Requirement / Require
PH03  Physical Presence belongs to Player Activity Profile
PH04  Require lifetime includes current occurrence + late joins
PH05  structural ensure/reconcile points
PH09  Route: Preserve Existing / Suppress
PH10  Route change alone does not imply dematerialization
```

## 4.10 Failure recovery

Retain:

```text
R03
Official Activity Entry Failure Recovery authoring surface.
```

The general reaction architecture moves to X14/X15.

## 4.11 Observation and command scope

Retain:

```text
C02  Slot-centered Activity observation projection
C03  independent Session observation
C05  Activity-contextual capabilities validate Who Participates
C06  Session capabilities remain independent from Activity scope
```

---

# 5. DERIVED RULES — stop treating these as independent architecture questions

The following IDs should move out of the first-class decision matrix:

```text
P04 S03 S04 S05 S06 S09 S10 PR07 A08 W07 W08 W10 E03 E10 E11 E12 T04 T05 J05 J06 J07 PH06 PH07 PH08 PH11 PH12 PH13 G03 G04 R01 R02
```

They remain useful as validation rules, runtime behaviors and boundary proofs.

## 5.1 Derived validation rules

### V01 — Missing inherited Activity Profile

Source:

```text
A08
```

Rule:

```text
Activity = Inherit
Route default cannot be resolved
    → authoring error
```

Derived from:

```text
X06 No silent fallback
X07 Structural invalidity vs runtime condition
A03 Route default
A04 Inherit / Override
```

### V02 — Unsupported explicit Slot

Source:

```text
W07
```

Rule:

```text
Explicit Who Participates contains unsupported Slot
    → authoring error
```

Derived from:

```text
X06
X07
S07 Supported Slots
W05 Explicit Slots
```

### V03 — No Slots consistency

Source:

```text
W08
```

Rule:

```text
Who Participates = No Slots
    → Ready When = None
    → Physical Presence = No Requirement
```

Any contradictory requirement is invalid.

### V04 — Impossible At Least N

Source:

```text
E10
```

Rule:

```text
At Least N > eligible Slot count
    → authoring error
```

### V05 — Route Suppress vs Activity Require

Source:

```text
PH11
```

Rule:

```text
Route Physical Presence = Suppress
Activity Physical Presence = Require
    → authoring error
```

### V06 — Route Suppress vs physical Entry stage

Source:

```text
PH12
```

Rule:

```text
Route Physical Presence = Suppress
Ready When requires Physical Available
or a necessarily later physical stage
    → authoring error
```

## 5.2 Derived Session/runtime behaviors

### B01 — Profile is not a live setter

Sources:

```text
S03 S06 PR07
```

Derived from:

```text
X02 Intent is not runtime state
X03 Explicit mutation
```

### B02 — Session mutable state changes explicitly

Sources:

```text
S04 S05 S09 S10
```

Derived from:

```text
X01 Single runtime authority
X03 Explicit mutation
```

### B03 — Outside-scope Join remains valid in Session

Source:

```text
W10
```

Derived from:

```text
W04 Who Participates is Activity scope
W09 Join may assign outside current Activity scope
```

### B04 — Late join does not reopen Entry

Source:

```text
E03
```

Derived from:

```text
X09 Snapshot vs live state
E02 Ready When is Entry-only
```

### B05 — Structural validity differs from occurrence satisfiability

Sources:

```text
E11 E12
```

Derived from:

```text
X07 Structural invalidity vs runtime condition
X08 Proven impossibility fails explicitly
```

Result:

```text
still satisfiable
    → Preparing

provably unsatisfiable
    → Failed with typed evidence
```

### B06 — Entry snapshot and live evidence stay separate

Sources:

```text
T04 T05
```

Derived from:

```text
X09
T03
```

### B07 — Join Inhibit identity/ownership/lifetime

Sources:

```text
J05 J06 J07
```

Derived from:

```text
X05 Explicit scope and lifetime
J03 Join Inhibits
```

### B08 — Activity Require is not respawn

Source:

```text
PH06
```

Derived from:

```text
X10 Framework vs gameplay
PH02/PH05
```

### B09 — Context declares physical need; Player runtime executes

Source:

```text
PH07
```

Derived from:

```text
X12 Context declares need; authority executes
```

### B10 — Reuse/reconcile physical representation

Source:

```text
PH08
```

Derived from:

```text
X13 Ensure/Reconcile over blind reconstruction
```

### B11 — Physical implementation remains internal

Source:

```text
PH13
```

Rule:

```text
Suppress / Require describe public intent and evidence.
They do not prescribe Destroy vs pooling vs hiding.
```

### B12 — Framework does not own character-selection flow

Sources:

```text
P04 G03 G04
```

Derived from:

```text
X10 Framework vs gameplay
P03 Actor may be unresolved
PR05 Actor resolution modes
```

### B13 — Typed failure does not choose game recovery

Sources:

```text
R01 R02
```

Derived from:

```text
X10
X15
```

---

# 6. HISTORICAL / REJECTED IDs

These IDs should exist only in the rejected-history section, not in the active model:

```text
G05 G06
```

Specifically:

```text
G05
    Allow/Require Reselect / Activity re-entry selection behavior
    → withdrawn

G06
    generic Runtime Participation Requirement derived from Ready When
    → withdrawn
```

The larger rejected-shape list from the original matrix remains valid as historical guardrail and should be preserved during the eventual normalized-document rewrite.

---

# 7. Explicit TECHNICAL REVIEW queue

These original IDs are not suitable for further conceptual questioning until current ADR/source/runtime is inspected:

```text
S01 A10 E06 T01 T02 T07 C07
```

## TR01 — Default Player Session Profile owner

Source:

```text
S01
```

Question for reconciliation:

```text
Is GameApplication already the canonical owner/source
for this kind of default Session configuration?
```

Do not create a new owner if an existing canonical surface already serves this role.

## TR02 — Cross-system Inherit/Override generalization

Source:

```text
A10
```

Question:

```text
Should X16 be promoted beyond Player Activity,
and in which existing systems is it actually appropriate?
```

## TR03 — Physical Actor Available readiness stage

Source:

```text
E06
```

Question:

```text
Does an equivalent explicit evidence/stage already exist?
If not, is a new readiness contract required?
```

## TR04 — Current Activity / Entry Attempt / Commit semantics

Sources:

```text
T01 T02 T07
```

Question:

```text
How does this conceptual shape map onto the existing transition gate,
release and Activity/Route transition ADRs/runtime?
```

Hard constraint:

```text
Do not introduce a second transaction manager,
duplicate commit authority or parallel transition state machine.
```

## TR05 — Consumer command/observation reachability

Source:

```text
C07
```

Question:

```text
How do official scoped Player commands and observation surfaces
map onto ADR-015 and current provisioning host/bridge/runtime APIs?
```

---

# 8. Additional domain decisions with mandatory reconciliation notes

These remain `DOMAIN DECISION`, but the original baseline itself already identifies concrete source/ADR questions:

```text
PR01 PR05
A01 A03
E05
T03
J03 J08 J09 J10
PH05 PH09
R03
```

These are not reclassified as `TECHNICAL REVIEW` because the domain decision itself is meaningful and should survive normalization. The reconciliation must determine whether the package already implements it, needs a compatible refinement, needs an ADR update, or conflicts.

---

# 9. Normalized active Player model

After removing global duplicates and moving consequences to Derived Rules, the active conceptual model becomes smaller.

```text
PLAYER IDENTITY
    PlayerSlotId
    Player Host
    Actor
    Physical Representation

PLAYER SESSION
    ownership of Player/Slot
    Supported Slots
    Current Capacity
    Slot allocation
    stable Slot identity

SESSION CONFIGURATION
    creation-time default / explicit override
    [owner pending S01 reconciliation]

PLAYER PROVISIONING
    Player Provisioning Profile
    Scene Provided / Manager Provisioned Host
    Host provisioning separate from Actor resolution
    Default Actor / unresolved-external Actor
    Session-scoped effective provisioning

PLAYER ACTIVITY PROFILE
    Route default
    Activity Inherit / complete Override
    Override local to one Activity
    Effective Profile scoped to Activity occurrence

PARTICIPATION
    Slot-based scope
    vacant Slots remain eligible
    All Supported / Explicit / No Slots
    scope stable for occurrence

ENTRY READINESS
    Who Participates
    Ready When
    Required Coverage
    zero-player policy

    Coverage:
        At Least N
        All Occupied
        All Eligible

ENTRY EVIDENCE
    Entry Participation Snapshot
    separate live Current Activity Participation

JOINING
    Joining Intent
    Join Inhibits
    admission composition
    public game-owned inhibit capability
    typed RequestJoin result

PHYSICAL PRESENCE
    Activity:
        No Requirement
        Require

    Route:
        Preserve Existing
        Suppress

    structural ensure/reconcile points

FAILURE RECOVERY
    Activity Entry Failure Recovery authoring

OBSERVATION / CAPABILITY SCOPE
    Session observation
    Activity Slot-centered projection
    Activity-contextual capability scope
    Session capability independence
```

Everything else should either be explained by X01–X15, derived mechanically, preserved as rejected history, or held in the technical-review queue.

---

# 10. What this normalization removes from future design questioning

The following types of questions are now considered already answered by transversal invariants and should not be asked again as independent Player-design decisions:

```text
Should Route/Activity silently change Session Capacity?
    → No. X01 + X03.

Should a Profile continuously overwrite live runtime state?
    → No. X02.

Should observation expose mutation authority?
    → No. X04.

Can a temporary blocker exist without scope/owner?
    → No. X05.

Should missing inherited configuration silently mean "None"?
    → No. X06.

Should a structurally impossible profile wait until runtime?
    → No. X07.

Should runtime wait forever once impossibility is provable?
    → No. X08.

Should late runtime changes rewrite historical Entry evidence?
    → No. X09.

Should framework own character-select UI or respawn policy?
    → No. X10.

Should Player/Actor/physical/gameplay state be collapsed?
    → No. X11.

Should Activity perform Player-authority mutations directly?
    → No. X12.

Should context change blindly reconstruct valid existing objects?
    → No. X13.

Can a game extension become a second authority?
    → No. X14.

Should recovery become a string-driven universal event/action bus?
    → No. X15.
```

This is the principal reduction achieved by this pass.

---

# 11. Next step after this document

The next task is now a **genuine gap scan** over only:

```text
1. the normalized DOMAIN DECISIONS;
2. the Derived Rules for internal contradiction;
3. the Technical Review queue only for identifying conceptual dependency,
   not yet source reconciliation.
```

The gap scan should **not** reopen X01–X15.

After gaps are corrected, perform ADR/source reconciliation and classify each surviving domain decision as:

```text
Already defined
Compatible refinement
New ADR / ADR update required
Conflict with existing ADR/runtime
```

---

# 12. Baseline rule

Until concrete contradictory evidence is found:

> Global invariants are not Player questions, derived rules are not independent architecture choices, and rejected historical shapes are not candidates.

The active design discussion should focus only on genuine domain decisions and genuine gaps between them.
