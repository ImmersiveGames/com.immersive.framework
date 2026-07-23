# ADR-PROD-0009 — Immutable product Profiles and runtime state separation

Status: Accepted  
Date: 2026-07-12  
Package: `com.immersive.framework`  
Area: Product Authoring / ScriptableObject Semantics / Runtime State  
Related: `ADR-PROD-0004`, `ADR-PROD-0006`, `ADR-PROD-0007`, `ADR-PROD-0008`, `ADR-PROD-0011`, `ADR-PROD-0012`

## Context

The framework uses ScriptableObjects for reusable authoring data, but not every ScriptableObject has the same product meaning.

Without explicit terminology, the following responsibilities can become mixed:

```text
stable product identity
reusable configuration
concrete scene/prefab composition
mutable runtime state
physical materialization
```

The product needs stable assets that identify selectable or distinguishable concepts such as:

```text
Actor identity/profile
Player Slot identity/profile
future product-defined identities
```

It also needs reusable configuration assets such as camera settings that may be shared across several identities and instances.

ScriptableObjects are project assets. Mutating them to represent Session state, occupancy, join/readiness or current gameplay state would create unsafe shared state and unclear lifetime.

## Decision

Adopt the following canonical product vocabulary:

```text
Profile
  immutable reusable product definition
  either stable identity/option or reusable policy/requirements

Recipe
  reusable construction or materialization configuration intent

Composer / Authoring Component
  concrete scene or prefab composition surface

Runtime State / Context / Session
  mutable scoped state that references Profiles

Materialization
  explicit creation/configuration of runtime instances
```

A ScriptableObject type must use the term that matches its responsibility.

## Profile

A Profile is an immutable reusable product definition.

There are two canonical Profile categories.

### Identity Profile

Answers:

```text
"Which stable product identity or selectable option is this?"
```

An Identity Profile:

```text
is a ScriptableObject
owns one canonical stable identity
contains static product metadata
is referenced directly by authoring surfaces
may reference canonical construction content
is treated as immutable during runtime
```

Examples:

```text
ActorProfile
PlayerSlotProfile
```

Identity Profile metadata may include:

```text
display name
icon
color
description
sorting/grouping metadata
static product tags
canonical prefab or construction reference when appropriate
```

### Policy Profile

Answers:

```text
"Which reusable immutable product policy applies here?"
```

A Policy Profile:

```text
is a ScriptableObject
contains reusable policy or requirements
is referenced explicitly by product authoring
is treated as immutable during runtime
does not represent mutable Session state
does not materialize technical components by itself
```

Example:

```text
PlayerParticipationRequirementsProfile
```

A Policy Profile is appropriate when several Activities or product surfaces need to
repeat the same explicit rule set.

It is not renamed to Recipe merely because it contains configuration. A Recipe answers
how technical composition or materialization should be constructed; a Policy Profile
answers which immutable rule set governs evaluation.

Both Profile categories must not contain:

```text
join/readiness state
current selection
occupancy
runtime ActorId
current health
current Route/Activity
connected device
mutable Session data
```

## Recipe

A Recipe represents:

```text
"how should this be configured?"
```

A Recipe:

```text
is reusable across several Profiles, Composers or instances
does not identify one product option
does not own runtime identity
contains configuration intent
may be copied/applied by a Composer
```

Examples:

```text
CameraRigRecipe
reusable presentation configuration
reusable capability configuration
```

A Recipe may be referenced by a Profile when several Profiles share configuration, but it must not replace Profile identity.

## Composer / Authoring Component

A Composer represents:

```text
"how is this concrete scene or prefab instance authored?"
```

It:

```text
references Profiles and/or Recipes
owns concrete authoring intent
materializes technical components idempotently
shows designer-first fields
shows technical evidence under Advanced/Debug
does not become mutable global runtime authority
```

## Runtime state

Runtime state is stored in scoped runtime objects, not Profiles or Recipes.

Representative scopes:

```text
Session
Route
Activity
explicit operation
runtime content handle
```

Runtime state may reference immutable Profiles and typed IDs.

Examples:

```text
Session Slot state
  PlayerSlotProfile
  PlayerSlotId
  joined
  ready
  selected ActorProfile

Runtime Actor state
  ActorProfile
  ActorId
  owner scope
  occupancy
  active presentation
```

Changing runtime state must never mutate the source Profile asset.

## Authoring references and IDs

The primary authoring link is the direct ScriptableObject reference.

```text
correct:
  Actor Profile: [Mage]
  Player Slot Profile: [Player 1]

incorrect:
  Actor Profile Id: "actor-profile.mage"
  Player Slot Id: "player.1"
```

Stable IDs remain necessary for Identity Profiles when they participate in:

```text
save
network transport
diagnostics
asset resolution after persistence
migration
```

The ID text is authored once inside the owning Identity Profile.

Consumers derive typed identity from the referenced Profile and must not duplicate the text.

A Policy Profile is primarily referenced directly as an asset. Whether a specific Policy
Profile type also requires a stable diagnostic/persistence ID is decided by that feature;
it must not invent an identity authority without a concrete need.

## Profile identity rules

### ActorProfile

`ActorProfileId` identifies the reusable Actor product identity.

It is distinct from runtime `ActorId` because one Profile can produce several Actor instances.

### PlayerSlotProfile

`PlayerSlotProfile` is the canonical authoring source for one stable `PlayerSlotId`.

A Session creates mutable runtime Slot state that references the Profile.

The Profile is not the joined player, connected device or occupancy.


### PlayerParticipationRequirementsProfile

`PlayerParticipationRequirementsProfile` is a Policy Profile.

It contains immutable Activity admission requirements and has no Player, Slot, Actor or
Session identity authority.

Activities reference it directly. Runtime admission state and evaluation results remain
scoped runtime data.

## Runtime immutability

Profiles and Recipes are read-only runtime inputs.

The framework must not:

```text
write current state into ScriptableObject fields
clone Profiles to represent mutable players or Actors
use asset mutation as Session persistence
store occupancy or selection transitions in Profiles
```

When runtime overrides are required, they belong to:

```text
a scoped runtime state object
an explicit request
a runtime descriptor
a materialization/configuration operation
```

## Product customization

Profiles are the primary product customization surface for stable identities.

They allow a final product to customize:

```text
names
colors
icons
descriptions
ordering
selection presentation
canonical logical host references
other static identity metadata
```

without changing framework runtime contracts.

This enables the framework package to provide generic contracts while each game supplies product-specific Profile assets.

## Creation and validation direction

Recurring Profile types should provide:

```text
Create menu or wizard
clear identity fields
designer-first Inspector
duplicate-ID validation
required-reference validation
Advanced/Debug identity evidence
short usage documentation
```

Validation must detect:

```text
missing Profile reference when required
empty Profile ID
duplicate Profile ID
invalid canonical prefab/reference
runtime attempt to use unresolved persisted ID
```

Failures are explicit and diagnostic. There is no silent fallback to names, paths or default assets.

## Guardrails

```text
Do not call an identity asset a Recipe.

Do not call arbitrary reusable configuration a Profile merely because it is a ScriptableObject.

Use a Policy Profile for immutable reusable product rules or requirements.

Use a Recipe for reusable construction/materialization configuration.

Do not mutate Profiles or Recipes to represent runtime state.

Do not duplicate Profile ID strings in authoring consumers.

Do not use asset name, path or GUID as the only gameplay identity.

Do not store PlayerInput, GameObject instances or current occupancy in Profiles.

Do not introduce a global Profile registry or service locator implicitly.

Do not make every framework concept a Profile; use Profiles only for stable
product identities or selectable definitions.
```

## Out of scope

This ADR does not decide:

```text
the universal base class or interface for all Profiles
whether Profile catalogs are required
Addressables/DLC loading
save migration implementation
network replication
the exact custom Inspector implementation
whether existing Recipe types remain permanently
```

Concrete Profile types may introduce small typed contracts without forcing a universal inheritance hierarchy.

## Technical acceptance criteria

```text
Profile, Recipe, Composer, runtime state and materialization have distinct roles.

ActorProfile and PlayerSlotProfile are immutable Identity Profile references.

PlayerParticipationRequirementsProfile is an immutable Policy Profile reference.

Profile IDs are authored once in the owning Profile.

Runtime state references Profiles and does not mutate them.

Persistence transports stable IDs, not raw mutable asset state.

Duplicate and missing identity failures are explicit.

No singleton, service locator, name lookup or path lookup is introduced.
```

## Product acceptance criteria

```text
A designer customizes Actor and Slot identities through Identity Profile assets.

A designer reuses explicit Activity admission rules through Policy Profile assets.

Authoring consumers use object references instead of repeated string IDs.

Names, colors and icons can vary by game without changing runtime contracts.

Advanced/Debug shows both Profile identity and mutable runtime state.

The terminology communicates whether an asset means:
"which identity/option",
"which immutable policy",
or "how technical construction is configured".
```

## Consequences

### Positive

```text
Product identity becomes explicit and designer-friendly.

Runtime state no longer risks mutating shared project assets.

String-based authoring links are replaced by safe asset references.

Games can customize names, colors, icons and canonical content cleanly.

Policy Profiles allow reusable requirements without mutable inline serialization.

Recipe remains available for genuinely reusable construction/materialization configuration.
```

### Cost

```text
Existing ScriptableObject names and responsibilities require reconciliation.

Profile ID validation and persisted-ID resolution are required.

Some current string identity fields must become derived from Profile references.

Documentation and Inspectors must consistently use the new vocabulary.
```

## Suggested commit message

```text
Docs: define immutable product Profiles and runtime state separation
```
