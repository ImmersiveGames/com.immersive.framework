# IF-ADR-007 — Activity Entry Readiness and Reveal Gating

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **96%**  
Implementation classification: **Runtime contract complete; current QA recertification remains**  
Related decisions: IF-ADR-003, IF-ADR-006, IF-ADR-009, IF-ADR-011, IF-ADR-012  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`  
Decision amendment baseline: package `20b03efff3fe284f2098e12daf1f9274612ea40a`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

An Activity may have technically loaded content while required participants, Actors, adapters, or local visibility are still preparing. Reveal policy must distinguish observing readiness, waiting while visible, and waiting while covered without deadlocking or treating expected authority replacement as failure.

## Decision

Activity entry uses explicit policies:

```text
ObserveOnly
WaitVisible
WaitCovered
```

Readiness is occurrence-scoped and aggregates required/optional contribution evidence. Preparing, Ready, terminal failure, invalidation, cancellation, and supersession are distinct. Loading/transition presentation may wait on readiness but does not own it. A newer Route or Activity authority may supersede an in-flight wait through a typed interruption cause.

## Covered readiness and externally-driven progression

`WaitCovered` deliberately retains the destination presentation and gameplay capabilities until the captured Activity readiness occurrence reaches `Ready`. A Required contribution is allowed to remain `Preparing` indefinitely when the condition it represents has not yet occurred. The framework does not fabricate readiness through timeout, presentation release, or Loading completion.

This means a product can create a dependency cycle even when every runtime authority is behaving correctly:

```text
Required readiness depends on an external/user action
→ WaitCovered keeps the destination covered
→ the only control that can perform the action is inside that covered destination
→ the action never occurs
→ readiness remains Preparing
→ WaitCovered remains covered
```

This is a control-plane composition problem, not a reason for Activity Readiness or `WaitCovered` to weaken their contracts.

The package cannot generically prove whether an arbitrary readiness participant needs a user action or whether that action remains reachable. It may, however, warn about known cross-domain combinations whose authoring data exposes the risk. The initial canonical warning is:

```text
EntryReadinessPolicy = WaitCovered
Player projection = ExplicitSlots
Player requirement >= JoinedSlots
```

This warning is non-mutating and advisory. The combination is valid when required Player state is satisfied before entry, progresses automatically, or can be advanced through a control plane outside the covered destination. `WaitVisible` is the appropriate policy when preparation is intentionally part of the visible Activity experience.

Rejected repairs:

```text
automatically downgrade WaitCovered to WaitVisible;
mark Required Player readiness Optional;
treat an unjoined Explicit Slot as NoParticipants;
timeout to Ready;
automatically Join a Player;
publish Loading success before aggregate Ready;
re-request the same Activity as a reconcile mechanism.
```

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.
- A validation warning for a potential control-plane dependency cycle must not silently change authored readiness, transition, participation, or gate configuration.

## Current implementation coverage

The package implements readiness policies, occurrence correlation, required/optional pending/completed/failed/released counts, progressive state, reveal gating, failure diagnostics, loading progress integration, and the new `Superseded` wait/execution path for authority replacement. This directly addresses the recent Route replacement/readiness issue.

Activity Player participation authoring now reports a warning for the known `WaitCovered + ExplicitSlots + Player requirement >= JoinedSlots` risk. The warning teaches composition intent; runtime semantics remain unchanged.

## Current QA evidence

The runtime contract is complete, but the reorganized QA harness must re-establish the canonical policy/replacement matrix at the current HEAD.

## Current FIRSTGAME evidence

FIRSTGAME has exercised real WaitCovered/Player-readiness interactions and Route replacement, providing high-value consumer evidence for the fix.

Focused investigation also showed that the Manager-Provisioned late-join path progresses to `Ready` when `RequestJoin` is emitted. The remaining WaitCovered product risk is ensuring that the operation required to advance readiness remains available while gameplay presentation is covered.

## What remains

- Rebuild QA for all three policies across Ready, Preparing, Failed, Released, Invalidated, Cancelled, and Superseded outcomes.
- Add explicit tests for required Player contribution when joining is closed, capacity changes, no Player exists, and Route replacement occurs.
- Add a public-only `WaitCovered + WaitingForJoin + RequestJoin while gate retained + same-occurrence Ready` regression.
- Publish a concise policy selection guide explaining when loading should remain covered.
- Expose occurrence/revision and pending contribution details consistently in Advanced/Debug presentation.

## Completion criteria

- WaitCovered never reveals before Ready and never deadlocks after typed supersession.
- WaitVisible permits visible preparation without losing terminal diagnostics.
- ObserveOnly never becomes an accidental blocking wait.
- Known authoring combinations that can create a covered control-plane dependency cycle are warned without rejecting valid automatic/pre-entry/external-control-plane compositions.
- Current QA passes the full policy and authority-replacement matrix.

## Completion assessment

```text
Estimated completion: 96%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
Decision amendment: package 20b03eff
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
