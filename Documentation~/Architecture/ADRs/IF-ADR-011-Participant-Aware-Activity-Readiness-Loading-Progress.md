# IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress

Status: Accepted  
Last updated: 2026-08-03  
Supersedes: implicit loading-completion semantics limited to technical Activity lifecycle progress  
Superseded by: none
Related decisions: IF-ADR-006, IF-ADR-007

## Context

Activity entry readiness and Loading presentation are related but distinct concerns.

An Activity entry may declare:

```text
ObserveOnly
WaitVisible
WaitCovered
```

`WaitCovered` retains visual cover and the input, interaction and gameplay gate until the captured Activity readiness occurrence reaches a terminal result.

When the Activity uses `FadeWithLoading`, the application may expose a determinate Loading surface. The current runtime reports technical progress for known scene and content operations, including release, unload, load and materialization. The readiness wait occurs after the target Activity lifecycle has materialized the occurrence.

That permits an invalid product state:

```text
Loading = 100%
Activity readiness = Preparing
Required participants remain pending
Loading and visual cover remain visible
```

The presentation ordering remains correct, but `100%` no longer means that the covered Activity is ready to reveal.

The framework already captures a fixed participant set for each readiness occurrence. Each captured participant has explicit identity, requiredness, state and reason. Participants not captured by the occurrence are not added later.

## Decision

For a `WaitCovered` Activity entry with a progress-capable Loading surface, the framework projects Activity readiness into the Loading progress envelope.

```text
Loading progress envelope
  -> technical progress phase
  -> Activity readiness phase
     -> equal subdivisions for captured Required participants
  -> terminal 100% only when aggregate Activity readiness is Ready
```

The Loading surface remains presentation. It does not own readiness, participants, Activity authority or completion.

Activity-owned scripts and readiness participants communicate only through Activity readiness contracts. They do not resolve or update the persistent Loading surface directly.

The framework runtime owns the mapping from occurrence-scoped readiness updates to Loading progress.

## Applicability

Participant-aware readiness progress applies only when all of the following are true:

```text
target Activity entry policy = WaitCovered
the operation owns a target Activity entry envelope
the operation uses a Loading surface
the Loading surface supports determinate progress
the target Activity readiness occurrence is valid
```

Initial operation coverage:

```text
direct Activity request
Route request with a Startup Activity
Game Application startup with a Startup Activity
```

A restart or future operation uses the same semantics when it re-enters an Activity through the same covered entry envelope.

### ObserveOnly

`ObserveOnly` does not project readiness into Loading. Technical Loading progress completes normally and later readiness remains observational.

### WaitVisible

`WaitVisible` does not retain Loading for readiness progress. Technical Loading may complete and hide before `Ready`; the target is visible while the capability gate remains retained.

### WaitCovered with Fade

`WaitCovered` with `Fade` retains visual cover but has no determinate Loading percentage to project.

### WaitCovered with FadeWithLoading

`WaitCovered` with `FadeWithLoading` uses participant-aware readiness progress and cannot publish successful `100%` before aggregate readiness is `Ready`.

## Frozen participant set

The participant set is frozen per Activity readiness occurrence.

The denominator is derived from the captured occurrence after target materialization:

```text
requiredTotal = RequiredCount
```

The framework must not:

```text
scan the scene every frame
add late-discovered participants to the active denominator
remove participants from the denominator after progress begins
infer participants from hierarchy, object names or gameplay objects
recalculate the global Loading denominator from a changing participant set
```

A participant update affects the occurrence only when the participant belongs to the captured set and the update matches the same Activity and transition sequence.

Reentry creates a new occurrence and a new frozen participant set.

## Required and Optional semantics

Only `Required` participants contribute to the readiness progress denominator.

```text
Required
  -> participates in determinate readiness progress
  -> blocks aggregate Ready while pending
  -> prevents successful 100% when failed or released before completion

Optional
  -> remains visible in diagnostics
  -> does not contribute to the determinate denominator
  -> does not block aggregate Ready
  -> does not prevent successful 100% when pending or failed
```

The internal readiness contracts must expose enough evidence to distinguish:

```text
RequiredCount
RequiredPendingCount
RequiredCompletedCount
RequiredFailedCount
RequiredReleasedCount or equivalent terminal-release evidence

OptionalCount
OptionalPendingCount
OptionalCompletedCount
OptionalFailedCount
```

Aggregate counts that mix Required and Optional completion are insufficient for Loading projection.

## Equal participant weighting

Every captured `Required` participant has equal weight inside the readiness phase.

The initial contract has:

```text
no authorable participant weights
no priority-based progress weight
no duration prediction
no object-count inference
no progress contribution from Optional participants
```

For `requiredTotal > 0`:

```text
readinessRatio =
    requiredCompletedCount / requiredTotal
```

For `requiredTotal == 0`, the readiness ratio is complete only when aggregate readiness is `Ready`.

Participant completion alone is not sufficient for terminal `100%`; technical blocking issues remain authoritative.

## Stable global progress envelope

The participant count is not known before the target scene and Activity content are materialized. The global Loading denominator must not change after determinate progress begins.

Activity readiness is therefore reserved as one phase before technical progress starts.

```text
technical phase units = known technical operation step count
readiness phase units = 1 when participant-aware readiness applies
total phase units = technical phase units + readiness phase units
```

The technical phase occupies its weighted range. The readiness phase occupies the final range and is subdivided equally by the captured Required participants after the occurrence exists.

Example:

```text
2 technical phase units
1 readiness phase unit
4 Required participants
```

```text
0%       operation begins
66.67%   technical work complete; 0/4 Required complete
75.00%   1/4 Required complete
83.33%   2/4 Required complete
91.67%   3/4 Required complete
100.00%  4/4 Required complete and aggregate readiness is Ready
```

Participant count changes subdivisions inside the reserved readiness range. It does not rewrite the already published technical range.

If there are no known technical steps but covered readiness applies, the readiness phase owns the complete determinate range.

## Monotonicity

Loading progress for one operation and occurrence is monotonic.

The framework must not publish a value lower than the last accepted determinate value.

A Required participant completed for progress purposes cannot return to pending within the same valid occurrence. An incompatible regression, occurrence replacement or mismatched update is rejected, invalidated or diagnosed; it is not repaired by decreasing Loading progress.

No polling, frame counting or time-based estimation is used. Progress updates are driven by typed readiness occurrence changes.

## Terminal completion

Successful `100%` requires all of the following:

```text
technical phase completed successfully
captured occurrence still matches the authoritative target Activity
all Required participants completed
no Required participant failed
no Required participant was released before valid completion
aggregate ActivityReadinessState.IsReady = true
waiting operation terminal result = Ready
```

The final determinate Loading update is published before Loading hide and before visual reveal.

Canonical successful ordering:

```text
technical progress completes below 100%
Required participant progress advances within readiness range
aggregate readiness becomes Ready
Loading publishes 100%
Loading hides
Transition cover releases
target Activity is revealed
capability gate releases
request returns success
```

## Failure, invalidation and cancellation

The framework does not publish successful `100%` when the readiness wait ends as:

```text
Failed
Invalidated
Cancelled
```

A Required participant failure or premature release is terminal blocking evidence.

```text
destination authority follows the committed-destination contract
Loading retains the last valid progress snapshot
request returns the typed committed-readiness failure result
recovery gate behavior remains governed by existing readiness orchestration
```

Optional failure does not fail aggregate readiness unless another explicit technical contract makes it blocking.

No silent fallback converts failure into `Ready`.

## Loading presentation contract

A progress-capable Loading adapter receives normalized snapshots from the framework-owned reporter.

The readiness phase exposes diagnostic metadata equivalent to:

```text
phase = ActivityReadiness
activity identity
occurrence transition sequence
required total
required pending
required completed
required failed
optional total
optional pending
last normalized progress
```

Exact UI wording is presentation-specific. The Loading surface may display progress but never becomes the source of readiness truth.

## Consumer authoring boundary

A consumer completes readiness participants through the normal readiness API.

```text
Chicken 01 reaches its destination
  -> Chicken 01 Required participant completes

Chicken 02 reaches its destination
  -> Chicken 02 Required participant completes
```

The framework counts completed Required participants. It does not count chickens, enemies, assets, scene objects or arbitrary gameplay concepts.

A consumer may use one aggregate Required participant for a compound condition. In that case, readiness progress advances once when that participant completes.

A consumer that needs multiple readiness increments authors multiple independent Required participants.

Activity-owned scripts must not:

```text
resolve the persistent Loading adapter
write directly to the Loading progress bar
find a FrameworkRuntimeHost
use global lookup
parse logs
duplicate the framework progress calculation
```

## FIRSTGAME reference proof

Demo 01 Activity Readiness should prove the contract using one reusable scenario and three Activities:

```text
Observe Only
Wait Visible
Wait Covered
```

The `WaitCovered` configuration is:

```text
Policy = WaitCovered
Presentation = FadeWithLoading
Transition Gate = InputInteractionAndGameplay
```

The reference scenario may use:

```text
4 independent Required readiness participants
1 Optional readiness participant
```

Each Required participant may be completed by one chicken reaching its target. Loading progresses because participants complete, not because the framework knows about chickens.

The Optional participant remains pending to prove that it does not enter the denominator and does not block `Ready` or successful `100%`.

FIRSTGAME is consumer proof. Permanent progress mapping belongs to `com.immersive.framework`.

## QA proof

QA must provide deterministic typed evidence for at least:

```text
participant set frozen for one occurrence
4 Required participants captured
1 Optional participant captured

technical progress completes below 100%
0/4 Required -> readiness range start
1/4 Required -> first monotonic increment
2/4 Required -> second monotonic increment
3/4 Required -> third monotonic increment
4/4 Required + aggregate Ready -> 100%

Optional pending -> no denominator change
Optional failed -> no denominator change and no Ready blockage
Required failed -> no 100%
Required released -> no 100%
occurrence invalidated -> no 100%
wait cancelled -> no 100%

Loading 100% before Hide
Hide before reveal completion
request success only after Ready
```

QA uses typed progress and readiness evidence. It does not use delays, timeouts, frame polling, log parsing or global object lookup.

## Diagnostics

Operation diagnostics distinguish:

```text
technical phase range
readiness phase range
required total
required completed
required pending
required failed
optional total
optional pending
readiness ratio
last published normalized progress
terminal readiness status
100% published
Loading hide completed
reveal completed
```

## Rejected alternatives

- Treating readiness as an unobservable delay after technical Loading reaches `100%`.
- Publishing `100%` while the Activity remains `Preparing`.
- Treating all readiness as one binary step when multiple Required participants exist.
- Counting Optional participants in the successful denominator.
- Counting gameplay objects directly.
- Dynamically changing the global Loading denominator after progress begins.
- Repeated scene or hierarchy scans for participants.
- Activity content controlling the persistent Loading surface directly.
- Time-based simulated readiness progress.
- Polling participant state.
- Silent fallback to technical-only progress for `WaitCovered`.
- Authorable participant weights in the initial implementation.
- A second Loading authority dedicated to readiness.

## Consequences

Positive:

```text
100% means the covered target Activity is ready to reveal
multiple Required participants produce meaningful determinate progress
Optional participants remain diagnostic without distorting completion
the global denominator remains stable
persistent Loading stays decoupled from Activity-owned gameplay scripts
the same contract applies to direct Activity and Startup Activity entry
FIRSTGAME can teach the feature through a reusable scenario
```

Tradeoffs:

```text
readiness state gains Required/Optional completed counts
Loading reserves a readiness phase
Game Flow forwards typed occurrence updates to the reporter
QA needs ordering and monotonicity evidence
progress is participant-granular, not continuous within one participant
```

Continuous progress inside one participant requires a separate future readiness-progress-source contract and is outside this ADR.

## Current implementation coverage

Implemented before this ADR:

```text
ObserveOnly, WaitVisible and WaitCovered policies
occurrence-scoped readiness wait
captured participant set per occurrence
Required and Optional readiness aggregation
WaitCovered retention of Loading, transition cover and capability gate
typed Ready, Failed, Invalidated and Cancelled results
technical Loading progress reporting
```

Not implemented when this ADR is accepted:

```text
RequiredCompletedCount and OptionalCompletedCount projection
reserved readiness range in the Loading envelope
participant-driven Loading progress updates
terminal 100% gating on aggregate Ready
QA regression for participant-aware readiness Loading progress
FIRSTGAME WaitCovered FadeWithLoading proof
```

This ADR accepts the architecture. It does not claim implementation completion.

## Required implementation order

```text
1. Update official package contracts and orchestration.
2. Validate participant-aware progress in QAFramework.
3. Configure and validate FIRSTGAME Demo 01 WaitCovered.
4. Update usage documentation and samples after the runtime contract passes.
```

## Pending decisions

The following remain deferred and do not block this ADR:

- Optional future custom weights for Required participants.
- Optional continuous progress source within one participant.
- Final designer-facing wording and visuals for the official Loading prefab.
- Whether detailed participant progress is exposed through a public diagnostic DTO or remains internal/experimental initially.
