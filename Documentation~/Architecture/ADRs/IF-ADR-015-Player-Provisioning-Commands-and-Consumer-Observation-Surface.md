# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: **Accepted / Reconciled / Implemented**  
Last updated: **2026-08-29**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021, IF-ADR-023  
Public-surface reconciliation: [IF-ADR-015B — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)  
Current Player Actor runtime certification: [IF-ADR-023 — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)

## Decision

The package exposes typed scoped Player Session access, immutable observation and explicit command components over existing Session/Player authorities.

```text
PlayerSessionObserver = read
explicit command component = request/change
```

Neither becomes Session authority.

## Current public command surface

```text
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
PlayerSessionJoinCommandTrigger
PlayerSessionSelectActorCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionReplaceActorSelectionCommandTrigger
PlayerSessionClearActorSelectionCommandTrigger
PlayerSessionLeaveCommandTrigger
```

Each component represents one operation and owns only its typed result. The former enum-driven generic command surface is superseded.

## Join

`PlayerSessionJoinCommandTrigger` requests ordinary public Join using the Session's current supported Slot order and eligibility.

Join is untargeted with respect to exact public Slot choice. Optional Input System hints supported by the Join request may be passed, but a complete public Slot/device/InputUser ownership contract remains future scope.

Join does not select an arbitrary Actor or materialize Actor representation itself.

## Actor selection

The four explicit logical Actor-selection commands are:

```text
Select Actor
Select Default Actor
Replace Actor Selection
Clear Actor Selection
```

All return `PlayerActorSelectionResult`.

Selection is revision-aware and Session-owned. Replace/Clear are allowed before the canonical preparation barrier and are not prepared physical Actor hot-swap operations.

`LeaveUnresolved` is a valid creation-time Session policy. Explicit Character Selection uses `PlayerSessionSelectActorCommandTrigger` after Join.

## Leave

`PlayerSessionLeaveCommandTrigger` targets one explicit Player Slot and current occurrence/revision evidence. Leave authority/release semantics remain governed by IF-ADR-020.

## Scoped access

Consumer access is:

```text
typed
Route- or Activity-scoped
lifetime-explicit
stale-scope rejecting
diagnostic when unavailable
free of serialized cross-scene authority references
```

Scope is Framework lifecycle ownership:

```text
Route scope     = Route lifecycle ownership
Activity scope  = Activity lifecycle ownership
scene location  != scope authority
```

Physical location in Route-discovered content does not force Route scope. An Activity-scoped consumer may be discovered there and bind while the Activity lifecycle is active.

Keep these contracts separate:

```text
TryValidateConfiguration()
  = authored configuration validity

BindingState / IsScopedAccessAvailable / TryGetAccess
  = runtime scoped-access availability
```

A valid authored component may temporarily have no live runtime binding. Runtime invocation fails closed without fallback.

`PlayerSessionScopedAccessConsumer.TryBind(...)` remains the canonical scope-match boundary.

No public static registry, service locator, reflection, scene-wide authority search or hierarchy/name inference is required.

## Teardown reconciliation — 2026-08-29

Unity destruction order may destroy a scene consumer before the persistent runtime owner that registered it.

Canonical teardown behavior is:

```text
consumer OnDestroy
  -> release consumer-side scoped binding

persistent runtime owner OnDestroy later
  -> tolerate Unity fake-null consumer wrapper
  -> no second semantic release through destroyed component
  -> diagnostics never dereference destroyed Unity objects
```

The reproduced failure was:

```text
PlayerSessionScopedAccessRuntimeHostModule.OnDestroy
→ ReleaseScopedAccess(destroyed consumer)
→ BuildFields
→ Unity-backed name
→ MissingReferenceException
```

The Framework consumer boundary was hardened so owner-side release is idempotent for an already destroyed consumer and diagnostic field construction is teardown-safe.

This is lifetime robustness only; it introduces no second scoped-access or Session authority.

## PlayerSessionObserver

`PlayerSessionObserver` is read-only scoped Session presentation evidence. It may be used by Hub/UI/presentation surfaces without locating the physically materialized Player GameObject.

It may expose current Session, Joining, Slot, Actor selection, preparation/materialization, Activity and gameplay evidence. It does not execute commands or own Player truth.

## Command result ownership

Each command component owns only its own typed result:

```text
Open/Close Joining -> PlayerParticipationOperationResult
Join               -> LocalPlayerJoinResult
Actor commands     -> PlayerActorSelectionResult
Leave              -> SessionPlayerLeaveResult
```

The Observer is not a global last-command store.

## Transaction boundaries

```text
Join
!= Actor Selection
!= Actor Preparation
!= Physical Materialization
```

The consumer surface must not collapse those transactions into a single opaque request.

## No-fallback rules

The public surface fails explicitly rather than:

```text
invalid scope -> search another Session
missing scoped access -> global lookup
Join failure -> silently choose alternate authority
Actor selection -> silently choose another Actor
Actor selection -> materialize directly
Replace -> hot-swap prepared physical Actor
Leave -> infer another Slot
```

## Deferred readiness issue

`PLAYER-COMMAND-SURFACE-READINESS / DEFERRED` remains a product-availability concern: a valid authored command may exist before its live scoped access is available. Such invocation rejects explicitly without fallback.

## Certification

Public Actor Selection aggregate evidence remains:

```text
PLAYER CURRENT AGGREGATE COMPLETE
27/27 PASS
actor=PASS
publicSurface=PASS
```

Current consolidated functional evidence adds:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
cases='14/14'
```

That run positively proves Route + Activity scoped access, validates the eight current explicit command components, and exercises Join/Leave/Rejoin under the current ADR-023 Actor composition.

Character Selection is public-surface unblocked and FIRSTGAME-proven. Exact-Slot public Join and the canonical Local Multiplayer Slot/device/input contract remain future work.
