# Player Usage

Status: Current package/technical surface implemented and QA-certified; current-model FIRSTGAME integration pending  
Last updated: 2026-08-09  
Decision source: `IF-ADR-003`, `IF-ADR-012`, `IF-ADR-015`, `IF-ADR-016`

## 1. Product model

The framework owns one Session-scoped Player participation authority.

Keep these concepts separate:

```text
Player Slot
  stable logical seat identified by PlayerSlotId

Local Player Host
  physical local input host, normally containing PlayerInput

Logical Player admission
  association between one Host and one Player Slot

Actor selection
  selected ActorProfile for a joined Slot

Logical Actor
  prepared/adopted Actor identity

Physical Actor materialization
  contextual runtime representation for the current Activity

Gameplay admission
  input, Camera and gameplay-action eligibility required by an Activity
```

`PlayerInput.playerIndex`, join order, hierarchy order and object name are not
`PlayerSlotId`.

## 2. Choose Host Provisioning

Host Provisioning is a Session-level decision in `PlayerSessionProfile`.

| Mode | Use when | Package/QA status |
|---|---|---|
| Scene Provided | Route/Activity composition already contains the physical Host | technically certified |
| Manager Provisioned | explicit Join creates the Host through `PlayerInputManager` | technically certified |
| Session-Persistent | Host/Player lifetime must outlive Route/Activity under a dedicated contract | not currently productized |

Scene Provided and Manager Provisioned are peer provisioning modes. Single versus
Multiplayer is a scale variation, not a different architecture.

## 3. Configure Player Session initialization

### 3.1 Stable Slot Profiles

Create one `PlayerSlotProfile` per supported logical seat.

```text
player.1
player.2
...
```

Slot identity is authored/stable and is not derived from `PlayerInput.playerIndex`.

### 3.2 Player Session Profile

Create one `PlayerSessionProfile` and configure:

```text
Supported Slots
  ordered PlayerSlotProfile references

Initial Joining
  Open or Closed

Host Provisioning
  Scene Provided
  or Manager Provisioned

Actor Resolution
  Resolve Configured Default
  or Leave Unresolved
```

`Supported Slots` is the complete structural Slot universe and canonical Join
order.

There is no separate Player provisioning Profile, no Initial/Current/Dynamic
Capacity and no per-Slot Host Provisioning override.

### 3.3 Game Application

Configure the Game Application's default Player Session Profile through the
current package surface.

An explicit creation-time `PlayerSessionProfile` replaces the application default
completely:

```text
no field merge
no partial inheritance
invalid explicit override does not fall back silently
```

The runtime resolves effective Session initialization once. Later Profile edits
do not mutate the current Session.

### 3.4 Join semantics

```text
Joining Open
+ one vacant Supported Slot
```

Join selects the first vacant Supported Slot in authored order. No vacant Slot is
an explicit rejection; do not reintroduce Capacity as a second admission limit.

## 4. Scene-Provided Player

Use Scene Provided when the Host already exists in the active composition.

Typical shape:

```text
Player_SceneProvided
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring

  Actor Mount
    Actor
      PlayerActorDeclaration
      gameplay components
```

The Host owns `PlayerInput`. The Actor does not become the input Host by accident.

Scene-Provided discovery is restricted to explicit Route/Activity composition
scope. It does not use `PlayerInputManager` as a provisioning requirement.

## 5. Manager-Provisioned Player

Use Manager Provisioned when explicit Join must create the physical Host.

Persistent Application Content contains the explicit provisioning composition,
including `PlayerInputManager` and the official package provisioning bindings.

`PlayerInputManager` must use the framework-authorized manual Join path. Do not
enable a second auto-join lane that bypasses Slot admission.

### 5.1 Derived PlayerInputManager limit

```text
serialized PlayerInputManager player limit
=
PlayerSessionProfile.SupportedSlotCount
```

This is derived technical materialization, not domain Capacity.

Runtime validates the final bridge and fails explicitly on divergence.

### 5.2 Local Player Host prefab

Typical Host prefab:

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author runtime Slot identity on the Host prefab. Admission associates
the Host with the selected Supported Slot.

## 6. Scoped consumer access

Route/Activity consumers do not serialize a cross-scene authority reference to
persistent provisioning.

Use the official scoped access binding with explicit Route or Activity scope.
Missing, wrong, stale, replaced or disposed scope is an explicit unavailable
state. There is no global fallback.

## 7. Public commands and observation

Accepted commands:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

Consumers must not invoke internal equivalents of:

```text
Reserve Slot
Prepare Actor
Materialize Actor
Ensure Gameplay Ready
Reconcile Activity
```

Observation is immutable evidence and may expose Session initialization, Joining,
Slot occupancy, Host/Actor correlation, materialization, gameplay admission and
Activity occurrence/revision data.

`PlayerProvisioningStatusBinding` may present read-only status and correlate the
latest explicit command result. It is not a global mutable result store.

## 8. Actor lifecycle

Join and Actor Selection are separate.

```text
Host available / created
-> Slot admitted
-> Actor selected
-> Logical Actor prepared
-> physical Actor materialized
-> gameplay admitted
-> Activity Player contribution reaches required level
```

Successful Join is not proof that later Actor/gameplay stages already completed.

## 9. Activity participation and readiness

Activity participation projects the existing Session; it does not own Session
configuration.

A required not-yet-joined Player may intentionally remain
`WaitingForJoin / Preparing`.

With `WaitCovered`, any command required to advance that condition must remain
reachable through an external/control-plane path. The framework does not repair
an unreachable game UI through fake readiness, auto-Join or timeout success.

## 10. Canonical technical QA

Entry point:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Current executed verdict:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
serialization='PASS'
session='PASS'
sceneProvided='PASS'
managerProvisioned='PASS'
actor='PASS'
publicSurface='PASS'
participation='PASS'
```

Serialized command identity is part of the certification:

```text
10 OpenJoining
20 CloseJoining
30 retired / unsupported
40 RequestJoin
50 RequestDefaultActorSelection
```

See `../Architecture/IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md`.

## 11. FIRSTGAME real integration

Current committed FIRSTGAME Player assets still contain parts of the superseded
Capacity/separate-provisioning-Profile authoring shape and are not current-model
integration proof.

The next real integration should reauthor against the current package surface,
keeping provisioning modes separate before layering participation policy.

Suggested proof order:

```text
Scene-Provided
  single Route-owned
  single Activity-owned
  multiplayer variation

Manager-Provisioned
  single
  multiplayer / late Join

Participation policies
  after the underlying provisioning mode works in the real game
```

FIRSTGAME integration counts toward functional feature proof. UX observations
found during this work are qualitative. They may justify a smaller Inspector,
Create action, Template or other product improvement, but additional tooling is
not a functional completion requirement.

## 12. Session-Persistent Player

Session-Persistent is not currently available as a canonical workflow.

Do not simulate it by putting an arbitrary Player prefab in Persistent Content.
Persistence of a GameObject alone does not establish Session admission, Slot
authority, Actor correlation or lifecycle semantics.

## 13. Diagnose in the correct order

```text
Session initialization
Supported Slots / Joining
Logical Player admission
physical Host identity
Actor selection
Logical Actor preparation
physical materialization
input eligibility
Camera eligibility
gameplay admission
Activity occurrence/readiness
```

Do not collapse all Player state into one boolean or infer runtime success from
authoring validity alone.

## 14. Anti-patterns

Do not add:

- static Host/provisioning access;
- global service locators;
- scene-wide name/tag lookup;
- `playerIndex` -> Slot conversion;
- silent fallback to another Slot/Profile/scope;
- Capacity as a second Session limit;
- separate Player provisioning Profile;
- per-Slot Host Provisioning overrides;
- automatic Actor replacement outside policy;
- consumer-side prepare/materialize/reconcile calls;
- cross-scene serialized authority references;
- mandatory creation tooling without a concrete product need.
