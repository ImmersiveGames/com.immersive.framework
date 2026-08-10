# IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

Status: **Accepted**  
Last updated: 2026-08-09  
Related decisions: IF-ADR-001, IF-ADR-005, IF-ADR-007, IF-ADR-011, IF-ADR-015

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Loading and Transition presentation must represent real operation state,
preserve persistent application surfaces, coordinate cover/reveal and gates,
expose terminal failures and correlate diagnostics without becoming Route,
Activity or readiness authority.

## Decision

The framework owns persistent Transition/Loading surfaces and a typed
orchestration path.

```text
cover
-> technical loading
-> optional readiness wait
-> reveal
-> terminal result
-> cleanup/recovery
```

Presentation reports state; it does not calculate readiness or own destination
authority.

## Transaction continuation

For startup, Route, Activity, Clear and Restart:

- a non-accepted Transition `Before` prevents the governing lifecycle mutation;
- a non-accepted Transition `After` after commit preserves the authority that
  actually committed and cannot become false success;
- committed authority is not blindly rolled back;
- intentional supersession is typed separately from ordinary failure.

## Gate/recovery diagnostics

```text
TransitionGateSnapshot
  -> pure Transition Gate

ActivityEntryReadinessGateSnapshot
  -> Transition Gate + readiness recovery
```

A committed-target readiness failure may retain Loading/Transition presentation
and a readiness recovery blocker after the ordinary Transition Gate is released.
Diagnostics must not report that state as a Transition Gate leak.

## Logging and diagnostics

Operational summaries belong at normal development levels; detailed state and
correlation evidence belong in Debug/Trace and Advanced / Debug surfaces.

Loading is not readiness authority. Transition presentation is not Route or
Activity authority.

## Constraints

- No supported terminal path reports false success.
- Successful Loading completion cannot reach terminal 100% before governing
  readiness permits it.
- Missing required presentation contracts fail explicitly where required.
- Optional presentation absence is explicit optional/NoOp behavior, not hidden
  object creation.
- Compensation/retry mechanisms are introduced only for concrete demonstrated
  partial-side-effect paths, not as a generic rollback manager.
