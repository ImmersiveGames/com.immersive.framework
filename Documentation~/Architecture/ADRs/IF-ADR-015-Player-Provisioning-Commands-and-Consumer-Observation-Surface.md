# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: Proposed  
Last updated: 2026-08-07  
Implementation completion: **30%**  
Implementation classification: **runtime foundations and consumer prototype exist; one canonical package product surface is not yet shipped**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014

## Current source baseline

```text
com.immersive.framework
  832cfb718cad7eb986523fc51c4cb96b1c9a2a8e
  IF-TXN-03A Docs

QAFramework
  c99df1e77a8408e6b48124a5d371f09e9af52019
  IF-TXN-03A

FIRSTGAME / planet-devourer
  ab1bfe65c09af8988c2fe21ce06db780fe12aa70
  Demo03Etapa04
```

This ADR consolidates the two IF-ADR-015 documents that existed simultaneously in
`Documentation~/Architecture/ADRs` at the package baseline above.

The ADR decision and implementation status are intentionally separate:

```text
Normative status
  Proposed

Implementation assessment
  30%

Meaning
  the architectural boundary is substantially defined,
  but the official consumer-facing package surface is not complete.
```

## Context

Manager-Provisioned Player flows require recurring product commands such as:

```text
open joining
close joining
change dynamic capacity
request a local Player join
request Actor selection when explicitly required
```

Route- or Activity-owned UI also needs immutable evidence from Session-scoped Player
authorities.

The existing runtime already owns substantial Player behavior:

```text
Session Player participation
Slot reservation and admission
Manager-Provisioned join
Actor selection
Logical Actor preparation
physical Actor materialization
gameplay admission
Activity Player readiness contribution
Activity occurrence / Session revision reconciliation
contextual Activity release
```

The product gap is not another Player authority.

The gap is the absence of one canonical package boundary through which a normal
consumer can:

```text
issue supported Player provisioning commands
+
observe immutable Player provisioning state
+
correlate Slot / Host / Logical Player / Actor / Activity evidence
```

without depending on internal runtime modules or inventing a permanent local
integration framework.

Without an official surface, consumers tend to create their own:

```text
ScriptableObject event channels
command enums
persistent receivers
status bridges
snapshot projections
cross-scene lookup conventions
```

That duplicates framework-facing integration and can produce incompatible command
semantics, stale diagnostics, hidden authority and accidental consumer dependency on
internal implementation details.

## Decision

The Immersive Framework owns the canonical Player provisioning **command** and
**observation** product boundary.

The package exposes typed, public and explicitly scoped commands for the supported
Player provisioning operations, together with immutable read-only observations of
the authorities that execute them.

The command surface is not another runtime authority.

The observation surface is not mutable runtime state.

Consumer UI presents state and requests operations. It does not reserve Slots,
prepare Actors, materialize gameplay Actors, calculate Activity readiness or invoke
Activity reconciliation directly.

## Authority boundary

```text
PlayerParticipation runtime
  owns Slot and Logical Player Session state

Local Player provisioning runtime
  owns Host provisioning and Join execution

Actor selection / preparation runtime
  owns Actor selection and Logical Actor preparation

Activity-owned Player Actor lifecycle
  owns contextual Actor materialization and release

Gameplay admission runtime
  owns contextual gameplay / input / camera admission evidence

Activity readiness runtime
  owns aggregate Activity readiness

Package command surface
  requests supported operations from those authorities

Package observation surface
  projects immutable evidence from those authorities

Consumer UI
  invokes commands and presents observations
```

No command or presentation component introduced by this ADR becomes an alternative
authority for Player participation, Actor lifecycle or Activity readiness.

## Canonical command vocabulary

The package must expose operations equivalent to:

```text
Open Joining
Close Joining
Set Dynamic Capacity
Request Join
Request Actor Selection
```

The exact public type names may follow existing package vocabulary during
implementation.

Only commands supported by the accepted Player lifecycle may be exposed.

The normal consumer surface must not expose commands equivalent to:

```text
Reserve Slot
Mutate Slot
Prepare Actor
Materialize Actor
Ensure Gameplay
Reconcile Activity
Mutate readiness
```

Those remain internal authority operations or consequences of accepted public
commands.

Every command result must be typed and diagnostic enough to distinguish at least:

```text
accepted / completed
no change / already satisfied
rejected by current state
capacity or availability rejection
invalid required configuration
in-flight / conflicting request when applicable
explicit runtime failure
```

Where an operation changes Session state, its result must preserve revision
correlation appropriate to the underlying authority.

## Canonical observation model

The package must expose immutable consumer-safe observations sufficient for both
normal presentation and public-only QA.

### Player Provisioning Snapshot

The aggregate provisioning projection must provide evidence equivalent to:

```text
configured Slot count
dynamic capacity
joining state
current Session revision
per-Slot provisioning / assignment evidence
last relevant provisioning result when appropriate
```

### Per-Slot assignment evidence

The framework currently has useful partial evidence, but no single canonical
projection answers the complete product question:

```text
Which Logical Player and Host occupy this Slot,
which Actor is selected/prepared/materialized,
and under which Session / Activity correlation?
```

The canonical observation surface must therefore provide enough immutable per-Slot
evidence to correlate, when applicable:

```text
PlayerSlotId
Joined state
Logical Player evidence
Host evidence
Player source / provisioning origin
selected Actor
Logical Actor preparation state
physical Actor materialization state
gameplay admission state
Session revision
contextual Activity owner / occurrence evidence
Activity revision or equivalent correlation evidence
```

Physical Unity object references and mutable runtime structures remain inside the
runtime unless a narrow public reference is explicitly justified.

Normal presentation may expose a compact subset. Detailed owner, token, occurrence
and revision evidence belongs in Advanced / Debug.

### Provisioning Operation Result

A command result should expose evidence equivalent to:

```text
operation
status
reason
affected Slot when applicable
requested revision when applicable
committed revision when applicable
```

### Activity Player Readiness Snapshot

The consumer observation boundary must also be able to correlate Player progression
with the active Activity readiness occurrence without becoming readiness authority.

Evidence is equivalent to:

```text
Activity identity
readiness occurrence
readiness state
readiness reason
projected Slots
pending Player requirements
```

The exact public split between provisioning and Activity-readiness snapshots remains
an implementation decision. The requirement is one coherent public observation
model, not one monolithic DTO.

## Cross-scene integration requirement

The canonical surface must support the recurring topology:

```text
Persistent Application Content
  PlayerInputManager
  provisioning authoring
  Session Player authority

Route / Activity content
  Join controls
  Actor-selection controls when applicable
  status presentation
```

A Route- or Activity-owned consumer must not require a serialized scene-object
reference to a persistent runtime object.

The integration mechanism must be:

```text
typed
explicitly scoped
lifetime-explicit
diagnostic
compatible with additive Route / Activity content
```

It must not depend on:

```text
public static runtime registry
service locator
reflection
FindObjectOfType / scene-wide search
hierarchy or object-name inference
generic global event bus
log parsing
consumer access to internal preparation or reconciliation modules
```

This ADR intentionally does **not** mandate a universal ScriptableObject event
channel.

The final implementation may use one or more specialized package surfaces such as:

```text
authorable command trigger
request component
typed scoped endpoint
specialized command channel
typed provider
Composer-materialized binding
```

provided the authority and lifetime requirements above are preserved.

## Product authoring direction

The preferred product experience is:

```text
Manager-Provisioned Player Recipe / Profile
  reusable provisioning intent

Manager-Provisioned Player Composer
  persistent concrete composition

Apply / Rebuild
  idempotent technical materialization when justified

Player Provisioning Command Trigger
  designer-facing supported command selection

Player Provisioning Status Binding / Presenter
  read-only observation binding

Advanced / Debug
  Slot
  Host
  Logical Player
  Actor selection
  preparation / materialization
  gameplay admission
  Session revision
  Activity occurrence
  last operation result
```

The normal Inspector presents product intent first:

```text
Operation
target Slot / Player when applicable
required configuration
current status
last result
validation
```

Internal ports, modules, registries, tokens and reconciliation internals remain
internal or Advanced / Debug.

Apply / Rebuild must be idempotent, non-destructive, Undo-aware and safe for prefab
workflows. It must not execute gameplay in Edit Mode or silently repair invalid
runtime state.

## Validation requirements

Authoring validation must identify actionable invalid configuration without
introducing fallback.

Validation should cover the configuration needed by the selected command or
observation surface, including where applicable:

```text
missing provisioning composition
invalid Player prefab / host configuration
invalid Slot target
unsupported Actor-selection command
incompatible command target
missing observation binding
ambiguous or invalid scope
```

The framework must not silently:

```text
open joining
auto-join
change participation policy
change Activity readiness policy
reserve another Slot
select an Actor outside the accepted policy
weaken a Required Player contribution
```

to make an invalid composition appear functional.

## FIRSTGAME Demo03 boundary

FIRSTGAME Demo03 is the temporary real-consumer prototype for this product surface.

Until the official package surface exists, Demo03 may contain consumer-local
integration equivalent to:

```text
Demo03 command emitter
Demo03-specific command channel
Demo03 persistent receiver
Demo03 read-only status projection
Demo03 presenter
```

The prototype must:

```text
remain Demo03-specific
use consumer namespaces
call only public package APIs
preserve package runtime authority
publish only read-only presentation evidence
record UX findings
```

It must not:

```text
become a shared consumer framework
use Immersive.Framework.* namespaces for game code
be documented as the canonical framework workflow
become a generic reusable global event bus
calculate Activity readiness locally
mutate Slots directly
prepare or materialize Actors directly
invoke internal reconciliation
parse logs as runtime state
```

When the official package surface is complete, Demo03 must migrate to it and its
temporary compatibility bridge must be removed.

## QA boundary

QAFramework must prove the same public surface expected from a normal consumer.

A canonical public-only suite must exercise the accepted command and observation
contracts without using:

```text
reflection
internal preparation APIs
internal reconciliation APIs
manual RuntimeScopeContext construction as the consumer path
external Slot mutation
consumer-side Actor materialization
global object lookup
log parsing as authority
```

At minimum, QA must prove:

```text
Open / Close Joining command semantics
Request Join success
explicit rejection / no-change semantics
capacity behavior relevant to the public contract
Actor-selection command behavior when supported
Session revision correlation
immutable per-Slot observation
Activity occurrence correlation
automatic downstream reconcile after accepted Session changes
clean contextual Activity exit and reentry
no duplicate Actor / Host / Slot assignment caused by repeated observation or command use
```

Negative provisioning hardening remains a package/QA responsibility, not a
FIRSTGAME fault-injection responsibility.

## Current implementation coverage

### Already present

```text
Session-scoped Player participation authority
Manager-Provisioned join flow
Slot reservation and admission
Actor selection / preparation contracts
physical Actor materialization
gameplay admission
Activity Player readiness contribution
cold-start and active-Activity reconciliation
partial public provisioning operations
partial runtime diagnostics and lifecycle snapshots
FIRSTGAME Demo03 consumer prototype
```

### Not yet complete as one canonical product surface

```text
final minimal command contracts
one coherent immutable consumer observation model
canonical Slot / Host / Logical Player / Actor assignment projection
explicit cross-scene command / observation integration
designer-facing command trigger
status binding / presenter
Manager-Provisioned Recipe / Composer workflow
canonical public-only QA suite
FIRSTGAME migration away from its temporary bridge
short canonical usage documentation
```

The implementation assessment remains **30%** because substantial runtime capability
already exists, while the package product boundary, public-only proof and final
consumer workflow remain incomplete.

## Out of scope

This ADR does not define:

```text
Session Player Leave
device disconnect / reconnect
Session-Persistent Player source
generic multiplayer networking
generic application event bus
game-specific UI layout, colors or text
game-specific input prompts
game-specific command grouping
Player movement
Activity readiness calculation
internal Actor preparation or reconciliation APIs as public product commands
```

Those require separate decisions when a real product requirement justifies them.

## Rejected alternatives

- Requiring every game to permanently create its own provisioning event channel.
- Promoting the Demo03 compatibility bridge into a generic shared consumer layer.
- Creating a generic global event bus inside the framework.
- Letting Route / Activity UI find persistent authorities through hierarchy or scene search.
- Exposing internal preparation or reconciliation modules to consumer UI.
- Treating Unity button callbacks as the provisioning contract.
- Inferring Player state from instantiated GameObjects, hierarchy names or logs.
- Creating a second mutable Player state store for presentation.
- Making Loading or Activity UI the source of Player readiness truth.

## Required implementation order

```text
1. Consolidate and canonicalize IF-ADR-015.
2. Use Demo03 findings to freeze the minimal public command vocabulary.
3. Define the coherent immutable provisioning / per-Slot observation model.
4. Implement the scoped cross-scene command and observation boundary in the package.
5. Implement designer-facing command and status authoring surfaces.
6. Validate the public-only contract in QAFramework.
7. Add / finalize the Manager-Provisioned Recipe / Composer workflow.
8. Migrate FIRSTGAME Demo03 to the package surfaces.
9. Remove the temporary Demo03 bridge.
10. Publish the canonical usage guide and close remaining package-owned UX findings.
```

Steps may be split into smaller implementation cuts. Runtime, product authoring and
QA responsibilities must remain separated.

## Acceptance criteria

This ADR may move to **Accepted** when:

```text
one canonical ADR-015 exists
the ownership boundary is approved
the public command vocabulary is explicit
command results are typed and diagnostic
consumer observations are immutable
Slot / Host / Logical Player / Actor evidence is coherently correlated
cross-scene integration has explicit scope and lifetime
no global lookup, service locator or generic event bus is introduced
consumer UI does not call internal prepare / materialize / reconcile authority
designer-facing command and status authoring exist
QA proves the supported flow through public APIs
FIRSTGAME Demo03 uses the official package surface
the temporary Demo03 compatibility bridge is removed
canonical usage documentation matches the shipped product surface
```

Package capability completion additionally requires disposition of provisioning
hardening and product-authoring findings according to IF-ADR-002 and IF-ADR-010.

## Completion interpretation

```text
Core Player runtime
  substantially implemented

Manager-Provisioned consumer product surface
  incomplete

Canonical command boundary
  pending final package implementation

Canonical observation / assignment projection
  pending

Public-only QA
  pending

FIRSTGAME real-consumer prototype
  present, temporary

ADR status
  Proposed

Implementation completion
  30%
```

The next implementation work should improve the public product boundary. It should
not create another Player authority, expose internal reconcile operations or expand
into Session-Persistent Player without a separate approved requirement.
