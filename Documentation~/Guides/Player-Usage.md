# Player Usage

Status: R1/R2 implemented; Unity validation pending  
Last updated: 2026-08-09  
Decision source: `IF-ADR-015`, `IF-ADR-016`

## 1. Product model

The framework owns one Session-scoped Player participation authority:

```text
PlayerParticipationRuntimeContext
```

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
  prepared, instantiated or adopted Actor identity

Physical Actor materialization
  contextual runtime representation for the current Activity

Gameplay admission
  input, Camera and gameplay-action eligibility required by an Activity
```

`PlayerInput.playerIndex`, join order, hierarchy order and object name are not `PlayerSlotId`.

## 2. Choose the Logical Player source

| Source | Use when | Product status |
|---|---|---|
| Manager-Provisioned | an explicit join creates a physical Host through `PlayerInputManager` | **Package surface implemented and QA-certified; FIRSTGAME manual product proof pending** |
| Scene-Provided | a Route or Activity scene already contains the Host and Logical Actor | Stable product subset; FIRSTGAME-validated |
| Session-Persistent | Logical Player identity must outlive Route and Activity scopes | Architecture accepted; runtime/product workflow not available |

Implemented sources converge into the same Session Player authority and typed `PlayerSlotId` model.

## 3. Configure Session Player initialization

For Session-enabled Player configuration, use the IF-ADR-016 Profile chain. Do **not** configure the canonical Slot roster by adding legacy Slot entries directly to `GameApplicationAsset`.

### 3.1 Create stable Slot Profiles

Create one `PlayerSlotProfile` per supported logical seat.

```text
PlayerSlotProfile: player.1
PlayerSlotProfile: player.2
...
```

Slot identity is authored/stable. It is not derived from `PlayerInput.playerIndex`.

### 3.2 Create the Player Session Profile

Create a `PlayerSessionProfile` and configure:

```text
Supported Slots
  ordered PlayerSlotProfile references

Initial Joining Open
Host Provisioning
  Scene Provided
  or Manager Provisioned

Actor Resolution
  Resolve Configured Default
  or Leave Unresolved
```

The Supported Slot order is the canonical initialization order and the complete
structural Slot universe. A Slot may remain vacant and be occupied by a later
Join. There is no Initial/Current/Dynamic Capacity and no per-Slot Host
Provisioning override.

### 3.3 Link the Game Application

On `GameApplicationAsset`:

```text
Player Session Enabled = true
Default Player Session Profile = <PlayerSessionProfile>
```

If Session creation supplies an explicit `PlayerSessionProfile` override, that override replaces the application default **completely**:

```text
no field merge
no partial inheritance
invalid explicit override does not fall back silently
```

The runtime resolves effective initialization evidence once for the created
Session. Later changes to the Profile do not mutate that Session. Joining is
runtime state; a Join requires Joining Open and a vacant Supported Slot.

R1/R2 implement this authoring shape and remove the separate runtime Capacity
mechanics. Joining remains governed by Joining Open plus a vacant Supported
Slot; do not add compatibility authoring around the removed model.

## 4. Configure Activity participation

Configure each `ActivityAsset` according to the participation/readiness contract it actually needs.

Readiness progression is cumulative:

```text
Joined Slots
→ Selected Actors
→ Logical Actors Prepared
→ Gameplay Ready
```

Use the lowest readiness level the Activity genuinely requires.

For externally driven Manager-Provisioned entry, `WaitCovered` is valid:

```text
Required Player not joined
→ WaitingForJoin / NotReady
→ loading remains covered and non-terminal

public join + Actor progression completes
→ readiness becomes Ready
→ loading/reveal may complete
```

Do not configure a gameplay-only control path that becomes unreachable because the destination is intentionally still covered.

## 5. Scene-Provided Logical Player

Use Scene-Provided when the Route Primary Scene or an Activity content scene already owns the physical Host and Actor.

Canonical shape:

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

The Actor prefab does not own `PlayerInput`. The outer Host owns `PlayerInput` and an explicit Actor Mount.

The Inspector surface is presented as the **Scene-Provided Player Composer**. Configure exact `PlayerSlotProfile`, `ActorProfile`, scene Actor declaration and admission timing. `Apply / Rebuild` is authoring/materialization support only; it does not reserve a runtime Slot or execute gameplay in Edit Mode.

The current runtime supports Scene-Provided authoring in the active Route Primary Scene and active Activity content scenes. It consumes explicit Game Flow lifecycle context and does not infer authority from names/tags/first-found objects.

On Activity/Route release, contextual admission/Host/Actor/gameplay evidence is released according to the Scene-Provided contract. Session/diagnostic snapshots should be used to verify release rather than assuming a missing Hierarchy object proves it.

## 6. Manager-Provisioned Logical Player

Use Manager-Provisioned when an explicit join must create the physical Host.

### 6.1 Persistent Application Content

Configure one explicit provisioning composition:

```text
PlayerInputManager
LocalPlayerProvisioningAuthoring
LocalPlayerProvisioningHostRegistration
LocalPlayerActorSelectionRequestAuthoring   # when public default selection is required
```

`PlayerInputManager` must use the framework-authorized manual join path. Do not enable an independent automatic join lane that bypasses Slot reservation/admission.

On `LocalPlayerProvisioningAuthoring`, configure the `PlayerInputManager` and Local Player Host Prefab.

The authored Local Player Host Prefab is the product authority. Framework boot may materialize it into `PlayerInputManager.playerPrefab` only under the package's explicit compatibility rule; divergent existing values fail diagnostically rather than being silently overwritten.

### 6.2 Local Player Host Prefab

Use a Host prefab shaped like:

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author `PlayerSlotId` on the Host prefab. Runtime admission binds the reserved Slot.

### 6.3 Scoped consumer access in Route / Activity content

Do not serialize a cross-scene reference from Route/Activity UI directly to the persistent provisioning authority.

Author a:

```text
LocalPlayerProvisioningConsumerAccessBinding
```

and choose the explicit scope:

```text
Route
or
Activity
```

The Framework binds this component to the current live scope. Missing, wrong, stale, replaced or disposed scopes remain explicit unavailable states; there is no global lookup fallback.

### 6.4 Public commands

A consumer may use `ILocalPlayerProvisioningConsumerAccess` directly or author a `PlayerProvisioningCommandTrigger` for explicit designer invocation.

Supported provisioning commands:

```text
Open Joining
Close Joining
Request Join
```

`Request Join` succeeds only when Joining is open and one Supported Slot is
vacant. Capacity commands are not part of the accepted model.

Default Actor selection uses the separate public Actor-selection surface:

```text
LocalPlayerActorSelectionRequestAuthoring.RequestDefaultActorSelection(...)
```

`PlayerProvisioningCommandTrigger` can expose that operation as an explicit authoring option while still delegating to the correct public boundary.

Do not expose or call internal operations equivalent to Slot reservation, Actor preparation/materialization, gameplay admission or Activity reconcile from game UI.

### 6.5 Public observation

The scoped consumer endpoint provides immutable current observation through `TryGetObservation`.

Use it to inspect, as applicable:

```text
initialization configuration evidence
current joining/participation state and Supported Slot occupancy
Session revision / applied revision
Activity owner / occurrence
per-Slot joined state
Host correlation
selected Actor
logical preparation
physical materialization
gameplay admission
```

Observation is not authority and does not mutate state.

### 6.6 Status / diagnostics binding

For an authorable read-only status surface, use:

```text
PlayerProvisioningStatusBinding
```

It projects the public observation and can optionally correlate the last explicit `PlayerProvisioningCommandTrigger` result. It does not create a global result store.

Use normal Inspector fields for product status and Advanced / Debug for technical correlation such as revisions, owner/occurrence evidence and detailed Slot lifecycle.

### 6.7 Intended join lifecycle after the migration

The normative flow is:

```text
Session initialized from PlayerSessionProfile
→ Route/Activity consumer binding becomes available
→ Activity enters and may wait for required Player
→ Open Joining
→ Request Join
→ PlayerInputManager creates Host
→ Slot/Host admission commits
→ Request Default Actor Selection
→ normal preparation/materialization/gameplay admission
→ Player contribution reaches Ready
→ WaitCovered loading terminates
```

Activity exit releases Activity-owned projection/materialization while preserving Session-owned join/Host state when that is the contract. Reentry creates a newer Activity occurrence without duplicating Slot/Actor state.

### 6.8 Negative semantics

The migrated public surface must reject explicitly:

```text
join while closed rejected
no vacant Supported Slot rejected
repeated Open/Close no-change
missing/wrong/destroyed/stale scoped access unavailable
stale Actor selection revision rejected
repeated default selection stable
unbound ActivityRequestTrigger fails explicitly
```

No silent fallback is part of the product contract.

### 6.9 Current product status

```text
Package P1–P4
  implemented

QA-PLAYER-SURFACE-01
  PASS 29/29

QA-PLAYER-SURFACE-02
  PASS 36/36

Joint technical verdict
  PLAYER SURFACE QA CERTIFIED

FIRSTGAME manual consumer proof
  pending

P5 creation workflow / tooling disposition
  pending after FIRSTGAME
```

Manual explicit composition is the canonical baseline for the upcoming FIRSTGAME proof. A Wizard/Composer is **not mandatory**. If real usage demonstrates recurring friction, P5 may add the smallest justified Create-menu, Inspector, template or Composer support. `NO ADDITIONAL TOOLING REQUIRED` is a valid outcome.

## 7. Session-Persistent Logical Player

This source is not currently available as a product workflow.

Do not simulate it by placing an arbitrary Player prefab in Persistent Content. Persistence of a GameObject alone does not establish Session admission, Slot authority, Actor correlation, contextual lifecycle or reconciliation.

## 8. Pause integration

Physical Pause input belongs to the official Player:

```text
PlayerInput
UnityPlayerInputGateAdapter
PausePlayerInputBinding
```

`Global` is an action map of that `PlayerInput`, not a second global Player.

Application-only Pause controls may work without an admitted Player binding where the Pause contract allows it. See `Pause-Usage.md`.

## 9. Gameplay Camera integration

A Player gameplay Camera request is separate from the persistent physical Camera Output.

Inside the Actor hierarchy, gameplay Camera authoring publishes contextual requests/eligibility. Persistent Application Content owns output/arbitration. Player/Activity release must release the contextual request according to scope.

See `Camera-Usage.md`.

## 10. Diagnose in the correct order

Inspect separate evidence rather than collapsing all Player state into one boolean:

```text
Session initialization configuration
current Slot roster/capacity/joining
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

## 11. Anti-patterns

Do not add:

- static Host/provisioning access;
- global service locators;
- scene-wide name/tag lookup;
- `FindObjectOfType` authority discovery;
- `playerIndex` → Slot conversion;
- silent fallback to another Slot/Profile/scope;
- automatic Actor replacement outside policy;
- consumer-side prepare/materialize/reconcile calls;
- hidden release repair;
- mutable diagnostic snapshots as a second authority;
- cross-scene serialized references to persistent Player authorities;
- mandatory creation tooling before real consumer evidence demonstrates the need.
