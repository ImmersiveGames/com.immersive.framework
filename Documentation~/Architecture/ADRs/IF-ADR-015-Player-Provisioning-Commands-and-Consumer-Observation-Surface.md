# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: Proposed  
Last updated: 2026-08-09  
Supersedes: none  
Superseded by: none  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016

## Context

Route- and Activity-owned consumers need to request the supported Player
operations and inspect immutable Session evidence without becoming Player
authority. This ADR defines that consumer boundary. It does not define a
second Session configuration source or a second mutable Player state store.

## Decision

The package exposes typed, scoped consumer access; a bounded public command
vocabulary; immutable observation; and optional designer command/status
surfaces. Existing Session and Player authorities execute the requests and
remain the single mutable truth.

```text
Package Player Surface
  → supported requests + immutable observation

Consumer UI / game code
  → requests operations + presents observation

Session / Player runtime
  → owns mutable Slot, Host, Actor and Joining state
```

The public vocabulary is:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

Default Actor selection remains a distinct public Actor-selection boundary.
`Request Join` is accepted only under IF-ADR-016's Joining and vacant
Supported Slot rule. The consumer neither chooses/reserves a Slot nor changes
capacity.

`SetCapacity`, `SetDynamicCapacity`, `Initial Capacity`, `Current Capacity`,
and `Dynamic Capacity` are not part of the canonical command or observation
model.

## Accepted scope

### Initialization boundary

IF-ADR-016 is the sole authored initialization source:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
  Actor Resolution
```

The Profile resolves once at Session creation. Commands operate only on the
created Session; they never mutate or reapply the Profile. The Profile has no
reference to a separate provisioning Profile, and the consumer surface does not
create a provisioning Profile, a Capacity source, or per-Slot Host Provisioning
override.

### Scoped access and observation

Consumer access must be typed, explicitly scoped to Route or Activity, have an
explicit lifetime, reject stale scope, and expose diagnostic unavailability.
It must not require a cross-scene serialized authority reference.

Observation is immutable, non-mutating evidence derived from the authorities.
It may present Joining state, Supported Slot occupancy, Host correlation, Actor
state and Session/Activity correlation as applicable. It must not create a
second mutable state store or infer authority from instantiated objects or logs.

### Authoring and diagnostics

An optional command trigger invokes only explicit user/game actions; it does
not execute provisioning from `Awake`, `OnEnable`, `Start`, or `OnValidate`.
An optional status binding is read-only and may correlate the latest explicit
operation result without becoming a global result store. Product information is
designer-first; technical correlation belongs in Advanced / Debug.

## Rejected scope

- Consumer Slot reservation, Slot mutation, Actor preparation/materialization,
  gameplay admission, readiness mutation or Activity reconciliation.
- Service locator, static registry, reflection, scene-wide search, hierarchy or
  name inference, generic event bus, or log parsing as state.
- Automatic Join, fake readiness, silent fallback, or capacity change to make a
  request succeed.
- Reintroduction of a separate provisioning Profile, Capacity, or per-Slot Host
  Provisioning override through the consumer API.

## Consequences

The consumer migration must remove capacity commands and present structural
availability through Joining state plus vacant Supported Slots. It must preserve
typed diagnostics for joining closed, no vacant Slot, invalid request and
unavailable/stale scope conditions. Existing public API and QA evidence that
depend on capacity are legacy evidence, not compatibility requirements.

## Current implementation coverage

The previously documented P1–P4 implementation and QA certification include
the superseded Capacity/Profile model. They must be revalidated after the
ADR-016 migration. This document does not claim that the accepted consumer
vocabulary is implemented.

## Pending decisions

The final creation-workflow/tooling disposition remains post-real-consumer
evidence. A Wizard or Composer is not required without demonstrated friction.
