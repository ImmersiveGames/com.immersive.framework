# F53A-B1 — FIRSTGAME Player Binding Preflight Strictness Fix Note

## Status

Patch accepted for F53A preflight.

## Context

The initial FIRSTGAME preflight proved consumer compilation and component availability, but allowed `status='Succeeded'` even when the expected gameplay action map was missing. It also created a temporary `PlayerInput`, which could compete with the real `PlayerPrototype` device/control-scheme pairing.

## Decision

F53A-B1 makes the preflight stricter:

```text
expectedGameplayActionMap must be true for status='Succeeded'
```

The FIRSTGAME creator must reuse an existing scene `PlayerInput` instead of creating a temporary duplicate `PlayerInput` on the probe object.

## Consequences

- FIRSTGAME preflight failure is more diagnostic.
- The preflight no longer creates avoidable PlayerInput pairing noise.
- No framework runtime behavior changes.
- No movement, actor spawning or gameplay command execution is introduced.

## Non-goals

```text
new package contracts
runtime lifecycle/coordinator
InputAction value routing
movement
gameplay command execution
actor spawning
production scene migration
```
