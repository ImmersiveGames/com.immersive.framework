# IF-ADR-016 — Player Session Initial Configuration and Provisioning Profiles

Status: **Proposed**  
Last updated: 2026-08-08  
Implementation completion: **0% for the ADR-specific product surface**  
Implementation classification: **Normative shape proposed; underlying Session/Player runtime foundations already exist, but canonical Session/Profile initialization assets and flow are not yet implemented**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-010, IF-ADR-012, IF-ADR-015  
Supersedes: none  
Superseded by: none

> This ADR defines authored Player Session initialization intent. It does not create a new Player runtime authority and does not replace the command/observation boundary proposed by IF-ADR-015.

---

## Context

The framework already has substantial Session-scoped Player runtime behavior:

```text
Session-scoped Player participation
Slot identity and admission
Scene-Provided Player sources
Manager-Provisioned Player sources
Player Host provisioning
Actor selection
Logical Actor preparation
physical Actor materialization
contextual gameplay admission
Activity Player readiness contribution
Session revision / Activity occurrence reconciliation
```

IF-ADR-001 establishes `Game Application / Session` as the owner of Session-scoped authorities and Logical Players. Route and Activity own contextual lifecycle and projection, not Session participant identity.

IF-ADR-002 establishes `Recipe / Profile / Template` as reusable authored intent above a scoped runtime authority.

IF-ADR-003 establishes the Player lifecycle separation between Slot configuration, Join, Logical Player participation, Actor selection, logical preparation, physical materialization, gameplay admission, readiness and contextual reconciliation.

IF-ADR-012 separately owns Activity Player participation authoring and normalized effective Activity policy.

IF-ADR-015 proposes the canonical consumer-facing Player provisioning command and immutable observation boundary for runtime use.

The remaining architectural gap is earlier in the lifecycle:

```text
How is the Player Session configured before the Session runtime exists?
```

Specifically, the framework does not yet have one canonical authored contract that answers:

```text
which Player Slot identities are structurally supported by the Session;
what the initial runtime Capacity is;
whether Joining initially begins open or closed;
which deterministic Slot allocation policy is used;
which Player provisioning intent applies to the Session;
how GameApplication supplies a reusable default;
how a creation-time override replaces that default;
when authored configuration stops being authority.
```

Without this boundary, a consumer is pushed toward manually assembling low-level Player configuration or treating contextual Route/Activity authoring as if it owned Session state.

---

## Decision

The Immersive Framework defines two reusable authored intent assets:

```text
Player Session Profile
Player Provisioning Profile
```

Their canonical conceptual responsibilities are:

```text
GameApplication
    Default Player Session Profile
                │
                │ default / explicit creation-time override
                ▼
Player Session Profile
    Supported Slots
    Initial Capacity
    Initial Joining Intent
    Slot Allocation Policy
    Player Provisioning Profile
                │
                ▼
Player Provisioning Profile
    Host Provisioning
    Actor Resolution
                │
                ▼
Session creation
    resolve one effective initial configuration
                │
                ▼
Existing Session-scoped runtime authorities
    become the mutable truth
```

The Profile assets declare **initial intent**. They are not live mutable runtime state.

---

## Player Session Profile

`PlayerSessionProfile` is the reusable authored definition of the initial Player Session configuration.

It owns intent equivalent to:

```text
Supported Slots
Initial Capacity
Initial Joining Intent
Slot Allocation Policy
Player Provisioning Profile reference
```

Exact serialized field names and supporting value types may follow existing package vocabulary during implementation, but the semantic boundary above is normative.

### Supported Slots

The Profile declares the structural universe of `PlayerSlotId` values supported by the Session.

```text
Supported Slots
    define which logical Player seats can exist in this Session
```

Supported Slots are not the same as current occupied Players and are not the same as Activity participation projection.

An Activity may project only part of the Session Slot universe, or may include currently vacant supported Slots according to its own accepted Activity policy. That does not redefine the Session Slot universe.

### Initial Capacity

The Profile declares the initial runtime Capacity.

The effective initial Capacity must be bounded by the structural Supported Slots.

Conceptually:

```text
Supported Slots = 4
Initial Capacity = 2
    -> valid initial configuration

runtime Request Capacity 4
    -> may be accepted by Session authority

runtime Request Capacity 5
    -> rejected because it exceeds the structural Session universe
```

The Profile defines the initial value. It does not continuously drive the live value after Session creation.

### Initial Joining Intent

The Profile declares the Session's initial Joining intent.

This is initialization intent only.

After Session creation, Joining is mutable Session state and changes only through explicit runtime capabilities/commands owned by the existing authority.

Temporary runtime blockers/inhibits, if adopted under a separate accepted decision, are not authored by changing this Profile at runtime.

### Slot Allocation Policy

The Profile declares a deterministic Slot allocation order/policy for normal Join.

The canonical policy requirement is:

```text
Request Join
    -> Session authority determines the first available supported Slot
       according to the configured deterministic allocation policy
```

The joining Player/consumer does not become Slot-allocation authority merely by issuing Join.

This ADR does not require one specific storage representation for the allocation policy. It requires deterministic, typed and diagnosable allocation behavior.

---

## GameApplication default and Session creation override

`GameApplication` may reference a Default Player Session Profile.

This reference is an authored default source, not mutable runtime authority.

Canonical resolution:

```text
Session creation request
    explicit Player Session Profile override present?
        yes -> use explicit override
        no  -> use GameApplication default when configured
                │
                ▼
        resolve effective initial configuration once
                │
                ▼
        initialize Session-scoped Player runtime
```

An explicit creation-time override replaces the default as a complete Session Profile source. This ADR does not define field-by-field merge between default and override.

Route or Activity must not act as an implicit override source.

### Missing effective configuration

No silent fallback is allowed.

If a Player Session configuration is required by the selected application/session composition and neither a valid default nor a valid explicit override can be resolved, creation must fail explicitly and diagnostically.

This ADR does not require every possible application composition to enable Player Session configuration. It requires explicit behavior whenever the Player Session feature is part of the composition.

---

## Resolution and runtime authority

Profile resolution occurs at Session creation.

The resulting effective initial configuration is Session-scoped and stable as configuration evidence for that Session.

After initialization:

```text
PlayerSessionProfile
    is not mutable runtime truth

PlayerProvisioningProfile
    is not mutable runtime truth

Session-scoped runtime authorities
    own current mutable state
```

Consequences:

```text
editing/replacing the authored Profile does not silently rewrite an existing Session;
entering a Route does not reapply the Session Profile;
entering an Activity does not reapply the Session Profile;
Activity participation does not raise/lower Session Capacity;
Activity participation does not replace Session provisioning intent;
post-creation Capacity/Joining changes use explicit runtime capabilities;
late joins use the Session's already-resolved provisioning intent.
```

If a future requirement needs a different effective Player Session configuration, it must use an explicit Session lifecycle/configuration contract. It must not be implemented as hidden live synchronization from assets.

---

## Player Provisioning Profile

`PlayerProvisioningProfile` is reusable authored intent for how Player Hosts and initial Actor resolution are provisioned for the Session.

It does **not** become a second provisioning runtime.

It composes two distinct concerns:

```text
Host Provisioning
Actor Resolution
```

### Host Provisioning

The canonical host provisioning modes reuse IF-ADR-003:

```text
Scene Provided
Manager Provisioned
```

This ADR does not redefine their runtime lifecycle. It makes the selected provisioning intent reusable and Session-scoped through the Session Profile.

### Actor Resolution

Host provisioning and Actor resolution remain separate.

The Profile may declare initial Actor-resolution intent equivalent to:

```text
Resolve Configured Default
Leave Unresolved / External
```

`Leave Unresolved / External` is a valid authored state when gameplay or another accepted external flow is responsible for deciding when/why Actor selection is requested.

This does not create a generic framework character-selection flow.

### Provisioning lifetime

The effective Player Provisioning Profile is resolved for the Session and remains stable for that Session, including late joins.

A Route or Activity does not automatically replace it.

Runtime Actor selection, preparation, materialization and contextual admission may evolve according to their existing authorities. Stability of the effective provisioning Profile does not mean that all Player runtime state is immutable.

---

## Authority model

The authoritative relationship is:

```text
PlayerSessionProfile
  authored creation intent

PlayerProvisioningProfile
  authored creation/provisioning intent

Session-scoped Player participation authority
  mutable Slot / Logical Player Session truth

Local Player provisioning runtime
  Host provisioning and Join execution

Actor selection / preparation runtime
  Actor lifecycle truth

Activity-owned Player lifecycle
  contextual physical materialization / release

ADR-015 command surface
  requests supported runtime operations

ADR-015 observation surface
  projects immutable runtime evidence
```

No object introduced by this ADR may become a parallel mutable Player state store.

---

## Relationship to Activity Player participation

This ADR does not replace IF-ADR-012.

Canonical separation:

```text
Player Session Profile
    what Player Session is initialized for the game/session

Activity Player Participation Profile
    how one Activity projects and requires Players from that Session
```

Examples:

```text
Session Supported Slots = 4
Session Current Capacity = 2
Activity projects Slots 1..4

Result:
    Activity projection does not automatically change Capacity to 4.
```

```text
Session Supported Slots = 4
Activity projects Slots 1..2

Result:
    Session still structurally supports Slots 1..4.
```

---

## Relationship to IF-ADR-015

IF-ADR-016 and IF-ADR-015 own different lifecycle boundaries.

```text
IF-ADR-016
    authored initial configuration
    default/override resolution
    Session initialization intent
    effective provisioning intent lifetime

IF-ADR-015
    runtime consumer commands
    immutable runtime observation
    cross-scene consumer reachability
    command/status authoring
```

IF-ADR-016 must not expose internal commands equivalent to:

```text
Reserve Slot
Mutate Slot
Prepare Actor
Materialize Actor
Ensure Gameplay
Reconcile Activity
Mutate readiness
```

Those remain governed by existing runtime authority and IF-ADR-015 boundaries.

Changing a Profile asset is not a substitute for an ADR-015 runtime command.

---

## Product authoring direction

The canonical minimum product experience is:

```text
Assets/Create/Immersive Framework/Player/Player Session Profile
Assets/Create/Immersive Framework/Player/Player Provisioning Profile
```

`GameApplication` authoring should expose:

```text
Player Session
  Default Player Session Profile
```

`Player Session Profile` normal Inspector should prioritize:

```text
Supported Slots
Initial Capacity
Initial Joining Intent
Slot Allocation
Player Provisioning Profile
Validation
```

`Player Provisioning Profile` normal Inspector should prioritize:

```text
Host Provisioning
Actor Resolution
Validation
```

Advanced / Debug may expose:

```text
resolved source/default/override evidence
effective Session configuration identity
resolved provisioning mode
Slot identities / allocation ordering
runtime correlation when Play Mode evidence is available
```

The normal Inspector must not expose internal runtime modules as the primary configuration workflow.

### Apply / Rebuild

The two Profiles themselves are authored assets and do not inherently require an `Apply/Rebuild` operation when no technical materialization is needed.

Where Manager-Provisioned Player requires concrete persistent composition, the existing IF-ADR-002/IF-ADR-015 Recipe/Composer direction remains authoritative. IF-ADR-016 must not create a second competing Composer solely to mirror Profile data.

---

## Validation requirements

Validation must be explicit and non-mutating.

At minimum, implementation must validate applicable structural contradictions such as:

```text
Initial Capacity exceeds Supported Slots
invalid/duplicate Supported Slot identity
missing required Player Provisioning Profile
unsupported provisioning configuration
invalid configured default Actor reference when default resolution is required
ambiguous or invalid allocation configuration
required effective Session Profile cannot be resolved at Session creation
```

The framework must not silently:

```text
invent Slots
increase Capacity
open Joining
replace an invalid Profile with another asset
switch Scene Provided to Manager Provisioned
select an arbitrary Actor
rewrite Activity participation
```

merely to make an invalid configuration run.

Exact validation rules must be limited to contradictions the framework can prove structurally.

---

## Diagnostics requirements

Session initialization must produce typed, correlated evidence sufficient to answer:

```text
Which Player Session Profile source was selected?
Was it GameApplication default or explicit override?
Which effective Supported Slots were resolved?
What Initial Capacity was applied?
What Initial Joining Intent was applied?
Which allocation policy was resolved?
Which Player Provisioning Profile was resolved?
Which Host provisioning mode was selected?
Which Actor-resolution intent was selected?
Did initialization succeed or fail, and why?
```

Diagnostics are evidence, not another authority.

---

## Architectural constraints

- Runtime authority remains scoped, typed and lifetime-explicit.
- `FrameworkRuntimeHost` remains the internal application/session composition root defined by IF-ADR-001.
- No public static current-Session/Player registry is introduced.
- No service locator, scene-wide lookup, hierarchy/name inference or reflection is introduced as consumer authority.
- No second mutable Player state store is introduced.
- Profile assets do not continuously synchronize runtime state.
- Route/Activity do not implicitly mutate Session configuration.
- Missing required configuration fails explicitly and diagnostically.
- Editor tooling is designer-first, idempotent where actions exist, non-destructive and Advanced/Debug-capable.
- QA proves the package contract; FIRSTGAME proves real consumer usability.

---

## Out of scope

This ADR does not define:

```text
Session Player Leave
device disconnect / reconnect
Session-Persistent Player source
network multiplayer/session replication
Activity Player participation semantics
Activity readiness calculation
Player physical-presence Activity policy
runtime Join inhibit model
runtime command/observation reachability
public Slot/Host/Actor assignment snapshot shape
Actor replacement rules after preparation
character-selection UX
Player movement/input mapping
Route/Activity Profile inheritance
```

Those remain with existing ADRs or require separate decisions.

---

## Rejected alternatives

### Live Profile authority

Rejected:

```text
PlayerSessionProfile remains synchronized with live Session state.
```

Reason: violates authored-intent versus runtime-authority separation and makes asset mutation an implicit runtime command channel.

### Route/Activity-owned Session configuration

Rejected:

```text
entering Route/Activity automatically reapplies Capacity, Joining or provisioning Profile.
```

Reason: gives contextual lifecycle hidden lateral mutation over Session-owned state.

### Second Player manager/state store

Rejected:

```text
PlayerSessionManager / PlayerProvisioningManager
that mirrors existing Slot/Player/Actor truth.
```

Reason: creates competing mutable authority.

### Field-by-field default/override merge

Rejected for the initial ADR scope:

```text
GameApplication default Profile
+ arbitrary per-field Session creation overrides
```

Reason: creates provenance and partial-authority ambiguity. Initial override selects a complete Session Profile source.

### Runtime configuration by editing ScriptableObjects

Rejected:

```text
change Profile fields during Play Mode to issue Capacity/Joining/Actor operations.
```

Reason: commands must remain explicit requests to runtime authorities.

### Duplicate Player Activity Profile responsibilities

Rejected:

```text
Player Session Profile contains Activity participation/readiness policy.
```

Reason: IF-ADR-012 already owns Activity Player participation.

---

## Initial implementation order

This ADR does not itself authorize implementation details, but the expected package-first sequence is:

```text
IF-SESSION-CONFIG-01
  exact source/type reuse audit
  freeze namespaces and value types

IF-SESSION-CONFIG-02
  PlayerProvisioningProfile authored contract
  validation

IF-SESSION-CONFIG-03
  PlayerSessionProfile authored contract
  Supported Slots / Capacity / Joining / allocation / provisioning reference
  validation

IF-SESSION-CONFIG-04
  GameApplication default reference
  explicit creation-time override contract
  effective configuration resolution

IF-SESSION-CONFIG-05
  Session initialization integration
  reuse existing Session/Player authorities
  typed initialization diagnostics

IF-SESSION-CONFIG-06
  Create menus + designer-first Inspectors + Advanced/Debug

IF-SESSION-CONFIG-07
  QAFramework public/authoring initialization suite

IF-SESSION-CONFIG-08
  FIRSTGAME manual consumer proof

IF-SESSION-CONFIG-09
  coordinate runtime mutations/observation with IF-ADR-015
  documentation and closure audit
```

Each cut must remain small enough to prove that no second authority or silent fallback was introduced.

---

## QA requirements

QAFramework must prove at least:

```text
GameApplication default resolves correctly
explicit creation override takes precedence as a complete Profile
Profile is consumed once for Session initialization
editing/replacing authored source does not silently rewrite live Session
Supported Slots are stable structural identities
Initial Capacity within bounds succeeds
Initial Capacity above Supported Slots fails explicitly
initial Joining intent is applied once
runtime Capacity/Joining changes occur only through supported runtime requests
Activity projection does not mutate Session Capacity
Scene Provided provisioning intent resolves to existing accepted lifecycle
Manager Provisioned provisioning intent resolves to existing accepted lifecycle
Actor default resolution and unresolved/external intent remain distinct
late Join uses the Session's effective provisioning intent
Route/Activity changes do not replace the effective provisioning Profile
invalid required configuration has typed diagnostic failure
```

Tests must not prove the feature by directly mutating private Session state.

---

## FIRSTGAME requirements

FIRSTGAME must prove the real consumer workflow, not internal failure injection.

A minimum demonstration should allow a new consumer to:

```text
create a Player Provisioning Profile
choose Scene Provided or Manager Provisioned intent
choose default Actor resolution or unresolved/external intent
create a Player Session Profile
configure Supported Slots
configure Initial Capacity
configure Initial Joining Intent
configure Slot allocation
assign the Provisioning Profile
assign the Session Profile as GameApplication default
enter Play Mode
observe that the Session was initialized from that authored intent
use normal public runtime commands for later Capacity/Joining changes
change Route/Activity without implicit Session reconfiguration
```

FIRSTGAME must not contain the permanent implementation of Session Profile resolution.

---

## Acceptance criteria

This ADR may move to **Accepted** when the decision boundary is approved and the implementation proves:

```text
one canonical Player Session Profile exists
one canonical Player Provisioning Profile exists
GameApplication can provide the default Session Profile
explicit creation-time override is typed and complete
Profile resolution occurs once at Session creation
Session runtime remains the mutable authority afterward
Supported Slots have explicit stable typed identity
Initial Capacity is bounded by Supported Slots
Joining initial intent is explicit
Slot allocation is deterministic and diagnostic
Scene Provided / Manager Provisioned reuse existing Player lifecycle authority
Host provisioning remains separate from Actor resolution
Actor may resolve a configured default or remain explicitly unresolved/external
effective provisioning intent remains Session-scoped for late joins
Route/Activity do not silently replace Session/provisioning configuration
no second Player manager/state store exists
no global lookup/service locator/reflection path exists
invalid required configuration fails explicitly
Editor flow is understandable without internal-contract assembly
QA proves technical initialization behavior
FIRSTGAME proves real manual consumer usability
runtime commands/observation remain coordinated with IF-ADR-015 rather than duplicated
```

---

## Consequences

### Positive

```text
Session initialization becomes authorable and reusable.
GameApplication gains a clear default source without becoming mutable Player authority.
Player provisioning intent becomes reusable across Sessions/games.
Session and Activity Player configuration stop competing for ownership.
Late joins inherit one explicit Session provisioning intent.
Runtime command semantics remain separate from authored initialization.
The product moves from internal contract assembly toward designer-facing configuration.
```

### Costs

```text
new authored assets and validation must be maintained;
Session creation needs explicit effective-configuration evidence;
existing ad-hoc Player setup may require migration;
Manager-Provisioned product tooling must be coordinated carefully with IF-ADR-015;
QA must distinguish initialization tests from runtime command tests.
```

---

## Completion interpretation

```text
Normative status
  Proposed

Existing runtime foundation
  substantial under IF-ADR-001 / IF-ADR-003

ADR-specific authored Session configuration
  not yet implemented

Player Provisioning Profile
  not yet canonicalized as this ADR defines it

GameApplication default Player Session Profile
  not yet canonicalized as this ADR defines it

Runtime command/observation surface
  separate IF-ADR-015 work
```

The next work should implement the authored initialization boundary in the official package without expanding it into another runtime Player authority.

---

## Suggested commit message

```text
docs(architecture): add ADR-016 player session initial configuration
```
