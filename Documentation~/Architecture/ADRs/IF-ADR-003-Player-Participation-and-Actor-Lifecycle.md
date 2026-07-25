# IF-ADR-003 — Logical Player Participation and Actor Lifecycle

Status: Accepted
Last updated: 2026-07-25
Supersedes: Player F45/F49 notes, P3 plans/manifests and product ADRs 0007–0018
Superseded by: none

## Context

Logical Player participation, physical Player origin, Slot identity, Actor
selection, Actor lifetime, materialization, Activity readiness and scene-owned
objects are related but distinct authorities.

Earlier documentation often used `Player` as shorthand for several different
things:

```text
Session participant
PlayerInput host
selected Actor identity
Logical Actor
materialized gameplay object
presentation
```

That shorthand obscures which part is actually provided by each Player source and
causes validation and authoring rules to require the wrong objects.

## Canonical terminology

```text
PlayerSlotProfile / PlayerSlotId
  stable participation seat

Logical Player
  Session participant associated with one PlayerSlotId
  independent of Actor, materialization and gameplay readiness

Local Player Host
  optional physical Unity Input System host
  commonly contains PlayerInput and LocalPlayerHostAuthoring

ActorProfile / ActorProfileId
  immutable selectable Actor identity

Logical Actor / ActorId
  contextual Actor identity and runtime state associated with a Logical Player

Actor materialization / presentation
  concrete gameplay and visual content prepared or adopted for the Actor

Activity participation intent
  projected Logical Players plus progressive contextual readiness requirements
```

A Logical Player is the common result of all accepted local Player sources.

```text
Logical Player
  does not imply Local Player Host
  does not imply Actor selection
  does not imply Logical Actor
  does not imply materialization
  does not imply gameplay readiness
```

Actor selection, Logical Actor preparation and materialization are later,
separate stages. A source may already provide some of those parts, but the
framework must validate and adopt them rather than duplicate them.

## Logical Player sources

The framework accepts exactly three local Logical Player sources in the current
product direction.

### Manager-Provisioned Logical Player

```text
explicit join request
-> reserve ordered PlayerSlot
-> PlayerInputManager manual join
-> validate the created Local Player Host
-> admit one Logical Player
-> bind typed PlayerSlotId
-> commit or explicit rollback
```

`PlayerInputManager` is the technical host provisioner. The framework owns Slot
reservation and Logical Player admission.

The Manager-Provisioned source normally supplies the Local Player Host first. The
framework then prepares any missing Actor, materialization, input, Camera and
gameplay parts according to later policy and Activity requirements.

### Scene-Provided Logical Player

A Route or Activity scene may already contain an object that provides a Logical
Player. The same scene object or hierarchy may also provide:

```text
Local Player Host
Actor selection evidence
Logical Actor
Actor materialization
presentation
```

The framework validates and admits the provided Logical Player into the canonical
participation domain. It adopts valid provided parts and does not instantiate,
destroy, deactivate or duplicate them silently.

Scene-Provided describes the origin of the Logical Player. Physical ownership and
contextual release remain explicit evidence.

#### Canonical Scene-Provided authoring shape

The package authoring model uses two prefab boundaries:

```text
Actor_PlayerSceneProvided
  PlayerActorDeclaration
  Actor-owned gameplay components
  Anchors
  Visual

Player_SceneProvided
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring
  Actor Mount
    Actor_PlayerSceneProvided
```

`ActorProfile.LogicalActorHostPrefab` references
`Actor_PlayerSceneProvided`, never the outer `Player_SceneProvided` composition.

The outer composition is the object a designer places in a scene. It owns the
stable Local Player Host and the Scene-Provided composer. The nested Actor prefab
owns the Logical Actor declaration and Actor-specific gameplay components.

The Scene-Provided composer resolves `LocalPlayerHostAuthoring` from the same
GameObject. The Host is therefore a structural invariant, not another manually
assigned cross-reference.

The composer keeps only the principal authoring intent visible:

```text
Player Slot Profile
Actor Profile
Scene Logical Player Actor
Admission Timing
```

`Apply / Rebuild` validates the nested prefab source and stores typed Actor Profile
evidence inside the composer. It does not add a visible evidence component to the
Actor, reserve a Slot, assign runtime identity or start gameplay.

The generic Local Player Host validator proves only shared host invariants:

```text
explicit same-root PlayerInput
exactly one PlayerInput in the Host hierarchy
explicit child Actor Mount
no second PlayerInput under Actor Mount
```

Whether Actor Mount must be empty or contain one authored Logical Actor is a
source-specific rule owned by the Manager-Provisioned validator or the
Scene-Provided composer validator.

### Session-Persistent Logical Player

Application/Session composition may provide a Logical Player outside any Route or
Activity.

```text
Game Application / Session
  -> Session-Persistent Logical Player
  -> PlayerParticipationRuntimeContext

Route / Activity
  -> project and consume participation
  -> never own the Logical Player identity or Session lifetime
```

The persistent source may provide only the Logical Player or may also provide a
Local Player Host, Actor and materialization. Missing later parts may be prepared by
the framework. Existing valid parts must be adopted rather than rebuilt.

This source is the accepted gap addressed by the next Player product cuts. It is not
declared implemented by this ADR update.

## Single participation authority

All three sources converge into the same Session-scoped authority:

```text
Manager-Provisioned Logical Player
Scene-Provided Logical Player
Session-Persistent Logical Player
  -> PlayerParticipationRuntimeContext
  -> PlayerSlotId
```

There is no second Logical Player runtime, parallel Slot registry or compatibility
participation lane.

Source describes how the Logical Player enters the Session. It does not create a
different participation model.

Physical ownership is a separate dimension. At minimum, diagnostics and contracts
must distinguish:

```text
framework-provisioned physical content
externally scene-owned physical content
session-persistent physical content
```

Source and physical ownership must not be inferred from object name, hierarchy or
Unity player index.

## Slot and Actor authority

`GameApplicationAsset` owns the ordered `PlayerSlotProfile[]` capacity and the
Actor duplicate-selection policy. Allocation is first available by configured
order. `PlayerInput.playerIndex` is diagnostic evidence, never Slot identity.

`PlayerParticipationRuntimeContext` is Session-scoped. It owns allocation,
reservation, joined state and selected `ActorProfile` per Slot. Join and Actor
selection are separate transactions. Selection targets `PlayerSlotId`, supports
revision checks, obeys the explicit duplicate policy and does not by itself create
an `ActorId` or materialize an Actor.

A provided Actor or materialization does not replace Slot or Logical Player
authority. The framework must correlate it explicitly with the admitted Logical
Player and preserve its declared physical ownership.

## Activity participation

An `ActivityAsset` owns its participation configuration inline:

```text
Projection: NoSlots | AllJoinedSlots | ExplicitSlots
Zero-participant policy
Ordered explicit PlayerSlotProfile references when applicable
Requirement: None | JoinedSlots | SelectedActors |
             LogicalActorsPrepared | GameplayReady
```

Projection selects Logical Players through their Slots; requirement defines
progressive contextual evidence. They are not reusable Profile assets. Invalid
combinations fail validation.

An Activity may prepare, adopt and release contextual Actor/materialization state.
It does not create the Session identity of a Manager-Provisioned or
Session-Persistent Logical Player.

Activity-scoped release occurs in reverse dependency order:

```text
gameplay
Camera/input eligibility
contextual Actor materialization or adoption
contextual host evidence when Activity-owned
Slot projection
```

Failures retain typed evidence for explicit retry; no silent rollback or fallback
Slot is allowed.

## Scene ownership and lifecycle scope

A Scene-Provided Player can be physically owned by a Route scene or by an Activity
content scene. Physical scene ownership and participation lifetime are separate:

```text
scene
  owns Host and Actor GameObjects

Activity
  requests and releases participation

PlayerParticipationRuntimeContext
  owns Slot reservation and Joined state
```

The accepted architecture supports both Route and Activity scene origins.

Current automatic lifecycle coverage remains narrower: the implemented admission
participant recognizes Scene-Provided authoring declared through the active
Activity content scene set. A composer located only in a Route Primary Scene is
not yet declared covered or validated by the current runtime implementation.

Route Primary Scene discovery/admission must be closed by an explicit runtime and
QA/FIRSTGAME cut. Documentation must not claim that this path already passes.

## Canonical naming rule

Documentation and new APIs must use:

```text
Logical Player
Manager-Provisioned Logical Player
Scene-Provided Logical Player
Session-Persistent Logical Player
Local Player Host
Logical Actor
Actor materialization
```

`Player` may remain informal shorthand in prose only when the exact meaning is
unambiguous.

Existing implementation class names do not override this vocabulary. Any API or
component rename required to align with these terms must occur in a separate,
explicit migration cut after usage and serialized-reference impact are audited.

## Accepted scope

- One Session-scoped Logical Player participation authority.
- Ordered Slot allocation and Session-persistent Actor selection.
- Manager-Provisioned Logical Player through manual `PlayerInputManager` join.
- Scene-Provided Logical Player admission with explicit physical ownership.
- Session-Persistent Logical Player as an accepted application/session source.
- Activity-owned participation projection and requirement level.
- Contextual Actor preparation or adoption, gameplay admission, Camera/input
  eligibility and reverse-order release.
- Optional Activity-owned Pause intent for one explicitly eligible Logical Player.

## Rejected scope

- Slot, Logical Player or Actor identity inferred from names, paths or Unity player index.
- Pre-authored Slot identity on a generic Local Player Host prefab.
- Automatic join, fallback Slot or first-discovered Actor selection.
- Runtime mutation of Profile assets.
- A second Logical Player participation runtime or parallel Slot authority.
- Treating Actor, materialization or presentation as inherent parts of Logical Player admission.
- Recreating Actor or materialization already validly provided by a source.
- Route/Activity ownership of a Session-Persistent Logical Player identity.
- Multiplayer Pause policy, networking, teams and role quotas in the current cut.
- Claiming Route Primary Scene admission without focused runtime evidence.

## Consequences

Logical Player admission can complete before Actor selection. Selection can persist
across Route/Activity changes while Logical Actors and materialization remain
contextual.

Different sources can provide different physical parts while sharing one
participation authority. The framework composes missing parts and adopts existing
parts without creating duplicate Player semantics.

Activities can gate readiness without owning Session participation.

Scene-Provided prefab composition is explicit and inspectable. Designers place one
outer prefab while retaining separate Host and Actor contracts.

## Current implementation coverage

The P3 participation lane, ordered Slot allocation, Actor selection, inline
Activity participation configuration and gameplay admission contexts exist.

The Manager-Provisioned Logical Player source exists through manual
`PlayerInputManager` provisioning.

The Scene-Provided Logical Player source exists through
`SceneLocalPlayerAdmissionAuthoring`, displayed as the Scene-Provided Player
Composer. The current authoring shape has been validated in FIRSTGAME with:

```text
Local Player Host validation: valid
Apply / Rebuild: valid
Composer validation: valid
nested Actor prefab source: compatible with ActorProfile
internal typed profile evidence: created and valid
```

This proof is authoring-only. It does not prove Play Mode admission from the Route
Primary Scene.

Automatic lifecycle admission is currently declared covered for authoring resolved
through Activity content scene declarations. Route Primary Scene coverage remains a
runtime integration gap.

The Session-Persistent Logical Player source is not yet implemented. Its authoring
surface, admission operation, validation and materialization reconciliation remain
a later product gap.

Current class names still reflect older shorthand in places. This ADR freezes the
canonical terminology but does not rename serialized components or APIs.

## Pending decisions

- Runtime discovery and admission contract for a Scene-Provided Player in the
  active Route Primary Scene.
- Focused QA and FIRSTGAME proof for Route-scene admission and release.
- Exact authoring component and request/result contract for Session-Persistent Logical Player.
- Exact rules for adopting source-provided Actor and materialization evidence.
- API/component rename map and serialized-reference migration strategy.
- Product policy for more than one eligible Logical Player in Activity Pause.
- Network/remote participation and reconnect semantics.
- Explicit Actor replacement transaction after Logical Actor preparation.
