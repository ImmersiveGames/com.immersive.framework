# Reset Usage

Status: IF-ADR-035 Resettable / Composition / Target model implemented; RESET-035-H removes the legacy Adapter path. Consumer scenarios 1-8 were integrated and manually validated in planet-devourer on the earlier canonical model as of 2026-10-02. UPM release validation remains separate.
Last updated: 2026-10-12

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

Do not author participant IDs. The owner-aware Resettable transaction generates deterministic capability IDs within each Resettable boundary.

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

Its Reset execution port and scene-authored request triggers are bound from each managed Scene's explicit roots. Resettable registration is owned by the Route/Activity materialization transactions; Scene Lifecycle only binds/releases request triggers. Persistent Content keeps trigger bindings for the Session lifetime. See [IF-ADR-040](../Architecture/ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md).

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

Generate the declaration's `ObjectEntryId` explicitly, then enter or paste it in the stable reference's Inspector field, or choose a discovered declaration from the picker. The picker discovers declarations in configured Route/Activity scenes even when their scene is not loaded. The optional **Resolution Owner** field remains independent and selects an exact Route or Activity definition only when that is the intended resolution context. Missing or invalid stored IDs are flagged and preserved until the designer explicitly changes them. Missing runtime bindings still resolve as unavailable; multiple matches remain ambiguous. Reload resolves the fresh physical occurrence. Reset does not serialize runtime subject identity.

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

## Migration from the removed Adapter

RESET-035-H deliberately removes `UnityResetSubjectAdapter`, its binding and registration ports, and its independent Scene Lifecycle path. This is a breaking change under IF-GOV-001; there is no shim or parallel registration path.

For each authored Adapter, remove the component and place `Resettable` at the root of the independently executable unit. Keep `UnityResetParticipantBehaviour` or custom `IUnityResettable` capabilities inside that boundary. Configure `Display Name`, `Requiredness` and `Order`; choose `ResetMembership` only when inclusion differs from owner-following behavior; address the resulting Resettable or a `ResetComposition` through `ResetTarget`.

Custom `IUnityResettable` implementations no longer need `ResetParticipantId`. `IUnityResettableMetadata` now contains only `ResetRequiredness`, `ResetOrder` and `ResetDisplayName`; remove Adapter-only source/reason metadata. `UnityResetParticipantBehaviour` is now a capability base, not an `IResetParticipant` descriptor provider; custom subclasses must remove calls to `ParticipantIdText`, `TryCreateResetParticipantDescriptor`, `ResolveSource` and `ResolveReason`, and use the provided Reset context for results. `ResetAuthoringIdentityUtility.GenerateMissingSubjectId` and `GenerateMissingParticipantId` are removed because the canonical workflow authors neither identity; its shared prefab-recording helper remains. The serialized Adapter component GUID and Adapter-only participant ID/source/reason data are not migrated automatically.

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

These scenarios are usage examples, not additional runtime APIs. The earlier planet-devourer proof did not validate Adapter removal or package import against this source cut.

## Technical validation

For Framework changes, run the relevant Reset Edit Mode coverage and QAFramework lifecycle coverage. At minimum, keep target resolution, composition, stable binding, authoring and Activity Restart regressions covered. Consumer Play Mode evidence should remain separate from automated technical certification.
