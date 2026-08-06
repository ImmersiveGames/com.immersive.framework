# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: Proposed  
Last updated: 2026-08-06  
Supersedes: inline Activity Participation authoring section defined by IF-ADR-003  
Superseded by: none  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-007, IF-ADR-011  

> **Implementation status:** not shipped.  
> `ActivityPlayerParticipationProfileAsset` does **not** exist in the package.  
> Current product surface keeps participation fields **inline on `ActivityAsset`**.  
> Requirement levels, readiness evidence and admission evaluation already exist in
> runtime; this ADR decides the authoring extraction and compatibility validation.

## Context

An Activity may require certain Players and Actors to be prepared before it is
considered ready.

Progressive requirement examples:

```text
Player joined the Session
Actor selected
Logical Actor prepared
Player ready for gameplay
```

The runtime already has one aggregate Activity readiness result:

```text
Activity Ready
  = technical baseline complete
  + every Required contribution complete
  + no blocking failure
```

Player participation contributes to that result. It is not a second readiness
authority.

Today `ActivityAsset` still holds Player-specific fields inline:

```text
playerParticipationProjectionMode
playerParticipationZeroParticipantPolicy
playerParticipationExplicitSlotProfiles
playerParticipationRequirementLevel
```

That mixes two different decisions:

```text
How should the Activity behave while it is not yet Ready?

Which Players participate, and when are they considered ready?
```

The mix also allows circular configurations that never complete. Example:

```text
Activity Entry Policy = WaitCovered
Player Ready When = Logical Actors Prepared
Player may only join after the Activity is revealed
```

There is no valid path:

```text
Loading waits for Activity
Activity waits for Player
Player waits for Loading to close
```

The framework must separate Player-specific authoring from general Activity
authoring and validate combinations before an indefinite wait starts.

## Decision

`ActivityAsset` will reference a dedicated profile asset:

```text
ActivityPlayerParticipationProfileAsset
```

That profile holds only the Activity's Player participation intent.

This cut will **not** create:

```text
ActivityReadinessProfileAsset
generic readiness-layer collection
open readiness-plugin system
generic profile shared across domains
second Activity Ready authority
```

The Activity keeps one aggregate readiness. Player participation remains one
contribution to that result.

## Activity authority

`ActivityAsset` remains the authority for general Activity intent and keeps:

```text
Activity identity
Activity Content Profile
Activity Entry Readiness Policy
Visual Transition Mode
Transition Gate Mode
Player Participation Profile
```

Entry policy remains:

```text
ActivityEntryReadinessPolicy
  ObserveOnly
  WaitVisible
  WaitCovered
```

That policy answers: how should the Activity be presented while aggregate
readiness has not reached Ready? It controls Loading retention, visual reveal,
capability-gate retention and Preparing behavior. It does not define what
“Player ready” means.

## Activity Player Participation Profile

### Projection

```text
Projection Mode
  NoSlots
  AllJoinedSlots
  ExplicitSlots

Explicit Player Slot Profiles
Zero Participant Policy
```

### Player readiness requirement

```text
Player Ready When
  None
  JoinedSlots
  SelectedActors
  LogicalActorsPrepared
  GameplayReady
```

Internal contracts may continue to use `PlayerParticipationRequirementLevel`.
Designer-first surfaces present **Player Ready When**.

### Player entry availability

```text
PlayerEntryAvailability
  BeforeActivityEntry
  WhileCovered
  AfterReveal
```

Availability is for validation and diagnostics. It does not perform join and
does not replace participation runtime.

## Single readiness architecture

```text
Player Participation Runtime
  publishes the Player contribution

Activity Readiness Runtime
  aggregates that contribution with the technical baseline

Game Flow
  applies the entry policy

Loading
  presents aggregate progress only
```

There are not two competing results (`Activity Ready` vs `Player Activity Ready`).
There is only `Activity Ready`.

## Circular-dependency validation

The package must jointly validate:

```text
Activity Entry Readiness Policy
Player Ready When
Player Entry Availability
Projection Mode
Zero Participant Policy
slot guarantees at entry
typed covered Join provider
```

### Initial matrix

| Activity Policy | Player Entry Availability | Result |
|---|---|---|
| `WaitCovered` | `BeforeActivityEntry` | Valid |
| `WaitCovered` | `WhileCovered` | Valid with typed provider |
| `WaitCovered` | `AfterReveal` | Blocking error |
| `WaitVisible` | `AfterReveal` | Valid |
| `WaitVisible` | `WhileCovered` | Valid |
| `ObserveOnly` | `AfterReveal` | Valid |
| any policy | `Player Ready When = None` | No Player block |

Example validation message:

> This Activity remains covered until Player Participation is ready, but the
> required Player can only enter after the Activity is revealed.

No automatic configuration repair is applied.

## Typed covered Join provider

`PlayerEntryAvailability = WhileCovered` must reference a typed package-recognized
integration (conceptual: `CoveredPlayerEntryProvider`). The framework must not
discover join capability through Button, PlayerInput, GameObject, UnityEvent,
hierarchy name or scene script search.

## Runtime preflight

Editor validation is not enough because Session state changes at runtime. Before
starting a `WaitCovered` entry, runtime must verify that required Players can
progress while covered. When no valid path exists, entry fails before an
indefinite wait. Runtime must not silently downgrade `WaitCovered`, invent Ready
state, release Loading/gameplay or create alternate Slots.

## Diagnostics

Activity diagnostics: occurrence, entry policy, aggregate readiness, required
pending/failed counts, Loading/reveal/capability-gate state.

Player participation diagnostics: projection, projected slots, Player Ready When,
entry availability, joined/selected/prepared/gameplay counts, current reason,
can progress while covered, last reconcile result.

These must be visible in Inspector or Advanced / Debug, not only in logs.

## Authoring

Activity Inspector:

```text
Activity Content
  Activity Content Profile

Activity Entry
  Entry Readiness Policy
  Visual Transition
  Transition Gate

Player Participation
  Activity Player Participation Profile
```

When no participation is required, the canonical “none” path is decided during
implementation (profile = None **or** Player Ready When = None — not both forever).

## Inline field migration

Current inline fields become legacy authoring. The package must provide an
explicit, idempotent, Undo-safe migration that creates a profile, copies values
and assigns the reference. Simultaneous active inline fields + assigned profile
is a blocking ambiguity error. No silent precedence.

## Relation to existing ADRs

- **IF-ADR-003** remains normative for Logical Player, sources, Session
  participation authority, Slot, Actor lifecycle and ownership. This ADR only
  replaces the rule that participation configuration must stay inline on
  `ActivityAsset`.
- **IF-ADR-007** remains normative for Activity readiness authority, occurrence
  scope, ObserveOnly / WaitVisible / WaitCovered, reveal and gate.
- **IF-ADR-011** remains normative for participant-aware Loading progress.
  The profile does not write Loading directly.

## Accepted scope

- `ActivityPlayerParticipationProfileAsset` and direct `ActivityAsset` reference
- Migration of Player-specific fields
- Separation of entry policy vs Player Ready When
- `PlayerEntryAvailability` and typed covered-entry provider
- Circular-dependency validation and runtime preflight
- Player participation diagnostics and designer-first Inspector
- Technical QA and FIRSTGAME M07 proof

## Rejected scope

- `ActivityReadinessProfileAsset` or generic readiness layers
- Second Activity Ready result or global Player Ready independent of Activity
- Second Loading dedicated to Player
- Silent timeout, auto-repair, hierarchy discovery of Join
- Permanent dual inline + profile authoring
- Premature generalization to NPCs, save, network or navigation

## Current implementation coverage

Already present:

```text
ActivityEntryReadinessPolicy
occurrence-scoped aggregate Activity Readiness
Required / Optional participants
Player readiness contribution
PlayerParticipationRequirementLevel
WaitCovered Loading integration
Player reconcile
Logical Actor preparation
inline participation fields on ActivityAsset
```

Not implemented as this decision:

```text
ActivityPlayerParticipationProfileAsset
PlayerEntryAvailability
covered-entry provider contract
circular dependency validator
runtime covered-entry preflight
inline-to-profile migration
designer-first profile Inspector
FIRSTGAME M07 using the new profile
```

This ADR accepts the architecture. It does **not** declare implementation done.

## Implementation order

```text
1. Keep this ADR in the official package (Proposed until accepted).
2. Update IF-ADR-003 when accepted.
3. Create ActivityPlayerParticipationProfileAsset.
4. Add ActivityAsset reference.
5. Adapt validators.
6. Implement PlayerEntryAvailability + typed covered provider.
7. Runtime preflight.
8. Inline migration.
9. Technical QA + FIRSTGAME M07.
10. Update Guides and tracker.
```

## Decision summary

```text
Activity has one aggregate readiness.

ActivityAsset defines how to wait and reveal.

ActivityPlayerParticipationProfileAsset will define:
  which Players participate
  when they are ready
  when they may enter

Player runtime publishes a contribution to Activity readiness.
Loading follows only the aggregate Activity result.
Validators and preflight block impossible configurations.
```
