# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: Proposed  
Last updated: 2026-08-06  
Supersedes: implicit expectation that every consumer authors its own recurring Player provisioning bridge  
Superseded by: none  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014

> **Implementation status:** not shipped.
>
> The package has Player participation and provisioning runtime contracts, but it
> does not yet provide one complete canonical product surface for issuing common
> provisioning commands and observing their results from consumer UI.
>
> FIRSTGAME Demo03 may implement a local prototype to expose UX and integration
> requirements. That prototype is consumer evidence, not the permanent framework
> solution.

## Context

Manager-Provisioned Player flows require recurring product commands such as:

```text
open joining
close joining
change dynamic capacity
request a local Player join
request Actor selection when explicitly required
```

A consumer also needs read-only evidence for:

```text
configured Slots
dynamic capacity
joining state
per-Slot participation state
selected Actor
Logical Actor preparation
physical Actor materialization
gameplay admission
active Activity readiness
last command result
revision and occurrence correlation
```

The runtime authorities for these operations are Session-scoped and commonly live
in persistent application content.

The UI that invokes and presents them may be owned by a Route or Activity scene.

That creates a recurring integration boundary:

```text
Route- or Activity-owned UI
  -> Session-owned Player provisioning authority
  -> typed result and immutable runtime evidence
  -> UI presentation
```

Without an official product surface, each consumer may create its own:

```text
ScriptableObject event channel
command enum
persistent receiver
status bridge
snapshot projection
cross-scene lookup convention
```

This produces several risks:

```text
duplicated framework-facing integration
different command semantics in every game
consumer access to internal runtime modules
reflection or hierarchy lookup
implicit global event buses
diagnostics that do not correlate with runtime revisions
consumer-owned compatibility facades becoming permanent
```

The repeated need for a bridge is evidence of a missing package product surface.

## Decision

The Immersive Framework owns the canonical Player provisioning command and
observation contracts.

The package must expose typed, public and scoped operations equivalent to:

```text
Open Joining
Close Joining
Set Dynamic Capacity
Request Join
Request Default Actor Selection
```

Only operations supported by the accepted Player lifecycle are exposed. The product
surface must not expose internal preparation, reconciliation or mutable runtime
state.

The package must expose immutable read-only evidence equivalent to:

```text
Player Provisioning Snapshot
  configured Slot count
  dynamic capacity
  joining state
  current Session revision
  per-Slot participation state
  selected Actor
  Logical Actor preparation state
  physical Actor materialization state
  gameplay admission state

Player Provisioning Operation Result
  operation
  status
  reason
  affected Slot
  requested revision
  committed revision

Activity Player Readiness Snapshot
  Activity identity
  occurrence
  readiness state
  readiness reason
  projected Slots
  pending requirements
```

The exact public types and names must follow the existing package vocabulary.

## Authority boundary

```text
PlayerParticipationRuntimeContext
  owns Slot and Logical Player state

Local Player provisioning runtime
  owns Host provisioning and join transaction

Actor preparation and gameplay modules
  own Actor and gameplay preparation

Activity readiness runtime
  owns aggregate Activity readiness

Package command surface
  requests operations from those authorities

Package observation surface
  publishes immutable evidence from those authorities

Consumer UI
  presents state and invokes commands
```

The command surface is not another runtime authority.

The observation surface is not mutable state.

The UI does not calculate readiness, reserve Slots, prepare Actors or reconcile an
Activity.

## Transport mechanism

This ADR does not require a universal `ScriptableObject Event Channel`.

The accepted transport may be implemented through one or more package product
surfaces such as:

```text
authorable command trigger
request component
scoped runtime endpoint
typed provider
specialized command channel
composer-materialized binding
```

The final mechanism must satisfy:

```text
typed commands
explicit scope
explicit lifetime
no implicit global lookup
no service locator
no reflection
no scene-wide search
no object-name inference
no consumer access to internal runtime modules
```

A generic global event bus is not accepted.

## Cross-scene integration

The canonical surface must support this topology:

```text
Persistent application content
  PlayerInputManager
  provisioning authoring
  Session runtime authority

Route or Activity content
  Join controls
  status presentation
```

The consumer must not require a serialized scene-object reference from an additive
Route scene to an object in persistent content.

The package must provide an explicit typed integration path between those scopes.

## Authoring direction

The preferred product experience is:

```text
Manager-Provisioned Player Recipe or Profile
  reusable provisioning intent

Manager-Provisioned Player Composer
  persistent concrete composition

Player Provisioning Command Trigger
  designer-facing command selection

Player Provisioning Status Presenter or Binding
  read-only presentation binding

Advanced / Debug
  Slot, Host, Actor, Activity occurrence and revision evidence
```

The exact component split remains an implementation decision.

The normal Inspector should present product intent:

```text
Operation
Player or Slot target
required configuration
last result
validation
```

Internal ports, modules and registries belong in Advanced / Debug or remain
internal.

## FIRSTGAME Demo03 temporary prototype

Until the package surface exists, Demo03 may implement a local bridge:

```text
Demo03 command emitter
Demo03 ScriptableObject command channel
Demo03 persistent command receiver
Demo03 runtime snapshot
Demo03 status presenter
```

This prototype must:

```text
remain under Assets/_Project/Demo03
use Demo03-specific names and namespace
call only public package APIs
preserve package runtime authority
publish only read-only presentation snapshots
avoid reflection and global lookup
avoid internal preparation and reconcile calls
record UX findings
```

It must not:

```text
move into a shared consumer framework folder
use Immersive.Framework.* namespaces
be documented as the canonical framework workflow
create a generic reusable global event bus
calculate Activity readiness locally
reserve or mutate Slots directly
prepare or materialize Actors directly
invoke internal reconciliation
parse framework logs as runtime state
```

When the official package surface is implemented, Demo03 should migrate to it and
remove the local bridge.

## Promotion criteria

A Demo03 finding should migrate to the package when it represents:

```text
a recurring provisioning command
a recurring cross-scene integration problem
a missing immutable public snapshot
a missing designer-facing command component
a missing validation rule
a repeated authoring sequence
```

The following remain consumer-owned:

```text
panel layout
text labels
colors and visual hierarchy
game-specific presentation
game-specific input prompts
game-specific command grouping
```

## Rejected alternatives

- Requiring every game to create its own provisioning event channel permanently.
- Creating a generic global event bus inside the framework.
- Letting Route UI find persistent authorities through scene or hierarchy search.
- Exposing internal preparation or reconciliation modules to consumer UI.
- Treating Unity button callbacks as the provisioning contract.
- Making FIRSTGAME's local channel the official framework implementation.
- Moving game-specific panel layout and text into the package.
- Inferring runtime state from instantiated GameObjects or logs.

## Consequences

Positive:

```text
Player provisioning becomes a product surface rather than an internal API exercise
consumer UI uses stable typed commands
cross-scene integration has one supported path
status presentation consumes immutable evidence
FIRSTGAME can prove UX without owning framework authority
QA can validate the same public surface used by consumers
```

Tradeoffs:

```text
the package gains additional public contracts
command lifetime and scope require explicit design
snapshot revision and occurrence correlation must be preserved
authoring components need validators and designer-first Inspectors
Demo03 temporarily duplicates integration that will later be removed
```

## Current implementation coverage

Already present:

```text
Session-scoped Player participation authority
Manager-Provisioned join flow
Slot reservation and admission
Actor selection and preparation contracts
Activity Player readiness contribution
public provisioning operations in partial form
runtime diagnostics in partial form
```

Not yet accepted as one canonical product surface:

```text
cross-scene command integration
complete command trigger vocabulary
immutable consumer-facing aggregate snapshot
status presentation binding
designer-first provisioning control surface
canonical creation and composition workflow
```

## Required implementation order

```text
1. Prototype the consumer experience in FIRSTGAME Demo03.
2. Record repeated commands, observations and authoring friction.
3. Define the minimal official package contracts.
4. Implement the package product surface.
5. Validate the public-only flow in QAFramework.
6. Migrate Demo03 to the package surface.
7. Remove the temporary Demo03 bridge.
8. Document the canonical workflow.
```

## Acceptance criteria

This ADR may move to Accepted when:

```text
the ownership boundary is approved
the package command vocabulary is explicit
the observation snapshots are immutable
the cross-scene integration has explicit scope and lifetime
no global lookup or service locator is introduced
FIRSTGAME can operate through public package surfaces only
QA proves positive and negative command paths
Demo03 no longer requires its local compatibility bridge
```

## Pending decisions

- Exact name of the command product surface.
- Trigger components versus one configurable command component.
- Whether a specialized ScriptableObject channel belongs in the package.
- How a Route-owned presenter subscribes to Session-scoped snapshots.
- Whether status presentation is push-based, snapshot-request-based or both.
- Whether dynamic-capacity commands belong in the normal Inspector or Advanced.
- Whether Actor selection requires a separate command surface.
- Whether the provisioning Composer materializes command bindings.