# F49D — PlayerEntry Passive Model

Status: Implemented / pending QA smoke  
Phase: F49 — Player Topology, Player Entry and PlayerView Ownership  
Type: Runtime contract / passive model

## Objective

Add a passive PlayerEntry model that connects:

```text
PlayerSlotId
ActorId
PlayerEntryState
ActorReadinessSnapshot
```

This gives future PlayerEntry, PlayerView and ControlBinding cuts a stable vocabulary without creating a coordinator or runtime lifecycle.

## Scope

Created runtime contracts under:

```text
Runtime/PlayerEntry/
```

Files:

```text
IPlayerEntry.cs
PlayerEntryState.cs
PlayerEntrySnapshot.cs
PlayerEntry.cs
```

## Out of scope

This cut does not create:

```text
PlayerEntryCoordinator
PlayerTopologyValidator
PlayerView
ControlBinding
PlayerInputManager bridge
spawn/runtime materialization
movement/gameplay controller
FIRSTGAME integration
```

## Rules accepted

```text
PlayerEntry is passive data/model language.
PlayerEntry does not join players.
PlayerEntry does not spawn actors.
PlayerEntry does not bind view or control.
ActorReady/ViewBound/Active/Suspended require Actor readiness for view.
Suspended requires an explicit suspension reason.
Control readiness is preserved as evidence, but ControlBinding remains a future cut.
```

## Expected QA smoke

Run the QAFramework synthetic smoke:

```text
Actor/PlayerEntry Passive QA
```

Expected final log:

```text
[F49D_PLAYER_ENTRY_QA] status='Succeeded'
```

## Acceptance criteria

- `Immersive.Framework.Runtime` compiles.
- `PlayerEntry` can be created from valid `PlayerSlotId` and `ActorId`.
- `PlayerEntrySnapshot` reports state, actor readiness and diagnostics.
- `ActorReady` without `ActorReadinessSnapshot.IsReadyForView` fails explicitly.
- `Suspended` without suspension reason fails explicitly.
- `IPlayerEntry` exposes the same snapshot data.

## Architectural gain

F49D turns PlayerEntry into explicit framework vocabulary before any runtime orchestration exists. Future cuts can consume `IPlayerEntry` and `PlayerEntrySnapshot` instead of guessing from loose combinations of `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerInput`, `ActorReadinessBehaviour` and scene objects.

## Suggested commit message

```text
F49D: add passive PlayerEntry model
```
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
