# IF-ADR-021 — Activity Player Actor Initial Placement Authority

Status: **Proposed**  
Date: 2026-08-11  
Type: architecture / product authoring / runtime integration  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-012, IF-ADR-016, proposed IF-ADR-019, proposed IF-ADR-020  
Source finding: pre-FIRSTGAME architecture review — R1 Spawn / Initial Placement

> This ADR defines Initial Placement for contextual Player/Actor representations.
> It deliberately does not define a generic Spawn system, respawn, checkpoints,
> enemy spawning, pooling or network spawning.

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

Proposed IF-ADR-019 separates:

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

This is invalid:

```text
PlacementAnchor
  becomes runtime parent of Player Actor
```

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

The product requirements are frozen:

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

This means R1 does not require R8 Slot-Targeted Join.

Join may continue to allocate the first vacant supported Slot according to the accepted
Session contract.

Once a Player is Joined, its actual Slot identity is known and Initial Placement resolves
the binding for that Slot.

### 8. At most one authoritative Initial Placement binding per Slot per Activity

For one Activity placement scope:

```text
Player1 -> Anchor A
Player1 -> Anchor B
```

is invalid.

There is no priority/order fallback.

Duplicate bindings fail authoring validation.

### 9. No implicit shared/default anchor in the first contract

The first accepted contract does not define:

```text
Default Anchor
Any Player Anchor
random anchor pool
first free anchor
fallback anchor
nearest anchor
```

If a later game demonstrates a real need for shared or dynamic placement policy, that
policy may be added explicitly.

The minimal contract remains deterministic and Slot-addressed.

## Placement policies

### 10. Manager-Provisioned contextual Actors use Activity Placement when placement is required

For a Manager-Provisioned Player, the technical Host is not the authored Activity
placement decision.

The contextual Actor representation is prepared under the explicit Actor Mount.

When the Activity requires Initial Placement for that representation:

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

The outgoing Activity position of a persistent technical Host is not an accepted incoming
placement source.

The prefab Transform is not an accepted fallback.

World origin is not an accepted fallback.

### 11. Manager-Provisioned placement targets the contextual Actor world pose

The semantic placement target is the root of the contextual Logical Actor
representation.

The Session-owned technical Local Player Host is not the authored world-placement entity.

Therefore the contract is:

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

The implementation must preserve the Actor's required parent relationship to
`LocalPlayerHostAuthoring.ActorMount`.

This allows the Session-owned Host lifetime defined by proposed IF-ADR-019 to remain
technical and avoids turning Host persistence into world-position persistence.

### 12. Scene-Provided has two explicit policies

Scene-Provided Player authoring owns an already existing scene Actor.

Its Initial Placement behavior must therefore be explicit.

Accepted policies:

```text
Preserve Authored Pose
Apply Activity Placement
```

No third implicit behavior is allowed.

#### Preserve Authored Pose

```text
scene-authored Actor Transform
  remains the initial Activity pose
```

The Framework validates/adopts the representation without changing its world
position/rotation through Initial Placement.

The authored pose is still observable as placement evidence for diagnostics.

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

This is an explicit opt-in mutation of externally scene-owned physical state.

The Framework must not silently change a Scene-Provided Actor from Preserve to Apply.

### 13. Scene-Provided defaults to preserving authored intent

For new Scene-Provided Player authoring, the safe product default is:

```text
Preserve Authored Pose
```

because the consumer deliberately authored the physical Actor in the scene.

A consumer that wants a separate Activity Placement Anchor selects:

```text
Apply Activity Placement
```

explicitly.

This prevents adding the feature from unexpectedly moving existing scene-owned Actors.

### 14. The Scene-Provided policy belongs with the Scene-Provided representation intent

The policy that decides whether a Scene-Provided Actor preserves or replaces its authored
pose must be visible from the Scene-Provided Player product surface or from a clearly
linked Initial Placement section.

The user should not need to inspect an internal runtime component to discover why the
Actor moved.

The exact serialized field location is an implementation detail to reconcile with
ADR-010 Inspector requirements.

## Required versus unused placement

### 15. Placement is required only for a representation configured to use Activity Placement

An Activity does not need an anchor merely because a Player Slot exists.

Examples:

```text
Player is Joined
Activity does not project that Player
  -> no placement required

Scene-Provided
Policy = Preserve Authored Pose
  -> no Activity anchor required

Manager-Provisioned contextual Actor is prepared for gameplay
  -> Activity placement required

Scene-Provided
Policy = Apply Activity Placement
  -> Activity placement required
```

This avoids making menus or non-player Activities configure meaningless Spawn Points.

### 16. Missing required placement is a preparation failure

If Activity Placement is required and the exact Slot has no valid binding:

```text
placement fails
Actor preparation does not become Ready
diagnostic identifies Activity + Slot + missing placement
```

The Framework must not fallback to:

```text
(0,0,0)
prefab pose
Host pose
previous Activity pose
first anchor
another Slot's anchor
scene search
```

This is a mandatory no-fallback invariant.

### 17. Unused extra bindings are diagnosable authoring evidence, not runtime allocation

An Activity may author an anchor for a supported Slot that is not currently Joined.

That is valid reusable Activity composition.

The existence of the anchor must not:

```text
create the Player
reserve the Slot
force Join
make the Player Required
```

Validation may report unsupported Slot references as errors, but a supported-yet-vacant
Slot binding is valid.

## Runtime ordering

### 18. Placement occurs after physical representation exists and before gameplay readiness

The required lifecycle order is:

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
gameplay admission / camera publication as applicable
        ↓
Player readiness contribution may become Ready
        ↓
Activity reveal according to loading policy
```

This ordering is architectural.

The exact runtime classes/modules are not frozen.

### 19. Placement success is typed runtime evidence

A required placement operation must produce explicit evidence containing enough
information to diagnose at least:

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

Scene-Provided Preserve Authored Pose also produces explicit evidence that no
Framework pose mutation was required.

Readiness must depend on the evidence for the current occurrence, not on a stale prior
Activity/Actor placement result.

### 20. Placement evidence is occurrence-scoped

This sequence must not reuse stale placement evidence:

```text
Activity A
  Player1 Actor occurrence 10
  Placement A succeeds

Activity B
  Player1 Actor occurrence 11
```

Occurrence 11 requires its own Initial Placement decision/evidence.

Likewise:

```text
same Slot
old Actor occurrence released
new Actor occurrence prepared
```

must not become Ready merely because the old occurrence was placed successfully.

### 21. Placement failure blocks the relevant readiness level

When the Activity's Player requirement needs a prepared Logical Actor, required Initial
Placement is part of that preparation boundary.

Therefore:

```text
Actor materialized
Placement missing/invalid
```

does not satisfy `LogicalActorsPrepared`.

The Framework must not publish fake Ready evidence and later teleport the Actor after
reveal.

### 22. Placement is applied before contextual gameplay starts

The Actor must not briefly receive normal gameplay admission at an unintended pose and
then be moved.

The placement operation occurs before normal contextual gameplay/input/camera authority
is considered ready for the new representation.

Control-plane operations remain separate from gameplay input according to the existing
Player architecture.

## Activity transitions

### 23. Outgoing world position does not become incoming Initial Placement

For a Session Player moving between Activities:

```text
Activity A
  Actor ends at X=184

transition

Activity B
  Anchor for Player1 = X=10
```

Activity B begins at its authored Initial Placement.

It does not inherit X=184 unless a separate future system explicitly defines that
behavior.

This is especially important for a persistent Manager-Provisioned technical Host.

Host lifetime continuity is not spatial continuity.

### 24. Covered transition supports deterministic placement before reveal

A covered transition may prepare an incoming contextual Actor while the visual transition
surface is covered.

The intended ordering is:

```text
Cover
incoming Activity technical loading
incoming Player representation preparation
Initial Placement
readiness completes
Reveal
```

This allows the first visible frame of gameplay to use the correct authored pose.

The placement system itself does not own Cover/Reveal.

### 25. Re-entry creates a new contextual placement decision when representation is rebuilt

If Activity lifecycle exit/re-entry releases and prepares a new contextual Actor
occurrence, Initial Placement is evaluated again.

If a Reset operation does not rebuild the Activity Player representation, this ADR does
not automatically reposition the Actor.

This contract must not be used as a hidden Player Reset/Respawn mechanism.

## Reset, restart and respawn boundary

### 26. Initial Placement is not Reset

The following are separate concepts:

```text
Initial Placement
  where a contextual representation begins an Activity occurrence

Reset
  restore accepted runtime state according to Reset contracts

Respawn
  future gameplay lifecycle after death/failure/checkpoint
```

A consumer must not call Initial Placement repeatedly as a substitute for a respawn
system.

### 27. Activity restart follows Activity lifecycle semantics

If an accepted Activity restart path tears down and reconstructs the Player Actor
representation:

```text
new contextual occurrence
  -> Initial Placement applies
```

If the restart path preserves the same representation and only invokes Reset semantics:

```text
same contextual occurrence
  -> this ADR does not mandate repositioning
```

The Reset architecture remains authoritative.

## Product authoring UX

### 28. Designer edits Activity intent, not runtime contracts

The intended workflow is:

```text
1. Open an Activity scene/composition.
2. Add or locate Player Initial Placement authoring.
3. Add one binding for each Player Slot that uses Activity Placement.
4. Position/rotate each Anchor visually in the scene.
5. For Scene-Provided Players choose:
     Preserve Authored Pose
     or
     Apply Activity Placement
6. Validate.
7. Enter Play Mode and inspect resolved placement evidence.
```

The designer must not manually wire internal runtime services or materialization
requests.

### 29. Placement Anchors should be scene-visible authoring markers

The product surface should make anchor location understandable in the Scene view.

A later implementation cut should provide appropriate:

```text
label/icon/gizmo
Slot identity
forward/orientation indication
```

without turning the marker into a gameplay component.

The exact editor visualization is not frozen by this ADR.

### 30. Apply/Rebuild is required only if real materialization exists

If the chosen authoring implementation stores the runtime bindings directly and has no
derived technical graph, an artificial Apply/Rebuild button is not required.

If the product implementation materializes technical bindings/evidence into separate
components or serialized structures, it must expose idempotent, non-destructive
Apply/Rebuild according to ADR-010.

The architecture does not require ceremony without materialization.

### 31. Inspector is designer-first

The default Inspector should prioritize:

```text
Placement Mode / Policy
Player Slot
Placement Anchor
Configuration Status
Last Validation
```

Advanced/Debug may expose:

```text
Activity owner
Runtime scope
Actor occurrence/materialization id
technical Local Player Host
Actor Mount
resolved world pose
placement result
readiness correlation
```

Technical components must remain inspectable in Advanced/Debug rather than hidden
irreversibly.

## Validation

### 32. Authoring validation is deterministic

At minimum validate:

```text
Activity placement surface is in a valid Activity context
Player Slot reference is explicit and valid
Player Slot belongs to accepted Session configuration when that information is available
Anchor reference exists
duplicate Slot binding does not exist
Anchor belongs to the intended Activity scene/scope
Scene-Provided placement policy is valid
Apply Activity Placement has an exact binding
```

Validation must not repair invalid bindings silently.

### 33. Anchor scope is Activity-local

A Placement Anchor used by one Activity must belong to that Activity's physical scene
composition.

Initial Placement must not resolve an arbitrary Transform from:

```text
another Activity
DontDestroyOnLoad
global registry
persistent utility scene
unrelated loaded additive scene
```

unless a future explicit contract broadens the boundary.

This keeps spatial intent owned by the Activity that consumes it.

### 34. Duplicate or ambiguous evidence is a failure

If runtime receives more than one authoritative placement candidate for one:

```text
Activity scope
Player Slot
Actor occurrence
```

the operation fails explicitly.

It does not choose by hierarchy order or registration timing.

## Physical ownership

### 35. Placement does not change physical ownership

For Manager-Provisioned:

```text
technical Host
  Session-owned according to proposed IF-ADR-019

contextual Actor representation
  framework materialized/released through Player Actor lifecycle
```

For Scene-Provided:

```text
Host + Actor
  externally scene-owned physical objects
```

Applying a pose does not transfer ownership.

Scene-Provided `Apply Activity Placement` is authorized pose mutation, not Framework
ownership of destruction or lifetime.

### 36. Anchor lifetime does not become Player lifetime

Destroying/unloading the Activity Anchor as part of Activity exit does not mean the
Session Player left.

The Anchor is contextual Activity evidence only.

Likewise, Session Player Leave does not need to destroy Placement Anchor authoring.

## Runtime implementation boundary

### 37. Use a scoped Initial Placement runtime boundary

The implementation should use a typed Activity-scoped placement resolver/application
boundary.

Conceptually:

```text
Activity Player Initial Placement authoring
        ↓
scoped placement configuration
        ↓
Player Actor preparation
        ↓
resolve exact Slot binding
        ↓
apply/observe pose
        ↓
placement result/evidence
```

It must not use:

```text
SpawnManager.Instance
FindObjectOfType<SpawnPoint>()
GameObject.Find("Spawn")
static dictionary of scene anchors
global service locator
```

The exact interface and runtime context names are an implementation cut.

### 38. RuntimeContent remains lower-level materialization infrastructure

The existing RuntimeContent materialization boundary remains responsible for explicit
physical materialization requests/results/handles and does not become an Initial
Placement product surface.

Initial Placement composes with Player Actor materialization.

It does not expand `IRuntimeMaterializationAdapter` into:

```text
spawn point registry
player placement policy
Player Slot allocation
Activity authoring
```

### 39. No runtime reflection is required

Placement can be applied to the explicitly known contextual Actor Transform supplied by
Player Actor preparation/materialization/adoption.

No reflection, tag lookup or component scanning beyond existing explicit evidence is
required by this architecture.

## Interaction with other Player decisions

### 40. Relationship to Session Player lifetime

Proposed IF-ADR-019 owns:

```text
whether the Logical Player persists across Activities
whether an Activity has a current contextual representation
technical Host lifetime
```

IF-ADR-021 owns only:

```text
initial spatial pose of that contextual representation
```

### 41. Relationship to Player Leave

Proposed IF-ADR-020 owns termination of one Session Player occurrence and resource
release.

Initial Placement does not:

```text
Leave
vacate Slot
destroy Session-owned Host
reposition departing Player as cleanup
```

### 42. Relationship to Actor Selection

Actor Selection answers:

```text
which Actor Profile should represent this Player?
```

Initial Placement answers:

```text
where should the resulting contextual Actor begin this Activity?
```

These decisions remain separate.

A Placement Anchor must not encode an Actor Profile.

### 43. Relationship to per-Slot provisioning and targeted Join

Per-Slot provisioning and targeted Join remain separate future candidates.

R1 only uses the Slot identity that already exists after accepted Session admission.

Therefore this is valid with the current allocation policy:

```text
RequestJoin
  -> current Session policy assigns Player1

Activity placement
  -> exact Player1 binding resolves Anchor A
```

No change to Join allocation is required.

### 44. Relationship to Camera

Camera authority may consume the prepared Actor as Follow/LookAt target only after the
correct contextual Actor occurrence is available.

Initial Placement does not choose Camera rigs or publish winner priority.

A camera may observe the Actor after placement; it does not provide Player spawn
authority.

## Diagnostics

### 45. Runtime diagnostics must identify spatial intent and outcome

Example success:

```text
PLAYER INITIAL PLACEMENT

Activity             Gameplay_A
Player Slot          Player1
Actor Occurrence     14
Provisioning         Manager-Provisioned
Policy               Apply Activity Placement
Anchor               P1 Start
Position             (12.0, 0.0, -4.0)
Rotation             (0.0, 90.0, 0.0)
Status               Applied
Readiness Eligible   Yes
```

Scene-Provided preserve example:

```text
PLAYER INITIAL PLACEMENT

Activity             Gameplay_B
Player Slot          Player1
Provisioning         Scene-Provided
Policy               Preserve Authored Pose
Actor                PlayerSceneActor
Position             authored scene pose
Status               Preserved
Framework Moved Actor No
```

Failure example:

```text
PLAYER INITIAL PLACEMENT

Activity             Gameplay_C
Player Slot          Player2
Policy               Apply Activity Placement
Anchor               Missing
Status               Failed
Readiness Eligible   No
Diagnostic           No exact Activity placement binding exists for Player2.
```

### 46. Diagnostics distinguish Actor Mount from Placement Anchor

Advanced diagnostics must not label `ActorMount` as Spawn Point.

Useful evidence should make the hierarchy explicit:

```text
Technical Host       LocalPlayerHost(Clone)
Actor Mount          ActorMount
Contextual Actor     Hero(Clone)
Placement Anchor     Player1_Start
```

This prevents users from editing the technical attachment point when they intend to
author Activity world placement.

## Rejected behavior

- Generic `SpawnManager` as the owner of Player lifecycle or world placement.
- Using `ActorMount` as the Activity Spawn Point.
- Using `PlayerInputManager` creation pose as canonical Initial Placement.
- Falling back to world origin.
- Falling back to prefab Transform.
- Carrying outgoing Activity world position into the incoming Activity implicitly.
- Searching scene objects by name/tag to find a spawn point.
- First-anchor / nearest-anchor / random-anchor fallback.
- Duplicate Slot bindings with priority decided by list/hierarchy order.
- Reparenting the Player Actor below the Placement Anchor.
- Applying Placement Anchor scale to the Actor.
- Placement Anchor allocating or reserving a Player Slot.
- Placement Anchor selecting an Actor Profile.
- Placement Anchor forcing Join.
- Scene-Provided Actor being moved without explicit Apply Activity Placement policy.
- Scene-Provided physical ownership being transferred to the Framework by placement.
- Marking `LogicalActorsPrepared` before required placement succeeds.
- Reusing placement evidence from a previous Actor occurrence.
- Using Initial Placement as hidden respawn/reset/checkpoint behavior.
- Global registry/singleton/service locator for anchors.
- Silent repair of missing or invalid placement configuration.

## Deferred / separate contracts

The following are outside this ADR:

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

A demonstrated game requirement may open one of those contracts later.

## Consequences

### Positive

The user gets one understandable answer to:

```text
Where does Player1 start this Activity?
```

Manager-Provisioned no longer depends on Unity prefab/Host placement accidents.

Scene-Provided keeps consumer-authored pose by default and may opt into the same Activity
placement model explicitly.

Player placement composes with the existing Slot identity without requiring targeted Join
or per-Slot provisioning.

Initial Placement becomes part of Player Actor preparation/readiness, so the Activity
cannot reveal a required Actor at an undefined pose.

The architecture avoids a global Spawn manager and preserves the separation:

```text
Player lifecycle
Actor selection
physical materialization
initial spatial placement
camera
readiness
```

### Cost

The package needs a new Activity-local product authoring surface and scoped runtime
placement evidence.

Player Actor preparation must integrate one additional required stage before readiness.

Scene-Provided authoring needs an explicit placement policy.

Editor validation/diagnostics must distinguish Placement Anchor from Actor Mount.

QA must cover both provisioning modes and negative/no-fallback cases.

## Required reconciliation after acceptance

This draft intentionally does not edit existing ADRs yet.

After IF-ADR-021 is accepted, architecture should be reconciled approximately as follows:

```text
IF-ADR-003
  reference Initial Placement as a separate spatial authority
  keep Player/Actor lifecycle ownership unchanged

IF-ADR-007
  clarify that required Player Actor readiness cannot complete
  before required current-occurrence placement evidence succeeds

IF-ADR-010
  register the designer-first Activity Initial Placement surface
  and its validation/debug expectations

IF-ADR-012
  include placement in the prepared contextual Actor boundary
  when Activity Placement is required

IF-ADR-016
  clarify that Slot allocation remains unchanged;
  placement consumes the resulting Slot identity

IF-ADR-019
  replace the R1 deferred boundary with IF-ADR-021
  define the incoming representation integration point as accepted

IF-ADR-020
  no ownership transfer from Placement Anchor;
  Leave remains Session membership/resource release authority
```

No existing ADR should be changed until IF-ADR-021 is reviewed and accepted.

## Expected implementation cuts after acceptance

The architecture should be implemented in small cuts rather than one large Spawn system.

### Cut P1 — Contracts and Activity authoring

Objective:

```text
define explicit Activity Slot -> Placement Anchor intent
and Scene-Provided placement policy
```

Expected product surface:

```text
Activity Player Initial Placement authoring
Placement Anchor authoring/marker
validation
designer-first Inspector
```

No runtime mutation beyond the minimum contract wiring if the cut is authoring-only.

### Cut P2 — Scoped runtime placement

Objective:

```text
resolve exact current Activity + Slot placement
apply/observe pose against current Actor occurrence
produce typed placement evidence
```

Integrate after materialization/adoption and before readiness.

### Cut P3 — QA

Prove:

```text
Manager-Provisioned placement
Scene-Provided Preserve
Scene-Provided Apply
missing required anchor
duplicate binding
wrong scope
stale occurrence
no fallback
readiness ordering
```

### Cut P4 — FIRSTGAME product proof

Prove a developer can:

```text
create anchors
see Slot mapping
position them in Scene view
configure both provisioning modes
understand Preserve vs Apply
run the game
observe correct initial pose
diagnose a broken binding
```

Permanent fixes discovered there migrate back to the package.

## Validation requirements

### Contract

```text
Activity placement binds exact Slot -> exact Anchor
duplicate Slot binding rejected
unsupported/invalid Slot rejected
invalid Anchor rejected
cross-Activity Anchor rejected
Anchor scale not applied
Actor not reparented to Anchor
```

### Manager-Provisioned

```text
Player joins
Actor selected
Actor materialized under Actor Mount
Activity anchor resolved from joined Slot
Actor world position/rotation applied
Actor remains under Actor Mount
required readiness completes only after placement
missing anchor blocks preparation
no prefab/origin/previous-position fallback
```

### Scene-Provided Preserve

```text
scene-owned Actor exists under exact Actor Mount
Policy = Preserve Authored Pose
Framework does not alter position/rotation through Initial Placement
placement evidence records Preserve
no Activity Anchor required
physical ownership remains external
```

### Scene-Provided Apply

```text
scene-owned Actor exists
Policy = Apply Activity Placement
exact Slot anchor required
world position/rotation applied before readiness
Actor remains scene-owned
Actor stays in authored ownership hierarchy
missing anchor blocks preparation
```

### Occurrence safety

```text
Activity A occurrence 1 placement evidence
does not satisfy Activity B occurrence 2

released Actor occurrence
does not leave reusable Ready placement evidence
```

### Transition

```text
outgoing gameplay position is not used implicitly
incoming Activity anchor wins
placement occurs before reveal when readiness waits for prepared Actor
```

### Negative

```text
no global lookup
no first-anchor fallback
no world-origin fallback
no silent duplicate resolution
no Scene-Provided movement under Preserve policy
no readiness before required placement
```

### Product

```text
designer can identify the placement surface
designer can identify Player Slot mapping
designer can visually identify anchor orientation
designer understands Actor Mount != Placement Anchor
designer understands Scene-Provided Preserve vs Apply
runtime diagnostic explains resolved pose or exact failure
```

## Acceptance of this architecture cut

```text
Initial Placement is Activity-scoped spatial intent
it is not generic Spawn lifecycle authority
Placement Anchor is position/rotation evidence only
Slot -> Anchor mapping is explicit
one authoritative binding per Slot per Activity
no default/random/first-anchor fallback in the initial contract
Manager-Provisioned contextual Actor uses Activity placement when required
semantic placement target is contextual Actor world pose, not technical Host authority
Scene-Provided explicitly supports Preserve Authored Pose or Apply Activity Placement
Scene-Provided defaults to Preserve Authored Pose
placement occurs after representation exists and before gameplay readiness
required placement failure blocks prepared readiness
placement evidence is Actor-occurrence scoped
Actor is not reparented to the Anchor
Anchor scale is not applied
outgoing Activity world position is not incoming placement policy
Initial Placement is not Reset/Respawn/Checkpoint
no global SpawnManager/registry/service locator is introduced
```

## Suggested commits

Architecture:

```text
docs(architecture): define activity player initial placement authority
```

Future runtime/editor/QA cuts should use separate scoped commits after the ADR is
accepted.
