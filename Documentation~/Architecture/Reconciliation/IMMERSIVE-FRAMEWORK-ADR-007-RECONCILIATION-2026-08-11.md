# Immersive Framework — ADR-007 Reconciliation and Stage A Certification

**Date:** 2026-08-11  
**Type:** technical, product-surface and QA reconciliation / Stage A certification  
**ADR:** IF-ADR-007 — Activity Entry Readiness and Reveal Gating  
**Package source baseline inspected:** `c0003445a95baecf54ada1d76a718d1118617c29` (`master`, read-only)  
**QA source baseline inspected:** `ba1529e653b25c1215d4f741cd3611b7f280bc49` (`main`, read-only)

## Objective

Reconcile IF-ADR-007 against the current package and QA implementation, correct the
one stale designer-facing statement found during reconciliation, execute the focused
behavioral evidence, and certify the accepted Stage A boundary without introducing a
new readiness architecture.

The closure confirms that the current occurrence-scoped readiness model is the
canonical implementation. No readiness manager, provider authority, elapsed-time
timeout, alternate gate system or FIRSTGAME workaround is required for Stage A.

## Closure baselines

```text
com.immersive.framework
  inspected source: c0003445a95baecf54ada1d76a718d1118617c29
  closure delta: ADR007-Editor-Readiness-UX-Fix
  access: Git read-only; change delivered as ZIP

QAFramework
  inspected source: ba1529e653b25c1215d4f741cd3611b7f280bc49
  closure delta: ADR007-QA-Fixture-Isolation-Fix
  access: Git read-only; change delivered as ZIP
```

The tracking file in this certification remains layered over the preceding ADR-006
Stage A certification state. ADR-006 therefore remains closed and is not regressed by
this ADR-007 update.

FIRSTGAME remains Stage B consumer/product evidence and is not used as the synthetic
technical test environment for ADR-007.

## Scope

This certification covers:

- `ObserveOnly`, `WaitVisible` and `WaitCovered` policy semantics;
- occurrence-scoped readiness authority;
- readiness terminal failure, invalidation, cancellation and supersession;
- presentation reveal ordering;
- Input / Interaction / Gameplay capability gating;
- stale/foreign occurrence isolation;
- required versus optional participant behavior;
- Loading/Transition integration and recovery boundary;
- Activity Inspector guidance for the three policies;
- focused QA fixture isolation and behavioral execution;
- terminal restoration/cleanup of the QA authority and presentation surfaces.

## Out of scope

This certification does not introduce or require:

- a global readiness manager or service locator;
- a singular readiness-provider authority;
- a hidden elapsed-time timeout;
- timeout/retry authoring;
- automatic rollback for committed Activity authority;
- fake Loading work used to hold presentation;
- a new generic Gate domain;
- Player participation redesign;
- ADR-011 participant-aware progress redesign;
- FIRSTGAME implementation.

## Final disposition

```text
Architecture
  ACCEPTED / RECONCILED

Package runtime
  IMPLEMENTED for the current accepted ADR-007 boundary

Package product surface
  IMPLEMENTED for the certified boundary
  stale Activity Inspector warning removed

Technical QA
  CERTIFIED
  Foundation: 18/18 across 2 passes
  Direct Policies: 42/42
  WaitVisible: PASS
  WaitCovered: PASS
  shared Progress: 32/32
  shared Terminal: 34/34

Package divergence
  NONE reproduced

Stage A
  CLOSED — 100%
  technical remaining: 0%

FIRSTGAME / Stage B
  PARTIAL and tracked separately
```

## Normative model confirmed

The accepted ADR and runtime converge on three policies:

```text
ObserveOnly
WaitVisible
WaitCovered
```

The current package enum is:

```text
ObserveOnly = 0
WaitCovered = 10
WaitVisible = 20
```

There is no separate `None` policy in the current model. `ObserveOnly` is the
default compatibility path that observes readiness without turning normal Activity
entry into a readiness wait.

### ObserveOnly

```text
materialize / commit Activity authority
-> normal Transition and operation capability release
-> readiness may continue to be observed afterward
```

Readiness is diagnostic/observable but does not become an accidental entry gate.

### WaitCovered

```text
materialize / commit Activity authority
-> retain visual cover
-> retain unsafe capability blocking
-> wait for the captured readiness occurrence
-> Ready
-> complete terminal Loading presentation as applicable
-> reveal
-> release readiness-owned capability protection
```

Required readiness may remain `Preparing` while its represented condition has not
occurred. The framework does not invent readiness merely to make the flow finish.

### WaitVisible

```text
materialize / commit Activity authority
-> reveal after materialization
-> retain unsafe capability blocking
-> wait for the captured readiness occurrence
-> Ready
-> release readiness-owned capability protection
```

Visible preparation is intentional; capability release still belongs to readiness.

## Readiness authority and composition

The current runtime is occurrence-scoped rather than provider-scoped.

Canonical shape:

```text
Activity occurrence
  -> technical readiness baseline
  -> current Player readiness contribution when applicable
  -> current authorable participant contributions
  -> one recomposed occurrence snapshot
```

The active occurrence owns the readiness state being waited on. A stale or foreign
occurrence cannot complete the current operation.

Zero applicable authorable participants is not a missing-provider error. It is an
explicit satisfied contribution in the current composition model; other applicable
technical or Player contributions still participate normally.

## Terminal and cancellation semantics

Current readiness waiting is event-driven and occurrence-scoped. The canonical
wait result distinguishes:

```text
Ready
Failed
Cancelled
Invalidated
```

GameFlow additionally preserves causal supersession when a newer authoritative
operation replaces the one that was waiting.

The waiter completes once, releases subscriptions/registrations and does not poll
for readiness.

### No hidden timeout contract

The accepted current scope does **not** impose a wall-clock timeout.

```text
Preparing
  -> remains pending while the represented condition remains pending

Ready
  -> successful terminal readiness

Required failure
  -> explicit terminal failure

owning operation cancelled / authority replaced / occurrence invalidated
  -> explicit causal terminal result
```

Timeout/retry authoring remains a separate future product decision, not missing
Stage A implementation.

## Loading, Transition and recovery boundary

ADR-007 reuses the authority split certified during ADR-006 closure:

```text
Loading
  reports technical/readiness-governed presentation progress

Transition
  owns presentation orchestration

Activity readiness occurrence
  owns readiness state

GameFlow
  coordinates the operation and authoritative destination

Transition Gate
  pure transition-operation protection

Activity Entry Readiness Recovery Gate
  retained readiness/reveal protection after applicable committed failures
```

A committed target can remain authoritative after readiness failure while
presentation/recovery protection is retained. Pure Transition Gate cleanup and
readiness recovery are separate states.

## Product-surface correction completed

During reconciliation, `Editor/Authoring/ActivityAssetEditor.cs` contained a stale
unconditional warning stating:

```text
Runtime entry waiting is not active yet. This field currently records and validates intent only.
```

That statement contradicted the already-operational `WaitCovered` and `WaitVisible`
runtime policies.

The package Editor cut removed the stale warning and changed the two waiting-policy
descriptions from future-tense wording (`The intended entry flow`) to current
behavior wording (`The entry flow`). No runtime logic, validation rule, `.meta`,
Recipe, Composer or authoring architecture was changed.

The existing policy-specific HelpBoxes remain the designer-facing explanation of
`ObserveOnly`, `WaitCovered` and `WaitVisible`.

## Contract-to-source reconciliation

| ADR-007 contract | Current package owner/evidence | Final disposition |
|---|---|---|
| Three explicit entry policies | `Runtime/ActivityFlow/ActivityEntryReadinessPolicy.cs` | Aligned |
| Activity-authored policy | `Runtime/Authoring/ActivityAsset.cs` | Aligned |
| Occurrence-scoped wait | `Runtime/ActivityFlow/ActivityEntryReadinessWaiter.cs`, `ActivityFlowRuntime.EntryReadinessWait.cs` | Aligned |
| Required failure / cancellation / invalidation explicit | `ActivityEntryReadinessWaitResult.cs`, `ActivityEntryReadinessWaitStatus.cs` | Aligned |
| Supersession causal, not generic failure | `Runtime/GameFlow/GameFlowRuntime.ActivityEntryReadinessOrchestration.cs`, scoped wait operation | Aligned |
| WaitCovered cover + capability retention | GameFlow readiness orchestration + transition/loading/gate owners | Aligned and behaviorally proven |
| WaitVisible visible preparation + capability retention | GameFlow readiness orchestration + gate owners | Aligned and behaviorally proven |
| ObserveOnly does not accidentally wait | `ActivityAsset.WaitsForEntryReadiness`, GameFlow readiness orchestration | Aligned and foundation-proven |
| Required/optional participant composition | `ActivityReadinessOccurrenceState.cs`, `ActivityReadinessRecomposer.cs` | Aligned; shared Progress/Terminal evidence retained |
| No fabricated timeout | event-driven waiter + accepted ADR scope | Aligned |
| Pure Transition Gate distinct from readiness recovery | GameFlow recovery policies/diagnostics | Aligned; ADR-006 evidence retained |
| Designer-facing Inspector guidance reflects runtime | `Editor/Authoring/ActivityAssetEditor.cs` | **CORRECTED / ALIGNED** |

## Focused QA certification

ADR-007 did not require a new monolithic smoke. The existing canonical regressions
proved the contract at the relevant authority boundaries.

### Activity Entry Readiness Foundation Regression

Final accepted evidence:

```text
[IF_M07_01_QA]
status='Passed'
passes='2/2'
required='18'
executed='18'
completed='18'
failed='0'
```

The 18 assertions across two passes cover the focused foundation cases, including:

```text
readiness snapshot / monotonicity
ObserveOnly compatibility
wait-eligible authoring
immediate terminal waiver
current occurrence -> Ready
future occurrence -> Ready
manual required failure terminal
wrong occurrence isolation
cancellation terminal
```

This is the certification evidence for ObserveOnly compatibility, occurrence
identity, terminal failure/cancellation semantics and repeatability.

### Direct Activity Readiness Policies Regression

The first closure attempt exposed a **QA fixture collision**, not a package defect.
`QA_IF_READY_04_DirectPoliciesContent.unity` is intentionally validated by the
Direct Policies regression as neutral Activity content, but the Player Surface QA
setup had begun reusing that same scene and materializing
`QA_PlayerSurface_ActivityConsumer` into it.

The QAFramework closure cut corrected fixture ownership instead of weakening the
Direct Policies assertion:

```text
QA_IF_READY_04_DirectPoliciesContent.unity
  -> restored as neutral IF_READY_04-owned content

QA_PlayerSurfacePublicActivityContent.unity
  -> dedicated Player Surface content fixture
```

After fixture isolation, the unchanged Direct Policies behavioral regression
completed successfully:

```text
[IF_READY_04_QA_DIRECT_POLICIES]
status='Passed'
cases='42'
waitVisible='Passed'
waitCovered='Passed'
presentationSource='HostOwned'
presentationResolution='HostRuntimeScene'
```

Key certified ordering:

```text
WaitVisible
  participant Preparing
  -> reveal observed before Ready
  -> Activity request remains pending
  -> readiness/capability gate remains retained
  -> readiness completes through public API
  -> request succeeds
  -> gate releases

WaitCovered
  participant Preparing
  -> transition/loading cover remains visible
  -> request remains pending
  -> gate remains retained while covered
  -> readiness completes through public API
  -> request succeeds
  -> presentation releases after Ready
  -> gate releases
```

Terminal cleanup additionally confirmed fixture cleanup, restoration of the initial
QA authority, destruction of the temporary presentation observer, hidden terminal
presentation surfaces and evidence cleanup.

### Shared current cross-ADR evidence retained

The immediately preceding ADR-006 closure already executed the participant-aware
Loading/readiness regressions against the same runtime boundary. They remain valid
supporting evidence and were not rerun redundantly for ADR-007 labeling.

```text
Participant-Aware Readiness Loading Progress
  PASS — 32/32
  Technical<100 while readiness waits
  optional pending/failure excluded from required denominator
  required 4/4 -> terminal 100 -> Hide -> Reveal -> GateRelease

Participant-Aware Readiness Loading Terminal
  PASS — 34/34
  required failure explicit
  committed destination remains authoritative
  Loading/Transition presentation retained
  pure Transition Gate released
  readiness recovery gate retained then cleaned
  replacement/late-old occurrence rejected
  owned cancellation and duplicate terminal handling proven
```

## Package divergence classification

No ADR-007 package runtime divergence was reproduced.

The only two closure defects were classified and routed to their owners:

```text
Package Editor UX
  stale factual warning
  -> corrected in ActivityAssetEditor

QAFramework fixture ownership
  Player Surface setup polluted IF_READY_04 neutral scene
  -> corrected by dedicated Player Surface content scene
```

Neither defect justified a runtime readiness change or weaker QA assertion.

## Stage A closure

The closure rule is now fully satisfied:

```text
[x] normative ADR aligned with occurrence-scoped model
[x] package runtime aligned
[x] stale Activity Inspector warning removed
[x] Activity Entry Readiness Foundation PASS — 18/18 across 2 passes
[x] Direct Activity Readiness Policies PASS — 42/42
[x] WaitVisible PASS
[x] WaitCovered PASS
[x] current Progress 32/32 evidence retained
[x] current Terminal 34/34 evidence retained
[x] QA fixture collision isolated without weakening assertions
[x] no package runtime divergence reproduced
```

Final Stage A disposition:

```text
Architecture: ACCEPTED / RECONCILED
Package / Product Surface: IMPLEMENTED
Technical QA: CERTIFIED
Stage A: CLOSED — 100%
Technical remaining: 0%
Package divergence: NONE
```

Reopen Stage A only for a reproduced accepted-contract regression, a documented
contract change or a newly accepted scope.

## Stage B / FIRSTGAME boundary

FIRSTGAME remains responsible for real consumer/product proof:

```text
choose ObserveOnly / WaitVisible / WaitCovered in a real Activity
understand the policy from the Inspector
configure compatible Transition/Gate intent
enter a real Activity
observe understandable preparing / covered / visible behavior
use diagnostics to identify what is still blocking readiness
avoid manually reconstructing internal occurrence/gate contracts
```

A consumer finding that represents repeated framework authoring friction or a
missing package capability should be promoted back to `com.immersive.framework`.
Game-specific presentation remains in FIRSTGAME.

## Files in this Stage A certification documentation cut

```text
EDIT
Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-ADR-007-RECONCILIATION-2026-08-11.md

EDIT
Documentation~/Architecture/Tracking/
  IF-TRACK-Framework.md
```

The normative ADR is not changed by this final certification cut. Its accepted
contract is already aligned; mutable QA counts and closure state belong in
reconciliation/tracking.

No runtime, Editor, QA, FIRSTGAME or ProjectSettings file is included in this
documentation ZIP.

## Acceptance criteria — final

### Technical/documentation

- current three-policy model preserved;
- no obsolete `None` policy reintroduced;
- no elapsed-time timeout invented as a missing runtime requirement;
- runtime owners remain explicit and scoped;
- focused behavioral evidence is recorded accurately;
- QA fixture collision is classified as QA-owned, not package divergence;
- ADR-006 certification remains preserved in tracking;
- ADR-007 Stage A reports 100% with technical remaining 0%.

### Product

- Activity Inspector no longer claims runtime entry waiting is inactive;
- designer-facing policy guidance describes current runtime behavior;
- Stage B/FIRSTGAME remains consumer proof rather than a technical closure gate.

## Suggested commits

Package documentation:

```text
docs(architecture): certify ADR-007 stage A readiness boundary
```

QA fixture correction, if committed separately:

```text
qa: isolate ADR-007 readiness and player surface fixtures
```
