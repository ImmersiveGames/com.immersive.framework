# IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress

Status: Accepted and implemented for the initial scope  
Last updated: 2026-08-04  
Supersedes: implicit Loading completion semantics limited to technical Activity lifecycle progress  
Superseded by: none  
Related decisions: IF-ADR-006, IF-ADR-007 — Activity Entry Readiness and Reveal Gating

## Context

A `WaitCovered` Activity retains visual cover and the input, interaction and
gameplay gate until the captured readiness occurrence reaches a terminal result.
Before this decision, technical Loading could reach `100%` while Required
readiness participants were still `Preparing`:

```text
Loading = 100%
Activity readiness = Preparing
Required participants remain pending
Loading and visual cover remain visible
```

The operation ordering was safe, but the percentage no longer represented the
covered target's actual release readiness.

## Decision

For a `WaitCovered` Activity entry with `FadeWithLoading` and a
progress-capable Loading surface, the framework projects the captured Activity
readiness occurrence into one operation-scoped Loading progress envelope.

```text
Loading Show
→ technical progress phase
→ explicit technical boundary below 100%
→ Activity readiness phase
   → equal subdivisions for captured Required participants
→ aggregate Ready
→ Loading 100%
→ Loading Hide
→ transition after / reveal
→ capability gate release
→ request success
```

The Loading surface remains presentation. It does not discover participants,
calculate readiness or become lifecycle authority. Activity-owned scripts never
resolve or update the persistent Loading surface directly.

## Applicability

Participant-aware determinate readiness progress applies only when all are true:

```text
target Activity policy = WaitCovered
visual transition = FadeWithLoading
the operation owns a target Activity entry envelope
an explicit Loading surface is present
the Loading adapter supports determinate progress
the target readiness occurrence is valid
```

Initial operation coverage:

```text
direct Activity request
Route request with Startup Activity
Game Application startup with Startup Activity
reentry through the same canonical request paths
```

Not applicable:

```text
ObserveOnly
WaitVisible
WaitCovered + Fade
Loading adapters without determinate progress
post-release operational readiness changes
```

## Frozen occurrence and denominator

The participant set is captured once per `ActivityReadinessOccurrence`.
The denominator is:

```text
requiredTotal = RequiredCount
```

The framework does not:

```text
scan scenes every frame
add late-discovered participants to the active denominator
remove captured participants after progress begins
infer readiness from hierarchy, names or gameplay objects
change the global denominator after determinate progress begins
```

Only updates matching the captured Activity and transition sequence can advance
the envelope. Reentry creates a new occurrence and a new denominator.

## Required and Optional semantics

Only Required participants contribute to progress and aggregate release:

```text
Required
  contributes equal weight
  blocks Ready while pending
  prevents successful 100% when failed or released before completion

Optional
  remains diagnostic
  never enters the denominator
  never blocks Ready
  never prevents successful 100% by remaining pending or failing
```

The immutable readiness projection exposes:

```text
RequiredCount
RequiredPendingCount
RequiredCompletedCount
RequiredFailedCount
RequiredReleasedCount

OptionalCount
OptionalPendingCount
OptionalCompletedCount
OptionalFailedCount
OptionalReleasedCount

ReadinessRatio
IsReady
HasTerminalFailure
Occurrence identity
```

## Equal participant weighting

For `requiredTotal > 0`:

```text
readinessRatio = requiredCompletedCount / requiredTotal
```

For `requiredTotal == 0`, the readiness range is terminal only when the
aggregate readiness state is `Ready`.

There are no authorable weights or duration estimates. One aggregate Required
participant yields one readiness increment. Several independent increments
require several independent Required participants.

## Stable progress plan

Before technical execution begins, the operation allocates:

```text
technical phase units = known technical Loading step count
readiness phase units = 1
all phase units = technical units + readiness unit
```

The participant count is intentionally absent from this initial plan because it
is known only after target materialization. The final readiness range is then
subdivided by the frozen Required count without changing the already published
technical range.

Example:

```text
2 technical units
1 readiness unit
4 Required participants
```

```text
0%       operation begins
66.67%   technical range complete; 0/4 Required complete
75.00%   1/4 Required complete
83.33%   2/4 Required complete
91.67%   3/4 Required complete
100.00%  4/4 Required complete and aggregate Ready
```

When no technical steps exist, the readiness phase owns the complete range.

## Technical boundary completion

A technical child reporter maps its normalized values into the reserved
technical range and cannot publish terminal `1.0` for the whole envelope.

The startup path explicitly invokes the envelope's technical-range completion
before readiness starts. This prevents the first participant update from
appearing to skip or overwrite an incomplete technical range.

Failure to reach the reserved technical boundary is explicit and throws an
operation error; it is not normalized silently.

## Monotonicity and update ordering

For one operation and occurrence:

```text
accepted determinate progress never decreases
duplicate snapshots are idempotent
stale occurrence updates are rejected
terminal completion is issued once
terminal failure blocks later successful completion
```

Updates are driven by typed readiness change evidence. No polling, frame count,
delay or time-based estimate is used.

## Terminal success

Successful `100%` requires:

```text
technical range completed successfully
captured occurrence still matches the target Activity
all Required participants completed
no Required participant failed
no Required participant was released before completion
aggregate readiness IsReady = true
waiting result = Ready
```

The terminal update is published before Loading Hide. Loading Hide completes
before transition reveal and capability release.

## Failure, invalidation and cancellation

The envelope never publishes successful `100%` after:

```text
Required failure
premature Required release
occurrence invalidation
wait cancellation
runtime disposal
explicit terminal failure
```

The last valid progress snapshot is retained for diagnostics. The committed
destination and scoped recovery-gate behavior remain governed by the Activity
entry-readiness decision. No silent fallback converts failure into technical-only
success.

Optional failure remains non-blocking unless a separate explicit contract makes
it blocking.

## Diagnostics

Operation diagnostics expose evidence equivalent to:

```text
technical step count
technical range start/end
readiness range start/end
captured Activity and occurrence
Required total/completed/pending/failed/released
Optional total/completed/pending/failed/released
readiness ratio
last accepted normalized progress
rejected stale snapshot count
terminal completion issued
terminal failure observed
Loading hidden
reveal completed
```

The host's Loading diagnostics project the final observed progress and the
activity-entry envelope diagnostics for direct Activity, Route Startup Activity
and Game Application Startup Activity paths. A retained Loading surface on
failure is reported explicitly instead of being formatted as a normal Hide.

## Consumer authoring boundary

A consumer completes readiness through the normal public participant API:

```text
participant.CompletePreparation()
participant.FailPreparation(reason)
```

The framework counts participants, not chickens, enemies, files, scene objects
or arbitrary game concepts. Consumer scripts must not:

```text
resolve the persistent Loading adapter
write directly to Loading progress
find FrameworkRuntimeHost
use global lookup or a service locator
parse logs as a command path
duplicate the envelope calculation
```

## FIRSTGAME reference proof

`planet-devourer` Demo 01 Activity Readiness uses:

```text
Policy = WaitCovered
Visual Transition = FadeWithLoading
Transition Gate = InputInteractionAndGameplay
4 independent Required participants
1 Optional participant kept pending
```

Each Chicken-to-target condition completes one assigned Required participant.
The framework advances progress because four Required participants complete.
The Optional participant stays pending without entering the denominator.

Validated Play Mode occurrences `4` and `6` both reported:

```text
Required completed = 4
Required total = 4
Required pending = 0
Optional total = 1
Optional pending = 1
Loading progress mode = Determinate
Loading progress phase = ActivityReadiness
Loading progress percent = 100
request kind = Succeeded
blocking issues = 0
```

The second occurrence proves exit to Intermission and clean reentry.

## QA proof

The technical harness proves:

```text
Required/Optional count invariants
frozen occurrence denominator
monotonic technical and readiness ranges
Optional non-participation in progress
no 100% on Required failure/release
no 100% on invalidation/cancellation
stale occurrence rejection
direct Activity path
Route Startup Activity parity
Game Application Startup Activity parity
100% before Hide
Hide before reveal
retained failure diagnostics
```

The package/QA program closed 111 focused checks across the positive,
terminal-path, Route Startup and Game Application Startup suites before the
FIRSTGAME consumer proof.

## Implementation coverage

Implemented package cuts:

| Cut | Commit | Result |
|---|---|---|
| `IF-READY-PROGRESS-01` | `2a9cb1eb7cf5dc5fc4403fbdbf99b06b062be5af` | immutable Required/Optional completion evidence |
| `IF-READY-PROGRESS-02` | `78405ef850bba942ba19161ab2196b784c026fdc` | stable operation-scoped Loading envelope and ranges |
| `IF-READY-PROGRESS-03` | `99893aa804a9f40cb057449d2b4900a00a2fc3ed` | WaitCovered integration across initial request paths |
| canonical host wiring fix | `c423d4c6c9b46bac5f5eaf106be5050f46120d52` | host dispatch and retained envelope diagnostics |
| startup technical-boundary fix | `72a6d9d4a63b2ec485053ae843ad229f325e63ff` | explicit startup technical range completion |

Audited package HEAD:

```text
272dd43cd70f3c793fb4bb2f3eef5d7d05a0df16
```

The single commit after the readiness baseline adds the Application Frame Rate
feature and does not alter the readiness-progress runtime files.

## Rejected alternatives

- Publishing `100%` while readiness remains `Preparing`.
- Treating readiness as an unobservable delay after technical Loading completes.
- Counting Optional participants in the denominator.
- Counting gameplay objects directly.
- Dynamically changing the global denominator.
- Polling participant state or scanning scenes repeatedly.
- Time-based simulated readiness progress.
- Activity content controlling the persistent Loading surface.
- A second Loading authority dedicated to readiness.
- Silent fallback to technical-only progress for `WaitCovered`.

## Consequences

Positive:

```text
100% means the covered Activity is ready to reveal
multiple Required participants produce meaningful progress
Optional participants remain diagnostic without distortion
the denominator remains stable
all canonical entry paths share the same semantics
Loading stays decoupled from gameplay scripts
```

Tradeoffs and current limits:

```text
progress is participant-granular, not continuous inside one participant
no authorable weights
no automatic timeout
no post-release automatic re-gating
inactive authored participant components are still included by explicit-scope discovery
```

The inactive-object discovery behavior is intentional in the current query
implementation but is an authoring friction: disabling a GameObject is not an
exclusion mechanism. Remove the component from the explicit scope or repurpose
it as a valid participant instead.

## Pending decisions

- Optional future custom weights for Required participants.
- Optional continuous progress source inside one participant.
- Product-facing timeout, retry and recovery authoring.
- Dedicated authoring validation for accidental inactive/legacy participants.
- ~~Dedicated ADR-numbering correction for the duplicate `IF-ADR-007` identity.~~ Resolved 2026-08-06: Audio BGM is IF-ADR-013.
