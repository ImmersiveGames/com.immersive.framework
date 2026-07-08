# F50A — Player Binding Authoring Validator

Status: Accepted / implemented as passive validation.

## Objective

Add a package-owned validator that can inspect authored Player binding evidence before any real binding lifecycle is introduced.

## Scope

F50A validates the passive chain:

```text
PlayerSlotDeclaration
PlayerSlotOccupancy
ActorReadinessBehaviour
PlayerEntryBehaviour
PlayerViewBehaviour
PlayerControlBehaviour
PlayerTopologyValidationResult
PlayerViewTopologyValidationResult
PlayerControlTopologyValidationResult
PlayerBindingReadinessSummary
PlayerBindingDiagnosticReport
```

The validator can receive explicit component arrays or discover components below a validation root GameObject.

## Out of scope

F50A does not:

```text
bind views
activate cameras
route PlayerInput
activate input
enable movement
spawn actors
own runtime lifecycle
integrate FIRSTGAME
```

## Acceptance

The QA smoke must prove:

```text
valid authored chain succeeds
missing root fails
missing required component categories fail
topology/readiness diagnostics propagate
passive boundary remains false for binding, camera, input, movement and actor spawning
```

## Architectural gain

The framework now has a package-owned gate between passive topology/readiness evidence and future binding adapters. Future PlayerView or PlayerControl binding work can depend on this validator rather than repeating scene/component checks.
