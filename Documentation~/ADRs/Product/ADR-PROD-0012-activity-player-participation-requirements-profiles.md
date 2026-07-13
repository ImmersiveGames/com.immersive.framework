# ADR-PROD-0012 — Activity Player Participation Requirements Profiles

Status: Accepted  
Date: 2026-07-12  
Package: `com.immersive.framework`  
Area: Activity Authoring / Player Participation / Admission Gate  
Related: `ADR-PROD-0007`, `ADR-PROD-0008`, `ADR-PROD-0009`, `ADR-PROD-0010`, `ADR-PROD-0011`

## Context

Different products resolve Player participation in different ways.

Representative flows include:

```text
join with an explicit default Actor
join followed by simultaneous character selection
lobby participation without gameplay Actors
drop-in join waiting for a spawn window
selection required before gameplay
menu Activities with no Players
```

The framework must not force one universal moment for Actor selection.

It must, however, prevent an Activity from becoming active when the Player state required
by that Activity is missing.

Inline serialized defaults are unsafe for this responsibility because an Activity can
appear configured even when the designer never made an explicit choice.

Reusable asset Profiles provide:

```text
explicit authoring
repeatable configuration
clear missing-reference validation
shared policy across Activities
future field evolution without changing every Activity reference
```

## Decision

Every Activity references one explicit `PlayerParticipationRequirementsProfile`.

The reference is mandatory.

```text
Activity
  Player Participation Requirements: [Profile]
```

A missing reference is invalid authoring and blocks Activity admission.

Activities that require no Player participation reference an explicit `None` Profile.

```text
correct:
  Main Menu Activity
    Requirements: Player Participation — None

incorrect:
  Main Menu Activity
    Requirements: null
    runtime silently assumes None
```

`PlayerParticipationRequirementsProfile` is an immutable Policy Profile as defined by
`ADR-PROD-0009`.

It contains reusable admission requirements. It does not contain runtime Player state.

## Initial Profile shape

The initial Profile uses one progressive requirement level rather than independent
booleans.

```csharp
public enum PlayerParticipationRequirementLevel
{
    None,
    JoinedSlots,
    SelectedActors,
    LogicalActorsPrepared,
    GameplayReady
}
```

The names may be refined during implementation, but the progressive model is frozen.

Each level includes the requirements of the previous level.

### None

```text
the Activity does not require Player participation
```

### JoinedSlots

For every Slot included by the Activity's participation projection:

```text
a valid PlayerSlotProfile is bound
the Slot is Joined
required local PlayerInput/session evidence is valid
```

This level supports:

```text
lobby
character selection
multi-pointer UI
ready screens
```

An `ActorProfile` is not required.

### SelectedActors

Includes `JoinedSlots` and requires:

```text
a valid selected ActorProfile for every required participating Slot
```

The Actor does not necessarily need to be materially prepared yet.

### LogicalActorsPrepared

Includes `SelectedActors` and requires:

```text
the logical Player Actor is valid
Actor-specific composition required by the product is prepared
contextual ownership/admission evidence exists
```

Presentation and gameplay activation requirements are evaluated according to the
current framework implementation and Profile evolution.

### GameplayReady

Includes `LogicalActorsPrepared` and requires the complete gameplay-readiness evidence
defined by the Player runtime integration.

Representative evidence may include:

```text
required Presentation/Skin ready
gameplay input gate ready
required occupancy confirmed
required gameplay capabilities ready
required camera/output participation ready when applicable
```

A per-Player camera is not universally implied; only requirements applicable to the
Activity/product are evaluated.

The asset may evolve with additional typed fields when real product requirements appear.
Activities continue referencing the same Profile asset.

## Activity ownership and Route evaluation

The Activity owns the participation requirement declaration.

The Route/Activity transition pipeline evaluates that Profile before the Activity becomes
active.

Canonical flow:

```text
Route/Activity transition requested
-> resolve target Activity
-> resolve mandatory PlayerParticipationRequirementsProfile
-> evaluate contextual participating Slots
-> requirements satisfied?
```

Typed results:

```text
Satisfied
PendingResolution
Blocked
Failed
```

Meaning:

```text
Satisfied
  target Activity may continue activation

PendingResolution
  product-owned selection/preparation operation is still in progress

Blocked
  required product state is absent or policy refuses admission

Failed
  invalid authoring, invalid runtime evidence or technical operation failure
```

The exact operation/result type is an implementation decision, but these distinct
semantics must not collapse into one ambiguous boolean.

## Product authority

The framework evaluates requirements. The product decides how they are fulfilled.

Examples:

```text
simple game
  explicit default ActorProfile is assigned after join

character-selection game
  Selection Activity allows JoinedSlots
  product writes ActorProfile selections
  Gameplay Activity requires SelectedActors or higher

drop-in game
  joined Player waits until product spawn policy prepares the Actor

custom consumer
  adapter resolves selection from save/account/game rules
```

The framework may provide official basic tools such as:

```text
explicit configured default Actor resolver
selection state API
admission diagnostics
templates for common requirement Profiles
```

It must not silently choose:

```text
first ActorProfile found
asset named Default
Actor based on Slot color
Actor based on prefab hierarchy
```

## Official reusable Profiles/templates

The package should provide creation templates or samples for common policies:

```text
Player Participation — None
Player Participation — Joined Slots
Player Participation — Selected Actors
Player Participation — Logical Actors Prepared
Player Participation — Gameplay Ready
```

These are explicit assets or template-generated assets, not hidden runtime defaults.

A product may reuse one Profile across several Activities or create specialized Profiles
that use the same requirement level and later additional policy fields.

## Validation

Authoring validation must detect:

```text
missing Requirements Profile
invalid requirement level
Profile incompatible with the current Activity shape
required Player participation projection missing
contradictory future extension fields
```

Runtime diagnostics must identify:

```text
Route
Activity
Requirements Profile
requirement level
PlayerSlotProfile / PlayerSlotId
selected ActorProfile when present
missing evidence
resolution status
blocking/failure reason
```

Example:

```text
Activity='Gameplay'
Requirements='Gameplay Players'
Level='GameplayReady'
Slot='player.2'
Missing='SelectedActorProfile'
Result='Blocked'
```

## Guardrails

```text
Do not require ActorProfile universally during local join.

Do not activate an Activity before its participation Profile is satisfied.

Do not use null to mean no Player requirements.

Do not create inline serialized requirement defaults on each Activity.

Do not use independent booleans for the initial progressive requirement chain.

Do not let the framework choose a missing Actor silently.

Do not mutate the Requirements Profile with runtime readiness.

Do not make Route the author of requirements that belong to the Activity.

Do not create a universal workflow engine for product selection screens.
```

## Out of scope

This ADR does not decide:

```text
the exact Activity asset/component field that stores the reference
the exact evaluator API
the exact transition cancellation/resume mechanism
the exact selection command API
Actor-selection uniqueness policies
spawn-point selection
leave/reconnect behavior
online/network admission
additional future Profile fields
```

These may evolve while preserving the mandatory Profile reference and progressive gate.

## Technical acceptance criteria

```text
Every Activity has an explicit PlayerParticipationRequirementsProfile reference.

Null is invalid and never means None.

The initial Profile uses one progressive requirement level.

Activity activation evaluates the Profile before becoming active.

Joined Slot and selected Actor remain distinct facts.

Unsatisfied requirements produce typed blocked/pending/failed evidence.

No silent Actor default or fallback is introduced.

Runtime evaluation does not mutate the Profile asset.
```

## Product acceptance criteria

```text
A product can build:
join-with-default,
join-then-select,
simultaneous selection,
lobby-only participation,
and delayed drop-in preparation.

A designer can reuse the same requirements across several Activities.

Forgetting to configure requirements is caught explicitly.

A no-Player Activity uses a visible None Profile.

The Profile asset can evolve without replacing Activity references.

Advanced/Debug explains exactly which Slot and requirement block admission.
```

## Consequences

### Positive

```text
Product workflows remain flexible.

Gameplay Activities cannot start with unresolved required Player state.

Requirements are reusable and explicit.

Missing authoring no longer degrades to permissive serialized defaults.

The progressive level prevents contradictory initial boolean combinations.
```

### Cost

```text
Every Activity needs one additional required asset reference.

The transition pipeline needs a typed admission gate.

Products must explicitly provide selection/default resolution when required.

Profile templates, validation and diagnostics must be implemented.
```

## Suggested commit message

```text
Docs: define Activity Player participation requirement Profiles
```
