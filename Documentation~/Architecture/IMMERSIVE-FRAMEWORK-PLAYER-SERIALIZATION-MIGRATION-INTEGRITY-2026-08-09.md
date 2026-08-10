# Immersive Framework — Player Serialization Migration Integrity

Date: 2026-08-09  
Status: **P0 TECHNICALLY CLOSED / CERTIFIED**  
Type: technical integrity / serialized command identity

## Objective

Preserve the semantic meaning of already serialized supported Player command
values while removing the former Capacity command/model.

The required invariant is:

```text
removing an old concept
must not silently reinterpret its serialized numeric value
as another supported operation
```

This closure does not restore Capacity or any compatibility command.

## Canonical command identities

Historical supported identity:

```text
OpenJoining                   = 10
CloseJoining                  = 20
SetCapacity                   = 30   # removed
RequestJoin                   = 40
RequestDefaultActorSelection  = 50
```

Current accepted identity:

```text
OpenJoining                   = 10
CloseJoining                  = 20
30                            = retired / unsupported
RequestJoin                   = 40
RequestDefaultActorSelection  = 50
```

Therefore:

| Serialized integer | Historical meaning | Current meaning | Result |
|---:|---|---|---|
| 10 | Open Joining | Open Joining | stable |
| 20 | Close Joining | Close Joining | stable |
| 30 | Set Capacity | unsupported | explicitly retired |
| 40 | Request Join | Request Join | stable |
| 50 | Request Default Actor Selection | Request Default Actor Selection | stable |

## Required failure behavior

Value `30` must:

```text
fail validation explicitly
execute no supported command
perform no Join
perform no Capacity fallback
produce a diagnostic
```

Do not map `30` to Request Join or another supported operation.

## Technical certification

Focused serialization regression:

```text
IF-PLAYER-SERIALIZATION-01
PASS — 5/5
```

The regression is now also part of the executed canonical full Player
certification:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
serialization='PASS'
```

P0 serialized migration integrity is therefore technically closed.

## FIRSTGAME evidence

Historical FIRSTGAME Player authoring helped expose the migration problem because
it contained pre-consolidation serialized command/Profile data.

That historical evidence is not current accepted-model Player integration proof.
Current FIRSTGAME Player composition remains a separate real-integration task and
should be deliberately reauthored/rebuilt rather than repaired by compatibility
fallback in the package.

## Provenance

```text
Package documentation baseline
  43b96a4b100b8273da1190520536007ba82dc081
  ADR-010B

QA source baseline
  b6a45728285ddb2ce08269fc1f88ae3f1a4235e4
  P0 — Serialized Player Migration Integrity

FIRSTGAME inspected state
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
```

The QA manifest uses a local `file:` framework dependency, so the Unity verdict
certifies the exercised workspace rather than independently pinning a package Git
SHA.

## Closure rules

```text
Do NOT restore SetCapacity.
Do NOT add a compatibility Capacity command.
Do NOT reuse serialized value 30.
Do NOT silently repair 30 into another command.
```
