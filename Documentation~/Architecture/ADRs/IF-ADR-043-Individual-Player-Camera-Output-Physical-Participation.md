# IF-ADR-043 — Individual Player Camera Output Physical Participation

Status: Accepted  
Last updated: 2026-10-06  
Supersedes: none  
Superseded by: none

## Context

An `IndividualPerPlayer` Assignment may reserve its configured Outputs before Players join. Assignment reservation, logical Fallback coverage and physical Camera participation are separate state. Disabling every physical Output at zero Players violates the invariant that the Session always has a camera.

## Decision

For the Outputs mapped by `IndividualPerPlayer`:

- With zero current Player → Output bindings, exactly one physical Fallback Camera participates. It is the first binding in `PlayerCameraOutputTopology`'s stable Player Slot order. Every other mapped Output Camera is disabled.
- With one or more current bindings, only bound Output Cameras participate. Unbound Outputs stay physically disabled even if their logical Fallback continues covering that Output.
- On Join/Rejoin, associate the exact `PlayerInput` with its mapped Output Camera, enable the resulting participation set, then disable stale Outputs. On Leave, clear the owned association, enable the resulting coverage set, then disable stale Outputs. These state changes are synchronous and must never leave a frame without a physical Camera.
- Assignment reservation and each Output's logical Fallback coverage remain unchanged by this policy.
- `PlayerInputManager` remains the sole owner of split-screen viewport rectangles. Framework Camera code does not write `Camera.rect` or `Camera.pixelRect`.

## Accepted scope

- `PlayerCameraOutputIntegrationRuntime` derives the physical participation set from current exact bindings, with one deterministic Fallback Output at zero bindings.
- Physical enable/disable is reconciled centrally and enables the new coverage set before disabling stale Cameras.
- `CameraOutputAuthoring` applies the requested enabled state independently of active Assignment state.
- Assignment/Occurrence lifecycle and logical Fallback coverage remain unchanged.

## Rejected scope

- Clearing or replacing an Assignment because its Player is absent.
- Mutating logical fallback/occurrence state to represent physical camera availability.
- Framework-authored viewport geometry or sample-specific Output changes.

## Consequences

At zero bound Players, one mapped Output Camera provides physical Fallback coverage while other mapped Cameras are disabled. With one or more bound Players, each bound Output participates and unbound Outputs cannot overlap their viewports. Leave and rejoin update participation without replacing the configured Outputs. When no Player is bound, the deterministic first Slot binding identifies which Output carries physical Fallback.

## Current implementation coverage

**Implemented / Integrated / Manual Play Mode: PASS / EditMode regression: PASS.** Runtime centralizes Output participation and preserves one physical Output at zero bindings. Manual validation confirmed 0 → 1 → 2 → 1 → 0 Players and Rejoin, with logical Assignment/Fallback intact and unbound Outputs excluded while Players are active.

Current automated evidence on 2026-10-06: Framework EditMode **175/175 PASS**, Camera **78/78 PASS**, including `PlayerCameraOutputIntegrationTests` **3/3** and `SessionCameraAssignmentRuntimeTests` **37/37**.

## Pending validation

Focused QAFramework certification for this physical-participation boundary remains pending. The current EditMode and manual Play Mode evidence do not relabel that separate certification gate.
