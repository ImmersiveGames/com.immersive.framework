# IF-ADR-021 — Activity Player Actor Initial Placement Authority

Status: **Proposed**  
Date: 2026-08-11  
Last updated: 2026-08-13  
Type: architecture / product authoring / runtime integration  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-012, IF-ADR-016, IF-ADR-019, IF-ADR-020  
Source finding: pre-FIRSTGAME architecture review — R1 Spawn / Initial Placement

> This ADR defines Initial Placement for contextual Player/Actor representations.
> It deliberately does not define a generic Spawn system, respawn, checkpoints,
> enemy spawning, pooling or network spawning.
>
> IF-ADR-019 and IF-ADR-020 are accepted related decisions. IF-ADR-021 itself remains
> Proposed and must not be treated as implemented or certified until its own cuts close.

## Context

The current Player architecture already contains two different concepts that are easy to
mistake for Spawn.

### Local Player Host Actor Mount

`LocalPlayerHostAuthoring` contains an explicit `ActorMount`.

Its role is:

```text
Local Player Host
  PlayerInput
  Actor Mount
    contextual Logical Actor
```

The Actor Mount defines the attachment location/hierarchy of a contextual Logical Actor
relative to the technical Local Player Host.

It is not an Activity world-space Spawn Point.

### Actor materialization

For Manager-Provisioned Players, the current attached Actor materialization adapter
creates the Logical Actor below the explicit Actor Mount and stages it inactive before
the later Player lifecycle continues.

This establishes:

```text
Player is already Joined
Host already exists
Actor Profile already selected
Actor physical representation is materialized
```

but does not establish:

```text
where this Actor should appear in the incoming Activity world
```

### Manager-Provisioned provisioning

`LocalPlayerProvisioningAuthoring` configures:

```text
one explicit PlayerInputManager
one technical Local Player Host prefab
```

It does not author an Activity position/rotation for the Player.

The underlying Unity provisioning backend creates the technical Host through
`PlayerInputManager.JoinPlayer(...)` without a Framework-owned placement transform.

Therefore the current result has no canonical Activity spatial intent.

### Scene-Provided Player

`SceneLocalPlayerAdmissionAuthoring` is an explicit designer-facing composer for a Player
that already exists in the scene.

It identifies:

```text
exact Player Slot
exact Actor Profile
exact scene-authored Logical Player Actor
same-root Local Player Host
```

The physical Actor is externally scene-owned.

Its authored Transform therefore already provides a physical pose, but the Framework has
not formalized whether that pose must be preserved or may be replaced by an Activity
placement rule.

### Session Player lifetime

IF-ADR-019 separates:

```text
Session Player
  Session-scoped Logical Player lifetime

Activity Player Representation
  contextual physical/gameplay representation
```

and establishes this integration boundary:

```text
incoming Activity representation prepared
        ↓
Initial Placement authority resolves/applies placement
        ↓
readiness may complete
```

It also rejects carrying an outgoing Activity world position silently into the incoming
Activity.

IF-ADR-020 separately owns termination of one exact Session Player occurrence and does
not make Initial Placement a Leave or cleanup authority.

R1 must now define the spatial authority at that boundary.

## Problem

Without an explicit Initial Placement contract, consumers must rely on one of several
implicit behaviors:

```text
prefab-authored position
PlayerInputManager-created Host position
world origin
outgoing Activity Host position
scene-authored Actor position
manual script repositioning
scene lookup for a GameObject named "Spawn"
custom singleton SpawnManager
```

These approaches produce different ownership and lifecycle semantics.

They also make it impossible for the Framework to answer reliably:

```text
where should this Player appear?
who authored that decision?
which Player Slot does this placement belong to?
was the placement applied before readiness?
why did placement fail?
```

The product needs one explicit, inspectable Activity-level answer.

## Decision

### 1. The canonical concept is Initial Placement, not generic Spawn

The Framework introduces an explicit **Activity Player Actor Initial Placement**
authority.

Its responsibility is exactly:

```text
resolve the authored initial world pose
for one contextual Player/Actor representation
when that representation is introduced into an Activity
```

The term "Spawn Point" may be used informally in documentation or UI affordances where it
helps users understand the concept.

The architectural contract remains **Initial Placement / Placement Anchor** because the
authority does not own creation or lifecycle.

### 2. Initial Placement is Activity-scoped spatial intent

Initial Placement belongs to the Activity in which the contextual Player representation
will participate.

Conceptually:

```text
Activity
  Player Initial Placement
    Player1 -> Anchor A
    Player2 -> Anchor B
```

The Activity owns the spatial intent.

It does not own the Session Player.
It does not own Player Join.
It does not own Actor selection.
It does not own Player Leave.

### 3. Placement Anchor is evidence, not authority

A Placement Anchor represents:

```text
world position
world rotation
Activity ownership/scope
authored identity sufficient for diagnostics
```

A Placement Anchor does not:

```text
instantiate an Actor
join a Player
reserve a Slot
select an Actor Profile
parent the Actor
own the Actor lifetime
register itself globally
execute respawn
track checkpoints
choose another anchor automatically
```

The Anchor is spatial evidence consumed by the Player/Actor lifecycle at the accepted
integration point.

### 4. Placement applies position and rotation, not scale

The canonical pose is:

```text
Position
Rotation
```

Initial Placement does not apply authored anchor scale to the Player/Actor
representation.

Actor scale remains owned by the Actor representation/prefab/consumer composition.
This avoids using marker hierarchy scale as hidden gameplay configuration.

### 5. Placement does not reparent the Actor

Applying an Activity Placement Anchor must not move the contextual Actor under the
Anchor in the Transform hierarchy.

```text
PlacementAnchor
  becomes runtime parent of Player Actor
```

is invalid.

The existing ownership hierarchy remains authoritative.

For Manager-Provisioned local Players this normally remains:

```text
Session-owned Local Player Host
  Actor Mount
    contextual Logical Actor
```

For Scene-Provided Players the consumer-authored scene hierarchy remains externally
owned.

The placement operation changes world pose only.

## Authored surface

### 6. One Activity exposes one clear Initial Placement authoring surface

The intended product shape is an Activity-local authoring component/composer that exposes
the Player placement intent in one place.

Conceptually:

```text
Activity Player Initial Placement

Bindings
  Player Slot     Placement Anchor
  Player1         P1 Start
  Player2         P2 Start
```

A likely implementation name is:

```text
ActivityPlayerInitialPlacementAuthoring
```

The exact class name is not frozen by this ADR.

Product requirements are frozen:

```text
Activity-local
designer-visible
Slot-explicit
diagnostic
no scene-wide runtime lookup
```

### 7. Slot-to-Anchor binding is explicit

Each authored binding identifies an exact configured `PlayerSlotId` and one exact
Placement Anchor.

The Framework must not infer a binding from:

```text
list index
Transform sibling index
PlayerInput.playerIndex
object name
tag
layer
distance
first available anchor
```

R1 does not require targeted Join. Once a Player is Joined, its actual Slot identity is
known and Initial Placement resolves the binding for that Slot.

### 8. At most one authoritative Initial Placement binding per Slot per Activity

```text
Player1 -> Anchor A
Player1 -> Anchor B
```

is invalid in one Activity placement scope. There is no priority/order fallback.
Duplicate bindings fail authoring validation.

### 9. No implicit shared/default anchor in the first contract

The first contract does not define:

```text
Default Anchor
Any Player Anchor
random anchor pool
first free anchor
fallback anchor
nearest anchor
```

If a later game demonstrates a real need, that policy may be added explicitly. The
minimal contract remains deterministic and Slot-addressed.

## Placement policies

### 10. Manager-Provisioned contextual Actors use Activity Placement when required

For a Manager-Provisioned Player, the technical Host is not the authored Activity
placement decision.

```text
Joined Player
        ↓
Actor selection resolved
        ↓
contextual Actor materialized inactive
        ↓
exact Activity Placement Anchor resolved by Player Slot
        ↓
Actor world position/rotation applied
        ↓
placement evidence committed
        ↓
later gameplay/readiness may continue
```

Outgoing Activity position, prefab Transform and world origin are not accepted incoming
placement fallbacks.

### 11. Manager-Provisioned placement targets contextual Actor world pose

The semantic target is the root of the contextual Logical Actor representation.

```text
Placement Anchor
        ↓
contextual Logical Actor world pose
```

not:

```text
Placement Anchor
        ↓
technical PlayerInput Host becomes gameplay spatial authority
```

The implementation preserves the Actor parent relationship to
`LocalPlayerHostAuthoring.ActorMount`.

This allows the Session-owned Host lifetime defined by IF-ADR-019 to remain technical and
avoids turning Host persistence into world-position persistence.

### 12. Scene-Provided has two explicit policies

Accepted policies:

```text
Preserve Authored Pose
Apply Activity Placement
```

No third implicit behavior is allowed.

#### Preserve Authored Pose

The scene-authored Actor Transform remains the initial Activity pose. Framework validates
and adopts the representation without changing world position/rotation through Initial
Placement. The authored pose remains diagnosable placement evidence.

#### Apply Activity Placement

```text
scene-authored Actor exists
        ↓
exact Activity Placement Anchor resolved by Slot
        ↓
Actor world position/rotation explicitly replaced
        ↓
admission/preparation continues
```

This is explicit opt-in mutation of externally scene-owned physical state. Framework must
not silently change Preserve to Apply.

### 13. Scene-Provided defaults to preserving authored intent

For new Scene-Provided authoring, safe product default is:

```text
Preserve Authored Pose
```

A consumer that wants separate Activity placement explicitly selects
`Apply Activity Placement`.

### 14. Scene-Provided policy belongs with representation intent

The user must be able to see why the Actor moved from the Scene-Provided Player product
surface or a clearly linked Initial Placement section. Exact serialized field location
remains an implementation detail reconciled with ADR-010.

## Required versus unused placement

### 15. Placement is required only for a representation configured to use Activity Placement

An Activity does not need an anchor merely because a Player Slot exists.

```text
Player Joined + Activity does not project Player
  -> no placement required

Scene-Provided + Preserve Authored Pose
  -> no Activity anchor required

Manager-Provisioned contextual Actor prepared for gameplay
  -> Activity placement required

Scene-Provided + Apply Activity Placement
  -> Activity placement required
```

### 16. Missing required placement is a preparation failure

If Activity Placement is required and exact Slot has no valid binding:

```text
placement fails
Actor preparation does not become Ready
diagnostic identifies Activity + Slot + missing placement
```

No fallback to world origin, prefab pose, Host pose, previous Activity pose, first anchor,
another Slot's anchor or scene search is allowed.

### 17. Unused extra bindings are authoring evidence, not runtime allocation

An Activity may author an anchor for a supported Slot that is not currently Joined. The
anchor must not create Player, reserve Slot, force Join or make Player Required.

Unsupported Slot references may be validation errors; supported-yet-vacant bindings are
valid reusable composition.

## Runtime ordering

### 18. Placement occurs after representation exists and before gameplay readiness

```text
Session Player truth
        ↓
Activity projection
        ↓
Actor selection where required
        ↓
physical representation materialized/adopted
        ↓
Initial Placement resolved/applied
        ↓
gameplay admission / Camera publication as applicable
        ↓
Player readiness contribution may become Ready
        ↓
Activity reveal
```

The ordering is architectural; exact runtime classes are not frozen.

### 19. Placement success is typed runtime evidence

Required placement evidence must diagnose at least:

```text
Activity/runtime scope
Player Slot
current Actor occurrence/materialization identity
placement policy
resolved Anchor identity
applied position
applied rotation
status
diagnostic
```

Scene-Provided Preserve also produces explicit evidence that no Framework pose mutation
was required.

### 20. Placement evidence is occurrence-scoped

Evidence from Activity A/Actor occurrence 10 cannot satisfy Activity B/occurrence 11.
Released Actor occurrence placement evidence cannot make a new occurrence Ready.

### 21. Placement failure blocks the relevant readiness level

When Player requirement needs a prepared Logical Actor, required placement is part of
that preparation boundary.

```text
Actor materialized
Placement missing/invalid
```

does not satisfy `LogicalActorsPrepared`.

### 22. Placement is applied before contextual gameplay starts

The Actor must not receive normal gameplay admission at an unintended pose and then move.
Placement precedes normal contextual gameplay/input/Camera readiness.

## Activity transitions

### 23. Outgoing world position does not become incoming Initial Placement

Activity B begins at its authored Initial Placement, not Activity A's outgoing gameplay
position, unless a separate future system explicitly defines continuity.

### 24. Covered transition supports deterministic placement before reveal

```text
Cover
incoming Activity technical loading
incoming Player representation preparation
Initial Placement
readiness completes
Reveal
```

Initial Placement does not own Cover/Reveal.

### 25. Re-entry creates a new placement decision when representation is rebuilt

If Activity lifecycle exit/re-entry creates a new Actor occurrence, Initial Placement is
evaluated again. If Reset preserves the same representation, this ADR does not implicitly
reposition it.

## Reset, restart and respawn boundary

### 26. Initial Placement is not Reset

```text
Initial Placement
  where a contextual representation begins an Activity occurrence

Reset
  accepted runtime-state restoration

Respawn
  future gameplay lifecycle after death/failure/checkpoint
```

Initial Placement is not a repeated respawn operation.

### 27. Activity restart follows Activity lifecycle semantics

New contextual occurrence -> Initial Placement applies. Preserved occurrence + Reset-only
semantics -> this ADR does not mandate repositioning.

## Product authoring UX

### 28. Designer edits Activity intent, not runtime contracts

Intended workflow:

```text
1. Open Activity scene/composition.
2. Add/locate Player Initial Placement authoring.
3. Add binding for each Slot using Activity Placement.
4. Position/rotate Anchors visually.
5. Scene-Provided: choose Preserve or Apply.
6. Validate.
7. Enter Play Mode and inspect resolved placement evidence.
```

Designer must not wire internal runtime services/materialization requests.

### 29. Placement Anchors should be scene-visible authoring markers

Implementation should provide appropriate label/icon/gizmo, Slot identity and forward
orientation indication without turning the marker into a gameplay component.

### 30. Apply/Rebuild is required only if real materialization exists

If authoring stores runtime bindings directly with no derived technical graph, artificial
Apply/Rebuild is not required. If implementation materializes derived technical
structures, idempotent ownership-aware Apply/Rebuild follows ADR-010.

### 31. Inspector is designer-first

Default Inspector prioritizes:

```text
Placement Mode / Policy
Player Slot
Placement Anchor
Configuration Status
Last Validation
```

Advanced/Debug may expose Activity owner, runtime scope, Actor occurrence/materialization,
technical Host, Actor Mount, resolved pose, result and readiness correlation.

## Validation

### 32. Authoring validation is deterministic

At minimum validate:

```text
valid Activity context
explicit valid Player Slot
Slot belongs to accepted Session configuration when available
Anchor exists
no duplicate Slot binding
Anchor belongs to intended Activity scope
Scene-Provided placement policy valid
Apply Activity Placement has exact binding
```

Validation does not silently repair invalid bindings.

### 33. Anchor scope is Activity-local

A Placement Anchor belongs to the Activity's physical scene composition. No arbitrary
Transform from another Activity, `DontDestroyOnLoad`, global registry, utility scene or
unrelated loaded scene may be used without a future explicit contract.

### 34. Duplicate or ambiguous evidence is a failure

More than one authoritative candidate for one Activity scope + Slot + Actor occurrence
fails explicitly. No hierarchy/list/registration-order winner is allowed.

## Physical ownership

### 35. Placement does not change physical ownership

```text
Manager-Provisioned
  technical Host -> Session-owned according to IF-ADR-019
  contextual Actor -> Framework Player Actor lifecycle

Scene-Provided
  Host + Actor -> externally scene-owned
```

Applying pose does not transfer ownership. Scene-Provided Apply is authorized pose
mutation, not Framework destruction/lifetime ownership.

### 36. Anchor lifetime does not become Player lifetime

Activity Anchor unload does not mean Session Player Leave. IF-ADR-020 Leave does not
require destruction of Placement Anchor authoring.

## Runtime implementation boundary

### 37. Use a scoped Initial Placement runtime boundary

```text
Activity Initial Placement authoring
  -> scoped placement configuration
  -> Player Actor preparation
  -> exact Slot binding
  -> apply/observe pose
  -> placement result/evidence
```

No `SpawnManager.Instance`, `FindObjectOfType`, `GameObject.Find`, static anchor dictionary
or global service locator.

### 38. RuntimeContent remains lower-level materialization infrastructure

RuntimeContent stays responsible for explicit physical materialization. Initial Placement
composes with Player Actor materialization; it does not expand runtime materialization
adapters into spawn registry, Slot allocation or Activity authoring.

### 39. No runtime reflection is required

Placement applies to the explicitly known contextual Actor Transform. Reflection, tag
lookup and opportunistic scene scanning are not required.

## Interaction with other Player decisions

### 40. Relationship to Session Player lifetime

IF-ADR-019 owns:

```text
whether Logical Player persists across Activities
whether an Activity has current contextual representation
technical Host lifetime
```

IF-ADR-021 owns only initial spatial pose of that contextual representation.

### 41. Relationship to Player Leave

IF-ADR-020 owns termination of one Session Player occurrence and resource release.

Initial Placement does not Leave, vacate Slot, destroy Session-owned Host or reposition a
departing Player as cleanup.

### 42. Relationship to Actor Selection

Actor Selection answers which Actor Profile should represent the Player. Initial
Placement answers where the resulting contextual Actor begins this Activity. Placement
Anchor does not encode Actor Profile.

### 43. Relationship to per-Slot provisioning and targeted Join

Per-Slot provisioning and targeted Join remain separate decisions. R1 consumes the Slot
identity that exists after accepted Session admission; it does not own admission policy.

### 44. Relationship to Camera

Camera may consume prepared Actor as target only after the correct occurrence exists.
Initial Placement does not choose Camera rigs or publish Camera priority/output authority.

## Diagnostics

### 45. Runtime diagnostics identify spatial intent and outcome

Success evidence should identify Activity, Slot, Actor occurrence, provisioning, policy,
Anchor, position/rotation, status and readiness eligibility.

Scene-Provided Preserve evidence should make explicit that Framework did not move the
Actor. Failure evidence should identify exact missing/invalid Slot-to-Anchor binding and
readiness consequence.

### 46. Diagnostics distinguish Actor Mount from Placement Anchor

Advanced diagnostics must never label `ActorMount` as Spawn Point. Useful evidence shows:

```text
Technical Host
Actor Mount
Contextual Actor
Placement Anchor
```

## Rejected behavior

- Generic `SpawnManager` as Player lifecycle/world-placement authority.
- Using `ActorMount` as Activity Spawn Point.
- Using `PlayerInputManager` creation pose as canonical Initial Placement.
- World-origin/prefab/previous-position fallback.
- Name/tag scene lookup for spawn point.
- First/nearest/random-anchor fallback.
- Duplicate Slot bindings resolved by order.
- Reparenting Actor under Placement Anchor.
- Applying Anchor scale to Actor.
- Placement Anchor allocating/reserving Slot, selecting Actor or forcing Join.
- Moving Scene-Provided Actor without explicit Apply policy.
- Transferring Scene-Provided physical ownership through placement.
- Marking prepared readiness before required placement succeeds.
- Reusing placement evidence across Actor occurrences.
- Using Initial Placement as hidden respawn/reset/checkpoint behavior.
- Global anchor registry/singleton/service locator.
- Silent repair of invalid placement configuration.

## Deferred / separate contracts

Outside this ADR:

```text
death / respawn
checkpoint respawn
save-game position restoration
seamless world streaming position continuity
portal/door destination routing
dynamic/random spawn selection
spawn occupancy/collision avoidance
spawn queues
enemy/NPC spawning
wave spawning
pooling
network spawning
split-screen output
device disconnect/reconnect
per-Slot provisioning
targeted Join
generic ObjectEntry physical binding
generic RuntimeContent prefab spawning
teleport gameplay ability
Reset-to-spawn command
```

## Consequences

### Positive

The user gets one deterministic answer to "Where does Player1 start this Activity?".
Manager-Provisioned no longer depends on prefab/Host pose accidents. Scene-Provided keeps
authored pose by default and can opt into Activity placement. Placement composes with Slot
identity and readiness without becoming Join/Leave/Actor/Camera authority.

### Cost

The package needs Activity-local product authoring, scoped placement evidence and a
pre-readiness integration stage. Scene-Provided needs an explicit placement policy.
Editor validation/diagnostics must distinguish Placement Anchor from Actor Mount. QA must
cover both provisioning modes and negative/no-fallback cases after acceptance.

## Required reconciliation after acceptance

This ADR remains Proposed; no acceptance reconciliation is applied by the ADR-020 closure.
When IF-ADR-021 itself is accepted, affected architecture should be reconciled approximately
as follows:

```text
IF-ADR-003
  Initial Placement remains separate spatial authority

IF-ADR-007
  required prepared readiness waits for required current-occurrence placement

IF-ADR-010
  designer-first Activity Initial Placement surface + validation/debug expectations

IF-ADR-012
  placement participates in prepared contextual Actor boundary when required

IF-ADR-016
  Slot allocation remains unchanged; placement consumes resulting Slot identity

IF-ADR-019
  incoming representation integration point becomes the accepted placement boundary

IF-ADR-020
  Placement Anchor gains no ownership transfer; Leave remains Session membership/resource authority
```

No existing ADR should be changed merely because IF-ADR-021 is Proposed.

## Expected implementation cuts after acceptance

### Cut P1 — Contracts and Activity authoring

Define explicit Activity Slot -> Placement Anchor intent and Scene-Provided placement
policy, with designer-first Inspector/validation.

### Cut P2 — Scoped runtime placement

Resolve exact current Activity + Slot placement, apply/observe pose against current Actor
occurrence and produce typed placement evidence after materialization/adoption and before
readiness.

### Cut P3 — QA

Prove Manager-Provisioned placement, Scene-Provided Preserve/Apply, missing anchor,
duplicate binding, wrong scope, stale occurrence, no fallback and readiness ordering.

### Cut P4 — FIRSTGAME product proof

Prove real developer workflow: create/see anchors, map Slots, position/orient, configure
both provisioning modes, understand Preserve vs Apply, run correct pose and diagnose a
broken binding.

## Validation requirements

### Contract

```text
exact Activity Slot -> exact Anchor
duplicate Slot rejected
unsupported/invalid Slot rejected
invalid/cross-Activity Anchor rejected
Anchor scale not applied
Actor not reparented to Anchor
```

### Manager-Provisioned

```text
Player joins
Actor selected/materialized under Actor Mount
Activity anchor resolved from joined Slot
Actor world position/rotation applied
Actor remains under Actor Mount
readiness only completes after placement
missing anchor blocks preparation
no prefab/origin/previous-position fallback
```

### Scene-Provided Preserve

```text
scene-owned Actor exists
Policy = Preserve Authored Pose
Framework does not alter pose through Initial Placement
Preserve evidence recorded
no Activity Anchor required
physical ownership remains external
```

### Scene-Provided Apply

```text
scene-owned Actor exists
Policy = Apply Activity Placement
exact Slot anchor required
pose applied before readiness
Actor remains scene-owned/in authored hierarchy
missing anchor blocks preparation
```

### Occurrence safety

Placement evidence from one Activity/Actor occurrence does not satisfy another.

### Transition

Outgoing gameplay position is not used implicitly; incoming Activity anchor wins and
placement occurs before reveal when readiness waits for prepared Actor.

### Negative

No global lookup, first-anchor fallback, world-origin fallback, silent duplicate
resolution, Scene-Provided movement under Preserve, or Ready before required placement.

### Product

Designer can identify placement surface, Slot mapping, anchor orientation, Actor Mount vs
Placement Anchor, Preserve vs Apply and the runtime placement diagnostic.

## Acceptance of this architecture cut

```text
Initial Placement is Activity-scoped spatial intent
not generic Spawn lifecycle authority
Placement Anchor is position/rotation evidence only
Slot -> Anchor mapping explicit
one authoritative binding per Slot per Activity
no implicit fallback in initial contract
Manager-Provisioned contextual Actor uses Activity placement when required
Scene-Provided supports Preserve or Apply and defaults to Preserve
placement occurs after representation exists and before gameplay readiness
required placement failure blocks prepared readiness
placement evidence is occurrence-scoped
Actor is not reparented; Anchor scale not applied
outgoing world position is not incoming placement policy
Initial Placement != Reset/Respawn/Checkpoint
no global SpawnManager/registry/service locator
```

## Suggested commits

Architecture after acceptance:

```text
docs(architecture): define activity player initial placement authority
```

Runtime/editor/QA cuts remain separate after the ADR is accepted.
