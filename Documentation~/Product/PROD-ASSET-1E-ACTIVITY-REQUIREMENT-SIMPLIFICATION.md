# PROD-ASSET-1E — Activity-owned Participation Requirement

## Type

UX/product, technical migration and documentation.

## Objective

Remove the external `PlayerParticipationRequirementsProfile` wrapper and make `ActivityAsset` the direct authoring authority for `PlayerParticipationRequirementLevel`.

## Scope

```text
package runtime contract and evaluation
Activity Inspector and validation
Player participation templates
QA fixtures, persistent Activities and authoring smokes
ADRs and current implementation notes
```

## Out of scope

```text
PlayerSlotProfile and ActorProfile identity assets
FIRSTGAME assets, which will be recreated
new admission levels or multiplayer topology
compatibility or fallback for the removed Profile type
```

## Product surface

```text
Activity
  Player Participation
    Slot Projection
    Zero Participants
    Explicit Slots
    Requirement Level
```

`None` is the valid serialized default. Unknown enum values are invalid and diagnostic.

## Runtime

Admission and lifecycle systems read the enum from the Activity and propagate only that value. Mutable readiness evidence remains in scoped runtime snapshots and contexts.

## Removed files

```text
Runtime/PlayerParticipation/Authoring/PlayerParticipationRequirementsProfile.cs
Runtime/PlayerParticipation/Authoring/PlayerParticipationRequirementsProfile.cs.meta
```

The custom Inspector and requirement-template creation path were removed from their shared editor files.

## Validation

```text
NoSlots + None is valid
NoSlots + non-None is rejected
unknown Requirement Level is rejected
projection validation remains non-mutating
project scan detects invalid Activity configuration
QA Activities preserve their previous effective levels
```

## Expected smoke

Run:

```text
Immersive Framework
> QA
> Regressions
> Player
> Run Player Participation Authoring Regression
```

Expected focused evidence:

```text
[P3C_PLAYER_PROFILE_AUTHORING_SMOKE] status='Passed'
[P3D_ACTIVITY_PARTICIPATION_AUTHORING_SMOKE] status='Passed' cases='11'
```

## Gains

Architectural:

```text
one authoring authority per Activity decision
runtime no longer carries an unnecessary Unity asset identity
fewer invalid states and no missing-reference branch
```

Usability:

```text
requirement is visible and editable in the Activity Inspector
no separate asset creation, naming, navigation or selection
complete template creates only identity Profiles
```

## Suggested commits

```text
Framework: refactor(player): move participation requirement into activity
QA: test(qa): migrate activity requirements to inline authoring
```
