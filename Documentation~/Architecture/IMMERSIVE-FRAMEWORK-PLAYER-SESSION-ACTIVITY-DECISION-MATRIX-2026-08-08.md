# Immersive Framework — Player / Session / Activity Decision Matrix

**Date:** 2026-08-08  
**Status:** Conceptual baseline frozen for gap analysis  
**Project:** Immersive Framework 1.1 / Unity 6.5

> This document records the decisions reached in the architecture discussion before source/ADR reconciliation.
>
> **Important:** items marked as conceptually closed are not yet proof that the current package/ADRs already implement or authorize that exact shape. The next phases are:
>
> 1. gap analysis against this matrix;
> 2. reconciliation with existing ADRs and current runtime/source;
> 3. classification as already defined / compatible refinement / new ADR required / conflict;
> 4. only then translate the reconciled model into implementation cuts.

---

# 1. Player identity and ownership

| ID | Area | Decision | Status |
|---|---|---|---|
| P01 | Identity | `PlayerSlotId` is the stable logical identity/seat of a Player in the Session. Do not create another `LogicalPlayerId`. | **Closed** |
| P02 | Ownership | Player/Slot belongs to the **Session**, including when the Player joins while an Activity is running. | **Closed** |
| P03 | Actor | A Slot may exist with `Current Actor = none`. This does not remove the Player. | **Closed** |
| P04 | Actor | Gameplay decides when/why to select or change Actor. Framework provides state, evidence and capability. | **Closed** |
| P05 | Host | An accepted Join establishes `Slot + Player Host` at Session level, even when no Activity exists. | **Closed** |
| P06 | Physical Actor | Physical representation is not the identity of the Player nor of the logical Actor. | **Closed** |

Canonical conceptual separation:

```text
Player Slot
    = stable logical Player identity / seat in Session

Player Host
    = runtime host associated with the joined Player

Actor
    = logical character/representation associated with the Slot

Physical representation
    = concrete world representation of the Actor
```

---

# 2. Session and initial configuration

| ID | Area | Decision | Status |
|---|---|---|---|
| S01 | GameApplication | `GameApplication` may provide the **Default Player Session Profile**. | **Conceptually closed** |
| S02 | Session Profile | Session creation may use the GameApplication default or an explicit override. | **Closed** |
| S03 | Lifetime | The Profile initializes the Session once; it does not continuously control runtime state. | **Closed** |
| S04 | Runtime | After creation, Session/runtime becomes the authority for mutable state. | **Closed** |
| S05 | Mutation | Capacity, Joining and equivalent mutable Session state change only through explicit requests/capabilities. | **Closed** |
| S06 | Route/Activity | Route or Activity do not automatically reapply the Player Session Profile. | **Closed** |
| S07 | Supported Slots | Session has a structural universe of supported Slot identities. | **Closed** |
| S08 | Capacity | `Current Capacity` is runtime-variable and bounded by Supported Slots. | **Closed** |
| S09 | Capacity | An Activity accepting Slots 1–4 does not automatically raise Session Capacity to 4. | **Closed** |
| S10 | Capacity | Increasing or reducing Capacity must be explicitly requested. | **Closed** |
| S11 | Allocation | Join assigns the first available Slot according to a defined allocation order/policy; the Player does not choose a Slot. | **Closed** |
| S12 | Slot stability | Once assigned, Slot identity does not renumber because another Player leaves. | **Closed** |

Conceptual creation flow:

```text
GameApplication
    Default Player Session Profile
            ↓
Create Player Session
    Default / explicit override
            ↓
Profile initializes Session once
            ↓
Player Session Runtime becomes authority
```

Example structural/runtime distinction:

```text
Supported Slots = 4
Current Capacity = 2

Request Capacity 4
    → may be accepted

Request Capacity 5
    → rejected
```

---

# 3. Player provisioning

| ID | Area | Decision | Status |
|---|---|---|---|
| PR01 | Profile | Initial Player provisioning deserves a dedicated **Player Provisioning Profile**. | **Closed** |
| PR02 | Composition | Player Session Profile references the Player Provisioning Profile. | **Closed** |
| PR03 | Host | `Scene Provided` and `Manager Provisioned` are provisioning decisions. | **Closed** |
| PR04 | Actor | How the Host is obtained is separate from how the Actor is resolved. | **Closed** |
| PR05 | Actor | Provisioning may resolve a default Actor or leave Actor unresolved for external/gameplay resolution. | **Conceptually closed** |
| PR06 | Lifetime | The effective Player Provisioning Profile remains stable for the whole Session, including late joins. | **Closed** |
| PR07 | Route/Activity | Route/Activity do not automatically replace the effective Provisioning Profile. | **Closed** |

Conceptual composition:

```text
Player Session Profile
    Supported Slots
    Initial Capacity
    Initial Joining Intent
    Slot Allocation
    Player Provisioning Profile
        Host Provisioning
            Scene Provided
            Manager Provisioned

        Actor Resolution
            Resolve Default
            Leave Unresolved / External
```

An accepted Join establishes the Player in the Session independently of Activity:

```text
Request Join
    ↓
Slot assigned
    ↓
Host resolved/provisioned
    ↓
Player exists logically in Session
```

Actor may still remain unresolved:

```text
Slot
  Joined = yes
  Host = valid
  Actor = unresolved
  Physical Actor = none
```

---

# 4. Player Activity Profile and inheritance

| ID | Area | Decision | Status |
|---|---|---|---|
| A01 | Profile | The main design surface is conceptually a complete **Player Activity Profile**. | **Conceptually closed** |
| A02 | Contents | The Profile composes participation rules and Physical Presence intent. | **Closed** |
| A03 | Route | Route provides a `Default Player Activity Profile`. | **Conceptually closed** |
| A04 | Activity | Activity chooses `Inherit Route Default` or `Override`. | **Closed** |
| A05 | Default UX | `Inherit` is the normal/default authoring behavior to avoid accidental duplicated decisions. | **Closed** |
| A06 | Override | Override replaces the **whole Profile**, not individual fields. | **Closed** |
| A07 | Override lifetime | Override applies only to that Activity. A later Activity using `Inherit` resolves the Route default again. | **Closed** |
| A08 | Missing inheritance | `Inherit` without a resolvable Route default is an authoring error. No silent fallback. | **Closed** |
| A09 | Occurrence | Effective Player Activity Profile is resolved for the Activity occurrence and stays immutable until that occurrence ends. | **Closed** |
| A10 | Generalization | `Inherit + deliberate Override` is a candidate pattern for other framework areas. | **Reserved for final review** |

Canonical authoring shape:

```text
Route
  Default Player Activity Profile
      Participation
      Physical Presence
            ↓

Activity
  Player Activity Profile
      Inherit Route Default
      or
      Override → complete Profile
```

Example:

```text
Route: World Gameplay
    Default Player Activity Profile
        → Standard Gameplay

Activity 1-1
    Inherit

Activity 1-2
    Inherit

Activity Bonus
    Override
        → Bonus Participation
```

An Activity override is local:

```text
Route Default = Require

Activity A → Inherit  = Require
Activity B → Override = No Requirement
Activity C → Inherit  = Require
```

There is no inheritance chain from the previously executed Activity.

---

# 5. Who Participates

| ID | Area | Decision | Status |
|---|---|---|---|
| W01 | Unit | Participation is defined in **Slots**, not GameObjects and not only currently existing Players. | **Closed** |
| W02 | Vacancy | An eligible Slot remains a valid Activity participation possibility while vacant. | **Closed** |
| W03 | Lifetime | Who Participates remains stable during the Activity occurrence. | **Closed** |
| W04 | Semantics | Who Participates is scope/permission, not a Player lifecycle driver. | **Closed** |
| W05 | Modes | `All Supported Slots / Explicit Slots / No Slots`. | **Closed** |
| W06 | Default | `All Supported Slots` is the normal/default choice. | **Closed** |
| W07 | Supported universe | Referencing an unsupported Slot is a structural authoring error. | **Closed** |
| W08 | No Slots | `No Slots` implies `Ready When=None` and `Physical Presence=No Requirement`. | **Closed** |
| W09 | Join | A Player may join the Session into a Slot outside the current Activity's Who Participates. | **Closed — do not reopen without conflict evidence** |
| W10 | Outside scope | A Slot outside Activity scope remains valid in Session but does not participate in that Activity's readiness or physical-presence requirements. | **Closed** |

Modes:

```text
Who Participates

All Supported Slots
    → every structurally supported Session Slot

Explicit Slots
    → deliberate subset

No Slots
    → Player does not participate in this Activity
```

Important separation:

```text
Activity eligibility
    ≠ current Slot occupancy

Who Participates = Slots 1,2,3,4

Runtime:
    Slot 1 occupied
    Slot 2 occupied
    Slot 3 vacant
    Slot 4 vacant
```

Slots 3 and 4 remain eligible for the Activity even while vacant.

---

# 6. Activity Entry Readiness

| ID | Area | Decision | Status |
|---|---|---|---|
| E01 | Separation | `Who Participates`, `Ready When`, and `Required Coverage` are independent dimensions. | **Closed** |
| E02 | Lifetime | `Ready When` is exclusively an **Entry Attempt** rule. | **Closed** |
| E03 | Non-regression | After Commit, late join does not reopen Activity Entry Readiness. | **Closed** |
| E04 | Evidence | Activity observation includes all relevant Slots, not only ready Slots. | **Closed** |
| E05 | Stages | Conceptual chain: Joined → Actor Resolved → Logical Prepared → Physical Available → Gameplay Ready. | **Conceptually closed** |
| E06 | Physical stage | Entry readiness must be able to explicitly require `Physical Actor Available`. | **Conceptually closed** |
| E07 | Coverage | `At Least N`, `All Occupied`, `All Eligible`; `Any` may only be a UX alias for `At Least 1`. | **Closed** |
| E08 | All Occupied | Captures the occupied cohort at Entry Attempt start; it does not silently shrink. | **Closed** |
| E09 | All Eligible | Requires all Slots allowed by the Profile, even if some are vacant. | **Closed** |
| E10 | At Least | `N > eligible Slot count` is a structural authoring error. | **Closed** |
| E11 | Runtime capacity | `N <= eligible Slot count` with insufficient current Capacity does not make the Profile structurally invalid by itself. | **Closed** |
| E12 | Satisfiability | While still satisfiable → Preparing. Once framework can prove the occurrence is unsatisfiable → Failed. | **Closed** |
| E13 | No Player | Zero occupied Players is controlled by explicit `If No Players Are Available`. | **Closed** |
| E14 | Empty | `Allow Empty Entry` permits the empty case; `Require Player` does not. | **Closed** |
| E15 | None | `Ready When=None` means Player participation does not contribute to Entry Readiness; it does not remove Slot scope/observation. | **Closed** |

Three independent dimensions:

```text
Who Participates
    Which Slots belong to / are accepted by the Activity?

Ready When
    How far must a Slot progress to count for Entry?

Required Coverage
    How many Slots must satisfy Ready When?
```

Conceptual evidence chain:

```text
Joined
↓
Actor Resolved / Selected
↓
Logical Actor Prepared
↓
Physical Actor Available
↓
Gameplay Ready
```

Coverage:

```text
At Least N
All Occupied
All Eligible
```

Structural validation example:

```text
Who Participates = Slots 1–2
Coverage = At Least 3

→ invalid Profile
```

Runtime-condition example:

```text
Who Participates = Slots 1–4
Coverage = At Least 3
Current Capacity = 2

→ Profile may still be valid
→ Entry Attempt evaluates actual satisfiability
```

Empty-set policy:

```text
If No Players Are Available

Allow Empty Entry
Require Player
```

---

# 7. Entry snapshot and transition model

| ID | Area | Decision | Status |
|---|---|---|---|
| T01 | Current Activity | Current Activity is the last valid committed context. | **Conceptually closed** |
| T02 | Next Activity | Request creates an attempt conceptually: Preparing → Ready → Commit, or Failed without Commit. | **Conceptually closed** |
| T03 | Snapshot | Entry Attempt captures Eligible Slots, Occupied Slots, Coverage, Ready When and Activity/occurrence identity. | **Closed** |
| T04 | Live evidence | Slot lifecycle evidence stays live during Preparing. | **Closed** |
| T05 | Historical evidence | After Commit, the Entry snapshot becomes immutable historical evidence of why entry was accepted. | **Closed** |
| T06 | Current participation | Live participation after Commit is a separate projection and may include late joins. | **Closed** |
| T07 | Conflict risk | Exact transition shape must be reconciled with existing transition ADRs/runtime to avoid creating a duplicate transaction concept. | **Mandatory review** |

Conceptual shape only:

```text
Current Activity
    = last committed valid context

Request next Activity
    ↓
Entry Attempt / Transition
    Preparing
      ↓
    Ready
      ↓
    Commit
      ↓
new Activity becomes Current

or

    Failed
    → no commit
```

Stable snapshot vs live evidence:

```text
Entry Participation Snapshot — frozen
    Eligible Slots
    Occupied Slots at capture
    Required Coverage
    Ready When
    Activity / occurrence identity

Live evidence during Preparing
    occupancy/lifecycle evidence
    Actor resolution
    logical preparation
    physical availability
    gameplay readiness
```

After Commit:

```text
Entry snapshot
    = immutable historical evidence

Current Activity Participation
    = live projection
    = may include late joins
```

---

# 8. Joining intent and temporary inhibits

| ID | Area | Decision | Status |
|---|---|---|---|
| J01 | Separation | `Joining Intent` and temporary blockers are separate concepts. | **Closed** |
| J02 | Intent | Open/Closed is game-controlled joining intent. | **Closed** |
| J03 | Inhibit | Transition/Recovery/etc. add temporary **Join Inhibits** without mutating Joining Intent. | **Closed** |
| J04 | Composition | Effective Joining is composed from Intent + Inhibits + Capacity + Slot availability. | **Closed** |
| J05 | Multiple blockers | Multiple independent inhibits may coexist and need identity. | **Closed** |
| J06 | Ownership | The creator/owner of an inhibit may release only its own inhibit. | **Closed** |
| J07 | Lifetime | Inhibit has Owner + Scope/Lifetime + Reason/Evidence and cannot survive scope end. | **Closed** |
| J08 | Consumer | Game may acquire its own inhibit through an official typed/scoped public capability. | **Conceptually closed** |
| J09 | Transition | Joining is inhibited during transition. Do not implement this by Close/Open restoration. | **Closed** |
| J10 | Result | RequestJoin should produce typed acceptance/rejection reasons such as closed, inhibited, capacity reached or no Slot. | **Conceptually closed** |

Composition:

```text
Joining Intent
    Open / Closed

Join Inhibits
    Transition
    Recovery
    game-specific temporary inhibit
    ...

Capacity
Slot availability
    ↓
Effective Joining / RequestJoin result
```

Each inhibit:

```text
Owner
Scope / Lifetime
Reason / Evidence
```

The owner may release it early, but scope termination guarantees the inhibit cannot remain active beyond its contractual lifetime.

---

# 9. Physical Presence

| ID | Area | Decision | Status |
|---|---|---|---|
| PH01 | Distinction | Physical existence is not equivalent to alive, visible, enabled, targetable or controllable. | **Closed** |
| PH02 | Activity | Activity uses `Physical Presence = No Requirement / Require`. | **Closed** |
| PH03 | Route default | Physical Presence is part of the Player Activity Profile defaulted by Route and locally overridable by Activity. | **Closed** |
| PH04 | Require lifetime | `Require` applies while that Activity occurrence is current and includes late joins. | **Closed** |
| PH05 | Structural points | `Require` causes ensure/reconcile at structural lifecycle points: Entry, late join, Actor resolution/change and explicit reconciliation request. | **Closed** |
| PH06 | Respawn | Loss of physical representation during gameplay does **not** automatically respawn/recreate it. | **Closed** |
| PH07 | Runtime ownership | Activity declares need; Player runtime owns how physical presence is ensured/reconciled. | **Closed** |
| PH08 | Existing physical | A valid existing physical representation may be reused; Activity does not rematerialize it on every entry. | **Closed** |
| PH09 | Route | Route may have `Preserve Existing / Suppress` physical-presence intent. | **Conceptually closed** |
| PH10 | Suppress | Route change alone does not imply dematerialization; `Suppress` is explicit desired absence. | **Closed** |
| PH11 | Contradiction | Route `Suppress` + Activity `Require` is a structural authoring error. | **Closed** |
| PH12 | Readiness conflict | Route `Suppress` + Entry stage necessarily requiring Physical Available / Gameplay Ready is a structural error. | **Closed** |
| PH13 | Implementation | `Suppress` does not prescribe Destroy vs pooling/hiding/etc.; implementation details are separate from public intent/evidence. | **Closed** |

Activity intent:

```text
Physical Presence

No Requirement
    Activity does not request physical materialization.
    It also does not itself require removal of an existing representation.

Require
    Current participating occupied Slots need valid physical presence
    at the structural points owned by the framework.
```

`Require` is **not** a respawn policy.

Structural ensure/reconcile points currently agreed:

```text
Activity Entry
Late Join
Actor newly resolved
Actor explicitly changed
Explicit reconciliation request
```

If the representation disappears during gameplay:

```text
Physical Available = false
    ↓
evidence / diagnostics

NOT
    automatic respawn
```

Route intent:

```text
Preserve Existing
Suppress
```

Composition examples:

```text
Route Preserve + Activity No Requirement
    → keep existing representation; Activity creates nothing solely from this intent

Route Preserve + Activity Require
    → Activity context requires physical presence

Route Suppress + Activity No Requirement
    → no physical presence desired by Route

Route Suppress + Activity Require
    → invalid configuration
```

---

# 10. Framework vs gameplay responsibilities

| ID | Area | Decision | Status |
|---|---|---|---|
| G01 | Framework | Framework owns state/truth, contracts, evidence, capabilities and progression/lifecycle it genuinely owns. | **Closed** |
| G02 | Game | Gameplay owns when/why capabilities are used, UI/interaction flow and game-specific rules/orchestration. | **Closed** |
| G03 | Actor selection | Framework does not own a generic character-selection flow. | **Closed** |
| G04 | Pending | Do not create global `Selection Pending` merely because Actor is unresolved. | **Closed** |
| G05 | Reselection | Do not add `Allow Reselect`, `Require Reselect` or Activity re-entry selection behavior. | **Permanently withdrawn** |
| G06 | Runtime requirement | Do not create a generic Runtime Participation Requirement derived from Activity Ready When. | **Permanently withdrawn** |
| G07 | UI dependency | Framework does not prove that game UI has an interaction path to solve an external dependency. | **Closed** |
| G08 | Diagnostics | Framework must clearly expose unresolved external dependency/evidence. | **Closed** |
| G09 | Validation | Hard errors are for contradictions intrinsically provable by the framework. | **Closed** |

General principle:

```text
Framework owns:
    state / truth
    contracts
    evidence
    capabilities / commands
    progression it truly owns
    intrinsic validation

Game owns:
    when / why capabilities are used
    UI / interaction
    gameplay rules
    orchestration between capabilities
```

External dependency is allowed and diagnostic:

```text
Actor unresolved
    → factual state

Activity waits for Actor Resolved
    → Activity evidence says requirement is not yet satisfied

Framework does not assume
    "character selection UI must be open"
```

---

# 11. Failure recovery and typed reactions

| ID | Area | Decision | Status |
|---|---|---|---|
| R01 | Failure | Readiness/runtime detects and reports typed failure; it does not know Lobby/Menu/etc. | **Closed** |
| R02 | Recovery | Game chooses recovery. | **Closed** |
| R03 | Product UX | Framework should provide official authoring to map Entry failure to reaction, avoiding mandatory custom bridge scripts. | **Conceptually closed** |
| R04 | Reaction model | Reactions use typed facts/conditions/actions/results. | **Conceptually closed** |
| R05 | Scope | Prefer context-specific reaction authoring components, not one universal visual-scripting system. | **Closed** |
| R06 | Extension | Game may extend conditions/reactions and invoke legitimate public capabilities without becoming another authority. | **Closed** |
| R07 | Forbidden | Reactions may not force readiness, mutate private authority, bypass occurrence/scope, or use implicit global lookup. | **Closed** |

Concept:

```text
Activity Entry Failure
    typed evidence
        ↓
Activity Entry Failure Recovery
    authorable matching/reaction
        ↓
typed public capability
        ↓
result/evidence
```

Shared pattern:

```text
typed fact
    ↓
typed condition
    ↓
typed action
    ↓
typed result/evidence
```

This should not become an unbounded universal mini visual-scripting system.

---

# 12. Observation and commands

| ID | Area | Decision | Status |
|---|---|---|---|
| C01 | Observation | Gameplay may observe Player/Session/Activity state without gaining mutation authority. | **Closed** |
| C02 | Activity view | Activity observation is Slot-centered and combines Activity scope with live Session truth. | **Closed** |
| C03 | Session view | Session has its own observation surface independent of the current Activity. | **Closed** |
| C04 | Commands | Commands are requests to existing authorities, not setters for internal mutable state. | **Closed** |
| C05 | Activity capabilities | Activity-contextual capabilities validate Who Participates. | **Closed** |
| C06 | Session capabilities | RequestJoin, Capacity and general Session observation do not depend on current Activity Who Participates. | **Closed** |
| C07 | Consumer boundary | Concrete scoped reachability for commands/observation must be reconciled against ADR-015 and current implementation. | **Open for technical review** |

Canonical Activity observation direction:

```text
Activity Participant
    Slot
    Occupied
    Host evidence
    Actor resolution / Current Actor
    Logical Prepared
    Physical Available
    Gameplay Ready
    ...
```

Not:

```text
List<GameObject> Players
```

Session and Activity views remain distinct:

```text
Session truth
    = actual Slots / Players / Actor correlations / runtime state

Activity participation projection
    = Activity Slot scope + live Session truth
```

---

# 13. Explicitly withdrawn / rejected proposals

These items must not return as open design questions unless source/ADR reconciliation produces concrete evidence that requires reconsideration.

| Previous proposal | Result |
|---|---|
| Generic `Gameplay Active` framework state | **Rejected** |
| Runtime Participation Requirement derived from Activity | **Rejected** |
| Actor Acquisition policy inside Activity Profile | **Rejected** |
| Missing Actor policy inside Activity | **Rejected** |
| Activity Reentry Behavior | **Rejected** |
| Allow/Require Reselect | **Rejected** |
| Global Selection Pending | **Rejected** |
| Previous/Preserved Actor + Pending Replacement as canonical state without an existing contract | **Rejected** |
| Activity copies configuration from previously executed Activity | **Rejected** |
| Field-by-field Activity Profile override | **Rejected** |
| Route/Activity implicitly changes Session Capacity | **Rejected** |
| Route change automatically dematerializes all physical Players | **Rejected** |
| Activity rematerializes every Player on every Activity entry | **Rejected** |
| Use `CloseJoining/OpenJoining` to represent and restore temporary transition blocking | **Rejected** |
| Single global `JoinBlocked` bool | **Rejected** |
| Separate `Player Cohort Integrity` readiness participant now | **Deferred — not justified yet** |
| Universal Reaction component as a mini visual-scripting system | **Rejected** |
| Framework validates that a game UI path exists to solve an external dependency | **Rejected** |
| Dynamic modification of Who Participates during the same Activity occurrence | **Rejected** |

---

# 14. Current conceptual shape

```text
GameApplication
└── Default Player Session Profile
    ├── Supported Slots
    ├── Initial Capacity
    ├── Initial Joining Intent
    ├── Slot Allocation
    └── Player Provisioning Profile
        ├── Host Provisioning
        │   ├── Scene Provided
        │   └── Manager Provisioned
        └── Actor Resolution
            ├── Resolve Default
            └── Leave Unresolved / External


Player Session Runtime
├── Slots / Players
├── Current Capacity
├── Joining Intent
├── Join Inhibits
├── Player Hosts
├── Actor correlations
├── physical-presence evidence
└── public capabilities + observation


Route
├── Default Player Activity Profile
│   ├── Participation
│   │   ├── Who Participates
│   │   ├── Ready When
│   │   ├── Required Coverage
│   │   └── If No Players Are Available
│   └── Physical Presence
│       ├── No Requirement
│       └── Require
│
└── Route Physical Presence Intent
    ├── Preserve Existing
    └── Suppress


Activity
└── Player Activity Configuration
    ├── Inherit Route Default
    └── Override → complete Player Activity Profile


Entry Attempt
├── Effective Player Activity Profile
├── Entry Participation Snapshot
├── live Player lifecycle evidence
└── Preparing → Ready → Commit
                 or
               Failed


Current Activity
├── immutable Effective Player Activity Profile for the occurrence
├── live Activity participation projection
└── Physical Presence intent applied to current participants / late joins
```

---

# 15. Validation classes already agreed

## 15.1 Structural authoring errors

Examples currently agreed:

```text
Activity = Inherit
Route Default Player Activity Profile = missing
    → invalid authoring
```

```text
Explicit Who Participates references unsupported Slot
    → invalid authoring
```

```text
At Least N > number of eligible Slots
    → invalid authoring
```

```text
Who Participates = No Slots
Ready When != None
    → invalid authoring
```

```text
Who Participates = No Slots
Physical Presence = Require
    → invalid authoring
```

```text
Route Physical Presence = Suppress
Activity Physical Presence = Require
    → invalid authoring
```

```text
Route Physical Presence = Suppress
Ready When requires Physical Available or a necessarily later physical stage
    → invalid authoring
```

## 15.2 Valid configuration that may fail at runtime

Example:

```text
Supported Slots = 4

Player Activity Profile
    Who Participates = All Supported Slots
    Coverage = All Eligible

Current Capacity = 2
```

The Profile itself can remain structurally valid because Capacity is mutable through an explicit runtime request.

For a concrete Entry Attempt:

```text
still satisfiable
    → Preparing + evidence

provably unsatisfiable for this occurrence
    → Failed + typed reason
```

No silent fallback and no indefinite wait once impossibility is provable.

---

# 16. Key lifetime model

```text
GameApplication default
    → source of initial Session configuration

Player Session Profile
    → consumed when Session is created

Player Provisioning Profile
    → resolved for the Session and stable for late joins

Player Session Runtime
    → mutable runtime authority

Route Default Player Activity Profile
    → stable authoring default for Activities in the Route

Activity Inherit / Override
    → resolves Effective Player Activity Profile

Effective Player Activity Profile
    → immutable for that Activity occurrence

Entry Participation Snapshot
    → immutable historical Entry evidence after Commit

Current Activity Participation
    → live projection while Activity is current
```

---

# 17. Candidate product UX

The exact Inspector/tooling shape is not yet reconciled with source, but the conceptual UX direction is:

```text
GameApplication
  Default Player Session Profile
      [asset]

Player Session Profile
  Supported Slots
  Initial Capacity
  Initial Joining Intent
  Slot Allocation
  Player Provisioning Profile
      [asset]

Player Provisioning Profile
  Host Provisioning
  Actor Resolution

Route
  Default Player Activity Profile
      [asset]

Activity
  Player Activity Configuration
      Mode:
        Inherit Route Default
        Override

      Effective Profile:
        <resolved profile>

      Source:
        <route or local override>
```

Designer-first surface should show effective configuration and inheritance source rather than requiring the designer to inspect technical materialization.

Advanced/Debug may expose the technical evidence, runtime authorities, snapshot/revision/occurrence and command results.

---

# 18. Items intentionally left for the next phase

This document is **not** the final ADR or implementation plan.

The following must be resolved after gap analysis and source/ADR confrontation:

1. Whether the current package already has types/assets that correspond to `Player Session Profile`, `Player Provisioning Profile`, `Player Activity Profile` or equivalent concepts under different names.
2. Whether `GameApplication` is already the correct canonical owner for the default Session configuration.
3. Exact existing semantics and names for Activity `Who Participates`, `If No Players Are Available`, and `Ready When`.
4. Whether `Physical Actor Available` already exists as an explicit readiness stage or requires a new contract.
5. How Route physical-presence intent relates to any existing Route/player preparation contracts.
6. Exact transition/Entry Attempt semantics vs existing transition ADRs and runtime state machine.
7. Whether Join blocking/inhibit semantics already exist under another contract and can be refined rather than duplicated.
8. Exact scoped consumer reachability for Player commands and observation under ADR-015.
9. Which reaction/failure contracts already exist and whether the proposed authoring is a compatible extension.
10. Which `Inherit + Override` semantics can safely be generalized to other framework areas.
11. Exact names, namespaces, assets and authoring components.
12. QA and FIRSTGAME cuts only after the official contract shape is reconciled.

---

# 19. Next work order

```text
1. Freeze this decision matrix as the conceptual baseline.
2. Perform a gap scan only for genuine architectural omissions/contradictions.
3. Correct the matrix where needed.
4. Inspect all relevant ADRs and current package source.
5. Classify every affected decision:
      Already defined
      Compatible refinement
      New ADR / ADR update required
      Conflict with existing ADR/runtime
6. Produce reconciled architecture model.
7. Only then resume technical/product implementation planning.
```

---

# 20. Baseline rule for future chats

Until reconciliation produces concrete contrary evidence:

> **A decision marked Closed in this document is not an open design question.**

Future discussion should reference the decision ID and only reopen it when one of these is true:

```text
new gap demonstrates an internal contradiction
existing ADR establishes incompatible semantics
current runtime makes the model technically invalid
real FIRSTGAME use demonstrates a product/UX failure
```

This avoids cycling through already-settled questions while preserving the ability to correct the architecture when evidence requires it.
