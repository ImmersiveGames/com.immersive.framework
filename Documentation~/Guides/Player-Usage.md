# Player Usage

Status: Current Player technical surface implemented and QA-certified; FIRSTGAME manual product proof pending  
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

`PlayerInput.playerIndex`, join order, hierarchy order and object name are not `PlayerSlotId`.

## 2. Choose Host Provisioning

Host Provisioning is a Session-level decision in `PlayerSessionProfile`.

| Mode | Use when | Current status |
|---|---|---|
| Scene Provided | a Route/Activity composition already contains the physical Host | **Technical QA certified** |
| Manager Provisioned | an explicit Join creates the Host through `PlayerInputManager` | **Technical QA certified** |
| Session-Persistent | Player identity/Host lifetime must outlive Route/Activity under a dedicated persistent contract | **Not currently productized** |

Scene Provided and Manager Provisioned are peer provisioning modes. Single vs Multiplayer is a scale variation, not a different architecture.

## 3. Configure Player Session initialization

### 3.1 Create stable Slot Profiles

Create one `PlayerSlotProfile` per supported logical seat.

```text
PlayerSlotProfile: player.1
PlayerSlotProfile: player.2
...
```

Slot identity is authored/stable. It is not derived from `PlayerInput.playerIndex`.

### 3.2 Create the Player Session Profile

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

`Supported Slots` is the complete structural Slot universe and canonical Join order.

There is no separate Player provisioning Profile, no Initial/Current/Dynamic Capacity and no per-Slot Host Provisioning override.

### 3.3 Link the Game Application

On `GameApplicationAsset` configure the Player Session default Profile through the current package surface.

An explicit creation-time `PlayerSessionProfile` override replaces the application default completely:

```text
no field merge
no partial inheritance
invalid explicit override does not fall back silently
```

The runtime resolves effective Session initialization once. Later Profile edits do not mutate the current Session.

### 3.4 Join semantics

A normal Join requires:

```text
Joining Open
+ one vacant Supported Slot
```

The framework selects the first vacant Supported Slot in authored order.

If no Supported Slot is available, the request is rejected explicitly. Do not reintroduce Capacity as a second admission limit.

## 4. Scene-Provided Player

Use Scene Provided when the Host already exists in the active composition.

Typical shape:

```text
Player_SceneProvided
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring

  Actor Mount
    Actor_PlayerSceneProvided
      PlayerActorDeclaration
      gameplay components
```

The outer Host owns `PlayerInput`. The Actor does not become the input Host by accident.

Scene-Provided discovery is composition-scoped:

```text
Route scope
  all RouteOwnedScenes

Activity scope
  Route scope
  + matching Activity-owned scenes
```

Primary Scene remains relevant to lifecycle/loading/identity, but component discovery is not Primary-Scene-only.

Scene-Provided does not use `PlayerInputManager` as a provisioning requirement.

Release/reentry behavior must be observed through typed Player/Activity evidence, not inferred only from whether a Hierarchy object exists.

## 5. Manager-Provisioned Player

Use Manager Provisioned when explicit Join must create the physical Host.

### 5.1 Persistent Application Content

Configure one explicit provisioning composition:

```text
PlayerInputManager
LocalPlayerProvisioningAuthoring
LocalPlayerProvisioningHostRegistration
LocalPlayerActorSelectionRequestAuthoring   # when default Actor selection is used
```

`PlayerInputManager` must use the framework-authorized manual Join path. Do not enable an independent auto-join lane that bypasses Slot admission.

### 5.2 Derived PlayerInputManager limit

For Manager Provisioned, the serialized Input System player limit is derived from the Session structure:

```text
PlayerInputManager player limit
=
PlayerSessionProfile.SupportedSlotCount
```

This is materialized technical configuration, not domain Capacity.

The Editor product action may apply the derived value. Runtime validates the final configuration and fails explicitly on divergence; it does not silently invent a different Session limit.

### 5.3 Local Player Host prefab

Typical Host prefab:

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author runtime Slot identity on the Host prefab. Admission associates the Host with the selected Supported Slot.

### 5.4 Scoped consumer access

Do not serialize a cross-scene reference from Route/Activity UI directly to the persistent provisioning authority.

Author:

```text
LocalPlayerProvisioningConsumerAccessBinding
```

with explicit scope:

```text
Route
or
Activity
```

Missing, wrong, stale, replaced or disposed scopes remain explicit unavailable states. There is no global lookup fallback.

### 5.5 Public commands

The accepted command vocabulary is:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

`Request Join` succeeds only when Joining is open and one Supported Slot is vacant.

Default Actor selection remains a separate Actor-selection boundary, even when exposed through a designer command trigger.

Consumers must not call internal equivalents of:

```text
Reserve Slot
Prepare Actor
Materialize Actor
Ensure Gameplay Ready
Reconcile Activity
```

### 5.6 Public observation

Use the scoped immutable observation surface to inspect, as applicable:

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

Observation is evidence, not authority.

### 5.7 Status / diagnostics binding

For authorable read-only status, use:

```text
PlayerProvisioningStatusBinding
```

It may project current observation and correlate the latest explicit command result. It must not become a global mutable result store.

Use normal Inspector fields for product status and Advanced / Debug for technical correlation.

## 6. Actor lifecycle

Join and Actor Selection are separate.

Canonical progression:

```text
Host available / created
→ Slot admitted
→ Actor selected
→ Logical Actor prepared
→ physical Actor materialized
→ gameplay admitted
→ Activity Player contribution reaches the required level
```

Do not treat successful Join as proof that the Actor is already selected, prepared, materialized or gameplay-ready.

## 7. Activity participation and readiness

Activity participation projects the existing Session. It does not own Session configuration.

Readiness can intentionally wait for a required Player:

```text
Required Player not joined
→ WaitingForJoin / Preparing
```

With `WaitCovered`, a valid control path must remain capable of issuing the required Join/selection while the destination remains covered.

The framework does not repair an unreachable game UI by faking readiness, automatic Join, timeout success or weakening participation requirements.

## 8. Canonical technical QA

The current Player surface is technically certified by QAFramework.

Normal entrypoint:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Certified phases:

```text
Player Session                         PASS
Scene-Provided                        PASS
Manager-Provisioned                   PASS
Actor lifecycle                       PASS
Public Player Surface                 PASS
Activity Participation integration    PASS

PLAYER QA CERTIFIED
```

Representative case counts:

```text
Player Participation Authoring        7
Scene-Provided route/negative matrix  25
Manager public contract               9
Manager waiting projection            14
Actor selection runtime binding       13
Player gameplay admission             114
Public Surface Q1                     28
Public Surface Q2                     36
Activity Session Projection           30
```

Q2 intentionally emits framework errors for expected rejected/unavailable cases. The authoritative result is the regression/master verdict.

See `../Architecture/IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md`.

## 9. FIRSTGAME product proof

Technical QA is not the final product criterion. FIRSTGAME should prove manual creation/configuration/understanding in this order:

```text
Demo02 — Scene-Provided Player
  Single / Route-Owned
  Single / Activity-Owned
  Multiplayer

Demo03 — Manager-Provisioned Player
  Single
  Multiplayer / late Join
```

Participation policies should be layered after the underlying provisioning mode is understandable.

P5 creation tooling comes after this evidence. A Wizard/Composer is not mandatory; `NO ADDITIONAL TOOLING REQUIRED` is a valid result if manual authoring is clear enough.

## 10. Session-Persistent Player

Session-Persistent is not currently available as a canonical product workflow.

Do not simulate it by placing an arbitrary Player prefab in Persistent Content. Persistence of a GameObject alone does not establish Session admission, Slot authority, Actor correlation or lifecycle/reconciliation semantics.

## 11. Diagnose in the correct order

Inspect separate evidence rather than collapsing all Player state into one boolean:

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

Do not infer runtime success from authoring validity alone.

## 12. Anti-patterns

Do not add:

- static Host/provisioning access;
- global service locators;
- scene-wide name/tag lookup;
- `FindObjectOfType` authority discovery;
- `playerIndex` → Slot conversion;
- silent fallback to another Slot/Profile/scope;
- Capacity as a second Session limit;
- separate Player provisioning Profile;
- per-Slot Host Provisioning overrides;
- automatic Actor replacement outside policy;
- consumer-side prepare/materialize/reconcile calls;
- hidden release repair;
- mutable diagnostic snapshots as a second authority;
- cross-scene serialized references to persistent Player authorities;
- mandatory creation tooling before real consumer evidence demonstrates the need.
