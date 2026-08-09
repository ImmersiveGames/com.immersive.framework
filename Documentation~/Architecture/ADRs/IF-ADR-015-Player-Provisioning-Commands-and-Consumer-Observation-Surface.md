# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: Proposed  
Last updated: 2026-08-09  
Implementation status: **Implemented and technically QA-certified for the accepted current scope**  
Supersedes: none  
Superseded by: none  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016

## Context

Route- and Activity-owned consumers need to request supported Player operations and inspect immutable Session evidence without becoming Player authority. This ADR defines that consumer boundary. It does not define a second Session configuration source or a second mutable Player state store.

## Decision

The package exposes typed, scoped consumer access; a bounded public command vocabulary; immutable observation; and optional designer command/status surfaces. Existing Session and Player authorities execute the requests and remain the single mutable truth.

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

Default Actor selection remains a distinct public Actor-selection boundary. `Request Join` is accepted only under IF-ADR-016's Joining and vacant Supported Slot rule. The consumer neither chooses/reserves a Slot nor changes a second capacity value.

The following are not part of the canonical command/observation model:

```text
SetCapacity
SetDynamicCapacity
Initial Capacity
Current Capacity
Dynamic Capacity
```

## Initialization boundary

IF-ADR-016 is the sole authored Session initialization source:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
  Actor Resolution
```

The Profile resolves once at Session creation. Commands operate only on the created Session; they never mutate or reapply the Profile. The Profile has no reference to a separate provisioning Profile and the consumer surface does not create a Capacity source or per-Slot Host Provisioning override.

## Scoped access

Consumer access must be:

```text
typed
explicitly Route- or Activity-scoped
lifetime-explicit
stale-scope rejecting
diagnostic when unavailable
free of serialized cross-scene authority references
```

The canonical implementation does not require a public static registry, service locator, reflection, scene-wide authority search or object-name/hierarchy inference.

## Observation

Observation is immutable, non-mutating evidence derived from runtime authorities. It may present, as applicable:

```text
Session initialization evidence
Joining state
Supported Slot occupancy
Session / applied revision
Activity owner / occurrence
Host correlation
selected Actor
Logical Actor preparation
physical Actor materialization
gameplay admission
```

Observation does not create a second mutable state store and does not infer authority from instantiated objects or logs.

## Authoring and diagnostics

`PlayerProvisioningCommandTrigger` invokes only explicit user/game actions; it does not execute provisioning from `Awake`, `OnEnable`, `Start` or `OnValidate`.

`PlayerProvisioningStatusBinding` is read-only. It may correlate current public observation with the last explicit trigger result but does not become a global result store.

Normal Inspector information is designer-facing. Revisions, owner/occurrence correlation and deeper technical evidence belong in Advanced / Debug.

## Rejected scope

- Consumer Slot reservation or Slot mutation.
- Consumer Actor preparation/materialization authority.
- Consumer gameplay admission or Activity reconcile authority.
- Readiness mutation from game UI.
- Service locator, static runtime registry, reflection, scene-wide search or hierarchy/name inference.
- Automatic Join, fake readiness or silent fallback.
- Capacity change to force a request to succeed.
- Reintroduction of a separate provisioning Profile or per-Slot Host Provisioning override.

## Technical certification — 2026-08-09

The current no-Capacity consumer surface was revalidated by the canonical QAFramework Player orchestrator after the IF-ADR-016 migration.

```text
Public Surface Q1
  PASS — 28/28

Public Surface Q2
  PASS — 36/36

Master Player QA
  publicSurface='PASS'
  verdict='PLAYER QA CERTIFIED'
```

Q2 proves negative behavior including rejected operations, missing/wrong/stale/destroyed scoped access and unbound public triggers. Expected framework error diagnostics from deliberate negative cases do not invalidate the certification when the runner returns PASS.

The full Player run also certified the surrounding runtime chain used by the public surface:

```text
Manager public contract          PASS — 9 cases
Manager waiting projection       PASS — 14 cases
Actor selection runtime binding  PASS — 13 cases
Player gameplay admission        PASS — 114 cases
Activity Session projection      PASS — 30 cases
```

See `../IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md`.

## Consequences

The previous ADR text stating that P1–P4 evidence belonged only to the superseded Capacity/Profile model is no longer current. The accepted consumer vocabulary has now been revalidated against IF-ADR-016.

The technical QA gate is closed for the current surface. Remaining work is product evidence and final disposition:

```text
FIRSTGAME manual real-consumer proof
P5 creation-workflow/tooling disposition
final ADR acceptance decision after product evidence
```

A Wizard or Composer is not required by this ADR. P5 may conclude that explicit manual composition is sufficiently usable, or may justify the smallest product authoring improvement based on observed friction.

Technical certification does not automatically promote Experimental/preview API stability metadata.

## Completion criteria for final ADR acceptance

- A real consumer can compose the feature using official package surfaces without internal runtime knowledge.
- FIRSTGAME demonstrates the current no-Capacity command/observation flow.
- Product authoring friction is either accepted or addressed through the smallest justified package tooling.
- No compatibility rail restores Capacity, separate provisioning Profile or internal authority access.
