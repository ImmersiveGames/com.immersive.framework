# Immersive Framework — Player Session Initial Configuration Reconciliation

**Date:** 2026-08-08  
**Project:** Immersive Framework 1.1 / Unity 6.5  
**Status:** Reconciliation record — Session initial configuration and Player provisioning  
**Scope:** `S01–S12` and `PR01–PR07` from the Player / Session / Activity Decision Matrix

---

## 1. Purpose

This document records the reconciliation of the Player / Session / Activity conceptual matrix against the existing architecture decisions and the source inspection performed before IF-ADR-016 was drafted.

The objective is to distinguish:

```text
already defined by accepted architecture
compatible refinement
new normative decision required
derived consequence
conflict with existing ADR
```

This is a decision record, not an implementation plan and not QA certification.

---

## 2. Sources used

Primary normative/reference documents:

```text
IF-ADR-001 — Core Lifecycle and Runtime Authority
IF-ADR-002 — Product Authoring Model
IF-ADR-003 — Player Participation and Actor Lifecycle
IF-ADR-010 — Editor and Inspector Product Surface Authority [Proposed]
IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility
IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface [Proposed]
IMMERSIVE-FRAMEWORK-PLAYER-SESSION-ACTIVITY-DECISION-MATRIX-2026-08-08
IMMERSIVE-FRAMEWORK-TRANSVERSAL-ARCHITECTURE-INVARIANTS-AND-DECISION-CLASSIFICATION-2026-08-08
IMMERSIVE-FRAMEWORK-NORMALIZED-PLAYER-DECISION-CLASSIFICATION-2026-08-08
IMMERSIVE-FRAMEWORK-ADR-COMPLETION-SUMMARY-2026-08-07
IF-TRACK-Framework
```

Source inspection baseline used in the preceding reconciliation:

```text
com.immersive.framework
  32d6786598316e0548dc26d9b08a5717c7a8503e
```

At that inspected package baseline, directed searches did not identify canonical package types/assets named `PlayerSessionProfile` or `PlayerProvisioningProfile`.

That source result is implementation evidence, not normative authority. The ADRs remain the authority for architecture.

---

## 3. Existing architectural boundary

### 3.1 Session authority already exists

IF-ADR-001 establishes the hierarchy:

```text
Game Application / Session
  -> Session-scoped authorities and participants
     -> Logical Players
  -> Route
     -> Activity
        -> contextual projection, readiness and materialization
```

Therefore the Session configuration work must not create another runtime owner for Player state.

### 3.2 Profile is authored intent, not mutable runtime truth

IF-ADR-002 establishes the product layering:

```text
Recipe / Profile / Template
  reusable intent

Composer / Authoring Component
  concrete composition

Technical materialization
  explicit contracts and bindings

Scoped Runtime Context / Session / Service
  runtime authority

Diagnostics
  validation and technical evidence
```

The missing Session/Profile work belongs above the existing Session runtime authority.

### 3.3 Player lifecycle and provisioning foundations already exist

IF-ADR-003 already separates:

```text
Slot configuration
joining/admission
Logical Player participation
Actor selection
Logical Actor preparation
physical Actor materialization
input/camera/gameplay admission
Activity readiness contribution
contextual release/reconcile
```

It also already supports `Scene-Provided` and `Manager-Provisioned` Player sources.

### 3.4 ADR-015 is a different boundary

IF-ADR-015 explicitly defines the missing consumer-facing command and observation boundary. It states that the gap is not another Player authority.

Its canonical command direction is equivalent to:

```text
Open Joining
Close Joining
Set Dynamic Capacity
Request Join
Request Actor Selection
```

It does not define the authored configuration consumed when a Session is first created.

### 3.5 Activity participation remains separate

IF-ADR-012 already owns Activity Player participation intent and normalized effective Activity policy.

Therefore:

```text
Player Session Profile
  configures initial Session intent

!=

Activity Player Participation Profile
  configures how an Activity projects/uses Session Players
```

An Activity must not become an indirect Session configuration authority.

---

## 4. Reconciliation result — S01–S12

| ID | Matrix decision | Classification after reconciliation | Disposition |
|---|---|---|---|
| **S01** | `GameApplication` may provide Default Player Session Profile | **New compatible domain decision** | Keep and formalize in IF-ADR-016. `GameApplication` is the authored default source, not mutable Player runtime authority. |
| **S02** | Session creation uses default or explicit override | **New compatible domain decision** | Keep and formalize. Resolution occurs at Session creation only. |
| **S03** | Profile initializes Session once | **Derived from accepted architecture** | Preserve as explicit consequence of intent/runtime separation. |
| **S04** | Session/runtime becomes mutable authority | **Already defined** | Preserve IF-ADR-001 authority. Do not add another manager/state store. |
| **S05** | Capacity/Joining mutate only through explicit requests/capabilities | **Already aligned / derived** | Preserve. Post-creation commands belong to runtime authorities and ADR-015 consumer surface. |
| **S06** | Route/Activity do not reapply Session Profile | **Derived** | Preserve as a hard boundary against hidden lateral mutation. |
| **S07** | Session has structural Supported Slots | **Existing domain foundation + new authored ownership** | Slot/Session model exists; IF-ADR-016 must define where the structural universe is authored for Session creation. |
| **S08** | Current Capacity is runtime-variable within Supported Slots | **Compatible refinement** | Formalize initial bound and runtime relationship. Capacity is mutable; Supported Slots are structural for the Session. |
| **S09** | Activity eligibility does not increase Capacity | **Derived** | Preserve. Activity projection does not mutate Session Capacity. |
| **S10** | Capacity changes require explicit request | **Already aligned / derived** | Preserve. Runtime/public mutation remains separate from Profile initialization. |
| **S11** | Join assigns first available Slot under deterministic allocation policy | **New/refined Session policy** | Formalize in IF-ADR-016. Normal Join does not make the joining Player the authority for choosing a Slot. |
| **S12** | Assigned Slot does not renumber after another Player leaves | **Aligned with stable Slot identity** | Preserve as Session-model stability rule. |

---

## 5. Reconciliation result — PR01–PR07

| ID | Matrix decision | Classification after reconciliation | Disposition |
|---|---|---|---|
| **PR01** | Dedicated Player Provisioning Profile | **Real product/domain gap** | Create a reusable authored Profile for initial provisioning intent. It must not create another provisioning runtime. |
| **PR02** | Player Session Profile references Player Provisioning Profile | **New compatible composition decision** | Formalize in IF-ADR-016. |
| **PR03** | Scene Provided / Manager Provisioned Host modes | **Already defined** | Reuse IF-ADR-003 vocabulary and runtime ownership. Do not redefine as new lifecycle modes. |
| **PR04** | Host provisioning separate from Actor resolution | **Already defined** | Preserve the existing lifecycle separation. |
| **PR05** | Resolve default Actor or leave unresolved/external | **Compatible refinement** | Formalize as authored initial Actor-resolution intent without adding a generic character-selection flow. |
| **PR06** | Effective Provisioning Profile stable for Session, including late joins | **New lifetime decision, compatible with accepted architecture** | Formalize in IF-ADR-016. Runtime state may evolve; effective provisioning intent does not get replaced implicitly. |
| **PR07** | Route/Activity do not replace Provisioning Profile | **Derived** | Preserve as a consequence of Session scope and explicit mutation. |

---

## 6. Reconciled canonical model

```text
GameApplication
    Default Player Session Profile [optional authored source]
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
        Scene Provided
        Manager Provisioned

    Actor Resolution
        Resolve configured default
        Leave unresolved / external
                │
                ▼
Session creation
    resolve effective initial configuration once
                │
                ▼
Existing Session-scoped runtime authority
    Slots / Logical Players
    Current Capacity
    Joining state
    Host assignments
    Actor state/evidence
```

The authority boundary is:

```text
Authored Profile
    declares initial intent

Session creation
    resolves effective configuration

Session runtime
    owns mutable truth thereafter
```

---

## 7. Key architectural finding

The gap is **not** a missing runtime Player authority.

The gap is:

```text
How is a Player Session configured before it exists?

Where are Supported Slots authored?
Where are Initial Capacity and Initial Joining Intent authored?
Where is deterministic Slot allocation intent authored?
How does GameApplication provide a default?
How does an explicit creation-time override replace that default?
How is Player provisioning intent composed into Session initialization?
When does authored intent stop being authority?
```

Existing ADRs answer adjacent questions but do not define this complete boundary.

Therefore a new ADR is justified.

---

## 8. No conflict found with Accepted ADRs

No accepted ADR requires Route/Activity to own Session initial Player configuration.

No accepted ADR requires Profile assets to remain live mutable runtime authority.

No accepted ADR requires a second Player manager/state store.

The proposed shape is compatible with:

```text
IF-ADR-001
  Session-scoped runtime authority

IF-ADR-002
  reusable authored intent above scoped runtime authority

IF-ADR-003
  existing Player/Slot/Actor lifecycle separation

IF-ADR-012
  Activity participation as a distinct contextual policy

IF-ADR-015 [Proposed]
  commands + immutable observation after/beside initialization
```

---

## 9. Boundaries that must not be blurred

### 9.1 Session Profile vs Session runtime

Rejected:

```text
Profile asset continuously synchronizes Capacity or Joining into live Session.
```

Required:

```text
Profile -> creation-time effective configuration -> runtime authority.
```

### 9.2 Session Profile vs Activity Player Profile

Rejected:

```text
Activity Player policy changes Supported Slots or Current Capacity implicitly.
```

Required:

```text
Activity projects Session Players without becoming Session authority.
```

### 9.3 Provisioning Profile vs ADR-015 commands

Rejected:

```text
Changing a Profile asset is the runtime command mechanism.
```

Required:

```text
Provisioning Profile = Session initialization intent
ADR-015 commands = supported explicit runtime requests
```

### 9.4 Provisioning Profile vs new runtime manager

Rejected:

```text
PlayerProvisioningProfileRuntimeManager
second mutable provisioning state store
public static Player manager
service locator
scene/hierarchy lookup authority
```

Required:

```text
reuse existing Session-scoped Player runtime authorities.
```

---

## 10. Product implications

The minimum product surface implied by the reconciliation is:

```text
Create > Immersive Framework > Player > Player Session Profile
Create > Immersive Framework > Player > Player Provisioning Profile

GameApplication Inspector
  Default Player Session Profile

Player Session Profile Inspector
  Supported Slots
  Initial Capacity
  Initial Joining Intent
  Slot Allocation
  Player Provisioning Profile

Player Provisioning Profile Inspector
  Host Provisioning
  Actor Resolution
```

Normal Inspector must present authored intent. Runtime evidence belongs in read-only diagnostics/Advanced/Debug.

A dedicated Composer is **not automatically required** merely because these two Profiles exist. Composer/materialization is required only where concrete technical composition must be built, and Manager-Provisioned composition remains coordinated with IF-ADR-002/IF-ADR-015 rather than duplicated by IF-ADR-016.

---

## 11. Resulting ADR decision

Create:

```text
IF-ADR-016 — Player Session Initial Configuration and Provisioning Profiles
```

IF-ADR-016 owns:

```text
S01 S02 S07 S08 S11
PR01 PR02 PR05 PR06
```

and explicitly records the following derived/previously-established consequences:

```text
S03 S04 S05 S06 S09 S10 S12
PR03 PR04 PR07
```

It must not absorb IF-ADR-015 command/observation scope or IF-ADR-012 Activity participation scope.

---

## 12. Next order after ADR creation

```text
1. Approve/revise IF-ADR-016 normative shape.
2. Audit exact existing package types/utilities that can be reused.
3. Define the smallest package implementation cut for PlayerSessionProfile.
4. Define the smallest package implementation cut for PlayerProvisioningProfile.
5. Integrate creation-time resolution without adding runtime authority.
6. Add Editor creation/validation/Inspector surface.
7. Prove technical initialization contract in QAFramework.
8. Prove manual real-game authoring/use in FIRSTGAME.
9. Coordinate post-creation commands/observation with IF-ADR-015.
```

No implementation should use FIRSTGAME as the permanent home of these contracts.

---

## 13. Implementation and QA closure update — 2026-08-08

The ADR-016 implementation progressed through IF-SESSION-CONFIG-07. Current evidence is:

```text
01 Canonical contracts / reuse boundary                 CLOSED
02 Player Provisioning Profile                          CLOSED
03 Player Session Profile                               CLOSED
04 Pure Effective Configuration Resolver                CLOSED
05 Session runtime integration                          CLOSED / QA 6 of 6 PASS
06 Designer-first Inspector / diagnostics               CLOSED for current package UX cut
07 QA contract closure                                  CLOSED / 17 of 17 PASS
08 FIRSTGAME real consumer proof                        DEFERRED by priority
09 Documentation / ADR acceptance                       PARTIAL
```

### 13.1 Implemented authority shape

The implemented flow now matches the intended authority separation:

```text
GameApplication authored default
  -> PlayerSessionProfile
  -> PlayerProvisioningProfile
  -> pure deterministic resolution
  -> EffectivePlayerSessionConfiguration
  -> one-time Session initialization
  -> existing PlayerParticipation runtime authority
```

The runtime integration preserves:

```text
ordered Supported Slots
Initial Capacity
Initial Joining intent
per-Slot frozen Host provisioning
Actor Resolution policy
Default Actor evidence
```

and does not introduce a second mutable Player manager/state store.

### 13.2 QA evidence

`IF-SESSION-CONFIG-05` certifies 6/6 runtime-integration cases:

```text
disabled valid absence
enabled missing Profile failure
Manager ordered allocation
mixed Scene/Manager provisioning with no skip/fallback
Profile edit after initialization does not rewrite Session
LeaveUnresolved blocks automatic default Actor selection
```

`IF-SESSION-CONFIG-07` certifies 17/17 contract-closure cases, with explicit classification into `PUBLIC-ONLY`, `PARTIAL PUBLIC EVIDENCE`, and `INTERNAL TECHNICAL`.

The suite covers default/override resolution, Slot identity/order, Capacity bounds, Scene-only and mixed provisioning, unsupported overrides, no provisioning fallback, late-Join frozen provisioning, Actor-resolution policy, typed failures, immutable evidence, and Session-vs-Activity structural separation.

### 13.3 Remaining gaps

The previous creation-time Session Profile override gap is now **closed** by IF-SESSION-CONFIG-05B:

- typed explicit `PlayerSessionProfile` input at canonical Session creation;
- explicit Profile replaces the GameApplication default completely;
- invalid explicit Profile fails without fallback;
- no field merge;
- QA smoke 4/4 PASS.

Remaining gaps:

1. **FIRSTGAME manual consumer proof** — intentionally deferred to prioritize more package implementation. The package UX cut is sufficient to continue architecture work, but real-game usability remains unproven.

The existing Edit Mode QA also does not directly execute a real Route/Activity transition to prove non-reapplication through full ActivityFlow. This remains an integration-evidence gap, not a contradiction of the Session/Activity authority model.

### 13.4 Closure rule

IF-ADR-016 remains **Proposed**. It should not move to Accepted until:

```text
creation-time complete Session Profile override is QA-certified through IF-SESSION-CONFIG-05B;
FIRSTGAME manual consumer proof is completed;
final documentation/closure audit confirms no duplicated ADR-015 consumer authority.
```


### 13.5 IF-SESSION-CONFIG-05B closure — 2026-08-08

The complete creation-time Session Profile override required by S02 is implemented and QA-certified.

```text
no explicit Profile
  -> GameApplication default

explicit Profile
  -> complete replacement source

invalid explicit Profile
  -> typed failure
  -> no fallback to default

field merge
  -> not performed
```

QA evidence:

```text
IF-SESSION-CONFIG-05B Session Profile Override Smoke
  PASS — 4/4
```

This removes the implementation gap previously recorded for S02. It does not close FIRSTGAME product-usability proof or full Route/Activity transition non-reapplication evidence.
