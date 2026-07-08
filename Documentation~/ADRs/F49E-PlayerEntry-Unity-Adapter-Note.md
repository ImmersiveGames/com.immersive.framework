# F49E — PlayerEntry Unity Adapter

Status: Implemented / Pending QA
Date: 2026-07-08

## Objective

Expose passive `PlayerEntry` evidence through a Unity-facing component without creating a coordinator, runtime lifecycle, PlayerInputManager bridge, PlayerView, ControlBinding or gameplay movement.

## Scope

- Add `PlayerEntryBehaviour` as a thin Unity adapter over the immutable `PlayerEntry` model.
- Resolve PlayerSlot identity from `PlayerSlotDeclaration` or explicit fallback id.
- Resolve Actor identity from `ActorDeclaration`, `PlayerActorDeclaration` or explicit fallback id.
- Resolve Actor readiness from `ActorReadinessBehaviour` or explicit fallback readiness state.
- Expose `IPlayerEntry` and immutable `PlayerEntrySnapshot` to consumers.
- Provide explicit methods for rebuilding evidence, refreshing readiness, applying passive state, suspension and release.

## Out of scope

- PlayerEntry coordinator.
- PlayerTopology validator.
- PlayerView ownership or camera binding.
- ControlBinding or movement permissions.
- PlayerInputManager join policy.
- FIRSTGAME integration.

## Architectural gain

`PlayerEntryBehaviour` allows authored scenes and QA fixtures to expose `PlayerEntry` evidence from GameObjects while preserving the F49D immutable model. The adapter is explicit and passive: it does not infer or advance gameplay lifecycle by itself.

## Acceptance criteria

- Component compiles in runtime assembly.
- Component implements `IPlayerEntry`.
- Component can build a configured entry from Unity declarations.
- Component can refresh Actor readiness from `ActorReadinessBehaviour`.
- Invalid ActorReady state without view readiness fails explicitly.
- Suspended state requires an explicit reason.
- Release and rebuild remain explicit.
