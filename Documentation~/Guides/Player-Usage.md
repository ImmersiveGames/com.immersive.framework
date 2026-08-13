# Player Usage

Status: Current package/technical surface implemented; ADR-019 certified; ADR-020 accepted/reconciled/implemented with focused Manager-Provisioned public Leave QA certified; current-model FIRSTGAME integration pending  
Last updated: 2026-08-13  
Decision source: `IF-ADR-003`, `IF-ADR-012`, `IF-ADR-015`, `IF-ADR-016`, `IF-ADR-019`, `IF-ADR-020`

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

Session Player occurrence
  one occurrence/revision of a joined Logical Player in that Slot

Actor selection
  selected ActorProfile for a joined Slot occurrence

Logical Actor
  prepared/adopted Actor identity

Physical Actor materialization
  contextual runtime representation for the current Activity

Gameplay admission
  input, Camera and gameplay-action eligibility required by an Activity

Session Player Leave
  explicit termination of one exact joined Session Player occurrence
```

`PlayerInput.playerIndex`, join order, hierarchy order and object name are not
`PlayerSlotId`.

A Slot identity may be reused across time; the Session Player occurrence/revision is what
protects destructive commands from affecting a later Player in the same Slot.

## 2. Choose Host Provisioning

Host Provisioning is a Session-level decision in `PlayerSessionProfile`.

| Mode | Use when | Physical ownership |
|---|---|---|
| Scene Provided | Route/Activity composition already contains the physical Host | consumer scene owns Host/Actor |
| Manager Provisioned | explicit Join creates the Host through `PlayerInputManager` | Session owns admitted Host/`PlayerInput` |

Scene Provided and Manager Provisioned are peer provisioning modes. Single-player versus
multiplayer is scale variation, not a different architecture.

### Session lifetime is not a provisioning mode

Do not add or expect a `Session-Persistent` Host Provisioning option.

IF-ADR-019 defines Session persistence as the canonical lifetime of a joined Logical
Player:

```text
Join once at Session scope
project into zero or more Activities
Leave explicitly through IF-ADR-020 or terminate with Session
```

Physical ownership still differs by provisioning mode.

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

`Supported Slots` is the structural Slot universe and canonical untargeted-Join order.

There is no separate Player provisioning Profile, no independent
Initial/Current/Dynamic Capacity and no per-Slot Host Provisioning override.

### 3.3 Game Application

Configure the Game Application's default Player Session Profile through the current
package surface.

An explicit creation-time `PlayerSessionProfile` replaces the application default
completely:

```text
no field merge
no partial inheritance
invalid explicit override does not fall back silently
```

The runtime resolves effective Session initialization once. Later Profile edits do not
mutate the current Session.

### 3.4 Joining semantics

Untargeted Join remains:

```text
Joining Open
+ one vacant Supported Slot
  -> first eligible vacant Supported Slot in authored order
```

No vacant Slot is an explicit rejection; do not reintroduce Capacity as a second
admission limit.

Joining Open/Closed controls **admission only**. It does not prevent an existing Player
from Leaving.

## 4. Scene-Provided Player

Use Scene Provided when the Host already exists in active composition.

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

Scene-Provided discovery is restricted to explicit Route/Activity composition scope. It
does not use `PlayerInputManager` as a provisioning requirement.

### 4.1 Activity transition

Scene-Provided physical objects are contextual/scene-owned. A joined Logical Player may
survive Activity A while the scene-owned A representation releases, then bind a distinct
scene-owned B representation in Activity B without a second Join.

### 4.2 Session Leave

Scene-Provided contextual `RequestRelease` and Session `Request Leave` are different:

```text
contextual release
  -> releases current scene/Activity participation
  -> Session Player may remain Joined

Session Player Leave
  -> releases current Framework participation authority
  -> ends exact Session Player occurrence
  -> Slot becomes Vacant / Available after required Framework release
  -> external scene-owned Host/Actor remain externally owned
```

Do not destroy an external Scene-Provided object as the semantic proof that the Slot is
vacant.

## 5. Manager-Provisioned Player

Use Manager Provisioned when explicit Join creates the physical Host.

Persistent Application Content contains the explicit provisioning composition,
including `PlayerInputManager` and official package provisioning bindings.

`PlayerInputManager` must use the framework-authorized manual Join path. Do not enable a
second auto-join lane that bypasses Slot admission.

### 5.1 Derived PlayerInputManager limit

```text
serialized PlayerInputManager player limit
=
PlayerSessionProfile.SupportedSlotCount
```

This is derived technical materialization, not domain Capacity. Runtime validates the
bridge and fails explicitly on divergence.

### 5.2 Local Player Host prefab

Typical Host prefab:

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author runtime Slot identity on the Host prefab. Admission associates the Host
with the selected Supported Slot.

### 5.3 Activity exit versus Session Leave

Normal Activity exit:

```text
contextual Actor occurrence releases
Activity-local gameplay/Camera/readiness releases
Session Player remains Joined
Manager-Provisioned Host/PlayerInput remains alive
```

Explicit IF-ADR-020 Leave:

```text
current Activity representation releases when present
Session-owned Manager-Provisioned Host/PlayerInput releases
Session Player occurrence ends
Slot -> Vacant / Available
```

Unity physical `Object.Destroy` observability may settle after the logical terminal Leave
result. Do not use an immediate same-frame Unity-null check as the only semantic proof of
Leave. Technical QA waits the canonical settle and then keeps the strong Host-absent
assertion.

## 6. Scoped consumer access

Route/Activity consumers do not serialize a cross-scene authority reference to persistent
provisioning.

Use official scoped access binding with explicit Route or Activity scope. Missing, wrong,
stale, replaced or disposed scope is an explicit unavailable state. There is no global
fallback.

The scoped access is the consumer path for supported Player commands, including Session
Player Leave. Consumers do not locate internal runtime hosts, contexts or services.

## 7. Public commands and observation

Accepted consumer intent includes:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
Request Leave
```

Other bounded commands such as targeted Join / explicit Actor Selection are governed by
their own reconciliation/implementation state; use the tracker as current authority.

Consumers must not invoke internal equivalents of:

```text
Reserve Slot
Prepare Actor
Materialize Actor
Ensure Gameplay Ready
Reconcile Activity
Release Host directly
Clear Slot directly
```

### 7.1 Request Leave

Request Leave targets a current joined Session Player explicitly.

Semantic request data is:

```text
exact Player Slot
expected current Session Player occurrence/revision
source
reason
```

Use the concrete current package request/result types exposed by the scoped consumer
surface; do not create a parallel consumer DTO.

Expected behavior:

```text
validate Slot + current occurrence
-> release current Activity representation when present
-> release provisioning-specific Session resources
-> terminal Session commit
-> Slot Available / Vacant
```

### 7.2 Leave while Joining is Closed

This is valid:

```text
Joining Closed
P1 Joined
Request Leave P1
  -> succeeds

post Leave
Joining remains Closed
P1 Slot Available
Request Join still blocked
```

Later, when Joining opens again, the vacant Slot may be reused.

### 7.3 Rejoin and stale occurrence safety

Rejoin creates a new Session Player occurrence.

```text
P1 occurrence A
  Leave succeeds

P1 occurrence B
  joins later

stale Leave request for A
  -> rejected
  -> B remains Joined
```

Do not authorize destructive mutation using stable Slot identity alone after reuse is
possible.

### 7.4 Observation is immutable evidence

Observation may expose Session initialization, Joining, Slot occupancy,
occurrence/revision, Host/Actor correlation, materialization, gameplay admission and
Activity state.

A retained summary object is not necessarily live authority. Released/baseline values
such as:

```text
Admission  NotAdmitted
Camera     NotEvaluated
Occupancy  Vacant
```

may remain diagnosable after release. Current authority is determined from operational
state + current scope/occurrence correlation, not merely from a non-null summary.

`PlayerProvisioningStatusBinding` may present read-only status and correlate latest
explicit command result. It is not a mutable result store.

## 8. Actor lifecycle

Join and Actor Selection are separate:

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

Session Player Leave is also separate from Actor selection/preparation. It may release the
current Actor occurrence as one stage, but the terminal operation is Session membership
termination.

## 9. Activity participation and readiness

Activity participation projects the existing Session; it does not own Session
configuration.

A required not-yet-joined Player may intentionally remain:

```text
WaitingForJoin / Preparing
```

With `WaitCovered`, any command required to advance that condition remains reachable
through an external/control-plane path. Framework does not repair unreachable game UI
through fake readiness, auto-Join or timeout success.

### 9.1 Readiness after Leave

When a required current Player Leaves, old occurrence readiness becomes stale and is
invalidated.

For the focused certified policy:

```text
Participation selection  ExplicitSlots
Requirement              GameplayReady
Zero-participant policy  Rejected
```

post-Leave behavior is:

```text
authored Slot projection remains
current Player occurrence absent
Player contribution = WaitingForJoin / Preparing
Activity Ready = false
```

Do not remove the explicit authored Slot merely to make the Activity Ready, and do not
retain stale Ready evidence from the departed occurrence.

## 10. Leave with no current Activity representation

A joined Session Player may validly have no current Activity representation.

```text
Session Player = Joined
Current Activity Representation = Absent
```

Request Leave remains valid. Runtime does not fabricate a representation only to release
it; it proceeds directly to Session/provisioning release.

Released/baseline observation summaries are acceptable and do not form a live gameplay
chain.

## 11. Canonical technical QA

### Full Player QA — ADR-019/current baseline

Entry point:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Recorded ADR-019-era verdict:

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

Serialized command identity remains governed by the dedicated Player serialization QA
record and current package enum/API source.

### ADR-020 focused public Manager-Provisioned Leave

Entry point:

```text
Immersive Framework/QA/Regressions/Player/Run ADR020-H Session Player Leave Public Manager Regression
```

Terminal verdict:

```text
[QA_ADR020_H_LEAVE]
status='Passed'
verdict='ADR020_H_PASS'
cases='26'
proof='PublicLeave,ManagerProvisioned,JoiningClosed,TerminalAvailable,ResourceRelease,ReadinessInvalidation,Rejoin,StaleOccurrence,NoActivityLeave'
```

Important cases include Host/resource release, readiness invalidation, Joining Closed,
rejoin/new occurrence, stale Leave rejection and Leave with no Activity representation.

See:

- `../Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md`
- `../Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md`

### Scene-Provided Session Leave certification

Scene-Provided ownership semantics are normative in IF-ADR-020, but this documentation
closure does not claim a separate Scene-Provided **Session Leave** regression terminal
unless that evidence is added to the tracker/reconciliation later.

## 12. FIRSTGAME real integration

FIRSTGAME is Stage B real-consumer/product proof, not the primary technical smoke harness.

Player proof should use the accepted current package surface and keep provisioning modes
separate before layering participation policy.

For IF-ADR-020, a real consumer should eventually prove:

```text
Player participates in gameplay
normal UI/control surface requests Leave
Activity representation releases
Slot visibly becomes Available
Joining Closed still permits Leave
optional rejoin creates new occurrence
developer can diagnose transition without internal runtime inspection
```

UX observations may justify Inspector/Create/template improvements, but do not alter
Session authority or technical completion by themselves.

## 13. Diagnose in the correct order

```text
Session initialization
Supported Slots / Joining
Logical Player admission
current Session occurrence/revision
physical Host identity
Actor selection
Logical Actor preparation
physical materialization
input eligibility
Camera eligibility
gameplay admission
Activity occurrence/readiness
Leave stage evidence
terminal Slot state
post-release current authority
```

Do not collapse all Player state into one boolean or infer runtime success from authoring
validity alone.

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
- arbitrary persistent Player GameObjects as Session authority;
- direct Slot clearing as Leave;
- destroying a Player GameObject as proof of Slot vacancy;
- Scene-Provided contextual release as Session Leave;
- blocking Leave because Joining is Closed;
- automatically reopening Joining or auto-joining replacement after Leave;
- stable-Slot-only destructive requests that ignore occurrence/revision;
- summary-object existence as proof of current runtime authority;
- mandatory creation tooling without a concrete product need.
