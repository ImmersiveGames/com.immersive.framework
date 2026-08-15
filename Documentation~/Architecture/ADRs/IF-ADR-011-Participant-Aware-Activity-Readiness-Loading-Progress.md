# IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress

Status: **Accepted / Reconciled for Player Boundary 2026-08-15**  
Last updated: **2026-08-15**  
Related decisions: IF-ADR-005, IF-ADR-006, IF-ADR-007, IF-ADR-012, IF-ADR-019

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Technical scene/content loading may finish before required Activity participants are Ready. Loading progress must reserve space for readiness without inventing progress, regressing, reaching successful 100% early or accepting stale occurrence updates.

## Decision

Activity entry uses a monotonic progress envelope with a technical range and an optional reserved readiness range.

Readiness progress derives from occurrence-scoped aggregate evidence. Terminal successful 100% is published only for Ready. Terminal failure stops successful completion. Stale or foreign occurrence snapshots are rejected.

## Required/optional semantics

Only Required participants enter the readiness progress denominator. Optional participants remain diagnostic and cannot block Ready.

The exact global percentages depend on the technical operation envelope; the framework does not promise fixed global fractions per participant.

For Player contributions, progress follows the effective requirement rather than the mere existence of a physical Host/Actor. Per IF-ADR-019:

```text
JoinedSlots / SelectedActors
  Session evidence may satisfy the requirement with Activity representation Absent

LogicalActorsPrepared / GameplayReady
  current Activity representation is part of the required evidence
```

Progress must not remain blocked solely because a Session-only requirement has no physical Activity representation, and it must not publish successful completion when a representation-required level lacks current occurrence evidence.

## Covered waits

A covered wait may legitimately remain below successful terminal completion while a Required contribution is Preparing. Loading must not compensate for an unreachable control-plane action through fake progress, timeout or premature hide/reveal.

A committed target may also end as `CommittedNotReady` because a Required Player/lifecycle contribution failed. That state does not retroactively turn a successful Route navigation commit into an uncommitted Route. Loading/readiness presentation follows the Activity readiness result, while Route commit remains separate navigation truth.

## Gate boundary

Loading/readiness recovery and the ordinary Transition Gate are distinct. A required failure may retain readiness recovery while the pure Transition Gate is already released.

Diagnostics must query the semantic surface they intend to describe.

For Player failures, Activity-owned RuntimeContent that belongs to the current committed Activity remains Activity lifetime. It must not be destroyed by Player rollback merely because readiness failed, and its presence must not be interpreted as a successful Player contextual admission or physical handoff.

## Constraints

- Progress is monotonic.
- Successful 100% occurs only after aggregate Ready.
- Stale/foreign occurrences cannot update the active operation.
- Failure, cancellation and supersession never publish false successful 100%.
- Loading remains presentation, not readiness authority.
- Route/navigation commit and Activity readiness remain separate truths.
- Activity-owned RuntimeContent is not Player physical ownership.
- No timeout or fake-completion fallback is introduced to repair composition.

## Player-boundary certification note

The 2026-08-15 Full Player QA completed `25/25` mandatory contracts. Its failed-adoption and failed-reprojection cases confirm that readiness failure remains explicit, does not fabricate successful completion and does not cause physical Player handoff.
