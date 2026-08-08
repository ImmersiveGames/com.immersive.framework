# Immersive Framework — ADR-016 Incremental Implementation Plan

**Date:** 2026-08-08  
**Scope:** Player Session Initial Configuration and Provisioning Profiles  
**Reference:** IF-ADR-016 — Player Session Initial Configuration and Provisioning Profiles

---

## Objective

Transform ADR-016 into small, independently compilable and verifiable implementation cuts, without anticipating tooling or runtime behavior before the underlying contracts are stable.

Target flow:

```text
GameApplication
    Player Session Enabled
    Default PlayerSessionProfile
        ↓
PlayerSessionProfile
    ordered Supported Slots
    Initial Capacity
    Initial Joining
    PlayerProvisioningProfile
        ↓
pure resolution / validation
        ↓
Effective Player Session Configuration
        ↓ once
existing Session-scoped runtime
        ↓
existing provisioning / Player lifecycle
        ↓
diagnostics / public observation
```

Recommended sequence:

```text
01 Source + Contracts
        ↓
02 Provisioning Profile
        ↓
03 Session Profile
        ↓
04 Effective Resolver
        ↓
05 Session Runtime Integration
        ↓
06 Product Inspector / Diagnostics
        ↓
07 QA Contract Closure
        ↓
08 FIRSTGAME Consumer Proof
        ↓
09 Documentation / ADR Acceptance
```

Two gates are mandatory:

1. Do not integrate with runtime before the resolver is stable.
2. Do not invest in sophisticated tooling before the runtime integration is proven.

## Implementation status — 2026-08-08

| Cut | Status | Evidence / note |
|---|---|---|
| IF-SESSION-CONFIG-01 | **CLOSED** | canonical effective contracts added |
| IF-SESSION-CONFIG-02 | **CLOSED** | `PlayerProvisioningProfile` authoring added |
| IF-SESSION-CONFIG-03 | **CLOSED** | `PlayerSessionProfile` authoring added |
| IF-SESSION-CONFIG-04 | **CLOSED** | pure deterministic resolver added |
| IF-SESSION-CONFIG-05 | **CLOSED** | runtime integration; QA smoke **6/6 PASS** |
| IF-SESSION-CONFIG-06 | **CLOSED for current UX cut** | designer-first Inspectors/diagnostics; fine UX polish deferred to FIRSTGAME |
| IF-SESSION-CONFIG-07 | **CLOSED** | contract-closure smoke **17/17 PASS** |
| IF-SESSION-CONFIG-08 | **DEFERRED** | manual FIRSTGAME proof intentionally postponed |
| IF-SESSION-CONFIG-09 | **PARTIAL** | docs refreshed; ADR remains Proposed |

### Open ADR-016 package gap discovered during closure

The normative ADR requires an explicit creation-time `PlayerSessionProfile` override that **replaces the GameApplication default as one complete source**. Current implementation evidence only reports GameApplication enablement plus `DefaultPlayerSessionProfile`. This must be audited and, if absent, implemented as a focused package cut before ADR acceptance. Per-Slot provisioning overrides do not satisfy this requirement.

### Evidence intentionally not overclaimed

The current Edit Mode QA proves Session-vs-Activity contract separation but does not execute a full Route/Activity transition through `FrameworkRuntimeHost`; real non-reapplication through ActivityFlow remains integration evidence to collect later.

---

# IF-SESSION-CONFIG-01 — Canonical Contracts and Reuse Boundary

**Type:** Technical / Package

## Objective

Freeze the minimum types that represent the effective ADR-016 configuration and confirm which existing canonical types are reused.

## Scope

Confirm/reuse from current source:

```text
PlayerSlotProfile
PlayerSlotId
existing Actor/default Actor ownership
existing Scene-Provided contracts
existing Manager-Provisioned contracts
existing Joining/Capacity runtime authority
```

Create only missing value types necessary to represent the effective configuration, conceptually:

```text
HostProvisioningMode
ActorResolutionPolicy

EffectivePlayerSlotProvisioning
EffectivePlayerSessionConfiguration

PlayerSessionInitializationResult / Failure
```

Final names must follow current package terminology.

## Out of scope

Do not create yet:

```text
PlayerSessionProfile
PlayerProvisioningProfile
GameApplication fields
custom Inspector
runtime provisioning
Composer
ADR-015 consumer access
```

## Expected files

Primarily:

```text
Runtime/PlayerParticipation/Contracts/
```

with reuse of:

```text
Runtime/PlayerParticipation/Authoring/PlayerSlotProfile.cs
```

No removals expected.

## Expected smoke

Prove that synthetic effective configurations:

- preserve Slot order;
- are immutable/read-only;
- represent provisioning per Slot;
- allow Scene-Provided and Manager-Provisioned to coexist;
- have no Editor dependency;
- do not introduce mutable runtime authority.

## Technical acceptance

The package compiles and exposes a runtime-safe representation of the future resolution result.

## Product acceptance

None yet. This is intentionally a non-designer-facing cut.

## Architectural gain

Creates the boundary:

```text
authored intent
    ↓
effective immutable configuration
    ↓
existing runtime
```

## Suggested commit

```text
feat(player): add player session initialization contracts
```

---

# IF-SESSION-CONFIG-02 — Player Provisioning Profile

**Type:** UX/Product + Authored Contract

## Objective

Create the first reusable intent asset from ADR-016:

```text
PlayerProvisioningProfile
```

## Scope

The asset should express:

```text
Default Host Provisioning
    Scene Provided
    Manager Provisioned

Slot Overrides
    PlayerSlotProfile -> Host Provisioning

Actor Resolution
    Resolve Configured Default
    Leave Unresolved / External
```

Provide `CreateAssetMenu`.

## Validation

Detect structurally:

- duplicate override;
- null Slot;
- invalid Host mode;
- invalid Actor Resolution;
- locally contradictory structures.

Validation requiring knowledge of the supported Slot universe belongs to the resolver.

## Out of scope

The asset does not execute provisioning.

It must not:

- create Host;
- Join;
- prepare Actor;
- materialize Actor.

## Product surface

The designer can create:

```text
Assets
  Create
    Immersive Framework
      Player
        Player Provisioning Profile
```

The default Inspector is acceptable initially. A custom Inspector comes later.

## Expected smoke

- create Manager-Provisioned Profile;
- create Scene-Provided Profile;
- create default Manager + Scene override;
- serialization/deserialization preserves data;
- loading/editing the asset causes no gameplay operation.

## Architectural gain

Formalizes provisioning intent without changing existing authority.

## Suggested commit

```text
feat(player): add player provisioning profile authoring
```

---

# IF-SESSION-CONFIG-03 — Player Session Profile

**Type:** UX/Product + Authored Contract

## Objective

Create the main authored asset:

```text
PlayerSessionProfile
```

## Scope

It should own only:

```text
Supported Slots
    ordered PlayerSlotProfile references

Initial Capacity

Initial Joining Intent

Player Provisioning Profile
```

The ordered Slot list is normatively the allocation order.

## Structural validation

Detect:

- null Slot;
- duplicate `PlayerSlotProfile`;
- duplicate stable `PlayerSlotId`;
- `Initial Capacity < 0`;
- `Initial Capacity > Supported Slots`;
- required Provisioning Profile absent;
- other locally provable contradictions.

Do not copy these into a second definition:

```text
PlayerSlotId
Default Actor
Slot metadata
```

## Out of scope

Do not modify GameApplication or initialize Session yet.

## Product surface

Minimum authored experience:

```text
PlayerSessionProfile
    Slots
      P1
      P2
      P3
      P4
    Initial Capacity = 2
    Initial Joining = Open
    Provisioning = LocalMultiplayer
```

The designer should understand that Slot order is allocation order.

## Expected smoke

- ordered list preserved;
- duplicates rejected;
- Capacity 2/4 valid;
- Capacity 5/4 invalid;
- references reuse `PlayerSlotProfile`.

## Suggested commit

```text
feat(player): add player session profile authoring
```

---

# IF-SESSION-CONFIG-04 — Pure Effective Configuration Resolver

**Type:** Technical / Domain

This is the first major architectural gate.

## Objective

Create a pure and deterministic resolution:

```text
initialization input
        +
PlayerSessionProfile
        +
PlayerProvisioningProfile
        ↓
resolve
        ↓
EffectivePlayerSessionConfiguration
```

or:

```text
typed explicit failure
```

The resolver must not mutate Session runtime.

## Rules to close

### Default versus override

```text
explicit override
    -> replaces complete GameApplication default

no override
    -> GameApplication default
```

No field-by-field merge.

### Feature absent

```text
Player Session disabled
    -> valid absence
```

### Feature required

```text
enabled + no resolvable Profile
    -> explicit failure
```

### Slots

Preserve exactly:

```text
Supported Slots authored order
```

### Capacity

Validate against the structural Slot universe.

### Provisioning

For each Slot:

```text
override?
    yes -> override
    no  -> authored default
```

Result example:

```text
P1 -> SceneProvided
P2 -> ManagerProvisioned
P3 -> ManagerProvisioned
```

with provenance:

```text
P1 -> SlotOverride
P2 -> ProfileDefault
P3 -> ProfileDefault
```

### Actor resolution

The resolver records the policy.

If `Resolve Configured Default` is used, the Actor definition still comes from `PlayerSlotProfile`.

## Out of scope

The resolver must not:

- create Host;
- open Joining;
- alter runtime Capacity;
- Join;
- query scene;
- search objects;
- use service locator;
- execute Activity lifecycle.

## Expected smoke

Prefer table-driven coverage for:

```text
disabled
enabled + missing Profile
default Profile
explicit override
capacity valid
capacity invalid
duplicate Slots
mixed provisioning
unknown override Slot
duplicate override
Actor default
Actor unresolved
```

## Technical acceptance

Do not proceed to runtime integration until this cut is stable.

If the rules are still changing here, changing runtime and tooling in parallel will only multiply rework.

## Suggested commit

```text
feat(player): resolve effective player session configuration
```

---

# IF-SESSION-CONFIG-05 — Session Creation and Runtime Integration

**Type:** Runtime Integration / Package

This is where ADR-016 starts operating in Play Mode.

## Objective

Connect the effective configuration to the existing Session authority exactly once.

## Scope

Add to GameApplication composition:

```text
Player Session Enabled
Default Player Session Profile
```

Add to the appropriate request/bootstrap path:

```text
optional explicit PlayerSessionProfile override
```

Target flow:

```text
GameApplication / Session creation
        ↓
resolve effective configuration
        ↓
validation success
        ↓
initialize existing Session Player authority once
        ↓
Profile leaves authority boundary
```

## Critical runtime rules

After Session creation:

```text
modify Profile asset
    ≠ mutate Session

change Route
    ≠ reapply Profile

change Activity
    ≠ reapply Profile
```

`Current Capacity` and `Joining` become exclusively owned by existing runtime authorities and commands.

## Provisioning per Slot

Connect effective provisioning to existing pipelines:

```text
P1 SceneProvided
    -> existing Scene-Provided lifecycle

P2 ManagerProvisioned
    -> existing Manager-Provisioned lifecycle
```

Do not create:

```text
PlayerSessionManager
PlayerProvisioningManager
parallel provisioning pipeline
```

## Late Join

A late Join uses the effective provisioning already frozen for the selected Slot.

The Profile must not be re-read to decide the mode.

## Failure behavior

If:

```text
P1 = SceneProvided
```

and its mandatory scene composition is absent:

```text
FAIL
```

Never:

```text
try ManagerProvisioned instead
```

## Mandatory runtime smoke

1. Player Session disabled;
2. default Profile;
3. complete override;
4. Profile edit after creation does not alter runtime;
5. homogeneous Manager-Provisioned;
6. homogeneous Scene-Provided;
7. mixed Scene + Manager;
8. late Join;
9. invalid Scene composition;
10. invalid Manager composition;
11. no provisioning fallback;
12. Slots above Current Capacity remain structurally configured.

## Technical acceptance

The existing runtime authority remains the only truth for:

```text
Capacity
Joining
Slot occupancy
Host
Actor lifecycle
```

## Suggested commit

```text
feat(player): initialize session runtime from player session profiles
```

---

# IF-SESSION-CONFIG-06 — Designer-First Inspectors and Diagnostics

**Type:** UX/Product + Editor

Custom tooling should only be added after runtime integration is proven.

## Objective

Turn technically correct assets into a feature understandable by a real framework user.

## GameApplication Inspector

Expected shape:

```text
Player Session

[✓] Enabled

Default Session Profile
    Local Multiplayer
```

When Enabled:

```text
missing Profile
    -> clear inline error
```

## Player Session Profile Inspector

Normal mode:

```text
Supported Slots
    1. Player One
    2. Player Two
    3. Player Three

Initial Capacity
Initial Joining
Provisioning Profile
```

The UI must explicitly communicate:

> Slots are allocated in this order.

## Player Provisioning Profile Inspector

```text
Default Host Provisioning
    Manager Provisioned

Overrides
    Player One -> Scene Provided

Actor Resolution
    Resolve Configured Default
```

## Advanced / Debug

Show normalized configuration rather than random internal component details:

```text
Effective Slots
P1  SceneProvided       Slot Override
P2  ManagerProvisioned  Profile Default

Initial Capacity: 2
Initial Joining: Open
Actor Resolution: Resolve Configured Default
```

In Play Mode, when available:

```text
Initialization source
Initialization result
effective configuration correlation
```

## Apply / Rebuild decision

Do not create Apply/Rebuild automatically.

If cut 05 proves that technical scene components need materialization, introduce a specific idempotent Composer.

If Profiles and existing composition are sufficient, do not invent a Composer merely to satisfy a pattern.

## Product acceptance

The designer must not need to understand:

```text
runtime host module
reservation tokens
internal reconciliation
actor preparation runtime context
```

in order to configure the Session.

## Suggested commit

```text
feat(player): add player session authoring inspectors and diagnostics
```

---

# IF-SESSION-CONFIG-07 — QAFramework Contract Closure

**Type:** Technical QA

This cut occurs only after package contracts are official.

## Objective

Certify ADR-016 without promoting internals into public APIs merely to simplify testing.

## Minimum QA suite

| Group | Main cases |
|---|---|
| Enablement | disabled valid; enabled without Profile fails |
| Resolution | default; complete override; no merge |
| Lifetime | Profile edit after creation does not mutate Session |
| Slots | existing identity; order; duplicates; stability |
| Capacity | bounds; Slots outside capacity remain supported |
| Allocation | first available Slot in authored order |
| Provisioning | default; override; mixed Session |
| Failures | unsupported override; duplicate; missing composition; no fallback |
| Actor | configured default vs unresolved |
| Late Join | uses frozen provisioning for Slot |
| Isolation | Route/Activity do not reapply configuration |
| Authority | runtime commands change live state, not initialization evidence |
| Diagnostics | typed failure + immutable evidence |

## Test classification

Continue using explicit categories:

```text
PUBLIC-ONLY
PARTIAL / PUBLIC EVIDENCE
INTERNAL TECHNICAL
```

This prevents a test that depends on privileged setup from being interpreted as proof of a complete public consumer journey.

## ADR-015 dependency

If a complete public test remains blocked by missing canonical scoped consumer access from ADR-015, record it as a dependency.

Do not solve ADR-015 inside ADR-016.

## Technical acceptance

```text
package compiles
positive tests pass
negative tests pass
no silent fallback
authority preserved
diagnostics correlated
```

## Suggested commit

```text
test(player): certify player session initialization contracts
```

---

# IF-SESSION-CONFIG-08 — FIRSTGAME Real Consumer Proof

**Type:** Real Integration / Product

This cut proves whether the feature is usable as a product rather than merely technically correct.

## Recommended scenario

Use the capability that most clearly distinguishes ADR-016:

```text
FIRSTGAME

Player Session
    Slots
        P1
        P2

    Initial Capacity = 2
    Joining = Open

Provisioning
    Default = ManagerProvisioned

    P1 Override = SceneProvided
```

Expected result:

```text
P1 -> already represented by scene composition
P2 -> joins and is Manager-Provisioned
```

A real Activity then consumes both through normal framework contracts.

## Rules

Build the scenario using only the product surface that a framework consumer is expected to use.

It does not count as product proof if it requires:

- internal calls;
- manual mutation of runtime contexts;
- scene search;
- temporary QA-only components;
- manual bindings that future users should not know about.

## Questions FIRSTGAME must answer

```text
How do I create it?
Where do I configure it?
Is Slot ordering clear?
Is Capacity understandable?
Is mixed provisioning understandable?
Does an invalid configuration explain what to fix?
Can I inspect the resolved state in Debug?
Did I need to understand internals?
```

Any generic workaround discovered here should migrate to the package.

## Product acceptance

A new consumer can build the Session from short documentation and the product Inspectors without understanding internal Player lifecycle implementation.

## Suggested commit

```text
test(firstgame): prove player session profile consumer flow
```

---

# IF-SESSION-CONFIG-09 — Documentation and ADR Closure

**Type:** Documentation / Architecture Closure

## Objective

Turn the implementation into an officially completed framework feature.

## Deliverables

Package documentation:

```text
short usage documentation
Profile creation flow
GameApplication configuration
mixed provisioning example
runtime mutation explanation
Advanced/Debug explanation
common failure cases
```

Add a small sample/template when it materially improves onboarding.

Update:

```text
ADR-016
    Proposed -> Accepted

implementation completion
decision matrix
reconciliation documents
```

Only after:

```text
Package ✓
QA ✓
FIRSTGAME ✓
```

## Suggested commit

```text
docs(player): accept player session initialization architecture
```

---

# Dependencies That Must Remain Separate

During implementation, known adjacent gaps must not be absorbed into ADR-016.

| Topic | Disposition |
|---|---|
| `AllJoinedSlots` vs `All Supported Slots` in Activity | Separate Activity participation cut |
| Physical Actor as explicit readiness level | Separate readiness cut |
| Join Inhibit | Separate decision/cut |
| Leave/disconnect | Outside ADR-016 |
| Session-Persistent | Outside ADR-016 |
| Public cross-scene consumer access | ADR-015 |
| Generic Slot allocation strategies | Explicitly rejected for this cut |
| Runtime switching Scene ↔ Manager | Outside ADR-016 |

ADR-016 should be completed without solving the entire Player architecture simultaneously.

---

# Implementation Milestones

## M1 — Foundation

```text
01 Contracts
02 Provisioning Profile
03 Session Profile
04 Resolver
```

### Gate

The effective configuration model is frozen and deterministic.

---

## M2 — Runtime

```text
05 Session/runtime integration
```

### Gate

Real runtime behavior is proven without introducing a second authority.

---

## M3 — Product

```text
06 Inspector / Diagnostics
```

### Gate

The feature is understandable and configurable by a framework user.

---

## M4 — Proof and Closure

```text
07 QA
08 FIRSTGAME
09 Docs / ADR Accepted
```

### Gate

Technical contracts, real consumer usability, and architectural documentation are all closed.

---

# Recommended Next Implementation Point

Cuts 01–07 are complete. FIRSTGAME proof is intentionally deferred.

Before moving ADR-016 to acceptance, perform the smallest remaining package audit/cut:

```text
IF-SESSION-CONFIG-05B — Creation-Time Session Profile Override Closure
```

Objective:

1. inspect current Session/bootstrap creation API for an existing typed explicit `PlayerSessionProfile` override;
2. if it already exists, certify that it replaces the GameApplication default completely and never field-merges;
3. if absent, add the smallest typed creation-time input necessary;
4. resolve exactly one effective Profile source before Session initialization;
5. do not add a second Session authority, live Profile synchronization, service locator or fallback;
6. add focused QA evidence only after the package contract exists.

After that, the remaining closure work is FIRSTGAME manual consumer proof plus final ADR acceptance/documentation.
