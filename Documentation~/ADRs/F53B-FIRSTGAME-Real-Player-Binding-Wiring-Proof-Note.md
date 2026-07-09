# ADR Note — F53B FIRSTGAME Real Player Binding Wiring Proof

## Status

Accepted for F53B implementation.

## Context

F52A-F52C created the passive PlayerControl / Unity PlayerInput chain and validated it in QA. F53A proved FIRSTGAME can compile and reference those contracts, but it introduced temporary proof assets in the consumer project. The next step must avoid leaving test-only clutter in FIRSTGAME and validate the real player path instead.

## Decision

F53B validates the real FIRSTGAME player object, currently expected as `PlayerPrototype`, against the accepted F52 chain:

```text
PlayerInput
PlayerControlBindingTargetBehaviour
UnityPlayerInputBridgeTargetBehaviour
UnityPlayerInputActivationTargetBehaviour
```

The FIRSTGAME delta may include editor-only validation/apply tooling, but it must not create runtime proof components or synthetic probe GameObjects.

## Consequences

- FIRSTGAME keeps canonical player wiring only.
- Temporary F53A proof assets are explicitly marked for cleanup.
- The current three-component framework shape remains visible for now.
- Later work may consolidate authoring ergonomics into a single higher-level adapter or installer, but F53B does not create that abstraction.

## Non-goals

```text
movement
InputAction routing
gameplay command execution
actor spawning
new framework contract
runtime lifecycle automation
```
