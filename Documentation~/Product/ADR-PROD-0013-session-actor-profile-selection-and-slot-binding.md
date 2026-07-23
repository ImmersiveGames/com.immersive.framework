# ADR-PROD-0013 — Session Actor Profile Selection and Player Slot Binding

Status: Accepted  
Date: 2026-07-13  
Revised: 2026-07-22 (`PROD-ASSET-1C`)
Package: `com.immersive.framework`  
Area: Player Participation / Actor Selection / Session State  
Related: `ADR-PROD-0007`, `ADR-PROD-0008`, `ADR-PROD-0009`, `ADR-PROD-0010`, `ADR-PROD-0011`, `ADR-PROD-0012`

## Context

P3G established an authorized manual local Player join flow:

```text
LocalPlayerJoinRequest
-> ordered Player Slot reservation
-> PlayerInputManager provisioning
-> PlayerInput / PlayerActorDeclaration validation
-> Slot commit as Joined
```

That flow intentionally does not decide which `ActorProfile` the participant will use.

The product must support:

```text
single-player default character
joined Players entering a selection screen
simultaneous local character selection
changing selection before gameplay materialization
Activity readiness requiring selected Actors
Session-persistent selection across Route/Activity transitions
optional duplicate-selection restrictions
```

The following identities must remain distinct:

```text
PlayerSlotProfile / PlayerSlotId
  which participation seat is occupied

ActorProfile / ActorProfileId
  which reusable Actor identity is selected

ActorId
  which concrete logical Actor instance exists

Unity PlayerInput.playerIndex
  technical diagnostic index only
```

Selection is mutable Session state. It must not mutate `ActorProfile`, `PlayerSlotProfile`, the local Player prefab, or `PlayerActorDeclaration`.

## Decision

The canonical model is:

```text
PlayerParticipationRuntimeContext
  Session-scoped authority
  owns ordered Slot allocation state
  owns selected ActorProfile state per Slot

PlayerSlotProfile
  immutable Slot identity and visual metadata
  may reference an optional default ActorProfile
  never stores current selection

ActorProfile
  immutable selectable Actor identity
  never stores which Slot selected it

PlayerActorDeclaration
  concrete runtime Actor declaration
  receives selected Profile evidence later during logical Actor materialization
  is not the Session selection authority
```

## Selection occurs after join

The ordinary local join request remains:

```text
LocalPlayerJoinRequest
  Source
  Reason
  optional PairWithDevice
  optional ControlScheme
```

It does not gain:

```text
ActorProfile
ActorProfileId
PlayerSlotId
ActorId
```

Canonical flow:

```text
join request
-> Slot becomes Joined
-> Slot identity/presentation is available
-> Actor selection may occur
-> Activity requiring SelectedActors becomes ready only after selection
-> logical Actor materialization may occur later
```

A joined Slot without a selected Actor remains valid and remains `Joined`.

This supports simultaneous character selection without reopening the Slot for another participant.

## Session selection authority

`PlayerParticipationRuntimeContext` owns the current selection for each configured Slot.

Representative runtime record:

```text
Session Player Slot Record
  ConfiguredIndex
  PlayerSlotProfile
  PlayerSlotId
  AllocationState
  Reservation evidence
  SelectedActorProfile optional
  SelectedActorProfileId optional/derived
  SelectionRevision
  SelectionSource
  SelectionReason
```

Rules:

```text
selection is associated with PlayerSlotId, never playerIndex
selection changes increment a Slot-local selection revision
runtime state references the immutable ActorProfile asset
runtime state never mutates either Profile
selection survives Route/Activity transitions within the same Session
selection does not create an ActorId
selection does not materialize a logical Actor host
```

## ActorProfile foundation

P3H requires the canonical runtime `ActorProfile` type described by `ADR-PROD-0008`.

Minimum product shape:

```text
ActorProfile
  ActorProfileId
  Display Name
  Description
  Icon optional
  Actor Kind
  Actor Role
  Logical Actor Host Prefab
```

The direct ScriptableObject reference is the runtime and authoring link.

`ActorProfileId` is authored once inside the Profile and is used for:

```text
persistence
network transport
duplicate-identity comparison
diagnostics
future asset resolution
```

The framework must not compare selectable Actor identity by:

```text
asset name
asset path
GameObject name
prefab name
reference equality alone
```

A duplicate `ActorProfileId` in project authoring is an explicit validation failure.

## Selection request

The initial typed operation is synchronous and Session-scoped.

Representative request:

```text
PlayerActorSelectionRequest
  PlayerSlotId
  ActorProfile
  Source
  Reason
  ExpectedSelectionRevision optional
```

The request does not carry a caller-supplied Session context ID. It is executed against one explicit `PlayerParticipationRuntimeContext` instance.

The optional expected revision supports stale UI/request rejection without global lookup.

The request must not use:

```text
Unity playerIndex
PlayerInput reference as Slot identity
ActorProfileId text copied by the caller
GameObject or hierarchy lookup
```

## Selection result

Representative result:

```text
PlayerActorSelectionResult
  Status
  PlayerSlotId
  PlayerSlotProfile
  PreviousActorProfile
  SelectedActorProfile
  PreviousSelectionRevision
  SelectionRevision
  DuplicatePolicy
  Source
  Reason
  Message
```

The result is operation evidence. The authoritative current state remains the Session Slot record/snapshot.

## LocalPlayerJoinResult boundary

`LocalPlayerJoinResult` remains the historical result of the join transaction.

It must not become the mutable Actor-selection authority and does not receive a required `ActorProfile` field.

The current Slot state is exposed through `PlayerSlotRuntimeSnapshot`, which will be extended with selection evidence:

```text
SelectedActorProfile
SelectedActorProfileId
SelectionRevision
HasSelectedActor
```

A `LocalPlayerJoinResult` created before later selection is not rewritten.

## Selection policy

Duplicate rules are explicit Session product policy, not Slot allocation behavior.

The `GameApplicationAsset` directly owns the duplicate-selection rule:

```text
GameApplicationAsset
  PlayerActorSelectionDuplicatePolicy
```

Initial duplicate policies:

```text
AllowDuplicates
  several Joined Slots may select the same ActorProfileId

UniqueAcrossJoinedSlots
  at most one Joined Slot may select a given ActorProfileId
```

This policy is one application-level enum decision, not a reusable product definition.
Creating a separate ScriptableObject for it adds navigation and an artificial asset identity
without providing a reusable rule set. This is consistent with `ADR-PROD-0009`: Policy Profiles
are appropriate for reusable rule sets shared by several product surfaces, not every enum-valued
configuration.

The enum value is supplied explicitly when the Session participation runtime is composed.

New `GameApplicationAsset` instances serialize `AllowDuplicates` as their explicit default.
`Unspecified` or an undefined serialized value is invalid and never becomes an implicit
`AllowDuplicates` fallback at runtime.

The uniqueness check and state mutation are atomic within the Session context:

```text
validate target Slot
validate ActorProfile
validate expected revision
validate duplicate policy against current Slot selections
commit selection
increment revision
return typed result
```

Limited copies, team restrictions, role quotas, account ownership and online reservation are deferred.

## Default Actor Profile

`PlayerSlotProfile` may reference an optional immutable default:

```text
Default Actor Profile: [Mage]
```

This is static authoring intent, not current runtime state.

Rules:

```text
DefaultActorProfile does not imply that the Slot is Joined.
DefaultActorProfile does not mutate SelectedActorProfile automatically inside the Profile.
The Session may apply the default only through the same typed selection operation.
Default application must obey the active duplicate policy.
Default failure is explicit and diagnostic.
No fallback selects the first discovered ActorProfile.
```

The initial implementation may expose an explicit `TrySelectDefaultActor` convenience operation after a Slot is Joined. It must delegate to the canonical selection transaction.

## Clear and replace selection

Before logical Actor materialization, a Joined Slot may:

```text
select an ActorProfile
replace its selected ActorProfile
clear its selection
```

Clearing selection:

```text
does not unjoin the Slot
does not release PlayerInput
does not make the Slot Available
causes SelectedActors readiness to become unsatisfied
increments SelectionRevision
```

Once a logical Actor has been prepared for the Slot, changing or clearing selection is rejected in the initial model.

Representative status:

```text
RejectedLogicalActorAlreadyPrepared
```

Actor replacement while a logical host exists requires a later explicit release/rematerialization transaction and must not be hidden inside selection.

## Activity readiness

`PlayerParticipationRequirementLevel.SelectedActors` evaluates the projected Slots selected by the Activity participation projection.

For each projected Slot:

```text
Joined Slot + valid SelectedActorProfile
  satisfies SelectedActors

Joined Slot + no selection
  blocks SelectedActors readiness

non-projected Slot
  does not participate in this Activity evaluation
```

The evaluation reads Session snapshots and does not mutate selection.

`SelectedActors` does not imply:

```text
logical Actor host exists
ActorId exists
presentation exists
gameplay input is active
camera is bound
occupancy is established
```

Those remain later progressive readiness levels.

## Materialization boundary

Selection answers:

```text
Which ActorProfile is selected for this Slot?
```

It does not answer:

```text
Which ActorId exists?
Which logical host is active?
Which presentation is visible?
Which Activity owns the Actor?
```

Later logical Actor materialization consumes a stable selection snapshot and produces separate materialization evidence.

For local Players, `PlayerInputManager` remains the physical host provisioner. P3H selection does not change that authority and does not yet reconcile the selected Profile's canonical logical-host prefab with the configured `PlayerInputManager.playerPrefab`.

## Failure statuses

Initial typed statuses:

```text
SucceededSelected
SucceededReplaced
SucceededCleared
RejectedInvalidRequest
RejectedRuntimeUnavailable
RejectedSlotNotConfigured
RejectedSlotNotJoined
RejectedActorProfileMissing
RejectedActorProfileInvalid
RejectedStaleSelectionRevision
RejectedDuplicateActorSelection
RejectedLogicalActorAlreadyPrepared
RejectedPolicyInvalid
```

Failures do not mutate the prior selection.

## Diagnostics

Advanced/debug evidence should show:

```text
Session context ID
configured Slot index
PlayerSlotProfile / PlayerSlotId
allocation state
selected ActorProfile / ActorProfileId
selection revision
selection source/reason
selection policy
conflicting Slot when uniqueness rejects
logical Actor prepared state when later available
```

## Guardrails

```text
Do not add ActorProfile to LocalPlayerJoinRequest.

Do not require Actor selection for Slot join success.

Do not store current selection in PlayerSlotProfile or ActorProfile.

Do not use PlayerInput.playerIndex as PlayerSlotId.

Do not infer selection from PlayerActorDeclaration, prefab name or GameObject name.

Do not instantiate or destroy logical Actors during the selection operation.

Do not assign ActorId during selection.

Do not silently select the first ActorProfile found in the project.

Do not treat Unspecified or an undefined policy as AllowDuplicates.

Do not compare uniqueness by display name, icon, color or prefab name.

Do not change selected Actor while a logical Actor host is active without an explicit replacement transaction.

Do not make duplicate policy part of Player Slot allocation.
```

## Out of scope

```text
logical Actor host materialization
ActorId generation
Profile-to-PlayerInputManager prefab reconciliation
Presentation / Skin selection and materialization
Actor catalogs or Addressables
save/load resolution
network replication
team/role restrictions
limited-copy selection
online lobby reservations
leave/reconnect selection retention
FIRSTGAME character-selection UI
```

## Technical acceptance criteria

```text
ActorProfile is an immutable Identity Profile.

PlayerParticipationRuntimeContext owns selected ActorProfile state per Slot.

Join succeeds independently from Actor selection.

Selection targets PlayerSlotId and never Unity playerIndex.

Selection policy is an explicit GameApplication value and immutable during the Session.

Unique selection is checked and committed atomically.

Stale revision requests are rejected explicitly.

PlayerSlotRuntimeSnapshot exposes current selection evidence.

LocalPlayerJoinResult remains historical join evidence.

SelectedActors readiness reads Session selection state without materializing Actors.

No Profile asset is mutated at runtime.

No singleton, service locator, name lookup or silent fallback is introduced.
```

## Product acceptance criteria

```text
A designer creates ActorProfile assets and assigns them directly.

A joined Player can exist in a character-selection state before choosing an Actor.

A Player Slot may define an optional default ActorProfile.

Products can explicitly allow or reject duplicate Actor selections.

Selection can be changed or cleared before logical Actor materialization.

Advanced/Debug clearly separates Slot identity, Actor selection and runtime Actor identity.

An Activity can require selected Actors without requiring logical Actor materialization.
```

## Consequences

### Positive

```text
Join remains simple and deterministic.

Character selection becomes explicit Session state.

Slot identity and Actor identity remain separate.

Selection persists across contextual transitions without preserving logical hosts.

Activity readiness can use the already-defined SelectedActors level.

Duplicate rules become an explicit application decision rather than ad hoc UI behavior.
```

### Cost

```text
ActorProfile and ActorProfileId require runtime/Editor implementation.

PlayerSlotProfile gains an optional ActorProfile dependency.

Session Slot snapshots and context operations require extension.

GameApplication policy authoring and validation are required.

Logical materialization must later consume and freeze selection explicitly.
```

## Implementation sequence

```text
P3H.2
  ActorProfile + ActorProfileId + initial selection policy foundation
  PlayerSlotProfile optional default reference
  Editor validation and QA authoring smoke

P3H.3
  Session Slot selection contracts and runtime state
  select/replace/clear/default operations
  duplicate/stale synthetic QA

P3H.4
  Runtime Host composition and public authoring/request surface
  SelectedActors readiness integration
  Play Mode QA

PROD-ASSET-1C
  remove the non-reusable PlayerActorSelectionPolicyProfile
  make GameApplicationAsset the direct enum authority
  carry only the enum through runtime snapshots and results

Later P3I
  logical Actor materialization from selected ActorProfile
```

## Suggested commit message

```text
Docs: freeze Session Actor Profile selection authority
```
