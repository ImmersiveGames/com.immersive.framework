# P3H.2 — Actor Profile and Selection Policy Authoring Foundation

## Type

Product authoring foundation and technical QA.

## Objective

Introduce the immutable product assets required by the accepted Session Actor-selection model without adding mutable Session selection state or logical Actor materialization.

## Product surface

```text
Assets/Create/Immersive Framework/Actors/Actor Profile
Assets/Create/Immersive Framework/Player/Actor Selection Policy Profile
Assets/Create/Immersive Framework/Player/Templates/Actor Selection Policy Set
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

PlayerActorSelectionPolicyProfile
  Duplicate Policy
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
selection Duplicate Policy is explicit
Player Slot default Actor references a valid ActorProfile
```

Project validation reports missing Actor Profiles or selection policies as optional skips until the runtime selection cut composes them as mandatory product inputs.

## Templates

The policy template command creates two valid editable assets:

```text
Actor Selection — Allow Duplicates
Actor Selection — Unique Across Joined Slots
```

The framework deliberately does not generate a placeholder `ActorProfile`, because a valid Profile requires a real project-owned Logical Actor Host prefab. Official tooling must not create knowingly invalid product assets.

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
P3H.2 — add Actor Profile and selection policy authoring

QAFramework:
P3H.2 — add Actor selection authoring QA
```
