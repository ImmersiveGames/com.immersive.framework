# IF-ADR-007 — Activity Entry Readiness and Reveal Gating

Status: **Accepted**  
Last updated: 2026-08-17  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-009, IF-ADR-011, IF-ADR-012  
Current reconciliation: [ADR-007 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-007-RECONCILIATION-2026-08-11.md)

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

An Activity may have technically loaded content while required participants,
Actors, adapters or local visibility remain preparing. Reveal policy must model
this explicitly.

## Decision

Activity entry uses:

```text
ObserveOnly
WaitVisible
WaitCovered
```

Readiness is occurrence-scoped and aggregates required/optional contribution
evidence. Preparing, Ready, terminal failure, invalidation, cancellation and
supersession are distinct states.

Loading/Transition may wait on readiness but does not own it.

## Required readiness level boundary

When Activity Player participation requests a concrete readiness level, that authored
level is the lifecycle boundary that must be satisfied for the current Activity
occurrence. Higher levels are not inferred from lower ones.

For the accepted Player baseline, the relevant distinction is:

```text
LogicalActorsPrepared
  -> the required contextual Logical Actor is prepared for the current Activity occurrence

GameplayReady
  -> LogicalActorsPrepared plus the current gameplay chain required by Player gameplay
     admission/input/camera consumers
```

Therefore an Activity authored at `LogicalActorsPrepared` may legitimately complete its
Player preparation requirement without creating a current gameplay binding. A consumer
that requires `GameplayReady` must be paired with Activity authoring that explicitly
requires `GameplayReady`; the runtime must not auto-promote the authored requirement,
fabricate readiness or infer consumer intent.

A downstream gameplay consumer remaining unbound while the Activity only requires
`LogicalActorsPrepared` is not, by itself, evidence of a readiness regression.

The current accepted boundary does not impose an elapsed-time timeout on Activity
entry readiness. A waiting operation remains pending until its captured occurrence
reaches Ready or terminal failure, or until the owning operation is causally
cancelled, invalidated or superseded. Timeout/retry authoring, if introduced in a
future scope, must be an explicit contract rather than a hidden timer or silent
policy weakening.

## WaitCovered

`WaitCovered` retains destination presentation and unsafe gameplay capabilities
until the captured occurrence reaches Ready.

A Required contribution may legitimately remain Preparing indefinitely while its
represented condition has not occurred. The framework must not fabricate
readiness through timeout, fake completion, premature Loading completion or
policy weakening.

A composition can deadlock itself when the only operation capable of satisfying
a Required condition is hidden behind the retained cover. That is a control-plane
composition problem.

Validation may warn about risky combinations such as a covered wait that depends
on a not-yet-joined required Player, but it must remain advisory/non-mutating.

## Recovery boundary

Transition Gate and Activity Entry Readiness Recovery Gate remain distinct. A
terminal readiness failure may leave readiness recovery active after the pure
Transition Gate is clean.

## Constraints

- WaitCovered never reveals before Ready.
- WaitVisible permits visible preparation while unsafe capabilities remain gated.
- ObserveOnly does not become an accidental wait.
- Stale/foreign occurrences cannot satisfy the active occurrence.
- Required failure remains blocking and diagnostic.
- Optional participants do not silently become required or enter the progress
  denominator.
- Validation does not auto-change policy, participation or Joining state.
