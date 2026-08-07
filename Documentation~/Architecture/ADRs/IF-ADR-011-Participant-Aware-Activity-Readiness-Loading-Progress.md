# IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **92%**  
Implementation classification: **Runtime complete; canonical participant-aware progress and terminal QA re-certified; product presentation and focused public-only/startup cases remain**  
Related decisions: IF-ADR-006, IF-ADR-007, IF-ADR-012  
Current package baseline: `d0955e0dc58a3cc70f8533f92d63246d941d5e20`  
Current QA baseline: `00cedcb78d200b1b2094eafc500e348e07dc36ab`  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Technical scene/content loading may finish before required Activity participants are ready. Loading progress must reserve space for participant-aware readiness without inventing progress, regressing, reaching 100% early, or accepting stale occurrence updates.

## Decision

Activity entry uses a monotonic progress envelope with a technical range and an optional reserved readiness range. Readiness progress is derived from occurrence-scoped aggregate evidence. Terminal 100% is issued only for Ready. Terminal failure stops completion. Stale occurrence snapshots are rejected.

## Covered waits that depend on external actions

A `WaitCovered` operation may legitimately remain below terminal completion while a Required readiness contribution is `Preparing`. One example is Player participation with an Explicit Slot that is waiting for Join or later Player lifecycle progression.

If the only user action capable of advancing that readiness is itself hidden behind the retained cover, the resulting lock is a product control-plane composition problem. Loading must not compensate for it.

Loading therefore continues to obey:

```text
Preparing Required contribution
→ retain the last valid progress state
→ do not publish successful 100%
→ do not hide/reveal as if Ready
```

The package addresses the known Player authoring risk through a non-mutating validation warning on `WaitCovered + ExplicitSlots + Player requirement >= JoinedSlots`. The warning does not change Loading behavior and does not claim the combination is invalid: pre-entry satisfaction, automatic progression, or an external/persistent control plane can make the composition correct.

Rejected Loading-side repairs:

```text
complete technical Loading as successful 100% while Activity is NotReady;
ignore the Player readiness phase;
simulate progress until timeout;
hide Loading to expose a Join control that belongs to covered gameplay;
let the Loading surface become a readiness or Player authority.
```

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package implements required/optional counts including completed and released evidence, readiness snapshots, range mapping, queued reporting, monotonic acceptance, stale occurrence rejection, terminal failure, and completion only when Ready. Integration exists for Activity entry and startup paths.

The Player/WaitCovered authoring warning is intentionally outside Loading runtime. It prevents a known product-composition trap without introducing a second completion authority.

IF-TXN-01 preserves this authority separation: Loading may complete its valid projection, but final GameFlow success still depends on the accepted Transition After/reveal terminal. A failed reveal cannot be converted into success by Loading.

## Current QA evidence

The canonical participant-aware progress suite is now re-certified:

```text
Participant-Aware Readiness Loading Progress Regression
  status: Passed
  cases: 32/32
  required: 4
  optional: 1
  optionalOutcome: FailedNonBlocking
  ordering:
    Technical<100
    → 0/4 → 1/4 → 2/4 → 3/4
    → 4/4=100
    → Hide
    → Reveal
    → GateRelease
```

The terminal/failure suite is also re-certified:

```text
Participant-Aware Readiness Loading Terminal Regression
  status: Passed
  cases: 34/34
  terminals:
    RequiredFailed
    RequiredReleased
    ReplacementRejected
    LateOldOccurrenceRejected
    DuplicateTerminal
    OwnedCancellation
```

The terminal regression confirms that on required failure the destination remains authoritative, progress does not publish terminal success, Loading/Transition remain retained as required, and the recovery gate remains active until cleanup.

Cross-cut evidence:

```text
Direct Activity Readiness Policies Regression: Passed — 42/42
IF-TXN-01 Transition Failure Authority Regression: Passed — 22/22
Activity Readiness Post-Transition Smoke: Passed
Identity Authority Regression: Passed — 6/6
```

This closes the previous statement that the current cleaned QA project still needed to re-register and run the canonical progress suite.

## Current FIRSTGAME evidence

FIRSTGAME loading demonstrations provide practical evidence, including Player readiness as the final loading phase. Focused investigation established that Loading retention during an unjoined Required Player is expected. When `RequestJoin` is available and emitted, Player reconciliation can progress the same Activity occurrence to `Ready`; Loading should then complete through the existing readiness projection.

## What remains

- Add focused public-only proof for `WaitCovered + WaitingForJoin + RequestJoin while gate retained + same-occurrence Ready`.
- Expand startup-path parity where the current executed suite proves contract/wiring but not every host presentation variant.
- Publish presentation guidance for determinate versus indeterminate technical phases.
- Expose readiness ratio inputs and rejection counts in Advanced/Debug diagnostics.
- Preserve stale occurrence, duplicate terminal, cancellation and supersession coverage as the QA harness evolves.

## Completion criteria

- Progress never decreases and never reaches 100% before Ready.
- Stale or foreign occurrences cannot update the active operation.
- Failure and supersession terminate correctly without false completion.
- A covered control-plane dependency cycle cannot be “repaired” by Loading-side false completion.
- Current QA and FIRSTGAME pass the same supported scenarios where consumer proof is required.

## Completion assessment

```text
Estimated completion: 92%
Normative status: Accepted
Participant-aware progress QA: PASS — 32/32
Participant-aware terminal QA: PASS — 34/34
WaitVisible/WaitCovered integration: PASS — 42/42
IF-TXN-01 non-regression: PASS — 22/22
Remaining: public-only waiting/joining proof, startup/product presentation parity, Advanced/Debug polish
```

The percentage is intentionally unchanged: canonical progress/terminal QA is now
current, but product presentation and focused consumer/public-only coverage remain.
