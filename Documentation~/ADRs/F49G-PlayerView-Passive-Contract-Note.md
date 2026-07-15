# F49G — PlayerView Passive Contract

Status: Implemented

## Decision

F49G introduces passive PlayerView evidence for the Player axis.

The cut adds:

- `IPlayerView`
- `PlayerViewState`
- `PlayerViewSnapshot`
- `PlayerView`
- `PlayerViewBehaviour`

`PlayerView` is not a camera director, camera priority solver, input router, control binder or runtime lifecycle coordinator.

## Rule

A PlayerView can only enter `Bound` or `Active` when it has `PlayerEntry` evidence in `ViewBound` or `Active` state.

`Declared` remains valid without PlayerEntry evidence.

Camera and target evidence are optional diagnostics at this cut. They are not activated, prioritized or moved.

## Out of scope

- CameraDirector integration
- Cinemachine integration
- camera priority selection
- PlayerInput bridge
- ControlBinding
- gameplay movement
- runtime coordinator
- FIRSTGAME integration

## Validation

QA must validate both valid and invalid passive states before any runtime coordinator is introduced.
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
