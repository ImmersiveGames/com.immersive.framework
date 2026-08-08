# Immersive Framework — IF-ADR-015 Player Provisioning Consumer Surface Closure Plan

**Date:** 2026-08-07  
**Status:** Proposed execution plan after IF-PLAYER-SURFACE-01A–01E1 audit  
**Primary objective:** close the real remaining IF-ADR-015 gap without creating a second Player authority  
**Repositories:** `com.immersive.framework`, `QAFramework`, `planet-devourer`

## Verified Git baselines

```text
com.immersive.framework
  d56cf3df51acab464c77ff6a3a7c6b28062bc29a
  IF-TXN-03A Docs

QAFramework
  c99df1e77a8408e6b48124a5d371f09e9af52019
  IF-TXN-03A

planet-devourer
  ab1bfe65c09af8988c2fe21ce06db780fe12aa70
  Demo03Etapa04
```

Repositories remain read-only. Every implementation cut is delivered as a ZIP containing created, edited and removed files, `CHANGESET.md`, source SHAs, validation executed and known limitations.

---

# 1. Executive correction

The current Player problem is not a missing runtime authority.

The audited package already contains:

```text
PlayerSlotId
Slot assignment evidence
Host correlation evidence
Actor selection/preparation/materialization evidence
physical Actor evidence
Session participation snapshot
Manager-Provisioned lifecycle snapshot
OpenJoining
CloseJoining
SetDynamicCapacity
RequestJoin
RequestDefaultActorSelection
typed operation results
revision-aware operation evidence
```

The remaining product gap is:

```text
existing Player authorities
        ↓
canonical public/scoped consumer access
        ↓
immutable consumer observation
        ↓
designer-facing command authoring
        ↓
status / diagnostics presentation
        ↓
public-only QA
        ↓
FIRSTGAME real consumer proof
        ↓
Recipe / Composer / documentation closure
```

The plan must not introduce:

```text
PlayerManager
second Player state store
global registry
service locator
scene search
name-based inference
reflection
generic event bus
silent fallback
manual prepare/reconcile buttons
```

---

# 2. Frozen architectural principles

## Authority

Existing Session, PlayerParticipation, Host, Actor preparation/materialization and Activity readiness authorities remain canonical.

No new component in this plan may own duplicate mutable Player truth.

## Commands

Consumer commands request supported operations. They do not mutate Slot, Host, Actor preparation or readiness state directly.

Initial command vocabulary remains bounded to capabilities already supported by the package:

```text
Open Joining
Close Joining
Set Capacity
Request Join
Request Default Actor Selection
```

Arbitrary Actor selection is not added unless a separate product requirement proves it necessary.

## Observation

Consumer observation is an immutable projection over existing authority.

Rich technical correlation should reuse current typed evidence whenever possible:

```text
Session revision
Activity occurrence
Slot
assignment token / owner / origin
HostBindingIdentity
ActorId
runtime content identity
selection revision
materialization revision
physical ownership evidence
gameplay admission evidence
```

Designer-first views may summarize this evidence. Advanced / Debug must preserve the typed technical evidence.

## Scope

Cross-scene access must be typed, scoped and lifetime-explicit.

Existing `LocalPlayerProvisioningHostRegistration`, bridge/runtime-host infrastructure must be reused when it already supplies the correct lifetime. A second registry must not be created merely to make UI buttons convenient.

## FIRSTGAME

FIRSTGAME may own:

```text
layout
visual presentation
real game prefabs/assets
game-specific movement
game-specific wording
```

FIRSTGAME must not own:

```text
framework command routing
Player authority lookup
Player snapshot aggregation
reconcile
Actor preparation
framework compatibility facade
```

---

# 3. Execution sequence

```text
A0  IF-PLAYER-SURFACE-01E2
    QA public-contract coverage audit

P0  IF-PLAYER-SURFACE-02
    Canonical consumer boundary freeze

P1  IF-PLAYER-SURFACE-03
    Scoped Player provisioning consumer access

P2  IF-PLAYER-SURFACE-04
    Consumer observation projection

P3  IF-PLAYER-SURFACE-05
    Designer command authoring + validation

P4  IF-PLAYER-SURFACE-06
    Status / diagnostics binding surface

Q1  QA-PLAYER-SURFACE-01
    Public-only positive contract proof

F1  FIRSTGAME-PLAYER-SURFACE-01
    Demo03 real command + status proof

P5  IF-PLAYER-SURFACE-07
    Recipe / Composer / creation workflow

Q2  QA-PLAYER-SURFACE-02
    Negative, stale-scope and lifecycle hardening

F2  FIRSTGAME-PLAYER-SURFACE-02
    UX disposition + final consumer proof

D1  IF-DOC-PLAYER-SURFACE-01
    Canonical documentation and ADR closure
```

`P1–P4` are deliberately separated. A single large “Player facade” cut would hide whether the real requirement is transport, observation, authoring or presentation.

---

# 4. Cut A0 — IF-PLAYER-SURFACE-01E2
## QA public-contract coverage audit

**Type:** technical audit / no implementation

### Objective

Determine exactly which Player lifecycle behaviors are already proven from public package surfaces and which QA runners still depend on internal/runtime-specific access.

### Scope

Classify existing QA coverage for:

```text
joining open / closed
capacity
RequestJoin
Slot creation/admission
Host evidence
Actor selection
Actor preparation
physical materialization
gameplay admission
Activity readiness completion
Session/Activity revision correlation
reentry
cleanup
stale revision
stale occurrence
duplicate/in-flight request
```

Each case is classified as:

```text
public-only
internal authority proof
partial
not covered
```

### Out of scope

No new QA code.

### Files created

```text
audit only:
IMMERSIVE-FRAMEWORK-IF-PLAYER-SURFACE-01E2-QA-PUBLIC-CONTRACT-COVERAGE-2026-08-07.md
```

### Files altered / removed

```text
none
```

### Product surface affected

None.

### Technical acceptance

The audit identifies the exact public surface used by each positive proof and does not treat internal QA access as consumer reachability.

### Architectural gain

Prevents implementation of APIs solely because a QA harness bypassed the intended consumer boundary.

### Suggested commit

Not applicable; audit artifact only.

---

# 5. Cut P0 — IF-PLAYER-SURFACE-02
## Canonical consumer boundary freeze

**Type:** architecture / package documentation

### Objective

Freeze the smallest official consumer model before code is added.

### Scope

Update IF-ADR-015 with audited facts:

```text
existing runtime authorities remain canonical
existing basic provisioning operations remain canonical
consumer surface is not a second authority
consumer access is scoped/lifetime-explicit
observation is immutable projection
cross-scene transport cannot rely on scene lookup
FIRSTGAME placeholders are not an official command channel
```

Freeze responsibility boundaries, but do not prematurely freeze every class name.

### Out of scope

Runtime implementation, Editor tooling, QA changes, FIRSTGAME changes.

### Files altered

Expected:

```text
Documentation~/Architecture/ADRs/
  IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md
```

Alter an existing ADR index/tracker only if it is already canonical.

### Files created / removed

```text
none expected
```

### Product surface affected

Architecture documentation only.

### Technical acceptance

The ADR explicitly rejects:

```text
new mutable Player authority
static/global registry
service locator
generic event bus
reflection-based runtime resolution
scene search
silent fallback
```

### Product acceptance

A framework consumer can distinguish:

```text
command
observation
authoring
runtime authority
diagnostics
```

### Suggested commit

```text
docs(adr): freeze player provisioning consumer boundary
```

---

# 6. Cut P1 — IF-PLAYER-SURFACE-03
## Scoped Player provisioning consumer access

**Type:** package runtime/public contract

### Objective

Provide a canonical way for scene/Route/Activity consumers to reach the existing provisioning capability without serialized cross-scene authority references or global lookup.

### Scope

First inspect and reuse:

```text
LocalPlayerProvisioningHostRegistration
LocalPlayerProvisioningBridge
FrameworkRuntimeHost scoped infrastructure
```

The implementation must expose a typed consumer endpoint over existing authority.

Conceptual responsibilities:

```text
resolve within explicit framework/runtime scope
forward supported commands
expose read-only observation
report unavailable/wrong-scope explicitly
preserve Session and Activity/occurrence correlation where applicable
```

Exact public type names are frozen only after source inspection proves which existing registration/bridge type should be extended.

### Out of scope

No new Player state, no Actor preparation API, no reconcile API, no UI.

### Files altered

Expected existing package areas:

```text
Runtime/PlayerParticipation/Runtime/
  LocalPlayerProvisioningHostRegistration.cs
  LocalPlayerProvisioningBridge.cs
  related runtime-host integration

Runtime/PlayerParticipation/Contracts/
  consumer-facing port/result contracts only when current contracts are insufficient
```

### Files created

Only if no current typed boundary can be safely extended:

```text
one small consumer command/observation contract
one scoped binding/access component
```

Do not create a parallel registry.

### Files removed

```text
none expected
```

### Product surface affected

Canonical runtime access for consumers.

### Expected flow

```text
scene consumer
→ scoped typed binding
→ existing LocalPlayerProvisioning authority
→ typed operation result
```

### Technical smoke

```text
valid scope resolves exactly one canonical provisioning authority;
missing scope fails explicitly;
disposed/replaced scope rejects stale access;
commands preserve current revision/result semantics.
```

### Technical acceptance

```text
no FindObjectOfType
no static singleton
no hidden fallback
no duplicate mutable state
no runtime reflection
no direct access to internal prepare/reconcile modules
```

### Product acceptance

A scene-local command component can operate the persistent Player provisioning system without knowing the runtime module graph.

### Architectural gain

Separates cross-scene transport from Player authority.

### Usability gain

Consumers stop needing persistent prefab references or framework-internal knowledge.

### Suggested commit

```text
feat(player): expose scoped provisioning consumer access
```

---

# 7. Cut P2 — IF-PLAYER-SURFACE-04
## Consumer observation projection

**Type:** package public contract / diagnostics

### Objective

Expose one consumer-safe read model that explains the current Manager-Provisioned Player state without reducing typed Slot–Host–Actor evidence to opaque strings.

### Scope

Prefer composition/reuse of existing immutable snapshots:

```text
PlayerParticipationSnapshot
ManagerProvisionedPlayerLifecycleSnapshot
PlayerSlotAssignmentSnapshot
CurrentPlayerSlotActorSnapshot
PlayerActorCorrelationEvidence
PlayerActorPreparationSummary
```

Normal view should answer:

```text
Is joining open?
What is capacity?
Which Slots exist?
What lifecycle state is each Slot in?
Which Actor is selected?
Is logical Actor prepared?
Is physical Actor materialized?
Is gameplay admitted?
What was the last public operation/result?
What Activity occurrence/revision is this evidence associated with?
```

Advanced / Debug should expose richer correlation.

### Out of scope

No mutable setters. No second authority. No log parsing.

### Files altered

Expected:

```text
Runtime/PlayerParticipation/Contracts/
  existing snapshots/evidence

Runtime/PlayerParticipation/Authoring/
  public read-only projection/access

Editor/PlayerParticipation/
  inspector diagnostics where appropriate
```

### Files created

A new aggregate snapshot is allowed only if composition through current types cannot provide a stable consumer contract. If created, it must remain a projection and own no mutable state.

### Files removed

```text
none expected
```

### Product surface affected

Consumer observation and Advanced / Debug diagnostics.

### Expected flow

```text
authorities change
→ existing immutable evidence updates
→ consumer projection composes current truth
→ UI / QA reads projection
```

### Technical acceptance

```text
typed Slot identity preserved;
Host correlation preserved;
ActorId/runtime-content evidence available in Advanced;
Session revision/Activity occurrence correlation explicit;
stale projection distinguishable;
no mutation through observation.
```

### Product acceptance

A designer can tell the difference between:

```text
WaitingForJoin
WaitingForActorSelection
PreparingLogicalActor
Materializing
GameplayAdmission
Ready
Failed
```

without inspecting raw logs.

### Suggested commit

```text
feat(player): expose consumer provisioning observation
```

---

# 8. Cut P3 — IF-PLAYER-SURFACE-05
## Designer command authoring and validation

**Type:** package UX/product

### Objective

Provide explicit scene-authorable command triggers for the supported provisioning operations.

### Scope

Authoring operations:

```text
Open Joining
Close Joining
Set Capacity
Request Join
Request Default Actor Selection
```

The trigger must use P1 scoped access.

Designer Inspector:

```text
Operation
operation-specific parameters
scope/binding status
last typed result summary
Validate
Advanced / Debug correlation
```

Operations execute only through explicit invocation, for example a UI/Button UnityEvent or another authored call. The component must not perform gameplay automatically on `Awake`, `OnEnable` or editor validation.

### Out of scope

No arbitrary Actor selector. No prepare/reconcile button. No automatic join.

### Files created

Expected package area:

```text
Runtime/PlayerParticipation/Authoring/
  PlayerProvisioningCommandTrigger.cs

Editor/PlayerParticipation/
  PlayerProvisioningCommandTriggerEditor.cs
  related validator only when reusable validator infrastructure is insufficient
```

Exact names may follow existing package naming conventions after source inspection.

### Files altered

Existing creation menus / validator integration only when needed.

### Files removed

```text
none expected
```

### Product surface affected

Scene/prefab command authoring.

### Expected flow

```text
designer adds/selects trigger
→ chooses supported operation
→ wires Button.OnClick
→ runtime resolves scoped provisioning consumer
→ operation returns typed result
→ Inspector/debug exposes last result
```

### Technical smoke

Every operation succeeds/fails through its current typed result contract; unavailable authority is explicit.

### Technical acceptance

```text
idempotent editor behavior
Undo-safe
no hidden scene mutation
no direct authority mutation
no silent fallback
```

### Product acceptance

The FIRSTGAME join/open/capacity controls can be authored without writing C# or repurposing a Route trigger.

### Suggested commit

```text
feat(player-authoring): add provisioning command trigger
```

---

# 9. Cut P4 — IF-PLAYER-SURFACE-06
## Status and diagnostics binding surface

**Type:** package UX/product + observation

### Objective

Make public Player state straightforward to present in game UI and inspect technically without forcing every consumer to rebuild snapshot interpretation.

### Scope

Provide a small binding/presenter layer over P2.

Minimum designer-facing concepts:

```text
Joining
Capacity
Slot lifecycle
Selected Actor
Prepared / Materialized / Gameplay Ready
Last Operation
Activity / occurrence state
```

Technical details remain under Advanced / Debug.

Before adding a UI dependency, inspect current package dependencies. Prefer dependency-neutral binding/presentation if the package does not already canonically depend on TextMeshPro/UI.

### Out of scope

No visual style system. No game-specific labels. No state authority.

### Files created

Expected:

```text
Runtime/PlayerParticipation/Authoring/
  PlayerProvisioningStatusBinding.cs or equivalent

Editor/PlayerParticipation/
  matching designer-first Inspector
```

Optional UI adapter files are created only if an existing canonical package UI dependency supports them.

### Files altered

Existing Player inspector/debug surfaces as needed.

### Files removed

```text
none expected
```

### Product surface affected

Runtime status presentation + Inspector diagnostics.

### Technical acceptance

```text
binding reads public observation only;
no internal runtime module reference;
no per-frame scene scan;
no log parsing;
stale/unavailable scope is explicit.
```

### Product acceptance

A consumer can present the Demo03 status panel without writing a framework compatibility facade.

### Suggested commit

```text
feat(player-authoring): add provisioning status binding
```

---

# 10. Cut Q1 — QA-PLAYER-SURFACE-01
## Public-only positive contract proof

**Type:** technical QA / public API certification

### Objective

Prove the happy path using only the same public surface available to a real consumer.

### Allowed surface

```text
GameApplicationAsset
RouteAsset
ActivityAsset
PlayerSlotProfile
ActorProfile
public Player provisioning command surface
public Player observation surface
public readiness/gate observation
```

### Forbidden

```text
reflection
InternalsVisibleTo as consumer path
manual RuntimeScopeContext construction
direct prepare
direct materialize
direct gameplay admission
direct reconcile
external Slot mutation
scene lookup
```

### Proves

```text
Activity enters WaitingForJoin
→ Open Joining
→ Request Join
→ Slot/Host evidence becomes stable
→ default Actor selection through accepted public path
→ Actor prepared/materialized
→ gameplay admitted
→ readiness becomes Ready
→ gate releases
→ exit releases Activity-owned evidence
→ Session Host persists
→ reentry does not duplicate Slot/Actor
```

### Files created

Expected QA area:

```text
Assets/ImmersiveFrameworkQA/PlayerParticipation/
  QaPlayerProvisioningPublicSurfaceRegression.cs

Assets/ImmersiveFrameworkQA/Documentation/
  QA-PLAYER-SURFACE-01-2026-08-07.md
```

Use current canonical QA organization after A0; do not create another parallel harness structure.

### Technical acceptance

Public APIs alone reach the accepted happy path and all assertions use typed evidence.

### Product acceptance

Not applicable; QA proves contract, not UX.

### Suggested commit

```text
test(qa): prove public player provisioning surface
```

---

# 11. Cut F1 — FIRSTGAME-PLAYER-SURFACE-01
## Demo03 real command and status proof

**Type:** real consumer integration / UX

### Objective

Replace the current cenographic/incomplete Demo03 command and status panel with real package-owned Player command/observation surfaces.

### Scope

Preserve real existing composition:

```text
LocalPlayerProvisioning prefab
PlayerInputManager
LocalPlayerProvisioningAuthoring
LocalPlayerProvisioningHostRegistration
Manager-Provisioned Host prefab
PlayerInput
LocalPlayerHostAuthoring
Route/Activity authoring
```

Replace placeholder controls:

```text
Request Join A/B
Open Joining
Set Capacity 2
```

with P3 command authoring.

Connect status presentation to P4/P2:

```text
Joining
Capacity
Player 1
Player 2
Last Operation
Activity
```

### FIRSTGAME-owned work

```text
layout
button labels
status wording
real prefabs/assets
game-specific visuals/movement
short Demo03 README
```

### Forbidden

```text
new framework bridge C#
FindObjectOfType
static registry
reflection
manual prepare/reconcile
direct Slot mutation
repurposed RouteRequestTrigger for Player commands
```

### Files altered

Expected FIRSTGAME area:

```text
Assets/_Project/Demo03/
  current scene/prefabs/assets
  README
```

### Files created

Only game-specific presentation scripts if truly necessary; they may consume public observation but may not implement framework transport or aggregation.

### Files removed

```text
orphan Demo03/Scripts.meta if confirmed safe and truly orphaned
obsolete placeholder wiring/assets only when no longer referenced
```

### Technical smoke

```text
Open Joining changes real provisioning state;
Set Capacity changes real capacity;
Request Join creates one real admitted Slot/Host;
status panel reflects public snapshots;
no Route navigation is accidentally triggered by Player controls.
```

### Product acceptance

```text
a user understands Host, Slot and Actor;
WaitingForJoin appears as a valid state;
one Actor materializes;
status changes are explainable;
exit/reentry remain clean;
no game-local framework bridge is needed.
```

### Usability gain

The existing Demo03 becomes an actual consumer proof instead of a staged visual mock.

### Suggested commit

```text
feat(demo03): wire public player provisioning surface
```

---

# 12. Cut P5 — IF-PLAYER-SURFACE-07
## Recipe / Composer / creation workflow

**Type:** package UX/product

### Objective

Turn the now-proven manual composition into a repeatable official product workflow.

### Scope

Candidate model:

```text
ManagerProvisionedPlayerRecipe
  host prefab
  initial capacity
  initial joining state
  accepted default-selection policy

ManagerProvisionedPlayerComposer
  Recipe
  materialized technical components
  Apply / Rebuild
  Validate
  Advanced / Debug
```

Composer materialization may include:

```text
PlayerInputManager
LocalPlayerProvisioningAuthoring
LocalPlayerProvisioningHostRegistration
command/status authoring only when explicitly part of the selected recipe/template
```

### Out of scope

No runtime gameplay authority in Composer. No hidden components without Advanced/Debug.

### Files created

Expected package area:

```text
Runtime/PlayerParticipation/Authoring/
  ManagerProvisionedPlayerRecipe.cs
  ManagerProvisionedPlayerComposer.cs

Editor/PlayerParticipation/
  ManagerProvisionedPlayerComposerEditor.cs
  ManagerProvisionedPlayerComposerUtility.cs
  ManagerProvisionedPlayerCreationMenus.cs
```

### Files altered

Canonical Player authoring documentation and existing creation menus.

### Files removed

```text
none expected
```

### Product surface affected

Create menu / Recipe / Composer / Inspector / Apply-Rebuild.

### Expected flow

```text
Create Manager-Provisioned Player
→ choose/create Recipe
→ configure host/capacity/joining
→ Apply/Rebuild
→ validator reports result
→ normal Inspector shows intent
→ Advanced shows materialized contracts
```

### Technical acceptance

```text
Apply/Rebuild idempotent;
Undo-safe;
prefab-safe;
non-destructive;
no Play Mode authoring mutation;
no duplicate provisioner/registration;
no hidden fallback.
```

### Product acceptance

A new user can create and configure the feature without manually assembling all technical contracts.

### Suggested commit

```text
feat(player-authoring): add manager-provisioned player composer
```

---

# 13. Cut Q2 — QA-PLAYER-SURFACE-02
## Negative, stale-scope and lifecycle hardening

**Type:** technical QA + smallest package fixes only when evidence requires them

### Objective

Certify the consumer surface under invalid and changing runtime conditions.

### Required cases

```text
joining closed
capacity exhausted
duplicate/in-flight RequestJoin
invalid capacity request
authority unavailable
scope disposed/replaced
stale Session revision
stale Activity occurrence
late command result
Host evidence divergence
Actor selection/preparation failure
materialization failure
gameplay admission failure
exit while WaitingForJoin
exit during preparation
reentry
repeated observation/binding subscription
```

Provisioning callback expiry/late-divergent callback remains included only where the current runtime contract actually supports that path.

### Technical acceptance

```text
no false success
no silent fallback
no stale mutation
no second Actor
no leaked subscription
no stale observation presented as current
failure reason typed and diagnosable
```

### Suggested commit

```text
test(qa): harden player provisioning consumer surface
```

Any package fix discovered here must be delivered as a separate smallest-possible ZIP, not folded invisibly into QA.

---

# 14. Cut F2 — FIRSTGAME-PLAYER-SURFACE-02
## UX disposition and final consumer proof

**Type:** UX/product disposition

### Objective

Run the completed Demo03 as a real user workflow and route every remaining friction to the correct owner.

### Classification

```text
game-specific presentation
  → remains FIRSTGAME

repeated creation/configuration friction
  → package Recipe/Composer/authoring

missing public runtime contract
  → package runtime/public surface

missing negative proof
  → QAFramework

unclear technical state
  → package diagnostics

unclear setup/use
  → package documentation
```

### Acceptance

No framework-owned issue remains solved only inside FIRSTGAME.

### Suggested commit

```text
docs(demo03): record player provisioning consumer findings
```

---

# 15. Cut D1 — IF-DOC-PLAYER-SURFACE-01
## Canonical documentation and ADR closure

**Type:** package documentation / architecture closure

### Objective

Close IF-ADR-015 only after package, QA and FIRSTGAME evidence agree.

### Documentation must teach

```text
what Manager-Provisioned means
Host vs Slot vs Actor
how to create/configure the feature
how commands are authored
how status is observed
WaitingForJoin semantics
how Actor selection/preparation/materialization happen
what persists across Activity exit
how to debug revisions/occurrences
what is intentionally not public
```

### Files altered/created

Expected:

```text
Documentation~/Guides/
  Manager-Provisioned-Player-Usage.md

Documentation~/Architecture/ADRs/
  IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md

existing Player/authoring guide/index/tracker
```

Do not create a parallel documentation taxonomy if equivalent canonical files already exist.

### Closure acceptance

```text
public command path certified
public observation path certified
scoped access certified
Demo03 uses official surface
Recipe/Composer usable
negative matrix green
Advanced/Debug explains correlation
no FIRSTGAME compatibility facade
ADR wording matches shipped product
```

### Suggested commit

```text
docs(player): close provisioning consumer surface
```

---

# 16. Closure gates

## Gate C — Consumer contract ready

```text
A0
+ P0
+ P1
+ P2
```

## Gate CP — Product-facing command/status surface ready

```text
Gate C
+ P3
+ P4
```

## Gate QP — Public contract certified

```text
Gate CP
+ Q1
```

## Gate FG — Real consumer proof

```text
Gate QP
+ F1
```

## Gate PRODUCT — Authoring workflow complete

```text
Gate FG
+ P5
```

## Gate HARDENED — Negative surface certified

```text
Gate PRODUCT
+ Q2
```

## Gate ADR-015 — Closed

```text
Gate HARDENED
+ F2
+ D1
+ all package-owned UX findings resolved or explicitly deferred
```

FIRSTGAME visual success alone does not close IF-ADR-015.

---

# 17. Recommended immediate next action

Do not implement the Composer first.

The next action should be:

```text
IF-PLAYER-SURFACE-01E2
QA public-contract coverage audit
```

Immediately after that:

```text
IF-PLAYER-SURFACE-02
canonical consumer boundary freeze
```

Then the first implementation ZIP should be the smallest package cut that establishes scoped public consumer access:

```text
IF-PLAYER-SURFACE-03-scoped-provisioning-consumer-access.zip
```

That ZIP must not include:

```text
Composer
FIRSTGAME
new mutable Player state
status UI
QA
hardening unrelated to the access boundary
```

This order prevents the product layer from being designed around Demo03 placeholders and prevents QA internals from becoming accidental public API.

---

# 18. Definition of success

The program is complete when this user flow is real:

```text
Create/configure Manager-Provisioned Player
→ author Open/Close/Capacity/Join command triggers
→ enter Activity
→ see WaitingForJoin
→ invoke Join through public scoped surface
→ observe Slot + Host
→ observe Actor selection/preparation/materialization
→ observe gameplay admission
→ Activity becomes Ready
→ leave Activity
→ Session Host remains when intended
→ reenter without duplication
→ inspect technical correlation in Advanced / Debug
```

and all of the above occurs without:

```text
custom FIRSTGAME framework bridge
manual reconcile
manual prepare
internal APIs
scene lookup
global manager
service locator
silent fallback
duplicate state authority
```
