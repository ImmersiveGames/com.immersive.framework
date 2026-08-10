# IF-ADR-007 — Activity Entry Readiness and Reveal Gating

Status: **Accepted**  
Last updated: 2026-08-09  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-009, IF-ADR-011, IF-ADR-012

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
