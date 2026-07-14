# ADR-PROD-0014 — Local Player Provisioning Host and Contextual Logical Actor Composition

Status: Accepted  
Date: 2026-07-13  
Package: `com.immersive.framework`  
Area: Player Participation / Local Provisioning / Actor Materialization  
Related: `ADR-PROD-0007`, `ADR-PROD-0008`, `ADR-PROD-0009`, `ADR-PROD-0010`, `ADR-PROD-0011`, `ADR-PROD-0012`, `ADR-PROD-0013`

## Context

The framework now proves the following independent operations:

```text
ordered Player Slot allocation
manual local Player provisioning through PlayerInputManager
Session-scoped ActorProfile selection
explicit default selection
Actor selection duplicate policy
```

The remaining P3 materialization problem is not simply “instantiate the ActorProfile prefab”.

`PlayerInputManager` already creates one Unity object that owns the local input user and device pairing. At the same time, `ActorProfile` references a canonical Logical Actor Host prefab that may change after join and may have Route- or Activity-scoped lifetime.

Treating both objects as the same universal root creates invalid constraints:

```text
ActorProfile must be known before join
PlayerInputManager.playerPrefab must change per selected Actor
post-join Actor replacement requires replacing PlayerInput/InputUser
concurrent joins compete over one manager prefab
Session participation becomes coupled to one gameplay Actor lifetime
```

Instantiating the `ActorProfile.LogicalActorHostPrefab` as a second independent local Player root is also invalid because it duplicates local-player creation authority and may create a second `PlayerInput`.

The current package has an additional implementation divergence:

```text
ActorDeclaration is sealed.
PlayerActorDeclaration is sealed and separately implements IActor.
PlayerActorDeclaration duplicates Actor identity instead of inheriting ActorDeclaration.
```

That shape contradicts the accepted Actor declaration hierarchy and must be corrected before logical Actor materialization is implemented.

## Decision

Local Player runtime composition uses two explicit physical layers plus one later presentation layer:

```text
Local Player Provisioning Host
  stable technical host created only by PlayerInputManager
  Session/join lifetime
  owns PlayerInput and InputUser/device evidence
  is not itself the gameplay Actor

Contextual Logical Player Actor Host
  selected through ActorProfile
  materialized after join through an explicit contextual operation
  Route- or Activity-owned according to the operation owner
  attached to one Local Player Provisioning Host
  owns PlayerActorDeclaration and Actor-specific logical behavior

Actor Presentation / Skin
  separate future materialization below or beside the Logical Actor Host
  does not define ActorId or PlayerSlotId
```

The stable provisioning host and the contextual logical Actor are not interchangeable.

## Canonical hierarchy

Representative runtime hierarchy:

```text
LocalPlayerHost                    <- PlayerInputManager provisioned
├── PlayerInput                    <- fixed technical evidence
├── LocalPlayerHostAuthoring       <- explicit PlayerInput and Actor mount references
├── PlayerSlotDeclaration          <- configured after successful join admission
├── UnityPlayerInputGateAdapter    <- optional fixed technical gate
└── ActorMount                     <- explicit typed Transform reference
    └── LogicalPlayerActor         <- ActorProfile materialization
        ├── PlayerActorDeclaration : ActorDeclaration
        ├── Actor-specific capabilities/endpoints
        ├── game-owned movement/combat when present
        ├── reset endpoints when present
        └── camera target sources when present
```

The exact child names are diagnostic only. Functional resolution uses references from `LocalPlayerHostAuthoring` and typed materialization handles, never hierarchy-path lookup.

## Fixed Local Player Provisioning Host

The `PlayerInputManager.playerPrefab` is a stable reusable technical prefab.

It contains:

```text
required:
  PlayerInput
  LocalPlayerHostAuthoring
  explicit Actor mount Transform

optional fixed technical content:
  input gate adapter
  local-user diagnostics
  other components proven to belong to every joined local Player independently of Actor choice
```

It does not contain:

```text
ActorProfile-specific movement or combat
PlayerActorDeclaration
ActorId
fixed PlayerSlotId
PlayerSlotOccupancy
Actor Presentation / Skin
local-player camera request bound to one Actor target
```

After join admission, the framework configures Slot evidence on the stable host. The shared prefab must not author a functional `player.1` fallback.

The provisioning host lifetime is the joined local Player lifetime. It remains valid while the Slot is Joined, including when no Actor is selected or no contextual Actor is currently prepared.

The host is released only by the explicit local leave lifecycle. Route or Activity exit releases contextual Actor composition, not the joined local Player host.

## Contextual Logical Player Actor Host

`ActorProfile.LogicalActorHostPrefab` remains the canonical logical Actor prefab.

For a Player `ActorProfile`, the prefab contains exactly one:

```text
PlayerActorDeclaration : ActorDeclaration
```

It does not contain a second `PlayerInput`.

`PlayerActorDeclaration` receives explicit local-player evidence during materialization through a typed binding to the provisioning host. It must not locate `PlayerInput` with parent traversal, object names, tags or scene search.

The logical Actor may contain Actor-specific components, including game-owned optional behavior. Those components are not copied to the provisioning host and are not materialized through serialized type names or runtime reflection.

## Actor declaration hierarchy correction

Before P3J materialization:

```csharp
public class ActorDeclaration : MonoBehaviour, IActor
{
    // common Actor identity and descriptor authority
}

public sealed class PlayerActorDeclaration : ActorDeclaration
{
    // Player-only host binding and evidence
}
```

`PlayerActorDeclaration` must not duplicate:

```text
ActorId
ActorKind
ActorRole
Actor display name
generic Actor descriptor authority
```

The current same-GameObject `[RequireComponent(typeof(PlayerInput))]` constraint is superseded for contextual Actor composition. `PlayerInput` belongs to the stable provisioning host and is injected through an explicit typed binding.

The generic `ActorDeclaration` remains concrete for non-Player Actors and becomes inheritable.

## Product authoring roles

### ActorProfile

Remains immutable product intent:

```text
ActorProfileId
Display metadata
Actor Kind
Actor Role
Logical Actor Host Prefab
```

It does not store:

```text
runtime ActorId
PlayerSlotId
provisioning host
context owner
materialization handle
current GameObject
```

### LocalPlayerHostAuthoring

New designer-facing authoring component for the shared `PlayerInputManager.playerPrefab`:

```text
PlayerInput
Actor Mount
Advanced/Debug validation evidence
```

It performs no join, selection or gameplay behavior.

### PlayerComposer

`PlayerComposer` remains an editor-first authoring and technical materialization utility for concrete/preconfigured player objects.

It is not the runtime authority for Actor replacement and must not be invoked at runtime.

Safe editor materialization helpers may be reused after extraction into typed utilities, but P3J must not depend on:

```text
UnityEditor
TypeCache runtime composition
SerializedObject runtime mutation
fixed Composer ActorId or PlayerSlotId as Session authority
```

Runtime-selectable local Player products use `LocalPlayerHostAuthoring` plus `ActorProfile` as their principal authoring surfaces.

## Materialization authority

The operation owner is an explicit Route or Activity runtime context.

```text
Route/Activity authority
  -> resolves projected Joined Slot
  -> reads selected ActorProfile
  -> requests Player logical Actor materialization
  -> provides explicit RuntimeContentOwner / RuntimeScopeContext
```

The Session participation context remains authority for:

```text
joined Slot
selected ActorProfile
selection revision
logical preparation summary evidence
```

It is not a physical prefab materializer and does not own mandatory `GameObject` references.

The `FrameworkRuntimeHost` may compose a host-scoped adapter, but that adapter only coordinates typed operations over explicit owners. It is not a global Player manager or service locator.

## RuntimeContent integration

P3J reuses the existing RuntimeContent protocol for:

```text
RuntimeContentOwner
RuntimeScopeContext
RuntimeContentIdentity
materialization request/result
handle registration
transition guards
logical release request/result
```

The generic RuntimeContent runtime is not itself a Unity materializer.

A typed Unity adapter specialized for attached Player Actor composition performs:

```text
instantiate ActorProfile.LogicalActorHostPrefab
parent under LocalPlayerHostAuthoring.ActorMount
keep the staged instance inactive
validate exactly one PlayerActorDeclaration
assign runtime Actor identity
bind PlayerInput and PlayerSlot evidence explicitly
return typed physical evidence and RuntimeContent handle
```

No second local Player root and no second `PlayerInput` are created.

## Runtime identity

`ActorProfileId` identifies reusable product identity.

`ActorId` identifies one physical logical Actor instance.

P3J generates `ActorId` from explicit runtime evidence:

```text
Session context identity
context owner identity
PlayerSlotId
monotonic materialization sequence
```

Required properties:

```text
unique within the running application
new identity for every replacement/rematerialization
not caller-authored by ordinary UI requests
not equal to ActorProfileId
not derived from GameObject name or prefab path
```

The exact serialized delimiter is an implementation detail, but all inputs and the resulting ID must be diagnostic.

## Typed materialization handle

Representative minimum handle:

```text
PlayerActorMaterializationHandle
  OperationId
  RuntimeContentIdentity
  RuntimeContentOwner
  PlayerSlotId
  ActorProfile / ActorProfileId
  ActorId
  LocalPlayerHostAuthoring
  PlayerInput evidence
  PlayerActorDeclaration
  physical logical Actor GameObject
  MaterializationRevision
  State
```

Public immutable snapshots may omit direct mutable Unity references where not needed. Physical references remain inside the scoped runtime adapter.

A handle is foreign or stale when its context, owner, Slot, operation or revision does not match the current record. Foreign/stale handles are rejected explicitly.

## Idempotency

Applying the same selected Profile for the same Slot and owner when a valid current handle exists returns:

```text
AlreadyPrepared
same ActorId
same physical instance
same RuntimeContentIdentity
no revision change
```

Idempotency is based on typed identity and current handle evidence, not hierarchy discovery.

## Replacement transaction

Simple `TryReplaceActorSelection` must reject while a logical Actor is prepared.

Actor replacement uses one explicit orchestration transaction:

```text
1. validate Joined Slot, current selection, replacement Profile and owner
2. preserve current selection and current materialization handle
3. stage the replacement logical Actor inactive
4. validate identity, declaration and host binding completely
5. register/commit the new RuntimeContent handle
6. atomically commit new selection + logical preparation evidence
7. activate the replacement logical Actor
8. release the previous logical Actor through its release adapter
```

If staging or validation fails:

```text
destroy/release staged replacement through the adapter
preserve previous selection
preserve previous logical Actor
preserve previous preparation evidence
return explicit failure and rollback evidence
```

If the new commit succeeds but previous release fails, the result is an explicit partial-release failure. The previous handle remains diagnostic and must not disappear silently.

P3J may initially expose separate `Prepare`, `Release` and `Replace` operations, but replacement must preserve the transaction semantics above.

## Release order

Context exit or explicit logical Actor release uses:

```text
1. release post-materialization input/camera bindings when present
2. clear contextual occupancy when present
3. release Presentation / Skin when present
4. deactivate logical Actor
5. release physical logical Actor through the typed adapter
6. release/unregister RuntimeContent handle
7. clear Session logical-preparation summary
```

P3J implements only the logical Actor and RuntimeContent portions. Occupancy, gameplay input and camera integration remain P3K responsibilities.

Release is idempotent. `AlreadyReleased` is successful preservation, not a hidden failure.

## Session evidence

The Session Slot record may retain immutable logical preparation summary:

```text
LogicalActorPreparationState
ActorId
ActorProfileId
RuntimeContentIdentity
RuntimeContentOwner
MaterializationRevision
Source
Reason
```

It must not require persistent references to the physical `GameObject` or `PlayerActorDeclaration`.

The physical materialization runtime owns those references and resolves them only through the current typed handle.

## Classification matrix

| Element | Classification | Reason |
| --- | --- | --- |
| `PlayerInputManager` | Technical provisioner | Sole creator of local input hosts |
| `PlayerInput` | Fixed provisioning host | Owns InputUser/device pairing independently of Actor selection |
| `LocalPlayerHostAuthoring` | Fixed host authoring | Explicit typed host and mount references |
| `PlayerSlotDeclaration` | Fixed host runtime evidence after join | Slot survives Actor replacement |
| input gate adapter | Fixed host optional technical component | Can block input before Actor/gameplay readiness |
| `ActorProfile` | Immutable Actor intent | Selects logical content; owns no runtime instance |
| `PlayerActorDeclaration` | Actor logical composition | Declares the currently materialized Player Actor |
| movement/combat/attributes | Actor composition or game-owned optional behavior | Varies by Actor/Profile/product |
| reset endpoints | Actor composition when configured | Released with the logical Actor scope |
| camera targets | Actor composition | Targets vary with selected logical Actor |
| `LocalPlayerCameraRequestBinding` | Context binding | Published only after P3K readiness; not fixed host authority |
| `PlayerSlotOccupancy` | Context binding/evidence | Created only after Actor admission, not by join or selection |
| Presentation / Skin | Presentation | Independent later materialization |
| `PlayerComposer` | Editor authoring utility | No runtime selection or materialization authority |

## Rejected alternatives

### Change `PlayerInputManager.playerPrefab` per selected Profile

Rejected because:

```text
selection may occur after join
concurrent join operations share one manager
manager prefab mutation is not per-operation authority
replacement would require replacing PlayerInput/InputUser
```

### Instantiate the ActorProfile prefab as a second local Player root

Rejected because it creates a second Player root or second `PlayerInput`, violating the one-provisioner rule.

### Keep `PlayerActorDeclaration` on the provisioning host and treat the ActorProfile prefab as modules only

Rejected because it makes `ActorProfile.LogicalActorHostPrefab` cease to be the logical Actor host and collapses Actor identity into the technical input shell.

### Runtime `PlayerComposer.Apply/Rebuild`

Rejected because current Composer materialization is Editor-only, identity-fixed and based on Editor APIs.

### Reflection-driven component lists

Rejected because runtime composition must be typed, inspectable and validator-backed.

### Session-owned physical Actor references as primary authority

Rejected because logical Actor lifetime is contextual and physical objects must release with Route/Activity ownership.

## Required P3J implementation sequence

### P3J.1 — Actor declaration hierarchy correction

```text
make ActorDeclaration inheritable
make PlayerActorDeclaration inherit ActorDeclaration
remove duplicated generic identity fields and descriptor authority
replace same-object PlayerInput requirement with explicit host binding evidence
update validators and regression smokes
```

### P3J.2 — Stable Local Player Host authoring and P3G contract migration

```text
add LocalPlayerHostAuthoring
validate PlayerInputManager.playerPrefab as a provisioning host
remove PlayerActorDeclaration requirement from the provisioning prefab
change join result evidence from logical Actor declaration to Local Player Host evidence
configure PlayerSlotDeclaration only after successful admission
preserve P3G correlation and rollback semantics
```

### P3J.3 — Logical Actor materialization contracts and adapter

```text
PlayerActorMaterializationOperationId
PlayerActorMaterializationStatus
PlayerActorMaterializationRequest
PlayerActorMaterializationResult
PlayerActorMaterializationHandle / snapshot
attached Unity prefab materialization adapter
RuntimeContent request/result/handle integration
framework-generated ActorId
```

### P3J.4 — Session evidence and explicit prepare/release/replace

```text
logical preparation summary per Slot
TryPrepareSelectedActor
TryReleasePreparedActor
TryReplacePreparedActor
selection mutation guard while prepared
idempotency, stale/foreign rejection and rollback evidence
```

### P3J.5 — Technical QA

```text
real PlayerInputManager stable host
join without Actor
select Profile after join
prepare Actor child
same apply idempotent
replacement stages then releases old
failed replacement preserves old Actor and selection
two Slots receive independent Actor instances
context release leaves joined host but removes Actor child
no leaked RuntimeContent handles
```

## Out of scope

```text
Actor Presentation / Skin implementation
PlayerSlotOccupancy materialization
post-materialization gameplay input activation
camera request publication
Activity admission timing
local Player leave/disconnect/reconnect
online/network spawning
save/persistence of selections
full split-screen output composition
game-owned movement or combat implementations
```

## Consequences

### Positive

```text
join no longer implies a gameplay Actor
Actor selection can occur after join
Actor replacement preserves PlayerInput/InputUser
one shared manager prefab supports several ActorProfiles
Route/Activity lifetime remains explicit
ActorProfile keeps its canonical Logical Actor Host meaning
RuntimeContent identity and release contracts are reused
Presentation remains separable
```

### Cost

```text
P3G provisioning prefab validation must be migrated
Actor declaration inheritance requires a deliberate breaking correction
new fixed-host authoring is required
attached materialization needs a specialized Unity adapter
selection replacement becomes an orchestration transaction
```

## Acceptance criteria

```text
DG-P3-05 is closed with an explicit component classification.
DG-P3-06 is closed with the accepted combined materialization mechanism.
PlayerInputManager remains the only local Player provisioning authority.
ActorProfile remains the canonical logical Actor prefab reference.
The provisioning host can exist Joined and Actor-less.
The logical Actor can be replaced without replacing PlayerInput.
ActorDeclaration inheritance divergence is explicitly scheduled before materialization.
No runtime reflection, hierarchy lookup, singleton or service locator is introduced.
```

## Suggested commit

```text
P3I — decide Local Player host and Actor composition boundary
```
