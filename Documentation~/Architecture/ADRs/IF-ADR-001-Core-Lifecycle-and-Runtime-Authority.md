# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: **Accepted**  
Last updated: 2026-08-09  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-007, IF-ADR-011, IF-ADR-014

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

The framework requires one explicit owner for application/session composition,
Route and Activity lifecycle, scene/content ownership and feature runtime bindings
without globally discoverable mutable state. Session-scoped participation may
outlive Route and Activity changes, so contextual gameplay ownership must not be
confused with Session authority.

## Decision

`com.immersive.framework` owns framework-specific lifecycle and product modules.
`FrameworkRuntimeHost` is the internal application/session composition root. It
must not expose a static current-host registry, service locator, hierarchy lookup
or implicit singleton access path.

Runtime dependencies are supplied through narrow typed ports and explicit
composition.

```text
Game Application / Session
  -> Session-scoped authorities and participants
     -> Logical Players
  -> Route
     -> Activity
        -> contextual projection, readiness and materialization
```

Route and Activity own contextual lifecycle, not Session participant identity.
Missing required bindings fail explicitly. Functional identity is typed and
domain-specific; names and hierarchy paths are diagnostic data, not authority.

## Transition outcome authority

Transition results govern continuation for Game Application startup, Route,
Activity, Activity Clear and Activity Restart.

```text
accepted Transition phase
  -> Completed
  -> or intentional policy/no-visual Skipped

non-accepted Before
  -> do not advance the governing lifecycle mutation
  -> preserve the previous committed authority
  -> typed pre-commit failure

non-accepted After after commit
  -> never report ordinary success
  -> preserve the authority that actually committed
  -> no blind rollback
  -> typed committed-target reveal failure/recovery
```

Clear post-commit authority remains `CurrentActivity=None`. Restart post-commit
authority remains the re-entered Activity/new occurrence.

## Transition Gate terminal integrity

The GameFlow Transition Gate is internal operation state, not an externally
acquired resource with a fallible release protocol.

```text
TransitionGateSnapshot
  -> pure Transition Gate state

CurrentTransitionGateMode
  -> pure Transition Gate mode

ActivityEntryReadinessGateSnapshot
  -> Transition Gate + Activity Entry Readiness Recovery Gate

CurrentGateSnapshot
  -> broader operational composition, including Pause/readiness
```

A committed readiness failure may validly have a released Transition Gate while
a readiness recovery blocker remains active. This is recovery protection, not a
Transition Gate leak.

Canonical Transition Gate release is unconditional internal state replacement.
Do not introduce a generic lease/release manager or transaction manager to model
a failure mode that the current authority does not have.

## Architectural constraints

- Runtime authority is scoped, typed and lifetime-explicit.
- Required invalid configuration fails explicitly and diagnostically.
- Consumer code does not depend on internal runtime modules, reflection,
  object-name inference or implicit global lookup.
- No silent fallback may change authority or policy.
- Runtime contexts/services remain scoped rather than globally discoverable.
- Editor authoring never becomes runtime authority.

## Deferred extensions

Session-Persistent Player and exceptional post-commit compensation are separate
future contracts. They do not weaken the current authority model and must not be
simulated through arbitrary persistent GameObjects or a generic rollback manager.

## Reopen criteria

Reopen this ADR only when a concrete requirement changes lifecycle ownership,
composition-root authority, transition continuation semantics or scoped runtime
access.
