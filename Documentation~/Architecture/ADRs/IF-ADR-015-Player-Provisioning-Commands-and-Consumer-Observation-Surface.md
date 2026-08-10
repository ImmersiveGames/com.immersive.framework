# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: **Accepted**  
Last updated: 2026-08-09  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Route- and Activity-owned consumers need to request supported Player operations
and inspect immutable Session evidence without becoming Player authority.

## Decision

The package exposes typed scoped consumer access, a bounded public command
vocabulary, immutable observation and optional designer command/status surfaces.
Existing Session and Player authorities execute requests and remain the single
mutable truth.

```text
Package Player Surface
  -> supported requests + immutable observation

Consumer UI / game code
  -> requests operations + presents observation

Session / Player runtime
  -> owns mutable Slot, Host, Actor and Joining state
```

Public commands:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

`RequestJoin` follows IF-ADR-016 Joining + first-vacant-Supported-Slot semantics.
The consumer does not reserve a Slot or mutate Capacity.

## Initialization boundary

IF-ADR-016 is the sole authored Session initialization source:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
  Actor Resolution
```

Commands operate on the created Session. They never mutate/reapply the Profile.

## Scoped access

Consumer access is:

```text
typed
Route- or Activity-scoped
lifetime-explicit
stale-scope rejecting
diagnostic when unavailable
free of serialized cross-scene authority references
```

No public static registry, service locator, reflection, scene-wide authority
search or hierarchy/name inference is required.

## Observation

Observation is immutable evidence derived from runtime authorities. It may expose:

```text
Session initialization evidence
Joining state
Supported Slot occupancy
Session/applied revision
Activity owner/occurrence
Host correlation
selected Actor
Logical Actor preparation
physical Actor materialization
gameplay admission
```

Observation is evidence, not a mutable second state store.

## Authoring boundary

`PlayerProvisioningCommandTrigger` executes only explicit user/game operations;
it does not provision from `Awake`, `OnEnable`, `Start` or `OnValidate`.

`PlayerProvisioningStatusBinding` is read-only and may correlate current
observation with the latest explicit trigger result.

Normal Inspector information is designer-facing. Deeper revisions,
owner/occurrence correlation and technical evidence belong in Advanced / Debug.

## Rejected scope

- Consumer Slot reservation/mutation.
- Consumer Actor preparation/materialization authority.
- Consumer gameplay admission or Activity reconcile authority.
- Readiness mutation from game UI.
- Automatic Join, fake readiness or silent fallback.
- Capacity commands or a second Session limit.
- Separate provisioning Profile or per-Slot Host Provisioning override.

## Integration and product improvement

The architectural decision is accepted independently of mutable implementation
status. Technical certification and FIRSTGAME real-integration status are tracked
in the framework Tracker.

FIRSTGAME integration is required to prove the supported consumer flow in a real
product. UX friction observed during that work may justify an optional product
improvement. A Wizard/Composer/Create flow is not an acceptance requirement and
`NO ADDITIONAL TOOLING REQUIRED` remains valid.
