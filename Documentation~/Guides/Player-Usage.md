# Player Usage

Status: **Current Player product contract — Scene-Provided documentation authority updated**
Last updated: **2026-08-31**  
Decision sources: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021, [IF-ADR-023](../Architecture/ADRs/IF-ADR-023-Player-Actor-Runtime-Host-and-Presentation-Authority.md), [IF-ADR-023A](../Architecture/Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)

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
  Actor identity/classification and PresentationPrefab
```

```text
Join
!= Actor selection
!= Activity Actor preparation
!= physical materialization
```

## Player Prefab Composition Baseline

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
│       └── PresentationMount
│           └── FG_FirstPersonPresentation
└── Scene-Provided Local Player
    └── SceneProvidedLocalPlayerAuthoring
```

The root GameObject name equals its prefab filename without `.prefab`.

`LocalPlayerHostAuthoring.PlayerActorRuntimeHostPrefab` is the canonical Runtime
Host reference. `ActorProfile.PresentationPrefab` is the canonical Presentation
reference. In Scene-Provided, their physical instances are already authored in the
composition. In Manager-Provisioned, the Framework uses the same sources to
materialize runtime instances.

For a generic Player Actor prefab:

```text
PlayerActorDeclaration.ActorId = EMPTY
```

Physical preparation/adoption creates the runtime occurrence identity. Do not write
a persistent authored Player Actor occurrence ID to a reusable prefab.

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
→ exact PlayerActorRuntimeHost
→ PresentationMount
→ exact Presentation
```

This resolution is deterministic. Name, tag, global search and implicit hierarchy
conventions are not fallback mechanisms.

Scene-Provided does not require Apply / Rebuild. It neither depends on derived
serialized Runtime Host/Presentation references nor treats those references,
duplicate profile/prefab evidence or an Apply / Rebuild stamp as authority.

### Authoring validation

Editor validation verifies, where applicable:

- the correct Host and Actor Mount;
- exactly one `PlayerActorRuntimeHost`;
- compatibility with the Host's configured Runtime Host prefab;
- a canonical `PlayerActorDeclaration`;
- Presentation under the exact `PresentationMount` and compatible with
  `ActorProfile.PresentationPrefab`;
- absence of concurrent or ambiguous composition.

Prefab provenance is Editor-owned validation. It is not a runtime certificate.

### Runtime resolution and adoption

Runtime validates/adopts the current Host and Slot, selected `ActorProfile`, exact
Actor/Presentation mounts, structural validity, occurrence identity, preparation,
adoption and runtime content. It fails closed on an invalid composition; it does not
repair, replace or infer missing content.

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
→ ActorProfile.PresentationPrefab
→ runtime occurrence identity
→ preparation/adoption
```

It may expose a joined technical Host before contextual Activity preparation. It
does not become Scene-Provided, and Scene-Provided never falls back to this path.

| Concern | Scene-Provided | Manager-Provisioned |
|---|---|---|
| Physical composition | Consumer authors it before Play | Framework creates it at runtime |
| Framework authority | Validate, resolve exact structure, adopt | Provision, materialize, prepare |
| Runtime Host/Presentation | Already authored and adopted | Instantiated from canonical sources |
| Apply / Rebuild | Not required and not authoritative | Not the Player provisioning contract |

## Sources of truth

- `LocalPlayerHostAuthoring.PlayerActorRuntimeHostPrefab`;
- `ActorProfile.PresentationPrefab`;
- `PlayerSlotProfile` and `ActorProfile`;
- the Local Player Host, admission timing and authored physical hierarchy.

Derived serialized Runtime Host/Presentation references, duplicate ActorProfile or
Presentation prefab evidence, and stamps proving an Editor operation ran are not
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
- a second Runtime Host or Presentation prefab authority;
- persistent generic `PlayerActorDeclaration.ActorId`;
- typed occurrence-ID reads before preparation;
- physical Actor hot-swap hidden behind logical Actor selection.
