# ADR-PROD-0012 — Activity-owned Player Participation Requirements

Status: Accepted  
Date: 2026-07-12  
Amended: 2026-07-22 — Requirement Level is Activity-owned serialized configuration, not a reusable Profile asset
Package: `com.immersive.framework`  
Area: Activity Authoring / Player Participation / Admission Gate  
Related: `ADR-PROD-0007`, `ADR-PROD-0008`, `ADR-PROD-0009`, `ADR-PROD-0010`, `ADR-PROD-0011`

## Context

An Activity must state how ready each projected local Player must be before its gameplay can proceed. The progressive requirement is contextual to that Activity and consists of one enum value. It has no independent identity, metadata, composition or runtime state.

The earlier shape wrapped this value in `PlayerParticipationRequirementsProfile`. That created a separate asset, selection step and missing-reference state without adding a reusable product concept. Shared references only demonstrated migration impact; they did not justify independent asset identity.

## Decision

Every `ActivityAsset` owns one serialized `PlayerParticipationRequirementLevel` value:

```text
Activity
  Player Participation
    Slot Projection
    Zero Participants
    Explicit Slots
    Requirement Level
```

The external `PlayerParticipationRequirementsProfile` type is removed. Runtime systems receive or record only the enum value. They do not retain a ScriptableObject reference.

The serialized default is `None`, which is a valid and explicit configuration. Unknown serialized enum values are invalid and diagnostic; there is no silent fallback.

## Progressive levels

```csharp
public enum PlayerParticipationRequirementLevel
{
    None = 0,
    JoinedSlots = 10,
    SelectedActors = 20,
    LogicalActorsPrepared = 30,
    GameplayReady = 40
}
```

Each level includes the preceding levels:

| Level | Required evidence for every projected Slot |
|---|---|
| `None` | No Player readiness requirement. |
| `JoinedSlots` | A valid configured Slot is joined. |
| `SelectedActors` | Joined evidence plus an explicit/default Actor selection. |
| `LogicalActorsPrepared` | Selected Actor evidence plus valid logical Actor preparation. |
| `GameplayReady` | Complete applicable gameplay-readiness evidence. |

## Projection relationship

`Requirement Level` does not choose participants. The Activity-owned projection defined by `ADR-PROD-0013` selects the Slot set.

Rules:

```text
NoSlots requires Requirement Level None.
AllJoinedSlots evaluates every currently joined Slot in canonical configured order.
ExplicitSlots evaluates the exact ordered PlayerSlotProfile references authored in the Activity.
```

Projection/requirement contradictions are invalid authoring and block admission.

## Runtime ownership

The `ActivityAsset` is immutable authoring input. Mutable facts remain in scoped runtime snapshots and contexts. Admission evaluation reads:

```text
Activity projection descriptor
Activity requirement level
Session participation snapshot
Actor preparation snapshot when required
Gameplay admission snapshot when required
```

Runtime never mutates the Activity asset and does not manufacture a default Profile.

## Product flow

```text
Create or select Activity
-> choose Slot Projection
-> configure Zero Participants and Explicit Slots when applicable
-> choose Requirement Level
-> inspect inline validation
```

The complete local Player Profile template creates only identity Profiles such as `PlayerSlotProfile`. It does not create requirement policy assets.

## Validation

Validation must report:

```text
unknown Requirement Level
NoSlots paired with a non-None requirement
invalid Projection configuration
missing or duplicate explicit PlayerSlotProfile references
zero-participant contradictions
```

Validation is non-mutating. `None` is not a missing configuration.

## Consequences

Positive:

```text
one fewer asset type and Create menu
no asset navigation for a single enum
Activity is the visible authority for its complete participation intent
runtime contracts remain small and independent from Unity asset references
```

Trade-off:

```text
changing the same requirement across several Activities requires editing those Activities
```

That trade-off is accepted because the decision belongs to each Activity; shared references do not create independent product identity.

## Migration

For each Activity, copy the former Profile's enum value into `playerParticipationRequirementLevel`, then remove the obsolete Profile asset. Do not retain compatibility fields, fallback lookup or migration runtime code.
