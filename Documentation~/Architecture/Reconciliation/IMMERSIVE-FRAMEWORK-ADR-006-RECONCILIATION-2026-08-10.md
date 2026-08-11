# Immersive Framework — ADR-006 Reconciliation

**Date:** 2026-08-10  
**Type:** technical source reconciliation and focused Stage A QA gap record  
**ADR:** IF-ADR-006 — Loading, Transition, Persistence and Diagnostics  
**Package baseline inspected:** `f34eb059254287e13a0ab48f9ecab8bda072744c` (`master`, read-only)

## Objective

Reconcile IF-ADR-006 against the current `com.immersive.framework` package,
separate implementation gaps from evidence gaps, and define the focused QA work
required before Stage A closure.

This record does **not** certify the pending exceptional-path cases. It records
that the package source is aligned with the current accepted ADR-006 boundary and
that the remaining Stage A work is focused technical evidence unless QA exposes
a concrete package divergence.

## Source baseline

```text
com.immersive.framework
  branch: master
  commit: f34eb059254287e13a0ab48f9ecab8bda072744c
  access: read-only inspection
```

`QAFramework` is intentionally not used as certification evidence in this
source-reconciliation record. The focused QA execution is the next technical cut.

`FIRSTGAME` remains Stage B consumer evidence and does not determine whether the
package source is aligned with this Stage A architectural boundary.

## Scope

This reconciliation covers:

- Transition `Before` / `After` transaction continuation;
- committed authority after post-commit presentation failure;
- typed supersession versus ordinary failure;
- technical Loading progress versus readiness-governed terminal completion;
- pure Transition Gate versus readiness/reveal recovery protection;
- required presentation failure versus explicit optional/NoOp presentation;
- transition/loading diagnostics and cleanup semantics.

## Out of scope

This reconciliation does not introduce or require:

- a new runtime architecture;
- a generic rollback or compensation manager;
- readiness authority inside Loading;
- Route or Activity authority inside Transition;
- new global managers, service locators or implicit runtime lookup;
- broad Transition/Loading UX redesign;
- FIRSTGAME implementation or Stage B closure.

## Executive disposition

```text
Architecture
  ACCEPTED

Package
  IMPLEMENTED for the current accepted ADR-006 boundary

Technical QA
  PARTIAL
  focused exceptional-path evidence remains

Stage A
  OPEN only for the focused QA evidence listed below

FIRSTGAME / Stage B
  PARTIAL and tracked separately
```

No runtime change should be added only to make documentation appear complete.
The next cut must first exercise the accepted contracts in `QAFramework`. If an
unchanged focused QA case reproduces a contract failure, the permanent fix belongs
in `com.immersive.framework`, followed by the same QA case again.

## Canonical package owners

The current package already separates the relevant responsibilities across
canonical runtime areas:

```text
Runtime/Transition
  ITransitionOrchestrator.cs
  TransitionEffectOrchestrator.cs
  NoOpTransitionOrchestrator.cs
  TransitionGateBlockerPolicy.cs

Runtime/GameFlow
  GameFlowRuntime.cs
  GameFlowRuntime.TransitionFailureAuthority.cs
  GameFlowRuntime.ActivityEntryLoadingProgress.cs
  GameFlowRuntime.ActivityEntryReadinessOrchestration.cs
  ActivityEntryReadinessExecutionStatus.cs
  ActivityEntryReadinessRecoveryGatePolicy.cs
  CommittedTargetRevealRecoveryGatePolicy.cs
  FrameworkTransitionDiagnostics.cs
  TransitionGateDiagnostics.cs

Runtime/Loading
  ActivityEntryLoadingProgressDiagnostics.cs
  ActivityEntryLoadingProgressEnvelope.cs
  ActivityEntryLoadingProgressPlan.cs
  FrameworkLoadingProgress.cs
  FrameworkLoadingProgressReporter.cs
```

These owners are evidence of the current package shape; they are not a mandate to
keep every internal filename forever. The architectural contracts remain defined
by the ADR.

## Contract-to-source reconciliation

| ADR-006 contract | Current package owner/evidence | Source disposition | QA disposition |
|---|---|---|---|
| Transition is presentation/orchestration and does not own Route or Activity authority | `Runtime/Transition/ITransitionOrchestrator.cs`, `TransitionEffectOrchestrator.cs` | Aligned | Preserve with focused negative evidence where applicable |
| Optional presentation absence is explicit NoOp behavior | `Runtime/Transition/NoOpTransitionOrchestrator.cs` | Aligned | Prove optional path remains accepted and non-magical |
| Non-accepted `Before` prevents governing mutation | `Runtime/GameFlow/GameFlowRuntime.TransitionFailureAuthority.cs` | Aligned | Focused exceptional-path proof required |
| Non-accepted `After` after commit cannot produce false success or blind rollback | `Runtime/GameFlow/GameFlowRuntime.TransitionFailureAuthority.cs`, request result/diagnostic paths | Aligned | Focused post-commit proof required |
| Intentional supersession is distinct from ordinary failure | `Runtime/GameFlow/ActivityEntryReadinessExecutionStatus.cs`, readiness orchestration | Aligned | Focused typed-result proof required |
| Technical loading completion is not equivalent to readiness-governed terminal completion | `Runtime/GameFlow/GameFlowRuntime.ActivityEntryLoadingProgress.cs`, readiness orchestration, `Runtime/Loading/*` progress contracts | Aligned in source | Highest-priority focused QA proof required |
| Pure Transition Gate is distinct from readiness/reveal recovery protection | `Runtime/GameFlow/TransitionGateDiagnostics.cs`, `ActivityEntryReadinessRecoveryGatePolicy.cs`, `CommittedTargetRevealRecoveryGatePolicy.cs` | Aligned | Focused diagnostic/gate proof required |
| Required presentation contract failures are explicit and diagnosable | `Runtime/Transition/TransitionEffectOrchestrator.cs`, transition diagnostics | Aligned | Focused negative proof required |
| Accepted terminal paths clean up pure Transition Gate state | Transition gate blocker policy + GameFlow cleanup/diagnostics | Aligned in current source shape | Focused residual-state proof required |

## Focused Stage A QA matrix

The following cases are the remaining ADR-006 technical evidence boundary. They
should become canonical QA scenarios without broadening the architecture.

### ADR006-QA-01 — Before failure blocks mutation

Given a governing lifecycle request whose Transition `Before` returns a
non-accepted terminal result:

- no governing Route/Activity mutation is committed;
- the request returns a typed non-success terminal result;
- diagnostics identify the failing transition phase/cause;
- no pure Transition Gate residue remains after terminal cleanup.

### ADR006-QA-02 — After failure preserves committed authority

Given a lifecycle mutation that commits successfully and whose Transition `After`
then returns a non-accepted result:

- the committed destination remains authoritative;
- the overall operation does not report success;
- no blind rollback to the previous authority occurs;
- any retained recovery protection is explicit and diagnosable.

### ADR006-QA-03 — Superseded is not Failed

Given an older readiness/entry operation intentionally replaced by a newer
authoritative operation:

- the older operation terminates as `Superseded` (or its current canonical typed
  equivalent), not ordinary `Failed`;
- the newer operation remains authoritative;
- diagnostics do not misclassify intentional supersession as a failure leak.

### ADR006-QA-04 — Technical loading complete while readiness waits

Given technical loading that reaches its technical completion boundary while the
governing readiness condition is still pending:

- Loading presentation does not reach successful terminal 100%;
- reveal is not permitted prematurely;
- the wait is attributable to readiness rather than a fabricated loading task;
- when readiness succeeds, terminal completion/reveal can proceed normally.

This is the highest-priority case because it proves the Loading/readiness authority
boundary directly.

### ADR006-QA-05 — Recovery protection is not a Transition Gate leak

Given a committed target whose readiness/reveal path fails and retains protective
presentation:

- the pure Transition Gate can be released/clean;
- readiness/reveal recovery protection can remain active;
- diagnostics distinguish those two states explicitly;
- the retained cover is not reported as a pure Transition Gate leak.

### ADR006-QA-06 — Missing required presentation contract fails explicitly

Given a Transition configuration that requires a presentation contract/adapter
which is absent or invalid:

- the operation fails explicitly at the appropriate boundary;
- the failure is typed/diagnosable;
- there is no silent fallback that converts the invalid required configuration
  into success.

### ADR006-QA-07 — Optional presentation uses explicit NoOp

Given a valid flow where Transition presentation is optional and intentionally
not configured:

- the canonical optional/NoOp path is used;
- no hidden presentation object is created to mask the absence;
- the governing lifecycle can continue according to its own authority;
- diagnostics/results remain coherent.

### ADR006-QA-08 — Terminal cleanup leaves no pure Transition Gate residue

For accepted terminal flows and the focused failure paths above where cleanup is
expected:

- no residual pure Transition Gate blocker survives terminal cleanup;
- any intentionally retained readiness/reveal recovery protection is reported
  separately and with a causal reason;
- repeated execution does not accumulate blockers or stale operation state.

## QA ownership rule

`QAFramework` owns the synthetic and negative proof for the matrix above.

QA should test public/canonical package behavior and diagnostics rather than
replicating package internals as a second implementation. Test-only adapters or
fault injectors are acceptable when needed to produce deterministic exceptional
paths, but they must remain QA infrastructure.

If a case fails:

```text
focused QA reproduces contract divergence
  -> record the first causal mismatch
  -> fix the official package owner
  -> rerun the same QA without weakening assertions
```

Do not resolve a failed QA case by adding a consumer-side workaround in
FIRSTGAME or by introducing a silent fallback.

## Stage A closure rule

ADR-006 can move from the current 95% planning estimate to Stage A closure only
when the focused matrix has sufficient deterministic evidence for the accepted
boundary.

If the matrix passes against the current package without package changes:

- mark Technical QA as `CERTIFIED` for the accepted ADR-006 boundary;
- mark Architecture as `ACCEPTED / RECONCILED` in tracking;
- move Stage A estimate to 100% and technical remaining to 0%;
- keep any remaining FIRSTGAME work as Stage B only.

If one or more cases expose a package defect, Stage A remains open until the
package fix is validated by the unchanged focused QA.

## Stage B / FIRSTGAME boundary

FIRSTGAME should prove the real consumer experience, not become the permanent
exception-path laboratory.

Useful Stage B evidence includes:

- a real Loading + Transition flow configured in a game scene/application flow;
- understandable cover/wait/reveal behavior during real activity entry;
- useful Advanced / Debug evidence when presentation remains covered;
- no requirement for the consumer to manually reconstruct internal framework
  contracts just to use the feature.

Synthetic `Before`/`After` failures, forced missing adapters and gate-leak probes
belong in QA unless a real consumer bug independently reproduces them.

## Documentation changes in this cut

Edited:

- `Documentation~/Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md`
- `Documentation~/Architecture/Tracking/IF-TRACK-Framework.md`

Created:

- `Documentation~/Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-006-RECONCILIATION-2026-08-10.md`

Removed:

- none.

## Acceptance criteria for this documentation cut

Technical/documentation:

- ADR-006 remains normative and does not contain mutable certification counts;
- tracking identifies the package as implemented for the current accepted
  boundary rather than broadly partial;
- tracking does not falsely certify the still-unexecuted focused QA matrix;
- the remaining 5% Stage A estimate is explicitly an evidence gap;
- reconciliation maps the accepted contracts to current canonical package owners;
- no runtime implementation changes are introduced by this cut.

Product/process:

- FIRSTGAME remains a real-consumer Stage B proof rather than synthetic QA;
- QA has a bounded next cut instead of an open-ended instruction to add more
  validators or smokes;
- any future permanent fix revealed by QA is routed back to the package.

## Architectural gain

This reconciliation prevents two opposite errors:

1. reimplementing already-aligned Loading/Transition architecture merely because
   technical evidence is incomplete;
2. declaring ADR-006 fully certified from source inspection without exercising
   the exceptional paths that define its transaction and recovery guarantees.

## Usability gain

The framework can continue evolving Loading/Transition as a coherent product
surface while technical exceptional cases stay in QA. Consumers are not asked to
assemble recovery, gate or readiness internals manually as part of normal use.

## Suggested commit

```text
docs(architecture): reconcile ADR-006 loading transition boundary
```
