# Reset Usage

Status: Transitional — RESET-035-B owner-aware registration, RESET-035-C membership, RESET-035-D composition, RESET-035-E semantic requests and RESET-035-F StableReference are implemented in Framework; legacy Reset authoring remains available during migration
Last updated: 2026-10-01

> **Architecture transition (IF-ADR-035):** The sections explicitly labeled legacy remain supported during migration but are not the target product model. New authoring converges on `Resettable -> ResetComposition -> ResetTarget`, with content ownership derived from composition/materialization origin and Reset membership modeled separately. Do not expand the legacy textual-ID/scope/list authoring model.

## RESET-035-B implemented surface

`Resettable` is registered by Route/Activity scene composition transactions using the target `RuntimeContentOwner` and their materialized scene roots. The runtime generates subject identity, keeps nested `Resettable` boundaries separate, rejects mixed legacy/new registration, rolls back failed admission, and releases registrations by owner. The legacy authoring flow below is still supported during migration.

## RESET-035-C membership

`Resettable` defaults to `ResetMembership.FollowOwner`. The transaction records its membership policy with the generated runtime subject:

| Content owner | Membership | Effective target |
|---|---|---|
| Activity | `FollowOwner` | Current Activity |
| Route | `FollowOwner` | Current Route |
| Route | `Activity` | Current Activity while that Route occurrence is current |
| Activity | `Route` | Invalid; registration rejects it because Activity content does not survive its Route boundary |

`CurrentRoute` includes only Route members registered by the current Route occurrence. `CurrentActivity` includes Activity members from the current Activity occurrence and Activity members explicitly contributed by the current Route occurrence. Membership does not alter content ownership or release authority. `ResetSubjectScope` remains a transitional registry/request bridge; do not use it as the membership policy or add it to normal authoring.

## RESET-035-D composition

Add `ResetComposition` to a GameObject to author a semantic set of Resettable members. It is not a Reset subject and does not register or execute Reset.

- `Descendants` walks the composition GameObject and its child hierarchy in depth-first sibling order. It deduplicates Resettable references and stops before any nested ResetComposition GameObject; that nested composition resolves its own set. Nested Resettable components remain distinct members, while each Resettable's own capability collection stops at its nested Resettable boundary.
- `ExplicitMembers` resolves only the serialized Resettable references, preserving list order and deduplicating repeats. Null references are skipped with diagnostics. No names or paths are used to find members.
- An explicit local `Activity` or `Route` value on a Resettable takes precedence. A local `FollowOwner` delegates to the composition's membership; composition `FollowOwner` leaves owner-derived membership intact. Shared members may appear in multiple compositions if they resolve to the same membership. Conflicting resolved memberships are rejected before registration.
- An explicit member must be among the materialized roots being registered by the owner transaction. This keeps membership assignment inside the transaction that supplies its lifetime owner. Empty compositions resolve successfully with a `reset-composition-empty` diagnostic.
- An explicit typed reference may intentionally name a member beneath another composition boundary. If both compositions include that Resettable, the same precedence and conflict rules apply.

Composition resolution prepares membership and member sets. RESET-035-E resolves a Composition target through the same typed member resolution and sends registered subjects to the existing executor.

## RESET-035-E semantic request

Use one `ResetRequestTrigger` with a typed `ResetTarget`:

| Target | Resolution |
|---|---|
| `Object` | Exactly one registered `Resettable` in the current Activity/Route owner context |
| `Composition` | Registered members resolved by the referenced `ResetComposition`, in deterministic collection order |
| `CurrentActivity` | Subjects with effective Activity membership for the current Activity and Route occurrence |
| `CurrentRoute` | Subjects with effective Route membership for the current Route occurrence |

Null, unregistered, stale or out-of-context Object/Composition members fail diagnostically; requests never register a target implicitly. Empty compositions resolve to an empty successful selection with a diagnostic. All targets use `ResetTargetResolver`, `ResetSelectionResolution` and the existing `ResetExecutor`.

Activity Restart defaults to `CurrentActivity`, but its pre-clear Reset includes only selected subjects owned by the current Route. For `CurrentActivity`, the Route-owned subject must also have effective Activity membership. Activity-owned scene content is recreated by Clear/Reenter and is excluded to prevent duplicate restoration. Reset failure blocks Clear/Reenter through the existing Activity Restart callback contract.

`StableReference` is an exceptional cross-boundary target backed by the generic `StableObjectBinding` authority from IF-ADR-014. Generate the declaration's stable `ObjectEntryId` under Advanced / Debug, then use Copy ID there and Paste ID in StableReference. Object Entry IDs follow the same explicit Generate/Copy/Regenerate pattern as Route and Activity IDs and do not change on rename or move. The owner-aware admission transaction supplies effective Route/Activity scope and owner, so the declaration only authors Requiredness in its normal Inspector.

When that logical identity is active under multiple owners, select the exact `RouteAsset` or `ActivityAsset` in StableReference; Framework derives stable identity and definition token. No textual owner field or duplicated owner metadata is authored. StableReference resolves only a unique live occurrence admitted by its content transaction, then requires that occurrence's currently registered Resettable. Rollback/release removes its binding; reload resolves the new occurrence and runtime Reset subject. Missing and ambiguous bindings fail diagnostically. Do not persist runtime subject IDs. Prefer Object or Composition when a direct typed reference is possible. Application-level StableReference use is validated separately from this Framework implementation.

## Remaining target authoring model

```text
Reset capability
  -> Resettable
      -> optional ResetComposition
          -> ResetTarget
              -> existing ResetRegistry / ResetExecutor during migration
```

Normal target authoring does not require textual Reset IDs, per-object ownership scope or explicit subject-ID groups. Stable identity remains opt-in for real cross-boundary specific targeting.

## Legacy operation mapping during migration



| Need | Surface |
|---|---|
| New semantic Reset request | `ResetRequestTrigger` + `ResetTarget` |
| Legacy individual Object Reset | `ObjectResetTrigger` |
| Legacy selected/scoped Object Reset | `ObjectResetGroupTrigger` + `ResetSelectionConfig` |
| Reset objects and restart the current Activity | `ActivityRestartTrigger` |
| Run Route/Activity lifecycle reset participants | `RouteCycleResetTrigger` / `ActivityCycleResetTrigger` |

Object Reset, Cycle Reset, Release, Snapshot and Save are separate operations.

## Author an Object Reset subject

1. Add `UnityResetSubjectAdapter` to the object.
2. Choose an authored stable id or runtime-instance id generation.
3. Choose Route, Activity or Runtime scope.
4. Add built-in Transform/active-state participants as needed.
5. Implement `IUnityResettable` for custom synchronous gameplay state.
6. Configure participant order and requiredness.
7. Point a trigger or selection configuration at the subject/scope.

Bootstrap/lifecycle binds the adapter's explicit Reset registration port.
Although the public registration method is still named
`RegisterWithCurrentHost`, it uses that bound port and performs no static host
lookup.

## Runtime flow

```text
UnityResetSubjectAdapter (legacy)
-> IResetRegistrationRuntimePort
-> ResetRegistry
-> ResetTargetResolver / ResetSelectionConfig (legacy)
-> ResetExecutor
-> ordered IResetParticipant.Reset
-> typed aggregate result
```

Required participant failure blocks the subject according to execution policy.
Optional failure remains diagnostic. Empty selection succeeds only when
explicitly allowed.

## Cycle Reset and Activity Restart

Cycle Reset executes explicitly registered lifecycle participants; it does not
reload scenes or reset gameplay objects automatically.

Activity Restart uses the semantic Reset target and canonical lifecycle/transition path. Its pre-clear selection is restricted to state that survives Activity Clear/Reenter; a blocking Reset failure prevents both lifecycle stages.

## Diagnose

Inspect subject id, origin, scope, owner, participant order/requiredness,
selection resolution and typed issues. Fix missing explicit port binding or
invalid ownership at its composition owner.

## Manual validation

1. Compile/import Framework and QAFramework in Unity.
2. Run `ResetTargetResolverTests`, `ResetCompositionTests`, and the focused
   registry/executor/legacy adapter suites.
3. Run a lifecycle integration QA for Activity Restart with Activity-owned
   recreated state and Route-owned Activity-membership state.
4. Confirm required Reset failure blocks Activity Clear/Reenter and that
   surviving state restores exactly once.
5. Confirm runtime subjects receive unique ids and stale owners are cleaned.
