# F49I — PlayerControl Passive Contract

## Objective

Add passive PlayerControl contracts and a Unity-facing adapter that can describe player control evidence without enabling gameplay control.

## Scope

- `IPlayerControl`
- `PlayerControlState`
- `PlayerControlSnapshot`
- `PlayerControl`
- `PlayerControlBehaviour`

## Rules

- `PlayerControlState.Bound` requires PlayerEntry evidence in `Active` state.
- `PlayerControlState.Active` requires PlayerEntry evidence in `Active` state and Actor readiness for control.
- `PlayerControlState.Suspended` requires an explicit suspension reason.
- Control target and input source are diagnostic evidence only.

## Out of scope

- PlayerInputManager bridge
- InputAction routing
- action map switching
- movement enable/disable
- ControlBinding runtime lifecycle
- camera activation
- FIRSTGAME integration

## Architectural gain

This cut introduces passive control evidence after PlayerView topology validation. It allows QA and later systems to reason about whether a player is eligible for control without actually binding input or moving gameplay objects.
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
