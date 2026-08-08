# IF-ADR-016 — Player Session Initial Configuration and Provisioning Profiles

Status: **Proposed**  
Last updated: 2026-08-08  
Implementation completion: **80% for the ADR-specific scope**  
Implementation classification: **Package contracts, authored Profiles, pure resolution, Session runtime initialization, designer-first Inspector/diagnostics and QA contract closure are implemented. FIRSTGAME consumer proof is intentionally deferred. The typed complete creation-time Session Profile override remains an explicit implementation gap until verified/implemented.**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-010, IF-ADR-012, IF-ADR-015  
Supersedes: none  
Superseded by: none

> This ADR defines authored Player Session initialization intent. The initialization decision set was frozen on 2026-08-08. It does not create a new Player runtime authority and does not replace the command/observation boundary proposed by IF-ADR-015.

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
which existing Player Slot definitions are structurally supported by the Session and in which authored order;
what the initial runtime Capacity is;
whether Joining initially begins open or closed;
how the fixed first-available Slot allocation rule uses the declared Supported Slots order;
how Host provisioning intent resolves per supported Slot, including mixed Scene-Provided / Manager-Provisioned Sessions;
how GameApplication supplies a reusable default;
how a creation-time override replaces that default;
how an intentionally absent Player Session feature differs from missing required configuration;
when authored structural configuration stops being authority;
what initialization facts are retained without turning Profiles into a live event/state channel.
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
    Optional Default Player Session Profile
                │
                │ default / explicit complete creation-time override
                ▼
Player Session Profile
    Ordered Supported PlayerSlotProfile references
    Initial Capacity
    Initial Joining Intent
    Player Provisioning Profile
                │
                ▼
Player Provisioning Profile
    Default Host Provisioning
    per-Slot Host Provisioning Overrides
    Actor Resolution
                │
                ▼
Session creation
    resolve + validate one effective structural configuration
    resolve exactly one effective Host provisioning mode per supported Slot
                │
                ▼
Existing Session-scoped runtime authorities
    become the mutable runtime truth

Initialization evidence
    records resolved source + effective structural facts
    does not become a mutable state/event authority
```

The Profile assets declare **initial intent**. They are not live mutable runtime state.

### Frozen initialization decisions

The following decisions are normative for IF-ADR-016:

| # | Decision | Normative result |
|---|---|---|
| 1 | Slot definition reuse | `PlayerSessionProfile` references existing ordered `PlayerSlotProfile` definitions; it does not create a parallel Slot schema. |
| 2 | Feature absence vs invalid configuration | Player Session may be intentionally absent from a composition; when enabled/required, missing or invalid effective configuration fails explicitly. |
| 3 | Join Slot allocation | Normal Join selects the first available Supported Slot in authored order. No generic allocation-strategy abstraction is introduced in this ADR scope. |
| 4 | Structural lifetime | Supported Slots and effective per-Slot provisioning are frozen at Session creation; mutable runtime state remains owned by existing Session/Player authorities. |
| 5 | Configuration / observation boundary | Profiles do not live-drive runtime state. Initialization retains/publishes typed immutable facts only; it does not add a general event bus or second state mirror. |
| 6 | Provisioning granularity | Host provisioning resolves per Supported Slot. Mixed Scene-Provided / Manager-Provisioned Sessions are valid through an explicit default plus per-Slot overrides, with no runtime fallback between modes. |

---

## Player Session Profile

`PlayerSessionProfile` is the reusable authored definition of the initial Player Session configuration.

It owns intent equivalent to:

```text
Ordered Supported PlayerSlotProfile references
Initial Capacity
Initial Joining Intent
Player Provisioning Profile reference
```

Exact serialized field names and supporting value types may follow existing package vocabulary during implementation, but the semantic boundary above is normative.

### Supported Slots and authored order

The Profile declares the structural universe of supported Player Slots by referencing the framework's existing reusable Player Slot definitions, conceptually `PlayerSlotProfile` references.

```text
Supported Slots
    = ordered references to existing Player Slot definitions
    = structural seats that may exist in this Session
```

IF-ADR-016 must not introduce a parallel Slot schema or copy stable Slot identity, Actor/default configuration, or equivalent Slot definition data into `PlayerSessionProfile` when the existing Player Slot definition already owns that information.

The declared collection order is normative. It is both authoring evidence and the deterministic order used by normal Join allocation.

Supported Slots are not the same as current occupied Players and are not the same as Activity participation projection.

An Activity may project only part of the Session Slot universe, or may include currently vacant supported Slots according to its own accepted Activity policy. That does not redefine or reorder the Session Slot universe.

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

### Join Slot allocation

Normal Join uses one canonical allocation rule in this ADR scope:

```text
Request Join
    -> iterate Supported Slots in their authored order
    -> select the first currently available supported Slot
    -> Session authority accepts or rejects the Join
```

The joining Player/consumer does not choose a Slot and does not become Slot-allocation authority merely by issuing Join.

IF-ADR-016 does **not** introduce a generic/configurable Slot allocation strategy abstraction. If a future product requirement needs a different algorithm, that requires an explicit decision rather than an unused strategy surface added in advance.

Assigned stable Slot identity does not renumber because another Slot becomes occupied, vacant, enabled by Capacity, or unavailable.

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

### Feature enablement versus missing effective configuration

The framework must distinguish these states explicitly:

```text
Player Session feature is not part of the selected application/session composition
    -> valid absence; no Player Session initialization is requested

Player Session feature is enabled/required by the selected composition
but no valid effective Player Session Profile can be resolved
    -> explicit configuration failure
```

No silent fallback is allowed. An absent/disabled Player Session feature must not be auto-enabled, and an enabled/required feature must not invent, discover, or substitute another Profile to make creation appear valid.

This ADR does not require every possible application composition to enable Player Session configuration. It requires explicit behavior whenever the Player Session feature is part of the composition.

---

## Resolution and runtime authority

Profile resolution occurs at Session creation.

The resulting effective initial configuration is Session-scoped and stable as configuration evidence for that Session.

The following structural configuration is frozen for the Session lifetime:

```text
ordered Supported Slots / PlayerSlotProfile references
effective per-Slot Host provisioning method
effective provisioning-profile provenance
other creation-time structural facts resolved by this ADR
```

The following remain mutable runtime state under existing Session/Player authorities:

```text
Current Capacity
Joining state
Slot occupancy / Joined state
Host lifecycle state
Actor selection / preparation state
physical materialization state
gameplay admission state
contextual Activity projection and readiness evidence
```

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
Activity participation does not replace per-Slot Session provisioning intent;
post-creation Capacity/Joining changes use explicit runtime capabilities;
late joins use the already-resolved provisioning intent for their assigned Slot;
Slots outside Current Capacity remain structurally configured supported Slots.
```

Initialization may retain/publish typed facts sufficient to correlate which source and effective structural values were applied. Those facts are immutable evidence. They must not become a general event bus, live Profile synchronization mechanism, or second mutable Player state store.

If a future requirement needs a different structural Player Session configuration, it must use an explicit Session lifecycle/configuration contract, normally a new Session or another separately approved lifecycle mechanism. It must not be implemented as hidden live synchronization from assets.

---

## Player Provisioning Profile

`PlayerProvisioningProfile` is reusable authored intent for how supported Player Slots obtain Player Hosts and how initial Actor resolution is interpreted for the Session.

It does **not** become a second provisioning runtime.

It composes two distinct concerns:

```text
Host Provisioning
Actor Resolution
```

### Host Provisioning is resolved per supported Slot

The canonical host provisioning modes reuse IF-ADR-003:

```text
Scene Provided
Manager Provisioned
```

The provisioning method is **not** one exclusive mode for the whole Session. It is resolved independently for every supported Slot while preserving one Session-scoped Player authority.

Canonical authored shape:

```text
Player Provisioning Profile
    Default Host Provisioning
        Scene Provided | Manager Provisioned

    Slot Host Provisioning Overrides
        PlayerSlotProfile / PlayerSlotId -> Scene Provided | Manager Provisioned
```

Example:

```text
Default Host Provisioning = Manager Provisioned

Slot Overrides
    P1 -> Scene Provided

Effective Session provisioning
    P1 -> Scene Provided
    P2 -> Manager Provisioned
    P3 -> Manager Provisioned
    P4 -> Manager Provisioned
```

The authored default is explicit reusable intent. It is **not** a runtime fallback.

At Session creation the framework must:

```text
resolve every Supported Slot in declared order
apply an explicit Slot override when one exists
otherwise apply the authored default
validate that exactly one Host provisioning method is effective for each Supported Slot
freeze that effective per-Slot provisioning intent for the Session lifetime
```

Mixed Sessions are valid. Scene-Provided and Manager-Provisioned Slots may coexist in the same Session.

A Slot override:

```text
must reference a Slot contained in Supported Slots;
must be unique for that Slot;
must not duplicate or replace the underlying PlayerSlotProfile definition;
must resolve to exactly one supported Host provisioning method.
```

The effective method describes **how that Slot obtains its Host**. It does not move ownership away from the Session or create a per-Slot Player authority.

Failure of one provisioning method must not silently attempt another method. For example, missing Scene-Provided binding must not fall back to Manager-Provisioned creation, and Manager-Provisioned failure must not search for a Scene-Provided Host.

This ADR does not redefine either mode's runtime lifecycle. It makes their per-Slot authored intent reusable and Session-scoped through the Session Profile.

### Actor Resolution

Host provisioning and Actor resolution remain separate.

The Profile may declare initial Actor-resolution intent equivalent to:

```text
Resolve Configured Default
Leave Unresolved / External
```

When `Resolve Configured Default` is used, implementation must reuse the existing Slot/Actor definition contracts rather than copy Actor references into a parallel ADR-016 Slot schema. Per-Slot default Actor data, when already owned by the referenced Player Slot definition, remains owned there.

`Leave Unresolved / External` is a valid authored state when gameplay or another accepted external flow is responsible for deciding when/why Actor selection is requested.

This does not create a generic framework character-selection flow.

### Provisioning lifetime

The effective Player Provisioning Profile source and the resulting **per-Slot effective Host provisioning map** are resolved for the Session and remain stable for that Session, including late joins.

A Route or Activity does not automatically replace them.

Runtime Actor selection, preparation, materialization and contextual admission may evolve according to their existing authorities. Stability of the effective provisioning configuration does not mean that all Player runtime state is immutable.

---

## Authority model

The authoritative relationship is:

```text
PlayerSessionProfile
  authored creation intent

PlayerProvisioningProfile
  authored default + per-Slot Host provisioning overrides
  authored Actor-resolution intent

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
  ordered PlayerSlotProfile references
Initial Capacity
Initial Joining Intent
Player Provisioning Profile
Validation
```

The Supported Slots list order is the visible normal-Join allocation order. There is no separate strategy selector in this ADR scope.

`Player Provisioning Profile` normal Inspector should prioritize:

```text
Default Host Provisioning
Slot Host Provisioning Overrides
Actor Resolution
Validation
```

Advanced / Debug may expose:

```text
resolved Session Profile source: default / explicit override
effective Session configuration identity
ordered effective Slot identities
per-Slot Host provisioning mode
per-Slot provisioning provenance: default / Slot override
effective Actor-resolution intent
initialization result / reason
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
null, duplicate or invalid Supported PlayerSlotProfile reference
duplicate stable Slot identity across Supported Slots
missing required Player Provisioning Profile
missing Default Host Provisioning
Slot provisioning override references a Slot outside Supported Slots
multiple provisioning overrides target the same Slot
one or more Supported Slots cannot resolve exactly one effective Host provisioning method
Scene Provided Slot is missing its required accepted binding/composition
Manager Provisioned Slot is missing its required accepted provisioning composition
unsupported provisioning configuration
invalid configured default Actor reference when default resolution is required
required effective Session Profile cannot be resolved at Session creation
```

The framework must not silently:

```text
invent or copy Slots into a parallel schema
increase Capacity
open Joining
replace an invalid Profile with another asset
switch Scene Provided to Manager Provisioned
switch Manager Provisioned to Scene Provided
select an arbitrary Actor
reorder Supported Slots at runtime to make Join succeed
rewrite Activity participation
```

merely to make an invalid configuration run.

Exact validation rules must be limited to contradictions the framework can prove structurally.

---

## Diagnostics requirements

Session initialization must produce typed, correlated evidence sufficient to answer:

```text
Was Player Session initialization intentionally absent for this composition,
or was it required and invalid?
Which Player Session Profile source was selected?
Was it GameApplication default or explicit override?
Which ordered effective Supported Slots were resolved?
What Initial Capacity was applied?
What Initial Joining Intent was applied?
Which Player Provisioning Profile was resolved?
For each Supported Slot, which Host provisioning mode was resolved?
For each overridden Slot, did the effective value come from default or explicit Slot override?
Which Actor-resolution intent was selected?
Did initialization succeed or fail, and why?
```

Diagnostics are immutable evidence, not another authority. Initialization evidence may be observed or logged through typed framework diagnostics, but IF-ADR-016 does not introduce a general-purpose event bus or mutable configuration mirror.

---

## Architectural constraints

- Runtime authority remains scoped, typed and lifetime-explicit.
- `FrameworkRuntimeHost` remains the internal application/session composition root defined by IF-ADR-001.
- No public static current-Session/Player registry is introduced.
- No service locator, scene-wide lookup, hierarchy/name inference or reflection is introduced as consumer authority.
- No second mutable Player state store is introduced.
- Profile assets do not continuously synchronize runtime state.
- Ordered Supported Slots and effective per-Slot provisioning are frozen structural configuration for the Session lifetime.
- Runtime Capacity, Joining and Player/Actor lifecycle state remain mutable only through their existing authorities and explicit capabilities.
- Initialization evidence is read-only evidence, not a general event bus or mutable state mirror.
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
configurable/general-purpose Slot allocation strategies
runtime switching of a Slot between Scene Provided and Manager Provisioned
field-by-field per-Slot Actor-resolution override policy beyond existing Slot/Actor contracts
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

### Duplicate Slot definition schema

Rejected:

```text
PlayerSessionProfile copies PlayerSlotId / Actor/default / Slot definition data
instead of referencing existing PlayerSlotProfile definitions.
```

Reason: creates two authored owners for stable Slot definition and makes provenance/migration ambiguous.

### Generic Slot allocation strategy in the initial scope

Rejected:

```text
PlayerSessionProfile exposes an allocation-strategy selector or strategy asset
without a concrete second product requirement.
```

Reason: normal Join already has a deterministic product rule: first available Supported Slot in authored order. A strategy abstraction would add extension surface before a justified alternative exists.

### Session-wide exclusive Host provisioning mode

Rejected:

```text
PlayerProvisioningProfile chooses Scene Provided or Manager Provisioned once
and forces every Supported Slot to use the same mode.
```

Reason: provisioning origin is Slot-centered. Mixed Sessions are valid while Session remains the single Player authority.

### Runtime provisioning fallback

Rejected:

```text
Scene Provided fails -> try Manager Provisioned
Manager Provisioned fails -> discover/use Scene Provided
```

Reason: changes authored intent implicitly, hides configuration errors and makes provisioning provenance non-diagnostic.

### Live structural reconfiguration from Profile assets

Rejected:

```text
editing Supported Slots or provisioning overrides during a Session
silently rewrites the current Session structure.
```

Reason: structural configuration is creation-time intent. Runtime mutations need explicit authority/lifecycle contracts.

### General initialization event bus

Rejected:

```text
IF-ADR-016 adds a mutable/global event channel that mirrors Session Player state.
```

Reason: initialization requires typed facts and diagnostics, not a second observation/state authority. Runtime observation remains coordinated with IF-ADR-015.

### Duplicate Player Activity Profile responsibilities

Rejected:

```text
Player Session Profile contains Activity participation/readiness policy.
```

Reason: IF-ADR-012 already owns Activity Player participation.

---

## Initial implementation order

The package-first sequence is now tracked as:

```text
IF-SESSION-CONFIG-01  CLOSED
  canonical contracts and reuse boundary

IF-SESSION-CONFIG-02  CLOSED
  PlayerProvisioningProfile authored contract

IF-SESSION-CONFIG-03  CLOSED
  PlayerSessionProfile authored contract

IF-SESSION-CONFIG-04  CLOSED
  pure effective configuration resolver

IF-SESSION-CONFIG-05  CLOSED
  GameApplication default + Session runtime initialization
  structural configuration frozen for Session lifetime
  mixed per-Slot provisioning integrated with existing authorities

IF-SESSION-CONFIG-06  CLOSED FOR CURRENT PACKAGE UX CUT
  designer-first Inspectors + Advanced/Debug diagnostics
  further UX refinement intentionally deferred to FIRSTGAME consumer proof

IF-SESSION-CONFIG-07  CLOSED / QA CERTIFIED
  CONFIG-05 runtime integration smoke: 6/6 PASS
  CONFIG-07 contract closure smoke: 17/17 PASS

IF-SESSION-CONFIG-08  DEFERRED BY PRIORITY
  FIRSTGAME manual consumer proof

IF-SESSION-CONFIG-09  PARTIAL
  architecture/plan/completion documentation refreshed
  ADR acceptance remains blocked by remaining acceptance gaps
```

### Remaining implementation gap before ADR acceptance

The normative ADR still requires a typed **creation-time Session Profile override** that replaces the GameApplication default as one complete source, without field-by-field merge. The implementation evidence produced through IF-SESSION-CONFIG-05 reports only:

```text
GameApplicationAsset
  PlayerSessionEnabled
  DefaultPlayerSessionProfile
```

Therefore the complete Session-creation override must remain **OPEN** until the current package source proves that the contract already exists or a focused package cut implements it. Do not infer this capability from per-Slot provisioning overrides.

The real Route/Activity non-reapply behavior is ADR-aligned and contract-separated, but the current Edit Mode QA suite intentionally does not certify a full ActivityFlow transition using `FrameworkRuntimeHost`. This is an integration-evidence gap, not evidence of a second authority.

---

## QA requirements

QAFramework must prove at least:

```text
Player Session feature intentionally absent is a valid no-initialization composition
Player Session required + no effective Profile fails explicitly
GameApplication default resolves correctly
explicit creation override takes precedence as a complete Profile
Profile is consumed once for Session initialization
editing/replacing authored source does not silently rewrite live Session
Supported Slots reuse existing PlayerSlotProfile definitions without duplicate Slot authority
Supported Slots preserve authored deterministic order
normal Join selects the first available supported Slot in that declared order
assigned stable Slot identity does not renumber
Initial Capacity within bounds succeeds
Initial Capacity above Supported Slots fails explicitly
Slots outside Current Capacity remain structurally configured
initial Joining intent is applied once
runtime Capacity/Joining changes occur only through supported runtime requests
Activity projection does not mutate Session Capacity
Scene Provided provisioning intent resolves to existing accepted lifecycle
Manager Provisioned provisioning intent resolves to existing accepted lifecycle
mixed Scene-Provided + Manager-Provisioned Slots resolve in one Session
Default Host Provisioning applies to non-overridden Supported Slots
per-Slot Host provisioning override wins only for its referenced Supported Slot
override targeting unsupported Slot fails explicitly
duplicate Slot override fails explicitly
provisioning failure never falls back to the other Host provisioning mode
Actor default resolution and unresolved/external intent remain distinct
late Join uses the effective provisioning intent frozen for its assigned Slot
Route/Activity changes do not replace the effective provisioning configuration
initialization evidence is immutable and does not become a second mutable Player state store
invalid required configuration has typed diagnostic failure
```

Tests must not prove the feature by directly mutating private Session state.

---

## FIRSTGAME requirements

FIRSTGAME must prove the real consumer workflow, not internal failure injection.

A minimum demonstration should allow a new consumer to:

```text
reuse/create the existing PlayerSlotProfile definitions needed by the game
create a Player Provisioning Profile
choose the Default Host Provisioning
optionally override Host provisioning for specific supported Slots
prove at least one mixed Session when the sample contains both Scene-Provided and Manager-Provisioned Slots
choose default Actor resolution or unresolved/external intent
create a Player Session Profile
assign ordered Supported PlayerSlotProfile references
configure Initial Capacity
configure Initial Joining Intent
assign the Provisioning Profile
assign the Session Profile as GameApplication default
enter Play Mode
observe which effective provisioning method was resolved per Slot
observe that normal Join uses first available Slot in the declared Supported Slots order
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
Player Session intentionally absent is distinct from required-but-invalid configuration
explicit creation-time override is typed and complete
Profile resolution occurs once at Session creation
Session runtime remains the mutable authority afterward
Supported Slots reference existing reusable PlayerSlotProfile definitions without parallel Slot schema
Supported Slots preserve explicit authored order and stable typed identity
Initial Capacity is bounded by Supported Slots
Joining initial intent is explicit
normal Join selects the first available Supported Slot in authored order
no generic Slot allocation strategy abstraction is introduced without a new requirement
structural Supported Slots and effective per-Slot provisioning are frozen for Session lifetime
Current Capacity / Joining / occupancy / Host / Actor lifecycle remain explicit mutable runtime state
Scene Provided / Manager Provisioned reuse existing Player lifecycle authority
Host provisioning is resolved exactly once per Supported Slot at Session creation
mixed Scene-Provided / Manager-Provisioned Sessions are supported
Default Host Provisioning + explicit per-Slot overrides have deterministic provenance
no provisioning failure silently falls back to the other Host provisioning mode
Host provisioning remains separate from Actor resolution
Actor may resolve a configured default or remain explicitly unresolved/external
late Join uses the effective provisioning intent frozen for its assigned Slot
Route/Activity do not silently replace Session/provisioning configuration
initialization publishes/retains typed immutable facts, not a general event bus or state mirror
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
Existing PlayerSlotProfile definitions remain the authored Slot-definition source instead of being duplicated.
Supported Slot ordering becomes both visible authoring intent and deterministic Join allocation order.
Player provisioning intent becomes reusable across Sessions/games and supports mixed Scene-Provided / Manager-Provisioned Slots.
Per-Slot provisioning provenance is explicit and stable for the Session lifetime.
Session and Activity Player configuration stop competing for ownership.
Late joins inherit the explicit provisioning intent already frozen for their assigned Slot.
Runtime command semantics remain separate from authored initialization.
Initialization diagnostics provide facts without adding a general event/state bus.
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

ADR-specific package contracts
  IMPLEMENTED
  Effective configuration/value contracts
  PlayerProvisioningProfile
  PlayerSessionProfile
  pure deterministic resolver

GameApplication default Player Session Profile
  IMPLEMENTED

Session runtime initialization
  IMPLEMENTED
  configuration consumed once
  structural Slot/provisioning facts frozen
  existing Session authority remains mutable runtime truth
  mixed Scene-Provided / Manager-Provisioned supported

Designer-first authoring / diagnostics
  IMPLEMENTED FOR CURRENT CUT
  further UX polish deferred to FIRSTGAME manual proof

QA
  IF-SESSION-CONFIG-05: PASS 6/6
  IF-SESSION-CONFIG-07: PASS 17/17
  public/internal classification explicit

Creation-time complete Session Profile override
  OPEN / NOT YET PROVEN IMPLEMENTED
  must replace GameApplication default as a complete source
  must not field-merge

Route/Activity real non-reapply integration evidence
  NOT DIRECTLY CERTIFIED by current Edit Mode smoke

FIRSTGAME consumer proof
  DEFERRED BY PRIORITY

Runtime command/observation surface
  separate IF-ADR-015 work
```

The next ADR-016 implementation work should first close or disprove the creation-time override gap with a focused source audit/package cut. FIRSTGAME remains the final product-usability proof and is intentionally postponed while higher-value package implementation continues.

---

## Suggested commit message

```text
docs(architecture): add ADR-016 player session initial configuration
```
