# P3H.2 — Actor Profile and Selection Policy Authoring Foundation

> Current contract correction (`PROD-ASSET-1C`, 2026-07-22):
> `PlayerActorSelectionPolicyProfile` was removed because the policy contains only one
> application-owned enum and has no reusable identity or rule set. Actor selection policy is
> now configured directly on `GameApplicationAsset`. The remainder of this document records
> the historical P3H.2 cut.

## Type

Product authoring foundation and technical QA.

## Objective

Introduce the immutable product assets required by the accepted Session Actor-selection model without adding mutable Session selection state or logical Actor materialization.

## Product surface

```text
Assets/Create/Immersive Framework/Actors/Actor Profile

GameApplication
  Local Player Participation
    Actor Duplicate Selection
```

The existing `Player Slot Profile` inspector now exposes an optional direct `Default Actor Profile` reference.

## Runtime-facing immutable assets

```text
ActorProfile
  ActorProfileId
  Display Name
  Description
  Icon optional
  Actor Kind
  Actor Role
  Logical Actor Host Prefab

GameApplicationAsset
  PlayerActorSelectionDuplicatePolicy
    AllowDuplicates
    UniqueAcrossJoinedSlots

PlayerSlotProfile
  Default Actor Profile optional
```

These assets contain product identity and policy only. They do not contain current Slot selection, `ActorId`, PlayerInput/device state, owner scope, occupancy, materialization handle or readiness state.

## Validation

The package validates:

```text
ActorProfileId is non-empty and unique across the project
Actor Kind and Actor Role are explicit non-Unknown values
Logical Actor Host is a prefab asset
Logical Actor Host contains exactly one Actor declaration
Player Profiles use PlayerActorDeclaration with PlayerInput evidence
non-Player Profiles use ActorDeclaration
Profile classification matches declaration evidence
GameApplication duplicate-selection policy is explicit
Player Slot default Actor references a valid ActorProfile
```

Project validation reports missing Actor Profiles or selection policies as optional skips until the runtime selection cut composes them as mandatory product inputs.

## Templates

The GameApplication Inspector provides the two valid choices directly:

```text
Actor Selection — Allow Duplicates
Actor Selection — Unique Across Joined Slots
```

No Actor Selection Policy assets are generated. The framework also deliberately does not
generate a placeholder `ActorProfile`, because a valid Profile requires a real project-owned
Logical Actor Host prefab. Official tooling must not create knowingly invalid product assets.

## Out of scope

```text
Session selected-Actor state
select / replace / clear operations
selection revision and stale request rejection
GameApplication policy binding
SelectedActors readiness evaluation
ActorId assignment
logical Actor materialization
Presentation / Skin
FIRSTGAME integration
```

## Expected QA

```text
[P3H2_ACTOR_SELECTION_AUTHORING_SMOKE] status='Passed' cases='18'
```

## Suggested commits

```text
Framework:
PROD-ASSET-1C — configure Actor selection policy directly on GameApplication

QAFramework:
P3H.2 — add Actor selection authoring QA
```
