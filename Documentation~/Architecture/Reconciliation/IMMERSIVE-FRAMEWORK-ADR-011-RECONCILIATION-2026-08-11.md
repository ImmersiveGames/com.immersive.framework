# Immersive Framework — ADR-011 Reconciliation

**Date:** 2026-08-11  
**Type:** technical / QA reconciliation  
**ADR:** IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress  
**Package source baseline inspected:** `8b7278e3683daef1b2eac6f78c1e0b156e4365da` (`master`, read-only)  
**QA source baseline inspected:** `ba1529e653b25c1215d4f741cd3611b7f280bc49` (`main`, read-only)  
**FIRSTGAME source baseline inspected:** `796618243c3ca76f70d582f38475320c6461420b` (read-only)

## Objective

Reconcile IF-ADR-011 against the current package implementation and current QA
evidence, separate ADR-011 Loading-progress obligations from historical Player
late-join audit work, and reduce the remaining Stage A work to the smallest
behavioral evidence that actually belongs to this ADR.

The reconciliation does not introduce a new Loading architecture, Player contract,
progress formula or authoring surface. The current participant-aware progress
envelope is the canonical package implementation.

## Source disposition

```text
Architecture
  ACCEPTED / RECONCILED

Package runtime
  IMPLEMENTED for the accepted ADR-011 boundary

Product surface
  IMPLEMENTED through existing Activity readiness policy + FadeWithLoading
  and the existing progress-capable Loading presentation surface

Technical QA
  CERTIFIED

Package divergence
  NONE reproduced during reconciliation or closure execution

Stage A
  CLOSED — 100%
  technical remaining: 0%

FIRSTGAME / Stage B
  PARTIAL / SEPARATE
```

## Scope

This reconciliation covers:

- monotonic participant-aware Loading progress;
- technical range separated from the reserved readiness range;
- successful terminal `100%` only after aggregate `Ready`;
- Required-only denominator semantics;
- Optional non-blocking diagnostic semantics;
- stale/foreign occurrence rejection;
- Required failure/release terminal behavior;
- cancellation and supersession without false successful `100%`;
- `WaitCovered + FadeWithLoading` direct Activity entry;
- Route Startup Activity parity;
- Game Application Startup Activity parity;
- terminal ordering: `100% -> Hide -> reveal -> gate release` when applicable;
- Loading/readiness recovery versus pure Transition Gate separation;
- typed diagnostics for participant-aware progress.

## Out of scope

This reconciliation does not require or introduce:

- Player provisioning redesign;
- a new Player Join command or consumer API;
- a dedicated ADR-011 Player lifecycle contract;
- participant weights;
- continuous percentage progress inside one participant;
- timer-based readiness progress;
- timeout/retry authoring;
- fake progress to unblock a covered wait;
- a new Loading surface or Loading manager;
- a new generic Gate domain;
- FIRSTGAME implementation.

## Normative contract confirmed

The current ADR is intentionally generic over readiness participants. It does not
name Player Join or Manager-Provisioned lifecycle as a special completion gate.
Its contract is:

```text
technical work
  -> technical progress remains below successful terminal completion
  -> readiness range is reserved when applicable
  -> occurrence-scoped Required completion advances readiness progress
  -> Optional remains diagnostic/non-blocking
  -> aggregate Ready
  -> successful 100%
  -> Loading Hide
  -> reveal
```

A Required contribution may remain `Preparing` indefinitely while the represented
condition remains pending. The framework must not manufacture completion through a
timeout, fake percentage or premature Hide/reveal.

## Package implementation reconciliation

The current package materializes the accepted contract through the existing scoped
runtime owners.

### GameFlow orchestration

`Runtime/GameFlow/GameFlowRuntime.ActivityEntryLoadingProgress.cs` owns the
operation-level integration for:

```text
Game Application startup with Startup Activity
Route request with Startup Activity
direct Activity request
```

The integration only projects participant-aware readiness progress when the target
entry uses `WaitCovered` and a real Loading progress reporter is available.
Non-applicable operations retain the normal technical Loading path.

### Progress envelope

The canonical Loading contracts remain under `Runtime/Loading/`:

```text
ActivityEntryLoadingProgressPlan
ActivityEntryLoadingProgressEnvelope
ActivityEntryLoadingProgressDiagnostics
FrameworkLoadingProgress
```

The envelope owns one operation-scoped monotonic projection. Technical lifecycle
work receives the mapped technical reporter; readiness snapshots advance only the
reserved readiness range. Successful terminal completion is idempotent and is not
published for terminal failure.

### Authority separation

```text
ActivityFlow / readiness occurrence
  readiness authority

GameFlow
  operation ordering and progress integration

FrameworkRuntimeHost / Loading adapter
  presentation adaptation and retained diagnostics
```

Loading does not discover participants and does not become readiness authority.

## Current direct positive evidence

The focused positive regression has already been executed in the current closure
work:

```text
[QA_READY_PROGRESS_01]
status='Passed'
cases='32'
required='4'
optional='1'
optionalOutcome='FailedNonBlocking'
ordering='Technical<100,0/4,1/4,2/4,3/4,4/4=100,Hide,Reveal,GateRelease'
```

This proves the direct Activity path with four Required participants and one
Optional participant, including:

- technical completion below successful `100%`;
- Required-only denominator progression;
- Optional failure without denominator/progress mutation;
- monotonic progress;
- `4/4 + Ready = 100%`;
- terminal progress before Loading Hide;
- Hide before reveal;
- gate release after readiness;
- authority/presentation cleanup.

## Current terminal evidence

The focused terminal regression has also been executed in the current closure work:

```text
[QA_READY_PROGRESS_02A]
status='Passed'
cases='34'
```

Its covered terminal contract includes:

```text
RequiredFailed
RequiredReleased
ReplacementRejected
LateOldOccurrenceRejected
DuplicateTerminal
OwnedCancellation
```

It proves that terminal non-success does not fabricate successful `100%`, that stale
occurrences cannot advance the replacement operation, and that committed-destination
failure retains the semantic recovery boundary without conflating it with the pure
Transition Gate.

## Startup parity evidence — certified

The current QAFramework already contains the canonical startup-parity regression:

```text
QaParticipantAwareStartupParityRegression
```

No new QA runner is justified.

The two canonical startup-parity modes were executed in separate fresh Play Mode sessions and both passed without package changes:

### ADR011-QA-01 — Route Startup Activity — PASS

Prepare in Edit Mode:

```text
Immersive Framework/QA/Setup/Activity Entry Readiness/
Prepare Route Startup Progress Parity
```

Enter a fresh Play Mode and run:

```text
Immersive Framework/QA/Regressions/Game Flow/
Run Participant-Aware Startup Loading Parity Regression
```

Observed terminal evidence:

```text
[QA_READY_PROGRESS_02B_ROUTE]
status='Passed'
cases='25'
path='RouteStartupActivity'
required='4'
optional='1'
optionalOutcome='FailedNonBlocking'
terminal='100BeforeHide'
```

### ADR011-QA-02 — Game Application Startup Activity — PASS

Run in a separate fresh session.

Prepare in Edit Mode:

```text
Immersive Framework/QA/Setup/Activity Entry Readiness/
Prepare Game Application Startup Progress Parity
```

Enter Play Mode and run the same regression menu.

Observed terminal evidence:

```text
[QA_READY_PROGRESS_02B_GAME_APPLICATION]
status='Passed'
cases='20'
path='GameApplicationStartupActivity'
required='4'
optional='1'
optionalOutcome='FailedNonBlocking'
terminal='100BeforeHide'
```

The observed runs prove participant-aware typed diagnostics, monotonic progress, Required 4/4 Ready, Optional failure non-blocking, terminal progress before Hide, presentation cleanup, gate release and authority restoration for both startup paths. Both setup sessions also completed automatic post-Play restoration to the canonical QA Hub and removed the generated fixture.

## Historical public waiting/joining tracking gap

The previous tracker wording:

```text
Focused public waiting/joining evidence remains
```

came from a broader readiness/Player audit that mixed ADR-011 Loading-progress
coverage with Manager-Provisioned Player late-join/reconciliation questions.

That wording is not an accurate ADR-011 completion gate for two reasons:

1. IF-ADR-011 contains no Player-specific Join or provisioning requirement; its
   denominator and terminal rules are generic over occurrence-scoped Required and
   Optional participants.
2. Newer Player QA independently provides public Manager-Provisioned waiting
   evidence. The certified Player baseline records the Manager-Provisioned Waiting
   Projection as `PASS — 14 cases`.

Player late join, Actor preparation/materialization and consumer command usability
remain owned by the Player participation/provisioning ADRs and their QA. They are
supporting cross-domain evidence only and must not be used to invent additional
ADR-011 runtime scope.

## Product-surface assessment

ADR-011 refines the behavior of an already authorable composition rather than
introducing a standalone feature asset.

The user-facing composition remains:

```text
ActivityAsset
  Entry Readiness Policy = WaitCovered
  Visual Transition Mode = FadeWithLoading

Loading presentation
  determinate/progress-capable surface when configured
```

No separate ADR-011 Recipe or Composer is justified because the intent already
belongs to Activity authoring and the Loading presentation surface. Advanced/debug
inspection is provided by typed Loading/readiness diagnostics rather than a second
authority.

## Stage A closure evidence

The complete accepted Stage A matrix is now green:

```text
Direct positive progression       PASS — 32/32
Terminal/failure semantics         PASS — 34/34
Route Startup parity               PASS — 25/25
Game Application Startup parity    PASS — 20/20
```

Final disposition:

```text
Architecture: ACCEPTED / RECONCILED
Package: IMPLEMENTED
Technical QA: CERTIFIED
Package divergence: NONE
Stage A: CLOSED — 100%
Technical remaining: 0%
Stage B: FIRSTGAME consumer/product proof only
```

No package, Editor or QA implementation change was required for ADR-011 closure. Reopen Stage A only on a reproduced accepted-contract regression, a documented contract change or newly accepted scope. Do not weaken the progress contract or invent fallback behavior merely to reach terminal Loading completion.

## Files affected by this reconciliation cut

```text
EDIT
Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-ADR-011-RECONCILIATION-2026-08-11.md

EDIT
Documentation~/Architecture/Tracking/
  IF-TRACK-Framework.md
```

No runtime, Editor, QAFramework, FIRSTGAME or ProjectSettings files are changed by
this documentation reconciliation.

## Architectural gain

- restores the ADR boundary to participant-aware Loading progress rather than Player
  provisioning details;
- preserves one operation-scoped progress authority;
- preserves readiness as occurrence-scoped runtime authority;
- keeps startup parity as the only remaining technical evidence instead of creating
  a parallel test architecture.

## Usability gain

- `100%` keeps one product meaning: the covered target Activity is actually Ready;
- the user does not need to understand internal participant counts or manually
  construct progress ranges;
- no fake timeout or presentation workaround is introduced for legitimate pending
  readiness.

## Suggested commit

```text
docs(architecture): certify ADR-011 stage A loading progress
```
