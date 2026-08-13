# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: **Accepted**  
Last updated: 2026-08-12  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-007, IF-ADR-011, IF-ADR-014, IF-ADR-019
Current reconciliation: [ADR-001 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-001-RECONCILIATION-2026-08-10.md)

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

## Object Entry runtime-context projection

Object Entry does not introduce another lifecycle authority.

The accepted ownership relationship is:

```text
Game Application / Session / Route / Activity lifecycle
        ↓ authoritative current scope and occurrence
FrameworkRuntimeHost
        ↓ derives
ObjectEntryRuntimeContextSnapshot
        ↓ read-only scoped semantic projection
Object Entry consumers
```

`ObjectEntryRuntimeContextSnapshot` is a derivative view of already-authoritative
lifecycle state. It may expose the currently valid Session/Route/Activity scope,
typed owner evidence and object-entry descriptors, but it cannot create, replace
or override Route/Activity authority or runtime occurrence identity.

Object Entry declarations and descriptors therefore cannot:

```text
keep a Route or Activity alive
select an arbitrary active owner
replace the current Activity occurrence
register a global runtime service
become a service locator
turn authored metadata into lifecycle authority
```

When Object Entry metadata is consumed by Reset or diagnostics, the authoritative
lifetime and occurrence remain the current scoped lifecycle authorities defined by
this ADR.

The historical F13 Object Entry Foundation is absorbed by this rule together with
IF-ADR-014 stable-identity semantics. It does not require a new independent
lifecycle ADR.

## Architectural constraints

- Runtime authority is scoped, typed and lifetime-explicit.
- Required invalid configuration fails explicitly and diagnostically.
- Consumer code does not depend on internal runtime modules, reflection,
  object-name inference or implicit global lookup.
- No silent fallback may change authority or policy.
- Runtime contexts/services remain scoped rather than globally discoverable.
- Editor authoring never becomes runtime authority.
- Object Entry runtime context remains a derivative projection and never becomes
  an independent lifecycle owner.

## Session Player lifetime boundary

IF-ADR-019 resolves the former `Session-Persistent Player` future direction.

A Joined Logical Player is Session-scoped while Route/Activity owns contextual
Player/Actor representation, readiness, bindings and materialization. Activity exit
does not end Session participation, and Activity entry does not re-Join an already
Joined Logical Player.

Provisioning-specific physical lifetime remains explicit:

```text
Manager-Provisioned Host
  Session-owned after successful Join

Scene-Provided Host/Actor
  consumer-scene-owned contextual occurrence
```

Neither case weakens this ADR's composition-root rule: Session persistence must not be
implemented through arbitrary persistent GameObjects, global discovery or a Player
service locator.

## Deferred extensions

Explicit Session Player Leave is owned by IF-ADR-020 and remains a separate contract
until that ADR is accepted. Exceptional post-commit compensation also remains separate
and must not be simulated through a generic rollback manager.

## Reopen criteria

Reopen this ADR only when a concrete requirement changes lifecycle ownership,
composition-root authority, transition continuation semantics, scoped runtime
access, or attempts to make Object Entry metadata an independent lifecycle
authority.
