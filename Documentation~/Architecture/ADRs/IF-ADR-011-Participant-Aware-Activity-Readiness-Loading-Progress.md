# IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **92%**  
Implementation classification: **Runtime complete; current QA and product presentation recertification remain**  
Related decisions: IF-ADR-006, IF-ADR-007, IF-ADR-012  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`  
Decision amendment baseline: package `20b03efff3fe284f2098e12daf1f9274612ea40a`

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

## Current QA evidence

Historical implementation cuts and QA evidence exist, but the current cleaned QA project must re-register and run the canonical progress suite.

## Current FIRSTGAME evidence

FIRSTGAME loading demonstrations provide practical evidence, including Player readiness as the final loading phase.

Focused investigation established that Loading retention during an unjoined Required Player is expected. When `RequestJoin` is available and emitted, Player reconciliation can progress the same Activity occurrence to `Ready`; Loading should then complete through the existing readiness projection.

## What remains

- Rebuild current QA for monotonicity, duplicate reports, stale occurrence, failure, release, zero participants, optional-only participants, and supersession.
- Validate all loading policies and startup paths use consistent phase/message semantics.
- Add a public-only integration proof that successful `RequestJoin` under a retained WaitCovered gate progresses readiness before Loading terminal success.
- Publish presentation guidance for determinate versus indeterminate technical phases.
- Expose readiness ratio inputs and rejection counts in Advanced/Debug diagnostics.

## Completion criteria

- Progress never decreases and never reaches 100% before Ready.
- Stale or foreign occurrences cannot update the active operation.
- Failure and supersession terminate correctly without false completion.
- A covered control-plane dependency cycle cannot be “repaired” by Loading-side false completion.
- Current QA and FIRSTGAME pass the same scenarios.

## Completion assessment

```text
Estimated completion: 92%
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
