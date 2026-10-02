# Reset Usage

Status: RESET-035-B through RESET-035-F implemented in Framework; Unity compile/import and Edit Mode execution remain subject to repository validation policy.
Last updated: 2026-10-01

Reset content ownership, Reset membership, semantic target kind and target addressing are separate concerns:

- `RuntimeContentOwner` controls content lifetime and registration release.
- `ResetMembership` controls inclusion in CurrentActivity or CurrentRoute.
- `ResetTargetKind` describes what the request resets.
- `ResetReferenceMode` selects direct or stable addressing for Object/Composition.

## Register resettable content

Add `Resettable` to a GameObject and author its local Reset capabilities. Route/Activity content transactions register it with their actual owner; they do not infer ownership from the current scene or component enable timing. `ResetMembership.FollowOwner` is the default. Route-owned content may use Activity membership when it survives Activity Clear/Reenter but should restore during Activity Reset. Activity-owned content cannot use Route membership.

`ResetComposition` groups Resettable members but is not a Reset subject. `Descendants` collects within its hierarchy boundary, stops at nested ResetComposition boundaries and preserves deterministic hierarchy order. `ExplicitMembers` uses only typed references, deduplicates them and preserves list order. Invalid/null references and empty compositions have defined diagnostics.

## Author a Reset request

Use `ResetRequestTrigger` as the single Reset request surface. Its target is selected by semantic intent and then addressed as follows:

| Target kind | Reference mode | Payload |
|---|---|---|
| Object | Direct | Typed `Resettable` reference |
| Object | Stable | `StableObjectReference` resolving to a GameObject with a currently registered `Resettable` |
| Composition | Direct | Typed `ResetComposition` reference |
| Composition | Stable | `StableObjectReference` resolving to a GameObject with `ResetComposition`; resolves its current members |
| CurrentActivity | None | No payload |
| CurrentRoute | None | No payload |

`Unknown` is only a default/serialization sentinel and is invalid. Changing target kind or reference mode clears inactive payloads. Null, unregistered, stale, ambiguous, missing-component or out-of-context targets fail diagnostically; Reset never registers a target implicitly.

## Stable addressing

Prefer Direct when an authored Unity reference can cross the relevant boundary. Stable addressing is for a real cross-scene/serialization boundary and uses the generic IF-ADR-014 `StableObjectBinding`:

```text
ObjectEntryId + optional typed RouteAsset/ActivityAsset selector
  -> current physical GameObject occurrence
  -> Resettable or ResetComposition on that same GameObject
```

Generate the declaration's `ObjectEntryId` explicitly under Advanced / Debug, then use Copy ID and Paste ID in the stable reference. The optional selector derives stable owner identity and exact definition token from the typed asset. Missing bindings fail as unavailable; multiple matches fail as ambiguous. Rollback/release removes the binding with its owner, and reload resolves the new physical occurrence. No runtime subject ID is serialized.

## Current targets and Activity Restart

`CurrentActivity` includes registered subjects with effective Activity membership owned by the current Activity or by the current Route. It excludes Route membership.

`CurrentRoute` is the parent scope of the current Activity (`Activity ⊂ Route`). It includes effective Route membership owned by the current Route, Route-owned subjects with Activity membership, and Activity-owned subjects from the current Activity context when registered. It excludes subjects from another Route and Activity-owned subjects from other Activities. With no active Activity, it still includes the current Route's Route and Activity memberships.

Activity Restart uses the semantic Reset target to restore surviving state before Activity Clear/Reenter. It filters out Activity-owned content that will be recreated and includes surviving Route-owned content selected for Activity membership. A failed Reset blocks Clear and Reenter.

## Registration adapter

`UnityResetSubjectAdapter` remains available for its independent subject registration contract. It is not a Reset request surface and does not replace `Resettable` owner-aware registration.

## Manual validation

1. Compile/import Framework and QAFramework in Unity.
2. Run `ResetTargetResolverTests`, `ResetCompositionTests`, `StableObjectBindingTests` and `ResetTargetAuthoringTests`.
3. Run the Activity Restart lifecycle QA for successful surviving-state restoration and Reset failure blocking Clear/Reenter.
4. Validate an Object and a Composition stable target across scene boundaries, including unload/reload and ambiguity.
