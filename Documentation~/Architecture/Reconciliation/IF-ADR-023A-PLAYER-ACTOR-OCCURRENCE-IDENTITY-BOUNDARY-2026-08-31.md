# IF-ADR-023A — Player Actor Occurrence Identity Boundary — 2026-08-31

Status: **RUNTIME RECONCILED / FIRSTGAME PLAY MODE PROVEN**

## Scope

This reconciliation records a post-certification correction to the current Player Actor runtime contract after IF-ADR-023.

The correction does not replace IF-ADR-023. It clarifies and hardens the runtime identity boundary used by both Scene-Provided and Manager-Provisioned physical Player Actor preparation.

## Problem found after IF-ADR-023 certification

`PlayerActorDeclaration` represents one physical Player Actor occurrence, but the reusable authored Player Actor template intentionally has no persistent occurrence identity.

Canonical authored state:

```text
PlayerActorRuntimeHost
├── PlayerActorDeclaration
│   └── ActorId = empty
└── PresentationMount
```

The empty authored value is valid for `PlayerActorDeclaration` because its `ActorId` is assigned only when a physical Player Actor occurrence is prepared.

The runtime previously contained consumers that could read typed `ActorId` before this occurrence identity had been established. Because `ActorId` and `FrameworkIdentityValue` correctly reject empty identity values, Scene-Provided preparation could fail with:

```text
ArgumentException
Framework identity value cannot be null, empty or whitespace.
```

This was a runtime ordering/boundary defect, not a prefab authoring defect.

## Canonical identity boundary

The Player Actor occurrence lifecycle is now explicitly divided into three semantic states:

```text
AUTHORED / UNPREPARED
  PlayerActorDeclaration exists
  stored ActorId may be empty
  typed occurrence ActorId is not available

        ↓

IDENTITY ESTABLISHED / PREPARING
  physical preparation owner generates the occurrence ActorId
  PlayerActorDeclaration receives that ActorId
  typed ActorId becomes valid
  transaction may still roll back

        ↓

PREPARED / COMMITTED
  required materialization/preparation evidence is retained
  downstream lifecycle/gameplay may consume the occurrence identity
```

The canonical runtime boundary is owned by:

```text
PlayerActorDeclaration.EstablishRuntimeOccurrenceIdentity(...)
```

Only physical Player Actor preparation owners establish this identity.

Current origins:

```text
Scene-Provided
  PlayerActorPreparationRuntimeContext.TryAdoptScenePlayerActor

Manager-Provisioned
  AttachedPlayerActorMaterializationAdapter.TryMaterialize
```

## Identity ownership

The Player model keeps these identities separate:

```text
ActorProfileId
  persistent Actor selection/profile identity

PlayerSlotId
  persistent Player Slot identity

PlayerActorDeclaration.ActorId
  runtime physical Player Actor occurrence identity
```

Do not substitute one identity for another.

Do not generate a persistent Player Actor occurrence ID in the prefab.

Do not weaken `ActorId` or `FrameworkIdentityValue` to accept an empty typed identity.

## Scene-Provided transaction

The corrected Scene-Provided flow is:

```text
Scene admission
→ Slot resolution
→ Actor selection
→ Route spatial entry using PlayerSlotId + physical Transform
→ generate Scene Player Actor occurrence identity
→ establish PlayerActorDeclaration runtime occurrence identity
→ register/materialize required runtime evidence
→ activate/commit physical preparation
→ retain Scene adoption/preparation records
→ canonical Activity Player lifecycle
```

Route spatial entry does not require Player Actor occurrence identity. It uses authored spatial intent resolved by `PlayerSlotId` and applies the resulting pose to the physical Transform.

This removes a false dependency on `PlayerActorDeclaration.ActorId` before the identity boundary.

## Manager-Provisioned consistency

Manager-Provisioned already established generated occurrence identity before consuming typed `ActorId`.

Its physical preparation remains semantically consistent with the same invariant:

```text
materialize physical Player Actor
→ generate occurrence identity
→ establish runtime occurrence identity
→ register/materialize evidence
→ apply required spatial gate
→ activate
→ retain preparation record
```

Scene-Provided and Manager-Provisioned remain distinct origins, but converge on the same rule:

> Typed Player Actor occurrence identity is unavailable before occurrence establishment and valid after occurrence establishment.

## Transaction and rollback

Scene-Provided physical adoption remains transactional.

For the reusable Player Actor template:

```text
authored stored ActorId = empty
→ establish runtime occurrence ActorId
→ preparation succeeds
→ runtime ActorId remains authoritative
```

If a transaction-owned failure occurs after identity establishment:

```text
authored stored ActorId = empty
→ runtime occurrence ActorId
→ failure
→ transaction rollback
→ previous stored ActorId restored
```

Raw/stored identity text exists only for internal transaction snapshot/restoration. It is not an alternative identity API for runtime consumers.

## Diagnostics

Diagnostics must not become identity prerequisites.

Before occurrence identity establishment, diagnostics may report that Player Actor identity is absent. They must not force construction of a typed `ActorId` merely to log an operation.

Activity Content execution diagnostics were also canonicalized during this investigation so Boot, Route Request and Activity Request use one shared Activity Content projection, including detailed participant diagnostics.

This observability work does not change Player lifecycle semantics.

## Readiness meaning confirmed

The corrected Scene-Provided path was verified in FIRSTGAME with both readiness checkpoints.

### LogicalActorsPrepared

Observed result:

```text
Activity readiness = Ready
Activity Content Enter = Succeeded
Scene admission = SucceededEntered
projected = 1
selected = 1
prepared = 1
failed = 0
requirement = LogicalActorsPrepared
```

This proves the Scene-Provided physical Player Actor occurrence was admitted, selected, adopted and retained as prepared Session physical state.

### GameplayReady

Observed result:

```text
Activity readiness = Ready
Activity Content Enter = Succeeded
projected = 1
selected = 1
prepared = 1
failed = 0
requirement = GameplayReady
reason = activity-player-actor-gameplay-ready-entered
```

`GameplayReady` proves the current contextual gameplay projection was established over retained Session physical Players.

It does **not** by itself certify that a game-owned Presentation contains locomotion, camera composition or a concrete gameplay input consumer. Those remain gameplay-owned composition concerns.

## Authoring rule

For a reusable Player Actor prefab:

```text
PlayerActorDeclaration.ActorId
  authored template value = empty
  runtime occurrence value = assigned during physical preparation
```

For ordinary persistent `ActorDeclaration`, the existing persistent authored identity requirement remains unchanged.

## Scene-Provided authoring shapes

Keep these two Editor operations distinct.

### Full creator

```text
GameObject
  > Immersive Framework
    > Player
      > Scene-Provided
        > Create Local Player
```

Creates a complete Scene-Provided Local Player composition.

### Add Scene-Provided module to an existing Local Player Host

```text
Add Component
  > Immersive Framework
    > Player
      > Scene-Provided
        > Local Player
```

When a reusable Local Player Host already exists, the Scene-Provided authoring component may live on a direct child module that references the ancestor Host.

Example reusable variant:

```text
SceneProvidedPlayer
├── PlayerInput
├── LocalPlayerHostAuthoring
├── UnityPlayerInputGateAdapter
├── ActorMount
└── Scene-Provided Local Player
    └── SceneProvidedLocalPlayerAuthoring
```

Do not invoke the full creator inside an already-authored Local Player Host merely to add Scene-Provided behavior.

## Verdict

```text
IF-ADR-023 composition authority                    PRESERVED
PlayerActorDeclaration template ActorId             EMPTY BY CONTRACT
Runtime occurrence ActorId                          PHYSICAL PREPARATION OWNED
Typed ActorId pre-boundary consumption              FORBIDDEN
Scene-Provided occurrence identity boundary         RECONCILED
Manager-Provisioned identity ordering               CONSISTENT
LogicalActorsPrepared Scene-Provided runtime proof  PASS
GameplayReady Scene-Provided runtime proof          PASS
Game-owned Presentation functionality               SEPARATE FROM FRAMEWORK READINESS
```
