# Immersive Framework — Package Completeness and Consumer Integration Plan

**Date:** 2026-08-04  
**Status:** Proposed canonical execution plan  
**Primary objective:** complete the official package before treating FIRSTGAME configuration as product closure  
**Repositories:** `com.immersive.framework`, `QAFramework`, `planet-devourer`

## Normative inputs

```text
IF-ADR-007
  Activity Entry Readiness and Reveal Gating

IF-ADR-011
  Participant-Aware Activity Readiness Loading Progress

IMMERSIVE-FRAMEWORK-ACTIVITY-READINESS-AND-M07-IMPLEMENTATION-PLAN-2026-08-02-v2
  Generic readiness and Manager-Provisioned Player integration

IMMERSIVE-FRAMEWORK-M07-CANONICAL-CAPABILITY-MATRIX-2026-08-02
  Canonical M07 product/runtime gaps

IMMERSIVE-FRAMEWORK-PARTICIPANT-AWARE-READINESS-PROGRESS-IMPLEMENTATION-CUTS-2026-08-03
  WaitCovered determinate Loading progress cuts
```

Operational rule:

```text
Git repositories remain read-only.
Each implementation is delivered as a ZIP.
Each ZIP includes:
  created files
  altered files
  removed files
  CHANGESET.md
  source SHAs
  validation executed
  known limitations
```

---

# 1. Executive correction

The participant-aware progress plan does not contain every package gap required by M07.

It contains:

```text
Required/Optional completion evidence
stable readiness progress range
WaitCovered + FadeWithLoading runtime integration
generic QA for progress and terminal ordering
FIRSTGAME M03 WaitCovered proof
final readiness documentation
```

It explicitly excludes:

```text
M07
M08
Player readiness contribution
late-join reconciliation
automatic reconcile after stable Player changes
Manager-Provisioned public reachability
Manager-Provisioned Composer
provisioning hardening
```

The M07 package gaps remain defined by the integrated readiness/M07 plan:

```text
IF-M07-10
  Player readiness contribution and delta reconcile

IF-M07-11
  stable notifications and automatic reconcile

IF-M07-12
  public-only reachability proof

IF-M07-14
  Manager-Provisioned authoring and documentation

IF-M07-15
  provisioning hardening
```

This plan consolidates both programs without merging their responsibilities.

---

# 2. Repository responsibility

## 2.1 `com.immersive.framework`

The package owns:

```text
official contracts
runtime authorities
operation ordering
public product surfaces
authoring assets and components
validators
Advanced / Debug diagnostics
Recipes / Composers / Templates when accepted
canonical usage documentation
```

The package must contain the permanent solution.

## 2.2 `QAFramework`

QA owns:

```text
contract proof
negative cases
terminal ordering
idempotence
stale occurrence rejection
rollback and cleanup evidence
public-only reachability
regression baselines
```

QA must not define or replace the official solution.

## 2.3 `planet-devourer`

FIRSTGAME owns:

```text
real manual authoring
game-specific visual rules
game-specific subject conditions
happy-path usability proof
prefab reuse proof
short consumer documentation
UX findings
```

FIRSTGAME must not own:

```text
readiness authority
Loading progress calculation
Activity reconciliation
Actor preparation authority
gate retention
framework compatibility facade
reflection-based access
manual RuntimeScopeContext construction
```

---

# 3. Package completeness model

Package completeness is split into three gates.

## 3.1 Package Runtime Complete

```text
generic readiness correct
WaitCovered determinate progress correct
Player readiness contribution correct
late join reconciles the active occurrence
Actor preparation/materialization is idempotent
terminal failures are explicit
```

## 3.2 Package Consumer Surface Complete

```text
normal consumer uses public authoring and public requests
consumer never calls internal preparation/reconcile modules
public snapshots explain current progress
validators identify invalid authoring
Advanced / Debug exposes technical evidence
```

## 3.3 Package Product Complete

```text
creation path exists
designer-first Inspector exists
Apply/Rebuild exists where repeated composition justifies it
short canonical usage guide exists
sample/reference paths are current
FIRSTGAME UX findings have been dispositioned
provisioning edge semantics are hardened
```

A FIRSTGAME demo cannot close a package capability unless all package-owned behavior required by that demo already exists.

---

# 4. Stream P — Package implementation

This stream receives primary attention.

---

# P0 — IF-DOC-READY-PROGRESS-00
## Canonical ADR alignment

### Type

```text
package documentation / architecture
```

### Objective

Integrate IF-ADR-011 canonically and reconcile the older indeterminate-only statement in IF-ADR-007.

### Scope

```text
add IF-ADR-011 with its free canonical number;
reference IF-ADR-011 from IF-ADR-007;
limit participant-aware determinate progress to:
  WaitCovered
  FadeWithLoading
  progress-capable Loading surface;
preserve ObserveOnly and WaitVisible behavior;
state that implementation remains pending until the package cuts pass.
```

### Out of scope

```text
runtime
QA
FIRSTGAME
```

### Files created

```text
Documentation~/Architecture/ADRs/
  IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md
```

### Files altered

```text
Documentation~/Architecture/ADRs/
  IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md
```

Update an existing ADR index only when one already exists.

### Product surface affected

```text
canonical architecture documentation
```

### Technical acceptance

```text
no ADR number collision;
no contradictory progress rule;
no implementation claim before validation.
```

### Suggested commit

```text
docs(adr): align readiness loading progress decisions
```

---

# P1 — IF-READY-PROGRESS-01
## Occurrence completion evidence

### Type

```text
package runtime contract
```

### Objective

Expose separated Required/Optional completion evidence without changing Loading behavior.

### Scope

```text
RequiredCount
RequiredPendingCount
RequiredCompletedCount
RequiredFailedCount
RequiredReleasedCount

OptionalCount
OptionalPendingCount
OptionalCompletedCount
OptionalFailedCount
OptionalReleasedCount
```

Add an immutable projection equivalent to:

```text
ActivityReadinessProgressSnapshot
  occurrence identity
  Required counts
  Optional diagnostic counts
  readiness ratio
  aggregate Ready
  terminal failure
```

### Out of scope

```text
Loading ranges
GameFlow wiring
Player reconcile
FIRSTGAME
```

### Files created

```text
Runtime/ActivityFlow/
  ActivityReadinessProgressSnapshot.cs
```

### Files altered

```text
Runtime/ActivityFlow/
  ActivityReadinessOccurrenceState.cs
  ActivityReadinessState.cs
  ActivityReadinessRecomposer.cs
```

### Expected flow

```text
captured occurrence
→ participant state changes
→ separated counts recomputed
→ immutable progress evidence available
```

### Technical acceptance

```text
Optional completion never enters RequiredCompletedCount;
released Required is terminal evidence;
count invariants hold;
reentry creates a new snapshot;
no polling or scene scan.
```

### Product acceptance

```text
Advanced / Debug can explain “3 of 4 Required completed”.
```

### Suggested commit

```text
feat(activity-flow): expose readiness completion evidence
```

---

# P2 — IF-READY-PROGRESS-02
## Operation-scoped Loading progress envelope

### Type

```text
package runtime foundation
```

### Objective

Create one reusable monotonic envelope that reserves a final readiness range.

### Scope

Contracts equivalent to:

```text
ActivityEntryLoadingProgressPlan
ActivityEntryLoadingProgressEnvelope
FrameworkLoadingProgressRange
```

Rules:

```text
technical range is known before operation execution;
one readiness unit is reserved when applicable;
technical reporter cannot publish 1.0;
Required completion subdivides the readiness range equally;
1.0 requires aggregate Ready;
terminal failure never publishes 1.0;
duplicate terminal observation is idempotent.
```

### Out of scope

```text
request-path wiring
Player
FIRSTGAME
Loading UI redesign
```

### Files created

```text
Runtime/Loading/
  ActivityEntryLoadingProgressPlan.cs
  ActivityEntryLoadingProgressEnvelope.cs
  FrameworkLoadingProgressRange.cs
```

Create the range type only when no equivalent existing type can be reused.

### Technical acceptance

```text
monotonic;
finite;
zero technical steps supported;
zero Required supported only with aggregate Ready;
no timer or frame dependence.
```

### Suggested commit

```text
feat(loading): add activity entry progress envelope
```

---

# P3 — IF-READY-PROGRESS-03
## WaitCovered determinate progress integration

### Type

```text
package runtime integration
```

### Objective

Integrate the envelope into all initial WaitCovered entry paths.

### Scope

```text
direct Activity request
Route request with Startup Activity
Game Application startup with Startup Activity
```

Canonical ordering:

```text
Loading Show
→ technical progress below 100%
→ occurrence captured
→ Required participant increments
→ aggregate Ready
→ Loading Update 100%
→ Loading Hide
→ Transition After
→ reveal
→ capability gate release
→ request success
```

### Files altered

```text
Runtime/ApplicationLifecycle/
  FrameworkRuntimeHost.cs

Runtime/GameFlow/
  GameFlowRuntime.cs

Runtime/ActivityFlow/
  ActivityFlowRuntime.cs
  ActivityFlowStartResult.cs

Runtime/RouteLifecycle/
  RouteLifecycleRuntime.cs
  RouteLifecycleStartResult.cs

existing Loading diagnostics owner
```

### Technical acceptance

```text
technical progress never reaches 1.0 before Ready;
Optional does not alter progress;
Failed/Released/Invalidated/Cancelled never publish 1.0;
old occurrence cannot advance replacement;
100% precedes Hide;
Hide precedes reveal;
ObserveOnly and WaitVisible remain unchanged.
```

### Product acceptance

```text
a progress-capable WaitCovered Loading surface never displays 100% while Preparing.
```

### Suggested commit

```text
feat(game-flow): project readiness into wait-covered loading progress
```

---

# P4 — IF-M07-10
## Player readiness contribution and delta reconcile

### Type

```text
package PlayerParticipation runtime
```

### Objective

Make the Manager-Provisioned Player lifecycle participate in the active Activity readiness occurrence and progress after late join.

### Scope

Initial supported shape:

```text
ExplicitSlots
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
```

Contribution states:

```text
Preparing / WaitingForJoin
Preparing / WaitingForActorSelection
Preparing / PreparingLogicalActor
Preparing / PreparingGameplayAdmission
Completed / RequirementSatisfied
Failed / explicit typed reason
Released / ActivityExit
```

Delta reconcile must:

```text
validate exact Activity and occurrence
validate RuntimeContentOwner
compare Session and per-Slot revisions
apply only missing work
select default Actor only under the current accepted policy
prepare logical Actor
materialize physical Actor
ensure gameplay/input/camera evidence when required
complete the contribution
```

### Out of scope

```text
Session Leave
disconnect/reconnect
AllJoinedSlots dynamic membership
Composer
callback expiry
```

### Files altered

Expected package area:

```text
Runtime/PlayerParticipation/Runtime/
  ActivityPlayerActorLifecycleParticipant.cs
  ActivityPlayerActorLifecycleParticipant.*.cs
  PlayerActorPreparationRuntimeHostModule.cs
  PlayerGameplayRuntimeHostModule.cs

Runtime/PlayerParticipation/Contracts/
  lifecycle/readiness snapshot contracts

Runtime/ActivityFlow/
  internal contribution adapter contracts when needed
```

### Technical acceptance

```text
unjoined Explicit Slot is Preparing, not failure;
one Actor per Slot/occurrence;
repeated reconcile with no revision is SucceededNoChange;
failure is explicit;
exit while waiting releases safely;
exit after Ready releases exact contextual ownership;
reentry creates one fresh Actor;
no runtime reflection.
```

### Product acceptance

```text
normal Activity participation authoring is sufficient;
no technical “Prepare Actor” button is required.
```

### Suggested commit

```text
feat(player): contribute manager-provisioned lifecycle to activity readiness
```

---

# P5 — IF-M07-11
## Stable notifications and automatic active-Activity reconcile

### Type

```text
package host-scoped orchestration
```

### Objective

Automatically reconcile the current Activity only after stable Session commits.

### Scope

Publish stable change only after:

```text
Slot Joined
assignment committed
Host admission committed
Host evidence registered
```

Selection change is published only after selection revision commit.

Add a host-scoped coordinator that:

```text
serializes reconcile
coalesces revisions
rejects stale occurrence
does not run through an incompatible transition mutation
queues another pass when revision changes during execution
publishes immutable diagnostics
```

### Files altered/created

Expected package area:

```text
Runtime/PlayerParticipation/Runtime/
  LocalPlayerProvisioningRuntimeHostModule.cs
  LocalPlayerProvisioningBridge.cs
  PlayerActorPreparationRuntimeHostModule.cs
  PlayerActivityReconciliationRuntimeHostModule.cs

Runtime/ApplicationLifecycle/
  FrameworkRuntimeHost*.cs

Runtime/GameFlow/
  scoped Player integration files when required
```

### Technical acceptance

```text
public RequestJoin is enough to trigger progress;
no race between Joined and Host evidence;
one stable change causes one effective reconcile;
repeated no-change creates no Actor/binding/request;
coordinator is scoped, not global;
revision requested/applied is diagnostic.
```

### Product acceptance

```text
FIRSTGAME never invokes reconcile directly.
```

### Suggested commit

```text
feat(player): reconcile active activity after stable player changes
```

---

# P6 — IF-M07-12A
## Public observability and consumer-safe surface

### Type

```text
package public/product contract
```

### Objective

Expose enough read-only evidence for a normal consumer and QA public-only proof without exposing internal runtime authority.

### Scope

Public or game-facing evidence equivalent to:

```text
current Activity entry policy
current readiness status
current readiness reason
gate held/released
joining state
Host count
Slot state
selected Actor
logical Actor preparation state
physical Actor materialization state
gameplay admission state
occurrence/revision correlation in Advanced / Debug
```

Normal commands remain:

```text
OpenJoining
CloseJoining
RequestJoin
RequestDefaultActorSelection when still explicitly required
```

Do not expose:

```text
RuntimeScopeContext
TryPrepareSelectedActor
TryEnsureCurrentGameplay
internal reconciliation module
mutable participation state
```

### Files altered

Expected package area:

```text
Runtime/ActivityFlow/
  public/read-only presentation snapshots or events

Runtime/PlayerParticipation/Contracts/
  public immutable diagnostics

Runtime/PlayerParticipation/Authoring/
  existing authoring diagnostics/events

Editor/PlayerParticipation/
  designer-first status and Advanced / Debug presentation
```

### Technical acceptance

```text
no second authority;
snapshots immutable;
occurrence and Slot evidence correlate;
consumer cannot mutate runtime state through diagnostics.
```

### Product acceptance

```text
a designer can understand WaitingForJoin → Ready without raw logs.
```

### Suggested commit

```text
feat(player): expose manager-provisioned readiness diagnostics
```

---

# P7 — IF-M07-14
## Manager-Provisioned authoring workflow

### Type

```text
package UX/product
```

### Objective

Provide a canonical creation and maintenance workflow for recurring Manager-Provisioned composition.

### Scope

Preferred shape:

```text
ManagerProvisionedPlayerRecipe
  Local Player Host Prefab
  Initial Capacity
  Initial Joining State

ManagerProvisionedPlayerComposer
  Recipe
  PlayerInputManager
  Apply / Rebuild
  Validate

Advanced / Debug
  LocalPlayerProvisioningAuthoring
  LocalPlayerProvisioningHostRegistration
  LocalPlayerActorSelectionRequestAuthoring
  runtime snapshots
```

The Activity remains authority for:

```text
Participation
Entry Readiness Policy
Transition
Gate
```

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

Documentation~/Guides/
  Manager-Provisioned-Player-Usage.md
```

### Files altered

```text
Documentation~/Guides/
  Player-Usage.md
```

### Technical acceptance

```text
Apply/Rebuild idempotent;
Undo/dirty/prefab handling correct;
non-destructive;
no scene mutation in Play Mode;
technical components remain visible in Advanced;
validation never creates fallback.
```

### Product acceptance

```text
user creates and configures the host without knowing runtime modules;
join, selection, preparation, Activity release and Session Leave are explained separately.
```

### Suggested commit

```text
feat(player-authoring): add manager-provisioned composition workflow
```

### Placement in execution

This package cut may be shaped after FIRSTGAME exposes final UX friction, but package completeness is not declared until it lands.

---

# P8 — IF-M07-15
## Provisioning hardening

### Type

```text
package runtime + diagnostics
```

### Objective

Close callback confirmation and repeated request semantics after the happy path is stable.

### Scope

```text
callback confirmation policy
deterministic expiry or explicit optional-callback decision
late callback
divergent callback
unexpected callback
request already in flight
second Player request
capacity/no Slot
cleanup
```

### Out of scope

```text
Session Leave
disconnect/reconnect
readiness contract changes
```

### Technical acceptance

```text
pending confirmation cannot remain unexplained;
stale callback cannot confirm another request;
expiry cannot silently destroy an admitted Player;
repeated RequestJoin has explicit additional-Player semantics;
diagnostics distinguish duplicate operation from capacity rejection.
```

### Suggested commit

```text
fix(player): harden local player provisioning confirmation
```

---

# P9 — IF-M07-16
## Session Player Leave

### Type

```text
separate package capability
```

### Status

```text
Deferred
```

### Open only for a real product requirement

```text
Player leaves the Session
```

It must not be implemented as an implicit Activity-exit effect.

---

# 5. Stream Q — QA validation

QA follows official package cuts.

---

# Q1 — QA-READY-PROGRESS-01
## Positive participant-aware Loading progression

### Proves

```text
4 Required
1 Optional

technical completion below 100%
0/4
1/4
2/4
3/4
4/4 + Ready = 100%

Optional pending does not alter denominator
Optional failed does not alter denominator
100% before Hide
Hide before reveal
gate release after Ready
```

### Constraints

```text
typed evidence
no Task.Delay
no timeout
no frame polling
no log parsing
no global object lookup
preserve QA-03 exact 42-case baseline
```

---

# Q2 — QA-READY-PROGRESS-02
## Terminal paths and startup parity

### Proves

```text
Required failed -> no 100%
Required released -> no 100%
occurrence invalidated -> no 100%
wait cancelled -> no 100%
late old occurrence -> no progress
duplicate terminal -> idempotent

direct Activity
Route Startup Activity
Game Application Startup Activity
```

---

# Q3 — QA-M07-INTERNAL
## Player reconcile authority and idempotence

### Proves

```text
exact owner
revision coalescing
one Actor per Slot/occurrence
delta rollback
exit while waiting
exit after Ready
replacement during reconcile
reentry
```

This QA may use package internal access when the intent is to prove internal authority.

---

# Q4 — QA-M07-PUBLIC
## Public-only Manager-Provisioned reachability

### Allowed surface

```text
GameApplicationAsset
RouteAsset
ActivityAsset
ActivityEntryReadinessPolicy
PlayerSlotProfile
ActorProfile
public provisioning authoring requests
public readiness and Player diagnostics
```

### Forbidden

```text
reflection
InternalsVisibleTo as the main path
manual RuntimeScopeContext
direct prepare/gameplay calls
external Slot mutation
consumer Destroy
```

### Proves

```text
WaitVisible Activity
→ WaitingForJoin
→ RequestJoin
→ Host and Slot stable
→ automatic reconcile
→ Actor selected/prepared/materialized
→ requirement satisfied
→ Ready
→ gate released
→ contextual exit release
→ Session Host preserved
→ reentry without duplication
```

---

# Q5 — QA-M07-HARDENING
## Provisioning negatives

### Proves

```text
joining closed
capacity
duplicate/in-flight request
callback expiry
late/divergent callback
stale owner
transition conflict
preparation failure
release failure
cleanup
```

---

# 6. Stream F — FIRSTGAME integration

FIRSTGAME only begins after the corresponding package and QA gates pass.

---

# F1 — FIRSTGAME-M03-READY-PROGRESS-01
## WaitCovered product proof

### Prerequisites

```text
P1, P2, P3 complete
Q1 and Q2 complete
```

### Scope

```text
Observe Only
Wait Visible
Wait Covered
Intermission

WaitCovered:
  FadeWithLoading
  InputInteractionAndGameplay
  4 independent Required participants
  1 Optional participant
```

Each chicken may complete one assigned Required participant. The framework counts participants, not chickens.

### FIRSTGAME-owned work

```text
scene composition
subject-target visual preparation
thin explicit participant bridge when required
menu button
persistent Loading presentation configuration
short README
UX findings
```

### Forbidden

```text
direct Loading updates
runtime host lookup
local progress calculation
framework authority replacement
```

### Acceptance

```text
technical phase stops below 100%;
each Required completion advances Loading;
Optional remains pending without distortion;
100% occurs only after Ready;
reveal occurs after Loading Hide;
reentry creates a new occurrence.
```

---

# F2 — FIRSTGAME-M07
## Real Manager-Provisioned consumer proof

### Prerequisites

```text
P4, P5, P6 complete
Q3 and Q4 complete
F1 complete
```

### Scope

```text
WaitVisible Activity
persistent Join control
Host creation
Slot admission
automatic reconcile
Actor selection/preparation/materialization
Ready
gate release
contextual exit
Session Host preserved
reentry
```

### FIRSTGAME-owned work

```text
real prefabs and assets
navigation
status presentation consuming public snapshots
game-specific movement/presentation when required
README
UX findings
```

### Forbidden

```text
manual prepare button
manual reconcile button
reflection
internal APIs
FindObjectOfType authority
external Slot mutation
Destroy repair
M08 policy mixing
```

### Acceptance

```text
designer understands Host, Slot and Actor;
WaitingForJoin is visible and not presented as failure;
Join remains usable while gameplay is gated;
one Actor materializes;
exit and reentry are clean;
only public package surfaces are used.
```

---

# F3 — FIRSTGAME UX disposition

### Objective

Record findings from M03 and M07 and route each finding correctly.

```text
game-specific presentation
  stays in FIRSTGAME

repeated authoring friction
  package authoring/template

missing public contract
  stop and return to package runtime

missing negative proof
  QAFramework

wrong or incomplete guide
  package documentation
```

Package Product Complete remains open until package-owned findings are resolved or explicitly deferred.

---

# 7. Canonical execution order

```text
P0  ADR alignment

P1  readiness completion evidence
P2  Loading progress envelope
P3  WaitCovered runtime integration

Q1  positive progress QA
Q2  terminal/startup QA

F1  FIRSTGAME M03 WaitCovered

P4  Player readiness contribution/delta reconcile
P5  stable notifications/automatic reconcile
P6  public observability surface

Q3  Player internal QA
Q4  Player public-only QA

F2  FIRSTGAME M07 real

P7  Manager-Provisioned Recipe/Composer/docs
P8  provisioning hardening
Q5  provisioning negative QA

F3  final UX disposition
package documentation closure
```

`P7` may begin with UX prototypes after `P6`, but final product closure should consume the findings from `F2`.

`P9 Session Leave` remains separate and deferred.

---

# 8. Closure gates

## Gate R — Generic readiness runtime complete

```text
P1 + P2 + P3
```

## Gate RQ — Generic readiness technically proven

```text
Gate R + Q1 + Q2
```

## Gate M03 — Generic readiness product proof complete

```text
Gate RQ + F1
```

## Gate P — Manager-Provisioned runtime complete

```text
P4 + P5 + P6
```

## Gate PQ — Manager-Provisioned public contract proven

```text
Gate P + Q3 + Q4
```

## Gate M07 — Manager-Provisioned real consumer proof complete

```text
Gate PQ + F2
```

## Gate PACKAGE — Package capability complete

```text
Gate M03
+ Gate M07
+ P7
+ P8
+ Q5
+ final canonical documentation
+ all package-owned UX findings resolved or explicitly deferred
```

M07 being visually functional in FIRSTGAME is not equivalent to `Gate PACKAGE`.

---

# 9. Current status against this plan

## Already completed before this plan

```text
IF-READY-01 aggregate semantics
IF-READY-02 policy authoring/validation
IF-READY-03 occurrence waiter
IF-READY-04 reveal/gate orchestration

generic QA foundation
generic readiness policy regression
QA causal async hardening

FIRSTGAME M03:
  Observe Only
  Wait Visible
  shared reusable scenario

FIRSTGAME M07:
  base assets
  persistent provisioning composition
  menu/navigation
  command channel
  Open/Close Joining
  RequestJoin
  default Actor selection request
```

## Pending package work

```text
P0 canonical ADR reconciliation
P1 completion counts/progress snapshot
P2 Loading progress envelope
P3 WaitCovered determinate integration
P4 Player contribution/delta reconcile
P5 stable notifications/automatic reconcile
P6 public observability
P7 Recipe/Composer/docs
P8 provisioning hardening
```

## Pending QA work

```text
Q1
Q2
Q3
Q4
Q5
```

## Pending FIRSTGAME work

```text
F1 M03 WaitCovered
F2 M07 real end-to-end
F3 UX disposition
```

---

# 10. Immediate next action

The next implementation ZIP remains package-only:

```text
IF-READY-PROGRESS-01-readiness-completion-evidence.zip
```

It contains only:

```text
Runtime/ActivityFlow/
  ActivityReadinessProgressSnapshot.cs
  ActivityReadinessOccurrenceState.cs
  ActivityReadinessState.cs
  ActivityReadinessRecomposer.cs

CHANGESET.md
```

It must not contain:

```text
GameFlow integration
PlayerParticipation changes
QAFramework
FIRSTGAME
```

After P1, proceed to P2 and P3 before returning to FIRSTGAME.

The first M07-specific package ZIP is opened only after Gate M03:

```text
IF-M07-10-player-readiness-contribution-and-delta-reconcile.zip
```

This preserves the package-first dependency order and prevents FIRSTGAME from becoming a local framework implementation.
