# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: Proposed  
Last updated: 2026-08-09  
Implementation completion: **80%**  
Implementation classification: **canonical package consumer surface P1–P4 shipped and QA-certified; FIRSTGAME proof, post-FIRSTGAME P5 disposition and final product closure remain**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016

## Current source / certification baseline

```text
com.immersive.framework
  cf0a37fbcbf72ad2a08556d6045c908521bfd2c1
  P4 — IF-PLAYER-SURFACE-06 — Status / Diagnostics Binding

QAFramework
  Git baseline inspected: 52a31aa9cd237d934ed3241392b87b7990f11dc8
  Unity Play Mode certification executed 2026-08-09

Player Surface QA
  QA-PLAYER-SURFACE-01  PASS — 29/29
  QA-PLAYER-SURFACE-02  PASS — 36/36
  joint verdict: PLAYER SURFACE QA CERTIFIED
```

The normative ADR status and implementation status remain intentionally separate:

```text
Normative status
  Proposed

Implementation assessment
  80%

Meaning
  the official package consumer boundary is shipped and technically certified;
  real-consumer product proof and final creation-workflow/documentation disposition remain.
```

## Context

Manager-Provisioned Player flows require recurring product commands such as:

```text
open joining
close joining
change dynamic capacity
request a local Player join
request default Actor selection when explicitly required
```

Route- or Activity-owned consumers also require immutable evidence from Session-scoped Player authorities without becoming those authorities.

The framework already owns the underlying runtime behavior:

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

The purpose of this ADR is therefore **consumer reachability and observation**, not a second Player authority.

## Decision

The Immersive Framework owns the canonical Player provisioning **command** and **observation** product boundary.

The package exposes:

```text
typed scoped consumer access
supported public Player provisioning commands
immutable current observation
explicit designer command authoring
read-only status / diagnostics presentation
```

The command surface requests operations from existing authorities. The observation surface projects immutable evidence from them. Neither surface owns duplicate mutable Player truth.

Consumer UI may request supported operations and present observations. It must not directly reserve Slots, prepare Actors, materialize gameplay Actors, calculate Activity readiness or invoke internal reconciliation.

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

Package Player Surface
  exposes supported requests + immutable observations

Consumer UI / game code
  invokes requests and presents observations
```

## Canonical initialization boundary — IF-ADR-016

IF-ADR-015 does not define a second Session configuration source.

Session initialization intent is authored through IF-ADR-016:

```text
PlayerSlotProfile(s)
  stable supported Slot definitions

PlayerProvisioningProfile
  default / per-Slot Host provisioning intent
  Actor resolution policy

PlayerSessionProfile
  ordered Supported Slots
  Initial Capacity
  Initial Joining Open
  PlayerProvisioningProfile

GameApplicationAsset
  Player Session Enabled
  Default Player Session Profile
```

An explicit creation-time `PlayerSessionProfile` override replaces the application default completely. It is not field-merged, and an invalid explicit override does not silently fall back.

Runtime commands in this ADR operate on the created Session. They do not mutate authored Profiles as an alternative runtime configuration authority.

## Canonical command vocabulary

The supported consumer vocabulary is:

```text
Open Joining
Close Joining
Set Dynamic Capacity
Request Join
Request Default Actor Selection
```

Default Actor selection remains a separate public Actor-selection boundary; it is not collapsed into the provisioning authority.

The normal consumer surface does **not** expose commands equivalent to:

```text
Reserve Slot
Mutate Slot
Prepare Actor
Materialize Actor
Ensure Gameplay
Reconcile Activity
Mutate readiness
```

Those remain internal authority operations or normal downstream consequences of accepted public requests.

Public results remain typed and diagnostic. The certified surface distinguishes successful operations, no-change, invalid state/request, joining closed, capacity reached, stale revisions and unavailable runtime/scope conditions as applicable to the underlying operation.

## Canonical scoped access — P1

P1 (`IF-PLAYER-SURFACE-03`) is shipped.

Primary public types include:

```text
LocalPlayerProvisioningConsumerAccessBinding
ILocalPlayerProvisioningConsumerAccess
LocalPlayerProvisioningConsumerScope
LocalPlayerProvisioningConsumerAccessSnapshot
```

The binding is authored in an explicit framework-owned Route or Activity scope. Runtime host integration injects the live endpoint for the matching current scope.

Required properties:

```text
typed
explicit scope
explicit lifetime
stale-scope rejection
diagnostic unavailable state
no cross-scene serialized authority reference
```

The implementation does not use a public static registry, service locator, reflection or name/hierarchy lookup.

## Canonical observation model — P2

P2 (`IF-PLAYER-SURFACE-04`) is shipped as a read-only projection through the scoped consumer surface.

The public observation composes existing authoritative evidence rather than creating a second state store. It includes, as applicable:

```text
Participation snapshot
immutable initialization configuration evidence
Manager-Provisioned lifecycle snapshot
Activity owner / occurrence
Session revision / applied Session revision
per-Slot observation
```

Per-Slot evidence can correlate:

```text
PlayerSlotId
Joined state
selected Actor
Host evidence
logical Actor preparation
physical Actor materialization
gameplay admission
current Activity correlation
```

Detailed owner/token/revision correlation belongs in Advanced / Debug. Mutable runtime structures remain internal.

Repeated observation is non-mutating.

## Designer command authoring — P3

P3 (`IF-PLAYER-SURFACE-05`) is shipped through:

```text
PlayerProvisioningCommandTrigger
```

The component provides explicit authorable invocation for supported operations. It does not execute gameplay accidentally from `Awake`, `OnEnable`, `Start` or `OnValidate`.

Default Actor selection delegates to the existing public Actor-selection authoring boundary rather than creating a second command authority.

The normal Inspector is designer-first; technical details remain Advanced / Debug.

## Status / diagnostics binding — P4

P4 (`IF-PLAYER-SURFACE-06`) is shipped through:

```text
PlayerProvisioningStatusBinding
```

The binding is read-only. It projects P2 observation and, when explicitly associated, the last P3 operation result. It does not create a global last-operation store, poll through hidden scene searches or become Player authority.

Status distinguishes available, unavailable and stale conditions and exposes richer correlation only in Advanced / Debug.

## Cross-scene integration requirement

The canonical topology remains:

```text
Persistent Application Content
  PlayerInputManager
  LocalPlayerProvisioningAuthoring
  LocalPlayerProvisioningHostRegistration
  LocalPlayerActorSelectionRequestAuthoring when used

Route / Activity content
  LocalPlayerProvisioningConsumerAccessBinding
  PlayerProvisioningCommandTrigger when useful
  PlayerProvisioningStatusBinding when useful
  game UI / presentation
```

A Route- or Activity-owned consumer does not require a serialized reference to a persistent runtime authority.

The package implementation intentionally rejects:

```text
public static runtime registry
service locator
reflection
FindObjectOfType / scene-wide authority search
hierarchy or object-name inference
generic global event bus
log parsing as state
direct consumer access to internal prepare / materialize / reconcile modules
```

## WaitCovered and externally-driven Player progression

The public certification directly proves the important Manager-Provisioned case:

```text
Required Player participation
+ WaitCovered
+ no joined Player yet
→ Activity entry remains pending / WaitingForJoin
→ Loading remains covered and non-terminal

public OpenJoining / SetDynamicCapacity / RequestJoin
+ default Actor selection
+ normal prepare / materialize / admit
→ Player contribution reaches Ready
→ WaitCovered loading becomes terminal
→ transition/loading gate is released according to the normal readiness contract
```

The framework does not fake readiness, auto-join, force reveal or silently weaken a Required Player contribution.

## QA boundary and certification

QAFramework must prove the same public surface expected from a normal consumer while retaining internal QA for authority invariants.

Public certification must not use, as the Player consumer path:

```text
reflection
internal preparation APIs
internal reconciliation APIs
manual runtime authority construction
external Slot mutation
consumer-side Actor materialization
runtime module lookup
global authority search
log parsing as authority
```

### Q1 — public-only positive contract proof

Certified 2026-08-09:

```text
QA-PLAYER-SURFACE-01
  PASS — 29/29
```

It proves authored public navigation, scoped access, joining, capacity, join, Host/Slot evidence, public default Actor selection, normal downstream lifecycle, WaitCovered pending-then-terminal behavior, Activity exit, Session persistence and reentry without duplicate Slot/Actor.

### Q2 — negative / stale-scope / lifecycle hardening

Certified 2026-08-09:

```text
QA-PLAYER-SURFACE-02
  PASS — 36/36
```

It proves closed-joining rejection, invalid/exhausted capacity, no-change behavior, missing/wrong/destroyed/stale scope handling, Activity exit/reentry lifetime behavior, stale Actor selection revision and deliberately unbound public navigation failure.

### Joint verdict

```text
PLAYER SURFACE QA CERTIFIED
```

Internal reservation/assignment/reconcile/preparation/materialization tests remain valuable authority QA but are not public product APIs.

## Product authoring direction and P5 disposition

The previously suggested mandatory Manager-Provisioned Recipe/Composer workflow is **not a required precondition** for the current surface.

The canonical manual baseline is intentionally explicit:

```text
PlayerSlotProfile
PlayerProvisioningProfile
PlayerSessionProfile
GameApplicationAsset
persistent provisioning composition
scoped consumer binding
optional command trigger
optional status binding
```

P5 (`IF-PLAYER-SURFACE-07`) is a **post-FIRSTGAME product disposition** step.

Possible outcomes include:

```text
NO ADDITIONAL TOOLING REQUIRED
small Create-menu / Inspector assistance
small template/sample
focused Composer/Wizard only if real usage proves recurring friction
```

A Wizard or Composer must not be introduced merely because the architecture can support one.

If tooling is justified, it must be idempotent, safe, non-destructive, Undo-aware, prefab-safe and must expose materialized technical components in Advanced / Debug rather than hiding authority.

## FIRSTGAME boundary

FIRSTGAME is now the next product evidence gate, not the source of technical Player authority.

FIRSTGAME should manually prove that a real consumer can:

```text
create the Profiles
configure the persistent provisioning composition
add scoped command/status consumers
enter a WaitCovered Activity
join a Manager-Provisioned Player
select/prepare/materialize/admit the Actor through normal runtime behavior
understand status and diagnostics
exit/reenter without duplicate Player state
```

FIRSTGAME may own layout, visuals, game-specific prefabs, movement and wording. Permanent framework-facing routing/projection solutions belong in the package.

## Validation requirements

Authoring/runtime validation must remain explicit and actionable for cases such as:

```text
missing provisioning composition
invalid Host prefab / PlayerInputManager configuration
invalid Session Profile / Slot configuration
unsupported command target
missing consumer binding
wrong/stale scope
unavailable runtime
```

The framework must not silently:

```text
open joining
auto-join
change capacity to satisfy a request
change participation/readiness policy
reserve another Slot
select another Actor outside policy
weaken Required readiness
fall back from invalid explicit Session configuration
```

## Current implementation coverage

### Shipped and technically certified

```text
IF-ADR-016 Session/Profile initialization contracts
P1 scoped provisioning consumer access
P2 immutable consumer observation
P3 PlayerProvisioningCommandTrigger
P4 PlayerProvisioningStatusBinding
public default Actor-selection request boundary
Q1 public positive Player Surface proof — 29/29 PASS
Q2 negative/stale/lifecycle Player Surface proof — 36/36 PASS
WaitCovered WaitingForJoin pending-then-terminal public proof
```

### Remaining before final ADR product closure

```text
FIRSTGAME manual real-consumer proof
post-FIRSTGAME P5 creation-workflow/tooling disposition
final consumer UX disposition
final canonical documentation reconciliation after real-consumer findings
formal ADR acceptance when those product gates are satisfied
```

## Out of scope

This ADR still does not define:

```text
Session Player Leave
device disconnect / reconnect
Session-Persistent Player implementation
generic multiplayer networking
generic application event bus
game-specific UI layout/colors/text
generic character-selection flow
Player movement
internal Actor preparation/reconcile operations as public commands
```

## Rejected alternatives

- Requiring every game to build a permanent provisioning event channel.
- Promoting a consumer-local compatibility bridge into shared framework architecture.
- Creating a generic global event bus.
- Letting consumers find persistent authorities through hierarchy, scene search or object name.
- Exposing internal preparation/reconciliation modules to consumer UI.
- Treating Unity button callbacks as the provisioning contract.
- Inferring Player state from instantiated GameObjects or logs.
- Creating a second mutable Player state store for presentation.
- Making Loading or Activity UI the source of Player readiness truth.
- Treating a Wizard/Composer as mandatory before real consumer evidence exists.

## Remaining implementation / product order

```text
1. P1 scoped consumer access                         CLOSED
2. P2 immutable observation                         CLOSED
3. P3 command authoring                              CLOSED
4. P4 status / diagnostics                           CLOSED
5. Q1 public positive QA                             CERTIFIED 29/29
6. Q2 negative/lifecycle QA                          CERTIFIED 36/36
7. documentation reconciliation                      CURRENT
8. FIRSTGAME manual real-consumer proof              NEXT
9. P5 creation-workflow/tooling disposition          AFTER FIRSTGAME
10. final FIRSTGAME UX disposition + ADR closure     PENDING
```

## Acceptance criteria

This ADR may move to **Accepted** when the already shipped technical surface and remaining product evidence jointly satisfy:

```text
one canonical ADR-015 exists
ownership boundary remains explicit
public command vocabulary is bounded
command results are typed and diagnostic
consumer observations are immutable
Slot / Host / Actor evidence is coherently correlated
cross-scene access has explicit scope and lifetime
no global lookup / service locator / generic event bus exists
designer command/status surfaces exist
QA proves supported positive and negative flows through public APIs
FIRSTGAME proves the official package surface in a real game composition
P5 creation-workflow disposition is explicitly recorded
temporary consumer compatibility bridges are removed or clearly non-canonical
canonical usage documentation matches the final product surface
```

## Completion interpretation

```text
Core Player runtime
  substantially implemented

Manager-Provisioned consumer product surface
  shipped

Canonical scoped command boundary
  shipped / QA-certified

Canonical immutable observation
  shipped / QA-certified

Designer command/status authoring
  shipped

Public-only technical QA
  certified — Q1 29/29; Q2 36/36

FIRSTGAME real-consumer proof
  pending

P5 creation workflow disposition
  pending after FIRSTGAME; tooling is not mandatory

ADR status
  Proposed

Implementation completion
  80%
```
