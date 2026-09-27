# IF-ADR-036 — Gameplay Readiness and Runtime Gameplay Availability Separation

Status: **Proposed**  
Proposed: **2026-09-27**  
Type: architecture / Player gameplay / Activity readiness / Input gating  
Related decisions: IF-ADR-005, IF-ADR-007, IF-ADR-012, IF-ADR-019, IF-ADR-033  
Normative relationship: **This ADR separates structural Gameplay Readiness from transient Runtime Gameplay Availability. It does not change Session Player ownership or Runtime Gate ownership.**

## 1. Context

The current Player gameplay runtime mixes two questions:

1. Is the Player contextually prepared for gameplay in the current Activity?
2. Can gameplay consume input at this instant?

`PlayerGameplayAdmissionRuntimeContext` currently derives admission state from `PlayerGameplayInputBindingSummary.IsAllowed`:

```text
input allowed     -> Ready
input not allowed -> BlockedByInputGate
```

Consequently a transient Runtime/Input Gate changes `GameplayReady`.

During Route transition the transition Gate may intentionally block gameplay while the destination Activity establishes its contextual Player representation. If Activity readiness also requires input to be allowed, readiness depends on a Gate that is itself released only after readiness. The current runtime requires special reconciliation to break that cycle.

## 2. Decision

**Gameplay Readiness and Runtime Gameplay Availability are independent dimensions.**

```text
GameplayReady
    = the current Activity gameplay representation is structurally complete and current

RuntimeGameplayAvailable
    = GameplayReady
    AND current runtime/input conditions allow gameplay consumption
```

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

Transient input availability is not an admission criterion.

Therefore `BlockedByInputGate` does not belong to Gameplay Admission state. Its semantics belong to Runtime Gameplay Availability.

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

Existing `PlayerGameplayInputAvailability` / input-binding availability must be evaluated as the representation of this dimension before introducing another abstraction.

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
-> RuntimeGameplayAvailable = true
```

Gate release does not require Gameplay Admission refresh when no structural/contextual capability changed.

## 8. Pause invariant

Pause follows the same separation:

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

After implementation:

- `BlockedByInputGate` is removed from Gameplay Admission semantics;
- Gameplay Admission no longer depends on transient `input.IsAllowed`;
- Activity Gameplay Readiness no longer depends on physical input availability;
- Gate changes do not refresh or re-admit gameplay;
- `IsOnlyBlockedByCurrentEntryGate` is no longer required to break readiness/Gate circularity;
- per-frame polling used only to promote admission after Gate release is removed;
- effective gameplay availability is explicit at the input-consumption boundary;
- contracted action-map restoration remains a physical input responsibility;
- Session Player ownership remains unchanged;
- the input Gate adapter remains independent from Route, Activity and admission.

Existing compensatory mechanisms must not be removed before the replacement contract is implemented in the same migration cut.

## 11. Compatibility and affected contracts

Before changing APIs, locate all consumers of:

- `PlayerGameplayAdmissionState`;
- `BlockedByInputGate`;
- `GameplayReady`;
- `IsAdmitted`;
- `TryRefreshReadiness`;
- `IsOnlyBlockedByCurrentEntryGate`;
- `PlayerGameplayInputReader.GameplayReady`.

Compatibility must not preserve the semantic conflation corrected by this ADR.

## 12. Relationship to existing decisions

- **IF-ADR-005:** Pause continues to own Pause state/gating, not Gameplay Admission.
- **IF-ADR-007:** Activity Entry Readiness consumes structural Gameplay Readiness.
- **IF-ADR-012:** Activity Player Participation continues to project Session Players without owning Session configuration.
- **IF-ADR-019:** Session Player lifetime remains independent from Activity contextual representation.
- **IF-ADR-033:** post-admission convergence/contextual binding remains valid; transient runtime availability is not part of admission.

Conflicting transitional wording in related ADRs must be reconciled as part of implementation.

## 13. Migration

Implement in small cuts:

1. define Admission/Readiness/Availability contract semantics;
2. remove transient Gate availability from Gameplay Admission;
3. expose effective gameplay availability at the input-consumption boundary;
4. make Activity readiness consume only structural Gameplay Readiness;
5. remove entry-Gate readiness exceptions and admission polling/refresh;
6. reconcile contracted action-map intent with Gate suppression/restoration;
7. update consumers, diagnostics and related documentation;
8. validate Route A -> B, A -> B -> A, Pause/Resume and Transition + Pause cleanup.

## 14. Validation criteria

This ADR is technically validated only when:

1. Activity `GameplayReady` can be true while a Transition Gate is active.
2. Gate release does not alter or recreate Gameplay Admission.
3. Session Player / Host / PlayerInput identity survives Route transition.
4. Gameplay becomes consumable after Gate release.
5. Pause/Resume changes availability without changing Gameplay Readiness.
6. No per-frame admission polling is required for Gate-release convergence.
7. The physical Gate adapter remains unaware of Route, Activity and Gameplay Admission.

Until then:

- **Implemented:** No
- **Integrated:** No
- **Unity tested:** No
- **Technically validated:** No
- **QA certified:** No
