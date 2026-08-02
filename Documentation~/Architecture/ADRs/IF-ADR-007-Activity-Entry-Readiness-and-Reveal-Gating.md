# IF-ADR-007 — Activity Entry Readiness and Reveal Gating

Status: Accepted
Last updated: 2026-08-01
Supersedes: none
Superseded by: none
Related decisions: IF-ADR-001, IF-ADR-006

## Context

Loading and materializing the Unity scenes owned by an Activity does not guarantee
that the Activity is ready to be presented or used.

Post-materialization work may still be required after the target objects and typed
runtime context exist:

```text
apply save data to materialized objects
admit or materialize Logical Players and Actors
apply Actor skins, loadouts and attributes
bind controls and gameplay receivers
resolve Camera targets and presentation bindings
prepare NPCs, navigation or procedural content
synchronize required network state
validate required game-specific conditions
```

These operations cannot always run before scene composition because they depend on
objects that only exist after Activity content has been loaded and materialized.

The framework already has an authorable Activity readiness model:

```text
ActivityReadinessParticipant
Required / Optional contribution
Preparing / Completed / Failed / Released states
occurrence-scoped aggregation
post-transition readiness updates
presentation events and diagnostics
```

The current runtime can commit an Activity as the active authority and complete its
loading/transition envelope while Required readiness participants are still
`Preparing`. The Activity later changes from `NotReady` to `Ready` as a
post-transition state update.

That behavior is useful for progressive or diagnostic scenarios, but it does not
solve the common product requirement:

```text
Do not reveal or release normal gameplay for the target Activity until all
Required post-materialization preparation has completed.
```

The product also needs a deliberate visible-preparation mode. A sample may reveal
the Activity while preparation is still running so a user can understand or
experience the readiness phase. The FIRSTGAME chicken demonstration is an example
of this mode.

The framework therefore needs an explicit Activity-owned policy that separates:

```text
Activity authority
Activity readiness
visual reveal
loading presentation
input / interaction / gameplay release
```

## Decision

Activity entry readiness is the occurrence-scoped post-materialization preparation
state used to decide when the target Activity may be released for normal use.

The framework treats the following as separate dimensions:

```text
Authority
  which Activity is the current typed runtime authority

Readiness
  whether the current occurrence satisfies all Required preparation contributions

Presentation
  whether the target Activity is visually covered or revealed

Capability gate
  whether input, interaction and gameplay are allowed
```

An Activity may be the current authority while still `Preparing`. Visual reveal and
capability release are then controlled by an explicit entry-readiness policy.

## Policy authority

`ActivityAsset` owns the entry-readiness intent through an explicit policy field.
The canonical policy type is:

```text
ActivityEntryReadinessPolicy
```

The initial policy vocabulary is:

```text
ObserveOnly
WaitCovered
WaitVisible
```

The default for existing and newly migrated assets is `ObserveOnly`. This preserves
current behavior unless an author explicitly opts into a waiting policy.

A Route does not duplicate this policy. A Route request that starts a Startup
Activity consumes the Startup Activity policy. A Route without a Startup Activity
has no Activity entry-readiness policy to evaluate.

Activity clear operations have no target Activity readiness to await.

## Policy semantics

### ObserveOnly

`ObserveOnly` preserves the current post-transition model.

```text
transition/loading begins
-> target Activity scenes load and materialize
-> target Activity becomes current authority
-> readiness participants begin
-> loading and transition are released
-> capability gate is released with the operation
-> readiness may complete or fail later
```

This mode is appropriate when:

```text
preparation may continue progressively after reveal
readiness is informational or diagnostic
content is intentionally usable before all preparation completes
compatibility with the current behavior is required
```

`ObserveOnly` does not retain loading, transition presentation or capability gates
for readiness.

### WaitCovered

`WaitCovered` retains the visual cover and capability gate until the initial
readiness occurrence reaches `Ready`.

```text
transition before / visual cover
-> loading presentation when authored
-> target Activity scenes load and materialize
-> target Activity becomes current authority
-> readiness participants begin
-> wait for the current occurrence
-> all Required participants complete
-> Activity readiness becomes Ready
-> hide loading
-> execute transition after / reveal
-> release input, interaction and gameplay gate
```

This is the normal production policy for Activities that must not be shown in an
incomplete state.

Typical uses include:

```text
Actor materialization and skin application
save restoration
control and Camera binding
required NPC or navigation preparation
required procedural generation
required network synchronization
```

### WaitVisible

`WaitVisible` reveals the target Activity after materialization while retaining the
capability gate until the initial readiness occurrence reaches `Ready`.

```text
transition before / visual cover when authored
-> target Activity scenes load and materialize
-> target Activity becomes current authority
-> readiness participants begin
-> hide loading
-> execute transition after / reveal
-> keep input, interaction and gameplay blocked
-> preparation remains visible
-> all Required participants complete
-> Activity readiness becomes Ready
-> release the capability gate
```

`WaitVisible` is appropriate when preparation is deliberately visible:

```text
didactic samples
world assembly or construction sequences
staged scene preparation
non-interactive visual introductions driven by readiness work
```

Visual reveal does not imply gameplay release.

The FIRSTGAME chicken demonstration remains valid and should use `WaitVisible` so
the readiness phase can be observed directly.

## Readiness participant semantics

An `ActivityReadinessParticipant` is a readiness contribution. It may perform work,
start work owned by another system, or only observe a condition owned elsewhere.

```text
PreparationStarted
  participant begins or observes its preparation

CompletePreparation
  the contribution completed successfully

FailPreparation
  the contribution reached an explicit failed state

Release
  the occurrence was exited, replaced or invalidated
```

Requiredness has the following entry-gate meaning:

```text
Required Preparing
  blocks Ready and blocks waiting-policy release

Required Completed
  contributes to Ready

Required Failed
  produces an explicit entry-readiness failure

Optional Preparing or Failed
  remains diagnostic and never blocks Ready or entry release
```

An Activity with no authorable participants may become Ready from its technical
baseline without an artificial delay.

A participant is not required to publish percentage progress. Boolean or terminal
condition evidence is sufficient.

## Initial readiness versus operational readiness

The entry policy applies only to the initial readiness occurrence created for the
current Route startup or Activity request.

After the Activity has been released for normal use, later readiness changes remain
observable but do not automatically reopen loading, close the transition curtain or
reapply the entry capability gate.

```text
initial occurrence
  may gate reveal and/or capability release

post-release Ready -> NotReady update
  updates runtime state and diagnostics only
  does not start a new visual transition
```

A future policy may define runtime re-gating, but it is outside this decision.

## Occurrence-scoped waiting

Waiting must be typed, event-driven and keyed to one
`ActivityReadinessOccurrence`. Polling and global lookup are not accepted.

The awaitable result must distinguish at least:

```text
Ready
Failed
Invalidated
Cancelled
```

The following rules are mandatory:

```text
A completion from an old occurrence cannot release a new occurrence.
Replacing or clearing the Activity invalidates the current wait.
Invalidation releases tracked participants.
Late participant completion remains rejected and diagnostic.
Exactly one terminal result may release or fail one entry gate.
```

No singleton, service locator or scene-wide fallback lookup is introduced.

## Transition and loading orchestration

`ActivityFlowRuntime` remains the authority for:

```text
Activity authority and occurrence identity
participant discovery and lifecycle
technical readiness baseline
authorable readiness aggregation
readiness updates and invalidation
```

`GameFlowRuntime` remains the authority for operation ordering and must consume the
Activity policy when deciding:

```text
when loading may be hidden
when Transition After may execute
when the operation capability gate may be released
```

`FrameworkRuntimeHost` may continue adapting the explicit loading and transition
surfaces to Game Flow operations. It does not become readiness authority.

`LoadingSurface` presents progress and issues. It does not discover participants,
decide readiness or own lifecycle policy.

`TransitionSurface` and transition-effect adapters remain visual envelopes. They do
not own Activity authority or readiness.

`ActivityReadinessEvents` remains a presentation observer. It must not become an
internal command path for releasing loading or gates.

## Loading progress

Waiting policies add a final semantic loading phase after scene composition:

```text
LoadingScenes
MaterializingActivity
PreparingActivity
Ready
```

`PreparingActivity` may be indeterminate. The framework must not fabricate a
percentage from participant counts or timers.

Diagnostics may report participant counts and identities while the loading surface
continues to present an indeterminate phase.

## Transition-mode compatibility

The policy and visual transition mode are independent authoring decisions, but
invalid combinations must be diagnosed explicitly.

```text
ObserveOnly + Seamless/Fade/FadeWithLoading
  valid

WaitVisible + Seamless/Fade/FadeWithLoading
  valid

WaitCovered + Fade
  valid; the transition cover remains until Ready

WaitCovered + FadeWithLoading
  valid; loading and transition cover remain until Ready

WaitCovered + Seamless
  invalid; no authored visual cover can satisfy the policy intent
```

The framework must not silently replace `Seamless` with a fade or loading mode.

## Capability-gate compatibility

Both waiting policies require input, interaction and gameplay to remain blocked
until the initial occurrence is Ready.

The authored transition gate configuration must provide that protection. An
insufficient gate mode is a blocking validation issue; the framework must not
silently strengthen the authored policy at runtime.

For an Activity request, validation evaluates the target Activity transition gate.
For a Route request with a Startup Activity, validation evaluates the Route gate
that wraps the startup operation against the Startup Activity entry-readiness
policy.

Lifecycle requests remain blocked while a readiness wait is actively pending for
the same operation.

## Failure behavior

A failed Required participant is terminal for the initial entry-readiness wait. It
must never be treated as Ready and must never trigger silent visual or gameplay
release.

The operation must publish a typed failure result containing:

```text
Activity and occurrence identity
entry-readiness policy
aggregate readiness snapshot
failed Required participant identities and reasons
visual reveal state
capability-gate state
whether the destination is already authoritative
```

For `WaitCovered`:

```text
visual cover remains active
normal input, interaction and gameplay remain blocked
```

For `WaitVisible`:

```text
target content remains visible
normal input, interaction and gameplay remain blocked
```

A terminal failure must not leave an unobservable request awaiting forever. The
transient in-flight operation may finish with an explicit committed-destination
failure, while a scoped recovery blocker preserves the unsafe capabilities.
Lifecycle recovery requests must remain possible after that terminal result.

Automatic rollback is not required by this ADR because the previous Activity may
already have exited and released owned content. Recovery is explicit and may use a
new Activity or Route request, retry operation or future recovery policy.

## Timeout behavior

The initial implementation has no automatic timeout.

```text
Required participant remains Preparing
  -> waiting policy remains pending
```

A diagnostic watchdog may report long-running preparation, but it must not convert
pending readiness into success or release presentation silently.

Timeout and retry authoring are deferred decisions.

## Authoring surface

The Activity Inspector exposes a designer-first section:

```text
Activity Entry Readiness
  Policy
    Observe Only
    Wait Covered
    Wait Visible
```

The Inspector explains the consequences of each policy and validates transition and
gate compatibility inline.

Advanced/Debug presentation should expose:

```text
current Activity and occurrence
entry-readiness policy
aggregate readiness state
Required and Optional counts
pending and failed participant identities
visual cover/reveal state
loading phase
gate state
last terminal reason
```

Technical components remain visible in an Advanced/Debug mode. The framework must
not hide participant materialization without diagnostic access.

## Route startup behavior

A Route request with a Startup Activity follows the same entry-readiness semantics
as a direct Activity request.

```text
Route transition begins
-> Route scenes compose
-> Startup Activity scenes compose
-> Startup Activity readiness begins
-> consume Startup Activity entry policy
-> release Route transition according to that policy
```

A Route does not author a second readiness policy. Route-level validation reports
cross-asset incompatibilities between the Route transition/gate settings and its
Startup Activity policy.

## Restart, reentry and replacement

Activity restart and reentry create a new occurrence and evaluate the policy again.

```text
old occurrence invalidated
-> old participants released
-> new occurrence begins
-> new initial readiness gate is evaluated
```

A replacement request must not inherit readiness, visual-release state or gate
release from the previous occurrence.

## Accepted scope

- Activity-owned entry-readiness policy.
- `ObserveOnly`, `WaitCovered` and `WaitVisible` semantics.
- Route Startup Activity consumption of the Activity policy.
- Occurrence-scoped, event-driven readiness waiting.
- Required/Optional gate semantics.
- Explicit visual reveal and capability-release separation.
- Loading and transition integration without presentation authority inversion.
- Typed failure, invalidation and cancellation results.
- Backward-compatible `ObserveOnly` default.
- Inspector validation and runtime diagnostics.
- QA coverage for direct Activity requests and Route startup.
- FIRSTGAME visible-preparation demonstration through `WaitVisible`.

## Rejected scope

- Loading or Transition surfaces deciding Activity readiness.
- Readiness presentation events used as hidden command paths.
- Global managers, singletons or service-locator access.
- Polling the scene or runtime state until it appears Ready.
- Silent fallback from a failed Required participant.
- Silent conversion of `WaitCovered + Seamless` into another visual mode.
- Timer-only readiness or fabricated percentage progress.
- Optional participants blocking entry release.
- Automatic reopening of loading for post-release readiness changes.
- Automatic rollback after target authority commit.
- Implicit timeout, retry or recovery behavior.

## Consequences

Scene loading and Activity readiness become explicitly different stages:

```text
Scene loaded
  does not imply Activity Ready

Activity materialized
+ all Required readiness participants completed
  implies Activity Ready
```

Games can guarantee that Actors, skins, controls, Camera bindings and restored state
are coherent before revealing gameplay.

Samples and stylized experiences can deliberately reveal preparation while keeping
unsafe capabilities gated.

The Activity becomes the single authoring authority for entry-readiness intent.
Routes consume Startup Activity intent without duplicating policy.

The runtime gains a longer-lived operation path for waiting policies and must handle
cancellation, failure, replacement and diagnostics without stale occurrence release.

Presentation remains replaceable and does not become lifecycle authority.

## Current implementation coverage

The following capabilities already exist:

```text
ActivityReadinessParticipant authoring
Required and Optional aggregation
Preparing, Completed, Failed and Released states
occurrence identity and late-completion rejection
post-transition readiness propagation
ActivityReadinessEvents presentation observer
runtime snapshots and diagnostics
```

The following capabilities are not yet implemented and are required to complete this
decision:

```text
ActivityEntryReadinessPolicy authoring
occurrence-scoped awaitable readiness result
WaitCovered orchestration
WaitVisible orchestration
loading phase integration
transition-after retention
capability-gate retention and recovery blocker
cross-asset validation for Route Startup Activity
failure presentation/recovery diagnostics
entry-gate QA smokes and FIRSTGAME policy integration
```

Until those capabilities exist, the framework must describe Activity readiness as a
post-transition state model and must not claim that Required participants retain
loading, fade or gameplay release.

## Validation requirements

The package and QA harness must prove at least:

```text
ObserveOnly preserves current behavior.
WaitCovered keeps visual cover and capabilities blocked while Required is pending.
WaitVisible reveals content but keeps capabilities blocked while Required is pending.
Required completion releases exactly once.
Optional pending or failed does not block release.
Required failure never reveals or releases silently.
An invalidated occurrence cannot release a replacement occurrence.
Activity reentry creates and waits on a new occurrence.
Route Startup Activity obeys its Activity policy.
WaitCovered + Seamless fails validation.
Insufficient transition gate configuration fails validation.
Loading reports PreparingActivity without fabricated progress.
Post-release readiness changes do not reopen the entry presentation.
```

FIRSTGAME must prove both product shapes:

```text
WaitVisible
  visible chicken preparation demonstration

WaitCovered
  production-like Activity entry hidden until Required readiness completes
```

## Pending decisions

- Product-facing retry and recovery authoring after Required entry failure.
- Optional explicit timeout policy and its recovery semantics.
- Participant-authored progress contributions beyond terminal state.
- Reusable readiness recipes/composers for common Actor, save, Camera and control preparation.
- Whether future runtime re-gating requires a separate operational-readiness policy.
