# IF-ADR-036 — Gameplay Readiness and Runtime Gameplay Availability Separation

Status: **Accepted — structural Gameplay Readiness and Runtime Gameplay Availability separation**
Proposed: **2026-09-27**  
Accepted: **2026-10-12**
Type: architecture / Player gameplay / Activity readiness / Input gating  
Related decisions: IF-ADR-005, IF-ADR-007, IF-ADR-012, IF-ADR-019, IF-ADR-033  
Normative relationship: **This ADR separates structural Gameplay Readiness from transient Runtime Gameplay Availability. It does not change Session Player ownership or Runtime Gate ownership.**

## 1. Context

The Player gameplay runtime exposes two distinct questions:

1. Is the Player contextually prepared for gameplay in the current Activity?
2. Can gameplay consume input at this instant?

Gameplay Admission establishes structural, Activity-scoped gameplay
participation. Runtime Gameplay Availability determines whether gameplay can be
consumed at this instant. The current implementation keeps these dimensions
separate: admission validates the current gameplay Input binding and its
identity, while effective availability accounts for runtime/input conditions.
The binding is a structural capability, not a claim that PlayerInput, the
Gameplay Action Map or physical gameplay consumption is currently enabled.

## 2. Decision

**Gameplay Readiness and Runtime Gameplay Availability are independent dimensions.**

```text
GameplayReady
    = the current Activity gameplay representation is structurally complete and current

RuntimeGameplayAvailable
    = GameplayReady
    AND current runtime/input conditions allow gameplay consumption
```

The current input reader additionally requires itself to be active and enabled
before reading gameplay actions. That consumer-level condition is not part of
structural GameplayReady.

The following is valid and expected during Transition, Pause, Recovery or another transient Gate:

```text
IsAdmitted = true
GameplayReady = true
RuntimeGameplayAvailable = false
```

A Runtime Gate does not create, revoke, promote or refresh Gameplay Admission merely by changing execution availability.

## 3. Gameplay Admission authority

Gameplay Admission validates the structural/contextual capabilities required by the current Activity occurrence, including as applicable:

- Activity contextual owner;
- Player Slot identity;
- Actor preparation;
- Gameplay occupancy;
- gameplay input binding identity;
- current capability tokens and revisions.

The current, identity-consistent gameplay Input binding is an admission
criterion. Its transient availability is not. Pause, Input Gate, physical
action-map posture and consumer-owned availability blocks do not revoke that
structural binding or change Gameplay Admission.

Therefore the former `BlockedByInputGate` Admission state is obsolete. Gate
blocks are represented by the existing Runtime Gameplay Availability statuses;
the old Admission state is not itself an Availability value.

A Gate change must not, by itself:

- change the admission token;
- change Gameplay Readiness;
- change Activity readiness contribution;
- require re-admission;
- manufacture readiness.

## 4. Runtime Gameplay Availability

Runtime Gameplay Availability is a transient projection over a valid gameplay participation.

The architecture is:

```text
Session Player
    -> Activity Projection
    -> Actor Preparation
    -> Gameplay Occupancy
    -> Gameplay Input Binding
    -> Gameplay Admission
    -> GameplayReady

Runtime Gates
    -> Runtime Gameplay Availability
    -> physical input posture
    -> gameplay consumption
```

Existing `PlayerGameplayInputAvailability` / input-binding availability is the
representation of this dimension; no additional abstraction or public API is
introduced by this decision.

## 5. Ownership and lifetime

| Concept | Owner / lifetime |
|---|---|
| Session Player / Host / PlayerInput | Session Player occurrence |
| Activity participation / projection | Activity occurrence |
| Gameplay occupancy | prepared Player/Actor occurrence |
| Gameplay input binding | Activity gameplay occurrence |
| Gameplay Admission / Readiness | Activity gameplay occurrence |
| Runtime Gate | operation that establishes the transient block |
| physical action-map posture | canonical input writer / adapter |

This ADR does not transfer Player or admission ownership to GameFlow, Pause or Input Gate.

## 6. Runtime Gate boundary

`UnityPlayerInputGateAdapter` remains a physical boundary and must not know Route, Activity or Gameplay Admission.

Its responsibility is to materialize the effective Gate posture through the canonical input writer:

```text
Gate blocked  -> gameplay consumption physically unavailable
Gate released -> contracted gameplay posture restored
```

Restoring physical input posture after Gate release remains required, but it is not a Gameplay Admission operation.

## 7. Activity readiness

`Ready When = Gameplay Ready` means that required projected Players have complete and current contextual gameplay representation.

It does not mean that those Players can physically consume gameplay input at that instant.

A destination Activity may therefore become Gameplay Ready while the Route transition Gate remains active.

Normative Route transition:

```text
Transition Gate ON
-> release contextual representation A
-> retain Session Player / Host / PlayerInput
-> establish contextual representation B
-> establish Gameplay Admission B
-> GameplayReady B = true
-> Activity B Ready
-> commit transition
-> Transition Gate OFF
-> restore physical gameplay posture
-> RuntimeGameplayAvailable reflects remaining Pause/Gate/consumer/input conditions
```

Gate release does not require Gameplay Admission refresh when no
structural/contextual capability changed. Gate/Pause changes alone do not
require Activity readiness reconciliation. The input runtime may still restore
the contracted physical action-map posture and refresh its operational
availability projection.

## 8. Pause invariant

With no other active availability block, Pause follows the same separation:

```text
GameplayReady = true
RuntimeGameplayAvailable = true

Pause
-> GameplayReady remains true
-> RuntimeGameplayAvailable = false

Resume
-> GameplayReady remains true
-> RuntimeGameplayAvailable = true
```

Pause must not change Gameplay Admission or Activity readiness solely because gameplay execution is blocked.

## 9. Input consumer semantics

A gameplay input consumer must distinguish structural readiness from effective input availability.

An API that also requires `PlayerInput.enabled`, active input and an enabled gameplay action map represents effective availability and must not expose that condition as structural `GameplayReady`.

The implementation should prefer an explicit semantic such as `RuntimeGameplayAvailable` or `CanReadGameplayInput`, reusing existing contracts where possible rather than introducing unnecessary infrastructure.

## 10. Consequences

The current Runtime reflects this separation:

- `BlockedByInputGate` is removed from Gameplay Admission semantics;
- Gameplay Admission no longer depends on transient `input.IsAllowed`;
- Activity Gameplay Readiness no longer depends on physical input availability;
- Gate changes do not refresh or re-admit gameplay;
- `IsOnlyBlockedByCurrentEntryGate` is no longer required to break readiness/Gate circularity;
- the former entry-Gate exception and admission promotion path are not part of
  the current Admission contract;
- per-frame physical action-map restoration remains an input responsibility
  and is not readiness polling;
- effective gameplay availability is explicit at the input-consumption boundary;
- contracted action-map restoration remains a physical input responsibility;
- Session Player ownership remains unchanged;
- the input Gate adapter remains independent from Route, Activity and admission.

## 11. Compatibility and affected contracts

Before changing APIs, locate all consumers of:

- `PlayerGameplayAdmissionState`;
- `BlockedByInputGate`;
- `GameplayReady`;
- `IsAdmitted`;
- `TryRefreshReadiness`;
- `IsOnlyBlockedByCurrentEntryGate`;
- `PlayerGameplayInputReader.GameplayReady`.

Consumers that need to read gameplay input must use effective availability at
the consumption boundary. The decision changes no API maturity and does not
expand any Stable contract. Deliberate consumer migration is acceptable; no
compatibility rail should restore the former conflated semantics.

## 12. Relationship to existing decisions

- **IF-ADR-005:** Pause continues to own Pause state/gating, not Gameplay Admission.
- **IF-ADR-007:** Activity Entry Readiness consumes structural Gameplay Readiness.
- **IF-ADR-012:** Activity Player Participation continues to project Session Players without owning Session configuration; `GameplayReady` is structural and implies neither Camera readiness nor physical input availability.
- **IF-ADR-019:** Session Player lifetime remains independent from Activity contextual representation.
- **IF-ADR-033:** post-admission convergence/contextual binding remains valid; transient runtime availability is not part of admission.

Related normative wording is reconciled with the current Runtime by this
decision.

## 13. Migration

No Runtime API or Stable contract change is authorized by this ADR. The
QAFramework `QA-IF-ADR-036` Play Mode proof is user-reported at 8/8, with
request B accepted and completed, `BaselineRestored` cleanup and no
divergences. It covers readiness and availability during the active Transition
Gate, the contractually rejected Pause request during that Gate, post-transition
Pause/Resume, identity stability and cleanup.

## 14. Validation criteria

The source-level separation and the supported Transition/Pause ordering have
integrated evidence. The proof establishes:

1. Activity `GameplayReady` can be true while a Transition Gate is active.
2. Gate release does not alter or recreate Gameplay Admission.
3. Session Player / Host / PlayerInput identity survives Route transition.
4. Gameplay becomes consumable after Gate release when no other availability
   block remains.
5. Pause/Resume changes availability without changing Gameplay Readiness,
   subject to other concurrent availability blocks.
6. Gate-release convergence does not require readiness polling or re-admission;
   physical action-map restoration may still run in the input runtime.
7. The physical Gate adapter remains unaware of Route, Activity and Gameplay Admission.

Pause admission is rejected while a Transition Gate is active under the current
Pause policy. The proof validates that rejection and exercises Pause only after
the Transition Gate is gone; it does not certify simultaneous active Pause and
Transition Gate blockers. That combination is not an executable composition
under the current policy.

Disposition (2026-10-12):

- **Decision:** Accepted.
- **Runtime source alignment:** Present; structural admission and operational
  availability are separate in the current implementation.
- **Stable maturity or contract change:** None.
- **Supported Transition Gate and post-transition Pause sequence:** QAFramework
  `QA-IF-ADR-036`, Play Mode, user-reported 8/8; cleanup `BaselineRestored`.
- **Pause requested during an active Transition Gate:** Correct rejection
  covered by the same proof.
- **Simultaneous active Pause and Transition Gate:** Unsupported by current
  Pause policy and not certified as a valid composition.
