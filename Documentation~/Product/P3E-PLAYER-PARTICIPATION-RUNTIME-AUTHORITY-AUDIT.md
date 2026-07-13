# P3E — Player Participation Runtime Authority Audit

**Status:** Accepted / DG-P3-02 closed  
**Date:** 2026-07-13  
**Package:** `com.immersive.framework`  
**Baseline:** `e172547a1d46ab61ba28a207faf60d5e18aef9bb` — P3D.2  
**Type:** Runtime reuse audit and architecture gate  
**Implementation:** None in this cut  
**Related:** `ADR-PROD-0003`, `ADR-PROD-0007`, `ADR-PROD-0009`, `ADR-PROD-0010`, `ADR-PROD-0011`, `ADR-PROD-0012`, `ADR-PROD-0013`, P3 implementation plan

---

## 1. Objective

Close `DG-P3-02` by determining where mutable Player participation state belongs before implementing Slot roster, dynamic capacity, reservation, join or Actor selection.

Questions resolved:

```text
Which runtime scope owns the participation roster?
Does an existing runtime object already have the correct lifetime?
Is a new PlayerParticipation context required?
How is it accessed without a service locator?
Which state survives Route changes?
Which state remains Route/Activity-scoped?
Which existing contracts are reusable?
Which existing classes must not be expanded?
```

This cut changes documentation only.

---

## 2. Scope

Inspected:

```text
FrameworkRuntimeHost
FrameworkRuntimeState
SessionRuntimeState
GameFlowRuntime
RouteLifecycleRuntime and Route RuntimeContent scope
ActivityFlowRuntime and Activity RuntimeContent scope
RuntimeContentRuntime, owners, contexts, handles and operation results
PlayerEntry / PlayerEntryBehaviour role from current P3A audit
PlayerSlotOccupancy and topology role from current P3A audit
PlayerControlRuntimeContext behavior and QA evidence
PlayerSlotProfile / GameApplication ordered Slot configuration
Activity participation Projection and Requirements authoring from P3D
Runtime/Common normalization and lifecycle-operation patterns
```

Out of scope:

```text
runtime implementation
LocalPlayerJoinRequest / Result final shape
PlayerInputManager calls
ActorProfile selection API
Actor materialization
Activity admission gate integration
leave / reconnect lifecycle
FIRSTGAME integration
```

---

## 3. Executive decision

Mutable Player participation requires one dedicated domain context:

```text
PlayerParticipationRuntimeContext
```

Initial architectural status:

```text
internal sealed plain C# runtime object
one instance per FrameworkRuntimeHost boot / Session lifetime
created and owned explicitly by FrameworkRuntimeHost
initialized from GameApplicationAsset.LocalPlayerSlots
not a MonoBehaviour
not static
not a singleton
not a service locator
not stored in a ScriptableObject
not owned by RouteLifecycleRuntime or ActivityFlowRuntime
```

`FrameworkRuntimeHost` already has the correct lifetime and composition role. It must own and inject the domain context, but it must not absorb the Slot state machine and become a Player manager.

Canonical relationship:

```text
FrameworkRuntimeHost
  Session/Application lifetime envelope
  creates and owns
    PlayerParticipationRuntimeContext
      ordered Session Slot roster
      allocation state
      dynamic capacity
      join window
      reservation state
      Session-persistent selection references
      typed snapshots and operation evidence
```

Route and Activity consume read-only participation evidence through explicit typed dependencies. They do not own or mutate the Session roster directly.

No separate `PlayerManager`, generic `SessionContext` registry or global access facade is accepted.

---

## 4. Why the existing Session lifetime is correct

### 4.1 FrameworkRuntimeHost

Current evidence:

```text
persistent DontDestroyOnLoad runtime object
one runtime host per framework boot
creates RuntimeContentRuntime
creates Session RuntimeContent root
creates GameFlowRuntime
creates Pause and Global UI runtime modules
injects Camera session dependencies
retains GameApplication and FrameworkRuntimeState
survives Route and Activity changes
```

This is the correct composition root for Session-owned participation.

Accepted use:

```text
FrameworkRuntimeHost creates PlayerParticipationRuntimeContext.
FrameworkRuntimeHost supplies explicit dependencies.
FrameworkRuntimeHost coordinates startup and final release.
FrameworkRuntimeHost may expose narrow internal forwarding/snapshot methods.
```

Rejected use:

```text
put all Player participation logic directly in FrameworkRuntimeHost
expose PlayerParticipationRuntimeContext through a public static Instance
spread FrameworkRuntimeHost.TryGetCurrent calls through Player components
turn the host into a generic service registry
let scene-authored objects discover the context through Find APIs
```

### 4.2 SessionRuntimeState

`SessionRuntimeState` is currently an immutable aggregate snapshot of:

```text
GameApplication
current Route
Route lifecycle result
Session/Route content sets
primary scene result
Activity flow result
Session started state
```

It is not an operational domain context.

Decision:

```text
Do not place mutable Slot records inside SessionRuntimeState.
Do not turn SessionRuntimeState into a mutation API.
```

Future diagnostics may include a `PlayerParticipationSnapshot` inside a higher-level framework diagnostic snapshot, but the mutable owner remains `PlayerParticipationRuntimeContext`.

### 4.3 Existing Session RuntimeContent root

The host already creates:

```text
RuntimeContentOwner.Session(...)
RuntimeScopeContext for Session
```

This root is reusable for physical Session-owned RuntimeContent when a later feature needs it.

It is not the Slot roster.

The participation context may retain the Session owner/context as an explicit dependency only when P3J or later materialization requires Session-scoped content. P3F must not register Slots as fake RuntimeContent handles.

---

## 5. Reuse map

| Existing type / area | Current authority | P3 reuse decision | Must not become |
| --- | --- | --- | --- |
| `FrameworkRuntimeHost` | Application/Session composition root | Own and create the participation context; provide explicit injection and diagnostics | Player manager or service locator |
| `GameApplicationAsset.LocalPlayerSlots` | Ordered immutable product configuration | Source of initial Slot roster and canonical allocation order | Mutable Session state |
| `PlayerSlotProfile` | Immutable Slot identity and presentation | Referenced by each runtime Slot record | Joined/reserved/selected mutable asset |
| `SessionRuntimeState` | Immutable aggregate lifecycle snapshot | Remain lifecycle snapshot; may later include read-only participation summary | Operational Slot state machine |
| `GameFlowRuntime` | Route/Activity request coordinator | Later consume an injected Activity admission evaluator | Participation roster owner |
| `RouteLifecycleRuntime` | Active Route and Route scope | Later own Route projection/materialization results | Session roster or join capacity owner |
| `ActivityFlowRuntime` | Active Activity and Activity scope | Later evaluate/retain Activity-scoped projection/admission results | Session roster or selection authority |
| `RuntimeContentRuntime` | Scoped physical/logical content roots and handles | Reuse owner/context/handle/release patterns for Actor materialization | Participation database or Slot registry |
| `PlayerControlRuntimeContext` | Per admitted Player control binding evidence | Reuse after provisioning/admission; one context per admitted Player | Pre-join Slot or Session roster state |
| `PlayerSlotOccupancy` | Effective Slot → Actor relation | Reuse only after contextual Actor admission | Join state, reservation or spawner |
| `PlayerEntry` / `PlayerEntryBehaviour` | Passive Slot/Actor/readiness vocabulary/evidence | Reference vocabulary where useful; do not make it the Session pipeline | Session authority or join orchestrator |
| Player topology sets/validators | Passive structural validation | Reuse targeted validation only when the final topology exists | Runtime roster authority |
| `ActivityParticipationProjectionProfile` | Immutable contextual selection policy | Runtime evaluator consumes descriptor + Session snapshot | Mutable projected participant list |
| `PlayerParticipationRequirementsProfile` | Immutable Activity readiness policy | Runtime admission evaluator consumes it later | Runtime readiness storage |
| `FrameworkStringExtensions` | Text normalization helper | Reuse for source/reason/diagnostic normalization | Identity authority |
| lifecycle operation/result patterns | Explicit typed requests/results/issues/snapshots | Follow for P3F operations and diagnostics | Ambiguous booleans or exception-only control flow |

---

## 6. Authority matrix

| Concern | Authoring authority | Mutable runtime authority | Contextual consumer |
| --- | --- | --- | --- |
| Configured Slot order | `GameApplicationAsset` | `PlayerParticipationRuntimeContext` copies ordered references at initialization | join allocation, diagnostics |
| Slot identity and presentation | `PlayerSlotProfile` | Runtime Slot record references Profile and typed `PlayerSlotId` | UI, diagnostics, join |
| Slot allocation state | none | `PlayerParticipationRuntimeContext` | join bridge, diagnostics |
| Dynamic join capacity | future explicit product operation | `PlayerParticipationRuntimeContext` | join admission |
| Join window open/closed | future explicit product operation | `PlayerParticipationRuntimeContext` | join admission |
| Reservation | none | `PlayerParticipationRuntimeContext` | P3G provisioning bridge |
| Pending local join correlation | none | Session participation context or a typed child operation owned by it | P3G only; final shape deferred |
| Joined participant evidence | none | `PlayerParticipationRuntimeContext` | UI, selection, Activity projection |
| Selected `ActorProfile` | future explicit default/selection authoring | `PlayerParticipationRuntimeContext` | materialization and Activity requirements |
| Activity Slot projection policy | `ActivityParticipationProjectionProfile` | no mutable Profile state | Activity-scoped projection evaluator/result |
| Activity requirement policy | `PlayerParticipationRequirementsProfile` | no mutable Profile state | Activity-scoped admission evaluator/result |
| Logical Actor instance | `ActorProfile` + contextual authoring | Route/Activity materialization operation and handles | occupancy/input/camera |
| Effective occupancy | none | Route/Activity/player admission scope | gameplay readiness |
| Per-Player input/control binding | player host / adapters | `PlayerControlRuntimeContext` per admitted Player | gameplay readiness |
| Camera winner | Camera requests/recipes | `CameraOutputContext` / session output authority | local Player request publisher |

---

## 7. Lifetime model

```text
Framework boot
└── FrameworkRuntimeHost                         [Application / Session]
    ├── RuntimeContentRuntime
    │   ├── Session RuntimeContent root          [Session]
    │   ├── Route RuntimeContent root            [current Route]
    │   └── Activity RuntimeContent root         [current Activity]
    │
    ├── PlayerParticipationRuntimeContext        [Session]
    │   ├── ordered Slot runtime records
    │   ├── dynamic capacity
    │   ├── join window
    │   ├── reservations / joined facts
    │   └── selected ActorProfile references
    │
    └── GameFlowRuntime
        └── RouteLifecycleRuntime                [current Route]
            └── ActivityFlowRuntime              [current Activity]
                ├── projection evaluation result [Activity transition / Activity]
                ├── admission result             [Activity transition / Activity]
                ├── Actor materialization         [Route or Activity]
                ├── effective occupancy           [Route or Activity]
                └── control/camera bindings       [admitted Player/context]
```

Route change:

```text
keep:
  PlayerParticipationRuntimeContext
  configured Slot order
  allocation/join state
  selected ActorProfile references
  Session-level input/user evidence that remains valid

release or recompute:
  Activity projection result
  Activity admission result
  Activity-owned Actor instances
  Activity-owned occupancy
  Activity-specific input/camera bindings
  Route-owned Actor instances when Route exits
```

Activity change inside one Route:

```text
keep:
  Session Slot roster
  join state
  persistent selection
  Route-owned Actor host when policy explicitly retains it

release or recompute:
  previous Activity projection/admission
  Activity-owned Actor host
  Activity occupancy and activation
  Activity-specific bindings
```

Framework host destruction/restart:

```text
release all reservations and runtime references explicitly
invalidate snapshots/tokens
release child contexts
remove Session RuntimeContent root through the established lifecycle
```

---

## 8. State ownership table

### 8.1 Session-owned mutable state

Owned by `PlayerParticipationRuntimeContext`:

```text
ordered runtime Slot records
PlayerSlotProfile reference per record
typed PlayerSlotId per record
configured index
allocation state:
  Unavailable
  Available
  Reserved
  Joined
  Leaving
current dynamic capacity
join window state
reservation identity/token and revision
pending operation correlation metadata when P3G defines it
joined technical evidence after successful admission
optional selected ActorProfile reference after P3H
per-Slot revision / context revision
last operation/result diagnostics
```

A joined Slot without `ActorProfile` remains Joined and unavailable for a new reservation.

### 8.2 Contextual Route/Activity state

Not stored as mutable Session Slot fields:

```text
current projection membership result
Activity requirement evaluation result
PendingResolution / Blocked / Failed admission status
logical Actor materialization handle
Route/Activity owner identity
spawn/activation position
current effective occupancy
Activity-specific readiness evidence
Activity-specific input gate state
Activity/local camera request
```

Session may expose the source facts required to calculate these states. It does not retain stale contextual results as universal truth.

### 8.3 Profile state

Never mutable at runtime:

```text
PlayerSlotProfile
PlayerParticipationRequirementsProfile
ActivityParticipationProjectionProfile
future ActorProfile
```

---

## 9. Typed access and injection

### 9.1 Accepted pattern

```text
FrameworkRuntimeHost
  constructs PlayerParticipationRuntimeContext
  passes it explicitly to the next runtime integration layer

P3G local join bridge
  receives a typed reservation/admission dependency

P3L Activity admission evaluator
  receives a read-only participation snapshot source

Diagnostics
  request a typed snapshot from the context
```

Preferred internal capability boundaries for later implementation:

```text
IPlayerParticipationSnapshotSource
  CreateSnapshot()
  TryGetSlotSnapshot(...)

IPlayerSlotReservationRuntime
  TryReserveNextAvailableSlot(...)
  TryReleaseReservation(...)
  TryMarkJoined(...)

IPlayerParticipationPolicyRuntime
  TrySetDynamicCapacity(...)
  TryOpenJoining(...)
  TryCloseJoining(...)
```

These are candidate boundaries, not mandatory P3E code artifacts. P3F should create only the minimum interfaces justified by actual consumers.

### 9.2 Rejected access

```text
PlayerParticipationRuntimeContext.Instance
FrameworkRuntimeHost.Current.PlayerParticipation
Object.FindFirstObjectByType<PlayerParticipation...>()
Resources.FindObjectsOfTypeAll as functional resolution
scene hierarchy path lookup
GameObject name/tag lookup
ScriptableObject mutation
public generic GetService<T>()
```

`FrameworkRuntimeHost.TryGetCurrent` already exists as an internal framework-core bridge. P3 must not spread new participation access through it. Scene adapters must be wired by explicit Framework Core injection or concrete authoring references.

---

## 10. P3F API candidates

Exact names may be refined during implementation, but the semantic boundary is closed.

### 10.1 Context initialization

```text
Create / Initialize
  input:
    ordered IReadOnlyList<PlayerSlotProfile>
    initial dynamic capacity
    initial join-window policy
    source
    reason

  result:
    typed initialization result
    immutable initial snapshot
    explicit issues for null/duplicate/invalid Profiles or identities
```

Initialization requirements:

```text
preserve GameApplication array order
resolve PlayerSlotId from each Profile
reject null Profile references
reject repeated Profile references
reject duplicate PlayerSlotId
never derive identity from array index or PlayerInput.playerIndex
never mutate Profiles
```

### 10.2 Capacity and join window

```text
TrySetDynamicCapacity
TryOpenJoining
TryCloseJoining
```

Capacity reduction remains non-destructive:

```text
joined Slots remain Joined
reservations already admitted are not silently evicted
new reservations are blocked while admitted/reserved count exceeds policy
```

P3F must define explicitly whether an in-flight reservation counts against capacity. Recommended initial rule:

```text
Reserved and Joined both consume current join capacity.
```

This follows the atomic reservation requirement and prevents same-frame over-admission.

### 10.3 Reservation

```text
TryReserveNextAvailableSlot
TryReleaseReservation
TryMarkJoined
TryGetSlotState
CreateSnapshot
```

Reservation result must contain enough typed evidence for P3G without defining the final join request:

```text
context/session revision
reservation id/token
configured index
PlayerSlotProfile
PlayerSlotId
previous allocation state
current allocation state
source
reason
status
issues
```

Foreign or stale reservation tokens must be rejected.

### 10.4 Result semantics

Follow existing framework conventions:

```text
Succeeded
AlreadyApplied / IgnoredNoChange where truly idempotent
RejectedInvalidRequest
RejectedInvalidState
RejectedJoiningClosed
RejectedCapacityReached
RejectedNoAvailableSlot
RejectedForeignOrStaleReservation
FailedInvalidConfiguration
```

Do not return a bare boolean for state-changing operations.

---

## 11. Snapshot and diagnostics model

The context should expose immutable snapshots, not internal collections.

Candidate shape:

```text
PlayerParticipationSnapshot
  context revision
  initialized
  configured Slot count
  dynamic capacity
  joining open
  available count
  reserved count
  joined count
  leaving count
  over-capacity state
  ordered PlayerSlotRuntimeSnapshot[]
  last operation summary
```

Per-Slot snapshot:

```text
configured index
PlayerSlotProfile
PlayerSlotId
display metadata references
allocation state
reservation id when applicable
joined technical evidence summary when applicable
selected ActorProfile when applicable
slot revision
last transition status/reason
```

Diagnostics must separate:

```text
Profile identity
Session allocation state
join/provisioning evidence
Actor selection
Activity projection
materialization
occupancy
GameplayReady evidence
```

Recommended logs:

```text
Info
  successful state-changing operation summary

Debug
  full snapshot/result evidence

Warning
  expected policy rejection such as capacity or closed join window

Error
  invalid authoring, foreign/stale identity, impossible transition or technical failure
```

No diagnostics menu is the primary product UX. Product-facing join/lobby tools come in later cuts.

---

## 12. Existing operation/result conventions to reuse

### RuntimeContent

Reusable patterns:

```text
explicit owner identity
scope context validation
request identity
transition guard/token
foreign owner rejection
stale request rejection
immutable snapshots
idempotent root creation/removal
typed operation status
ToDiagnosticString-style evidence
```

Not reusable as direct domain storage:

```text
RuntimeRootRegistry is not a Slot roster.
RuntimeContentHandle is not a joined Player.
RuntimeContentState is not PlayerSlotAllocationState.
```

### Framework lifecycle operations

Reusable patterns:

```text
source and reason on every operation
explicit previous/current state
ordered cleanup
no silent fallback
separate summary and detailed diagnostics
```

### PlayerControlRuntimeContext

Reusable behavioral quality:

```text
explicit initialization
idempotent same-dependency initialization
foreign Slot/Input/target rejection
explicit binding clear
idempotent release
snapshot after every operation
```

Its domain remains per admitted Player control binding. Its state names such as `AvailableUnbound` must not be reused as Slot allocation states because the semantics differ.

---

## 13. Files to reuse in P3F+

Primary reuse:

```text
Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs
Runtime/ApplicationLifecycle/FrameworkRuntimeState.cs
Runtime/SessionLifecycle/SessionRuntimeState.cs
Runtime/Authoring/GameApplicationAsset.cs
Runtime/PlayerParticipation/Authoring/PlayerSlotProfile.cs
Runtime/PlayerParticipation/Authoring/ActivityParticipationProjectionProfile.cs
Runtime/PlayerParticipation/Authoring/PlayerParticipationRequirementsProfile.cs
Runtime/PlayerParticipation/Contracts/ActivityParticipationProjectionDescriptor.cs
Runtime/RuntimeContent/RuntimeContentRuntime.cs
Runtime/RuntimeContent/* owner/context/result primitives where contextual materialization needs them
Runtime/Common/FrameworkStringExtensions.cs
Runtime/Common/LifecycleOperations/* cleanup/result patterns
```

Later targeted reuse:

```text
PlayerControlRuntimeContext
PlayerSlotOccupancy
PlayerEntry vocabulary
PlayerInput/Gate adapters
Camera target-source and request bindings
```

---

## 14. Files/types that must not be expanded into participation authority

```text
GameFlowRuntime
  remains flow coordinator

RouteLifecycleRuntime
  remains Route lifecycle owner

ActivityFlowRuntime
  remains Activity lifecycle owner

SessionRuntimeState
  remains immutable lifecycle snapshot

RuntimeContentRuntime / RuntimeRootRegistry
  remain scoped content authority

PlayerControlRuntimeContext
  remains per admitted Player control evidence

PlayerEntryBehaviour
  remains passive/contextual evidence, not Session orchestration

PlayerSlotOccupancy
  remains effective Slot -> Actor relation, not join state or spawning

PlayerComposer
  remains authoring/materialization surface for a concrete authored Player shape

PlayerInputManager
  remains Unity technical provisioner, not Slot or Session policy authority
```

---

## 15. Activity projection evaluation boundary

P3D created immutable projection descriptors. P3E closes where runtime data comes from:

```text
ActivityParticipationProjectionDescriptor
  immutable authoring intent

PlayerParticipationSnapshot
  immutable Session facts at evaluation time

ActivityParticipationProjectionEvaluator
  pure/typed contextual evaluation

ActivityParticipationProjectionResult
  Activity transition-scoped result
```

Evaluation behavior:

```text
NoSlots
  returns an explicit empty contextual set

AllJoinedSlots
  iterates Session Slot snapshots in configured GameApplication order
  includes Slots whose allocation state is Joined

ExplicitSlots
  preserves authored Profile order
  resolves each Profile against Session Slot snapshots by canonical Profile/PlayerSlotId
  does not silently drop a missing or non-joined required Slot
```

The result is not written back into the Projection Profile or Session roster.

P3L will decide the exact point before Activity activation where this evaluation and requirements gate execute.

---

## 16. ADR decision

No new ADR is created in P3E.

Reason:

```text
The accepted authority is internal and is a direct application of:
  ADR-PROD-0003 domain-scoped runtime contexts
  ADR-PROD-0007 Session participation composition
  ADR-PROD-0009 immutable Profiles versus runtime state
  ADR-PROD-0010 manual join authority boundary
  ADR-PROD-0011 ordered Slot allocation

P3E does not expose a new public API or cross-domain authority.
```

Create `ADR-PROD-0014` only if P3F/P3G requires a public runtime-access contract, a new Session lifecycle contract, or a boundary broader than this internal module.

---

## 17. P3F implementation direction

P3F should implement the minimum Session runtime foundation:

```text
Runtime/PlayerParticipation/Contracts/PlayerSlotAllocationState.cs
Runtime/PlayerParticipation/Contracts/PlayerParticipationOperationStatus.cs
Runtime/PlayerParticipation/Contracts/PlayerSlotReservation.cs
Runtime/PlayerParticipation/Contracts/PlayerSlotRuntimeSnapshot.cs
Runtime/PlayerParticipation/Contracts/PlayerParticipationSnapshot.cs
Runtime/PlayerParticipation/Runtime/PlayerParticipationRuntimeContext.cs
```

Exact file count may be reduced if small contracts can remain cohesive without creating oversized files.

Framework integration should be limited to:

```text
FrameworkRuntimeHost field and initialization
explicit typed internal snapshot/operation forwarding or dependency injection
host lifecycle release/invalidation
structured diagnostics
```

P3F must not yet add:

```text
PlayerInputManager
PlayerInput
LocalPlayerJoinRequest final shape
ActorProfile selection command
Actor GameObject references
RuntimeContent materialization
Activity admission gate
camera/input gameplay activation
```

---

## 18. Technical acceptance for DG-P3-02

```text
PASS — Session/Application lifetime is confirmed as the correct owner lifetime.
PASS — FrameworkRuntimeHost is confirmed as composition root, not domain implementation.
PASS — A dedicated internal PlayerParticipationRuntimeContext is required.
PASS — GameFlow, Route and Activity runtimes are not expanded into roster ownership.
PASS — Session-persistent and contextual state are separated.
PASS — PlayerControlRuntimeContext remains per admitted Player.
PASS — RuntimeContent patterns are reused without treating content handles as Slots.
PASS — typed injection replaces global/service-locator access.
PASS — Profiles remain immutable.
PASS — P3F scope is bounded before implementation.
```

`DG-P3-02` is closed.

---

## 19. Product impact

P3E adds no visible authoring surface.

Architectural gain:

```text
one clear Session participation authority
no competing Player managers
joined state survives Route changes correctly
Activity projection can remain contextual
future join, selection and materialization have a stable owner boundary
```

Usability gain is deferred to later cuts:

```text
P3G join operation
P3H selection API
P3L admission diagnostics
P3O FIRSTGAME product proof
```

---

## 20. Files

Created:

```text
Documentation~/Product/P3E-PLAYER-PARTICIPATION-RUNTIME-AUTHORITY-AUDIT.md
```

Altered:

```text
None
```

Removed:

```text
None
```

---

## 21. Validation

Static validation for this documentation-only cut:

```text
document follows current P3D.2 repository baseline
no runtime or Editor code changed
no QAFramework file changed
no FIRSTGAME file changed
no new singleton/service locator accepted
P3F boundaries are explicit
```

Unity compile and smoke are not required for this cut because no C# or Unity asset is changed.

---

## 22. Suggested commit

```text
P3E — audit Player participation runtime authority
```
