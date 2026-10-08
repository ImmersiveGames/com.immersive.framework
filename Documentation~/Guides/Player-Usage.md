# Player Usage

Status: **Scene-Provided authoring and runtime adoption are implemented; Unity validation pending. ActorProfile and admission timing remain Experimental.**
Last updated: **2026-10-08**
Decision sources: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021, [IF-ADR-038](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md), [IF-ADR-044](../Architecture/ADRs/IF-ADR-044-Consumer-Controlled-Player-Runtime-Gameplay-Availability.md)

## Product model

Keep Session intent, Slot identity, Local Player Host, Actor selection, Actor
runtime composition and Activity context separate.

```text
PlayerSessionProfile
  Session initialization / provisioning policy

PlayerSlotProfile
  authored Slot identity and configuration

LocalPlayerHostAuthoring
  technical Player Host, PlayerInput boundary and ActorMount

ActorProfile
  reusable Actor identity/classification and optional visual-content reference
```

```text
Join
!= Actor selection
!= Activity Actor preparation
!= physical materialization
```

## Player Prefab Composition Baseline

For the Scene-Provided path, `PlayerSlotProfile` and `ActorProfile` references on
`SceneProvidedLocalPlayerAuthoring` are both required. The Actor Profile must
classify the Actor as Player / Protagonist. Its visual-content prefab is optional;
if supplied, the scene composition must contain the matching authored visual
instance. This is a mixed-maturity workflow: `SceneProvidedLocalPlayerAuthoring`
and `PlayerSlotProfile` are Stable, while `ActorProfile` and
`SceneLocalPlayerAdmissionTiming` are Experimental.

The default admission timing is `OnActivityEnter`; `Manual` is also available,
but remains Experimental. The Editor creator is Development Tooling: it creates
an initial technical shell and does not admit or own a Player at runtime.

The Local Player Host is the shared technical composition for both origins.

```text
FG_SceneProvidedPlayer
├── PlayerInput
├── LocalPlayerHostAuthoring
├── UnityPlayerInputGateAdapter
├── ActorMount
│   └── FG_PlayerActor
│       ├── PlayerActorRuntimeHost
│       ├── PlayerActorDeclaration
│       ├── physical Actor components / ActorCameraSubjectAuthoring
│       ├── explicit ObservationTransform
│       └── optional visual content
└── Scene-Provided Local Player
    └── SceneProvidedLocalPlayerAuthoring
```

The root GameObject name equals its prefab filename without `.prefab`.

`LocalPlayerHostAuthoring.PlayerActorRuntimeHostPrefab` remains the technical Host
reference. The Actor occurrence root owns physical/spatial state. Optional visual
content is subordinate. `VisualContentMount` and `ActorProfile.VisualContentPrefab`
are optional authoring/runtime content; neither is required for Actor validity,
activation, placement, relocation or replacement. Unity validation remains pending.

For a generic Player Actor prefab:

```text
PlayerActorDeclaration.ActorId = EMPTY
```

Physical preparation/adoption creates the runtime occurrence identity. Do not write
a persistent authored Player Actor occurrence ID to a reusable prefab.

### Actor occurrence, embodiment and spatial authority

The generic Player Actor prefab contains only:

```text
<Prefix>_PlayerActor
├── PlayerActorRuntimeHost
├── PlayerActorDeclaration
├── physical body / locomotion as required by the game
├── ActorCameraSubjectAuthoring + explicit ObservationTransform
└── optional visual content
```

Movement, physics, placement and preserved pose belong to the Actor occurrence/root.
Visual content, when authored, is optional and subordinate; it is never the spatial
authority. The Actor explicitly supplies `ObservationTransform`, which may differ
from its root and must belong to that Actor occurrence. Camera performs no name or
hierarchy lookup and has no implicit root fallback.

## Scene-Provided

Scene-Provided is a physical consumer composition, not an Editor materialization
workflow:

```text
Create/author composition
→ Validate
→ Play
→ Resolve
→ Adopt
```

At runtime the Framework resolves the exact authored structure:

```text
LocalPlayerHost
→ ActorMount
→ exact Actor occurrence / PlayerActorRuntimeHost evidence
```

This resolution is deterministic. Name, tag, global search and implicit hierarchy
conventions are not fallback mechanisms.

The application must have Player Session enabled and reference a
`PlayerSessionProfile`. Include the Scene-Provided `PlayerSlotProfile` in that
Profile’s Supported Slots and set its Host Provisioning to `SceneProvided`.
`PlayerSessionProfile` and `PlayerHostProvisioningMode` are Experimental, so the
complete Scene-Provided setup combines Stable authoring components with
Experimental Session configuration.

Scene-Provided does not require Apply / Rebuild. It neither depends on derived
serialized Runtime Host/Presentation references nor treats those references,
duplicate profile/prefab evidence or an Apply / Rebuild stamp as authority.

### Authoring validation

Editor validation verifies, where applicable:

- the correct Host and Actor Mount;
- exactly one `PlayerActorRuntimeHost`;
- compatibility with the Host's configured Runtime Host prefab;
- a canonical `PlayerActorDeclaration`;
- exact Actor occurrence and explicit Camera Subject evidence when required;
- absence of concurrent or ambiguous composition.

Prefab provenance is Editor-owned validation. It is not a runtime certificate.

### Runtime resolution and adoption

Runtime validates/adopts the current Host and Slot, selected Actor occurrence,
structural validity, occurrence identity, preparation, adoption and runtime content.
It fails closed on invalid evidence; it does not repair, replace or infer missing
content. SceneProvided and ManagerProvisioned converge on the same Actor/Subject
semantics.

After Scene-Provided adoption, the admitted physical Host is promoted to Session
lifetime and persists across scene loads. Activity or Route transitions do not
recreate or remove it; explicit Session Leave or Session termination ends that
lifetime.

Before admission, the Scene-Provided candidate’s `PlayerInput` is held inactive
while the Framework resolves the exact Slot/Host authority. After admission,
`UnityPlayerInputGateAdapter` applies the configured gameplay Input gate. Keep
these gates on the exact Local Player Host; do not manually enable input as an
admission workaround.

### Create Local Player

Use the creator only as development tooling for an initial technical structure:

```text
GameObject
  > Immersive Framework
    > Player
      > Scene-Provided
        > Create Local Player
```

It can create `PlayerInput`, `LocalPlayerHostAuthoring`,
`UnityPlayerInputGateAdapter`, `ActorMount` and
`SceneProvidedLocalPlayerAuthoring`. It is not runtime authority, does not replace
explicit authoring intent, is not Apply / Rebuild, and does not need to materialize
the final Actor or Presentation.

To add the module to an existing Host, author the Scene-Provided module separately:

```text
Add Component
  > Immersive Framework
    > Player
      > Scene-Provided
        > Local Player
```

## Manager-Provisioned

Manager-Provisioned owns runtime materialization from provisioning intent:

```text
PlayerSessionProfile
→ Framework provisioning
→ Local Player Host
→ Slot admission
→ Actor selection
→ Activity preparation requirement
→ PlayerActorRuntimeHost
→ Actor occurrence configuration
→ runtime occurrence identity
→ preparation/adoption
```

It may expose a joined technical Host before contextual Activity preparation. It
does not become Scene-Provided, and Scene-Provided never falls back to this path.

| Concern | Scene-Provided | Manager-Provisioned |
|---|---|---|
| Physical composition | Consumer authors it before Play | Framework creates it at runtime |
| Framework authority | Validate, resolve exact structure, adopt | Provision, materialize, prepare |
| Actor occurrence | Already authored and adopted | Materialized from provisioning configuration |
| Apply / Rebuild | Not required and not authoritative | Not the Player provisioning contract |

## Sources of truth

- `LocalPlayerHostAuthoring.PlayerActorRuntimeHostPrefab`;
- exact Actor occurrence configuration;
- `PlayerSlotProfile` and `ActorProfile`;
- the Local Player Host, admission timing and authored physical hierarchy.

Derived serialized Runtime Host/Presentation references, duplicate ActorProfile or
visual-content prefab evidence, and stamps proving an Editor operation ran are not
sources of truth.

## Occurrence identity

```text
AUTHORED / UNPREPARED
  ActorId = empty

→ preparation/adoption assigns runtime occurrence identity

IDENTITY ESTABLISHED / PREPARING
  typed ActorId valid

→ commit

PREPARED / COMMITTED
  runtime identity consumable
```

`ActorProfileId`, `PlayerSlotId` and `PlayerActorDeclaration.ActorId` are separate.
`ActorId` and `FrameworkIdentityValue` remain strict; consumers cannot ask for a
typed occurrence identity before preparation establishes it.

## Activity readiness and consumer boundaries

### Temporarily block gameplay input for a Player

For scene-authored UnityEvent or UI workflows, add `PlayerGameplayAvailabilityBlockTrigger` and configure its Route or Activity `Scope`, `Player Slot Profile` and optional diagnostic `Reason`. Wire `RequestBlock()` and `RequestRelease()` to the desired UnityEvents. The component owns one block token and releases only that token, including when its scoped access is released. It does not model turns or select an active Player.

The underlying IF-ADR-044 runtime contract has focused QAFramework evidence at **8/8 PASS** with `BaselineRestored`. The `PlayerGameplayAvailabilityBlockTrigger` authoring surface is also consumer-validated in the Local Multiplayer sample (2026-10-08): P1/P2 Block/Release projected through the canonical Gate adapter, independent token ownership was preserved, and a held block was released on scoped-consumer teardown.

Scope is ownership, not scene location. An Activity-scoped trigger must live in content discoverable by the active Activity scope; placing the component in arbitrary Persistent Content does not make it Activity-scoped or eligible for binding. Use the same Route/Activity content composition rules as the other `PlayerSessionScopedAccessConsumer` authoring surfaces.

An authorized `IPlayerSessionScopedAccess` consumer can temporarily block gameplay
input without leaving the Player Session, changing readiness, replacing the Actor
or changing Camera membership. Store the returned token for the full reason that
owns the block and release that exact token when the reason ends:

```csharp
private PlayerGameplayAvailabilityBlockToken _waitingPlayerBlock;

PlayerGameplayAvailabilityBlockResult blocked = access.RequestBlockRuntimeGameplay(
    playerSlotId,
    source: "TurnController",
    reason: "waiting-for-other-player");

if (blocked.Succeeded)
{
    _waitingPlayerBlock = blocked.BlockToken;
}
```

When that consumer reason ends, release the stored token:

```csharp
PlayerGameplayAvailabilityBlockResult released = access.RequestReleaseRuntimeGameplay(
    _waitingPlayerBlock,
    source: "TurnController",
    reason: "player-turn-started");
```

Each successful acquisition has its own token. Releasing one token does not
release another consumer's block. Tokens belong to the exact joined Player
occurrence and become stale when that occurrence leaves. Pause, Transition and
other Runtime Gates continue to block input until their own owners release them.
Consumers must not enable or disable `PlayerInput` or its gameplay Action Map
directly; the canonical Framework input adapter applies the combined posture.

```text
None
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
```

`LogicalActorsPrepared` means the required physical Player Actor occurrences are
selected/prepared. `GameplayReady` means the contextual gameplay projection is
established over retained prepared Session Players. Neither state certifies
game-owned locomotion, camera, gameplay input consumers or Presentation
completeness.

Route scope and Activity scope are lifecycle ownership; scene location is not scope
authority. Gameplay code uses the public current-gameplay binding and never bypasses
it through direct `PlayerInput` reads, hierarchy guesses or scene scans.

## Current implementation coverage

The current source validates prefab provenance in the Editor, resolves the authored
physical composition transiently at runtime and adopts it without Player Apply /
Rebuild, serialized provenance evidence or `ScenePlayerActorPresentationEvidence`.
Prepared physical evidence is authoritative after adoption commit.

## Anti-patterns

- global Player manager or service locator;
- name/tag/global-scene lookup as authority;
- manual Join as fallback for normal Scene-Provided admission;
- hidden default Actor fallback;
- a second Runtime Host or visual-content prefab source;
- persistent generic `PlayerActorDeclaration.ActorId`;
- typed occurrence-ID reads before preparation;
- physical Actor hot-swap hidden behind logical Actor selection.
