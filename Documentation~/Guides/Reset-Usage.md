# Reset Usage

Status: IF-ADR-035 Resettable / Composition / Target model implemented; consumer scenarios 1-8 are integrated and manually validated in planet-devourer as of 2026-10-02. UPM release validation remains separate.
Last updated: 2026-10-02

Reset content ownership, Reset membership, semantic target kind and target addressing are separate concerns:

- `RuntimeContentOwner` controls content lifetime and registration release.
- `ResetMembership` controls inclusion in CurrentActivity or CurrentRoute.
- `ResetTargetKind` describes what the request resets.
- `ResetReferenceMode` selects direct or stable addressing for Object/Composition.

## Author a Resettable

Add `Resettable` to the root of one independently executable Reset unit. Route/Activity content transactions register it with their actual owner; ownership is not inferred from the current scene or component enable timing.

A Resettable is a **hierarchy boundary for Reset capabilities**. Capability collection starts at the Resettable root, traverses its descendants deterministically, and stops when another nested `Resettable` is encountered. Capabilities may therefore live on the Resettable GameObject or on different descendant GameObjects inside the same boundary.

Example:

```text
Multiple Participants
  Resettable
  ├─ NPC - Transform Participant
  │    UnityTransformResetParticipant
  └─ NPC - Active Participant
       UnityGameObjectActiveResetParticipant
```

The example is one runtime Reset subject with two participants. One request can restore the first NPC's Transform and the second NPC's active state.

`ResetMembership.FollowOwner` is the default. Route-owned content may use Activity membership when it survives Activity Clear/Reenter but should restore during Activity Reset. Activity-owned content cannot use Route membership.

## Author Reset capabilities

The normal Resettable path does not require authored participant IDs. Runtime registration wraps each collected capability with deterministic runtime participant identity. Normal authoring should focus on the capability configuration, `Display Name`, `Requiredness` and `Order`.

The independent `UnityResetSubjectAdapter` path retains its authored descriptor/ID contract. Do not copy Adapter-specific ID authoring into a Resettable composition.

### Transform

`UnityTransformResetParticipant` restores an authored Transform baseline.

Typical authoring:

```text
Target
Capture Baseline On Enable
Restore Position / Rotation / Scale
Display Name
Requiredness
Order
```

Use the Inspector baseline-capture action when the current Edit Mode transform should become the explicit baseline.

### GameObject active state

`UnityGameObjectActiveResetParticipant` restores `activeSelf` to its captured/authored baseline.

Typical authoring:

```text
Target
Capture Baseline On Enable
Baseline Active
Display Name
Requiredness
Order
```

The target may be the participant's own GameObject or another GameObject inside the owning Resettable boundary. Use the Inspector baseline-capture action when the current Edit Mode active state should become the explicit baseline.

## Compose multiple Resettables

`ResetComposition` groups Resettable members but is not itself a Reset subject.

- `Descendants` collects members within its hierarchy boundary, stops at nested ResetComposition boundaries and preserves deterministic hierarchy order.
- `ExplicitMembers` uses only typed references, deduplicates them and preserves serialized list order.

A nested Resettable remains an independent executable subject. Invalid/null references and empty compositions have defined diagnostics.

## Author a Reset request

Use `ResetRequestTrigger` as the single Reset request surface.

Its Reset execution port and scene-authored Reset Subject adapters are composed from each managed Scene's explicit roots. Persistent Content uses the same bind/release participants for the Session lifetime; scene release detaches request and registration ports before unload. See [IF-ADR-040](../Architecture/ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md).

| Target kind | Reference mode | Payload |
|---|---|---|
| Object | Direct | Typed `Resettable` reference |
| Object | Stable | `StableObjectReference` resolving to a GameObject with a currently registered `Resettable` |
| Composition | Direct | Typed `ResetComposition` reference |
| Composition | Stable | `StableObjectReference` resolving to a GameObject with `ResetComposition`; resolves its current members |
| CurrentActivity | None | No payload |
| CurrentRoute | None | No payload |

`Unknown` is only a default/serialization sentinel and is invalid. Changing target kind or reference mode clears inactive payloads. Null, unregistered, stale, ambiguous, missing-component or out-of-context targets fail diagnostically; Reset never registers a target implicitly.

### Direct addressing

Prefer Direct when the authored Unity reference is valid across the relevant composition boundary.

```text
Object / Direct
  -> Resettable

Composition / Direct
  -> ResetComposition
  -> current member Resettables
```

### Stable addressing

Use Stable only for a real cross-scene/serialization boundary. Reset consumes the generic IF-ADR-014 `StableObjectBinding` authority:

```text
ObjectEntryId + optional typed RouteAsset/ActivityAsset selector
  -> current physical GameObject occurrence
  -> Resettable or ResetComposition on that same GameObject
```

Generate the declaration's `ObjectEntryId` explicitly, then use Copy ID / Paste ID in the stable reference. The optional typed owner selector disambiguates the exact Route or Activity definition. Missing bindings fail as unavailable; multiple matches fail as ambiguous. Reload resolves the fresh physical occurrence. Reset does not serialize runtime subject identity.

## CurrentActivity and CurrentRoute

`CurrentActivity` includes registered subjects with effective Activity membership owned by the current Activity or by the current Route. It excludes Route membership.

`CurrentRoute` is the umbrella scope of the current Activity (`Activity ⊂ Route`). It includes:

- effective Route membership owned by the current Route;
- Route-owned subjects with effective Activity membership;
- Activity-owned subjects from the current Activity context when one is active.

It excludes subjects from another Route and Activity-owned subjects from other Activities. With no active Activity, it still includes the current Route's Route- and Activity-membership subjects.

## Activity Restart

Activity Restart is lifecycle orchestration, not a second Reset engine.

```text
resolve restart target
  -> reset selected surviving state
  -> Activity Clear
  -> Activity Reenter
```

The restart Reset filters out Activity-owned content that will be recreated and includes surviving Route-owned content selected for Activity membership. A failed required Reset blocks Clear/Reenter.

Use `ActivityRestartTrigger` for restart. Do not replace it with a plain `ResetRequestTrigger`.

## Diagnostics

Reset participants may emit focused verification evidence through the Framework's canonical logging path. Treat this as diagnostic evidence, not as gameplay authority.

For scenario validation, record both the requested selection and the observable result. For multiple-participant cases, verify that every expected participant restored its own state under the same Resettable execution.

## Registration adapter

`UnityResetSubjectAdapter` remains available for its independent registration contract. It is not the normal IF-ADR-035 Resettable path, is not a Reset request surface, and does not replace owner-aware Resettable registration.

## Consumer validation scenarios

The closed consumer proof set covers these distinct contracts:

1. Object / Direct.
2. Object / Stable.
3. Composition / Direct with Descendants.
4. Composition / Stable with Explicit Members.
5. CurrentActivity.
6. CurrentRoute as the Route umbrella.
7. Activity Restart with surviving Route-owned / Activity-membership state.
8. One Resettable with multiple participants restoring different state.

All eight scenarios are integrated in the planet-devourer authoring/proving workspace and were manually validated. This consumer closure does not claim UPM package-import validation.

These scenarios are usage examples, not additional runtime APIs.

## Technical validation

For Framework changes, run the relevant Reset Edit Mode coverage and QAFramework lifecycle coverage. At minimum, keep target resolution, composition, stable binding, authoring and Activity Restart regressions covered. Consumer Play Mode evidence should remain separate from automated technical certification.
