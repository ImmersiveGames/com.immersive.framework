# IF-ADR-007 — Activity Entry Readiness and Reveal Gating

Status: Accepted and implemented for the initial scope  
Last updated: 2026-08-04  
Supersedes: none  
Superseded by: none  
Related decisions: IF-ADR-001, IF-ADR-006, IF-ADR-011

> Numbering note (2026-08-06): the Optional Audio BGM Adapter decision was
> renumbered to **IF-ADR-013**. This file is the sole IF-ADR-007.

## Context

Loading and materializing the Unity scenes owned by an Activity does not prove
that the Activity is ready to be presented or used. Post-materialization work
may still be required after the target objects and typed runtime context exist:

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

The framework has an authorable readiness model:

```text
ActivityReadinessParticipant
Required / Optional contribution
Preparing / Completed / Failed / Released states
occurrence-scoped aggregation
presentation events and diagnostics
```

Activity authority, Activity readiness, visual reveal, Loading presentation and
capability release are separate concerns. An Activity may be the current typed
authority while its captured readiness occurrence remains `Preparing`.

## Decision

`ActivityAsset` owns the initial entry-readiness intent through:

```text
ActivityEntryReadinessPolicy
```

The policy vocabulary is:

```text
ObserveOnly
WaitVisible
WaitCovered
```

The default is `ObserveOnly`. A Route does not duplicate this policy. A Route
request that starts a Startup Activity consumes the Startup Activity policy.
Activity clear operations have no target Activity readiness to await.

`ActivityFlowRuntime` remains the authority for Activity identity, occurrence
identity, participant lifecycle and readiness aggregation. `GameFlowRuntime`
owns operation ordering, visual retention and capability-gate release.
`FrameworkRuntimeHost` adapts explicit Loading and transition surfaces but does
not become readiness authority.

## Policy semantics

### ObserveOnly

```text
transition/loading begins
→ target Activity scenes load and materialize
→ target Activity becomes current authority
→ readiness participants begin
→ Loading and transition are released
→ capability gate is released with the operation
→ readiness may complete or fail later
```

Use `ObserveOnly` when preparation may continue after reveal or readiness is
informational. It does not retain Loading, visual cover or capabilities for
readiness.

### WaitVisible

```text
transition before / cover when authored
→ target Activity scenes load and materialize
→ target Activity becomes current authority
→ readiness participants begin
→ Loading hides
→ transition after reveals the target
→ input, interaction and gameplay remain blocked
→ all Required participants complete
→ Activity becomes Ready
→ capability gate releases
```

Use `WaitVisible` for deliberately visible preparation, didactic samples or
staged assembly. Visual reveal does not imply gameplay release.

### WaitCovered

```text
transition before / visual cover
→ Loading presentation when authored
→ target Activity scenes load and materialize
→ target Activity becomes current authority
→ readiness participants begin
→ wait for the captured occurrence
→ all Required participants complete
→ Activity becomes Ready
→ terminal Loading progress is published when supported
→ Loading hides
→ transition after reveals the target
→ input, interaction and gameplay gate releases
```

Use `WaitCovered` when the Activity must not be shown in an incomplete state.

## Readiness participant semantics

An `ActivityReadinessParticipant` is one readiness contribution. It may perform
work, start work owned by another system or observe a condition owned elsewhere.

```text
Preparation Started
  begin or observe local preparation

CompletePreparation()
  complete this contribution successfully

FailPreparation(reason)
  complete this contribution with explicit failure

Preparation Released
  cancel or release local work when the occurrence exits
```

Requiredness controls entry release:

```text
Required Preparing
  blocks Ready and waiting-policy release

Required Completed
  contributes to Ready

Required Failed or Released before completion
  produces terminal blocking evidence

Optional Preparing, Completed, Failed or Released
  remains diagnostic and never blocks Ready
```

The participant set is frozen for one occurrence. Reentry creates a fresh
occurrence. Completion from an old or released occurrence cannot release a new
one.

## Initial versus operational readiness

The entry policy applies only to the initial readiness occurrence created for
the current startup or request. Later readiness changes remain observable but
do not automatically reopen Loading, close the transition curtain or reapply
the entry capability gate.

## Occurrence-scoped waiting

Waiting is typed, event-driven and keyed to one
`ActivityReadinessOccurrence`. The terminal result distinguishes:

```text
Ready
Failed
Invalidated
Cancelled
```

Mandatory rules:

```text
an old occurrence cannot release a replacement
replacement or clear invalidates the current wait
invalidation releases tracked participants
late completion is rejected and diagnostic
exactly one terminal result completes one entry wait
no polling or global lookup
```

## Loading progress

Loading remains presentation and never becomes readiness authority.

### ObserveOnly and WaitVisible

These policies do not project participant completion into Loading. Their
technical Loading behavior remains independent of the later readiness state.

### WaitCovered with Fade

The visual cover remains until `Ready`, but there is no determinate Loading
surface to receive participant-aware progress.

### WaitCovered with FadeWithLoading

When the persistent Loading surface supports determinate progress, the
framework uses the participant-aware envelope defined by IF-ADR-011:

```text
known technical phase range
→ explicit technical boundary below 100%
→ final readiness range
→ equal increments for captured Required participants
→ 100% only when aggregate readiness is Ready
→ Hide
→ reveal
```

Only Required participants enter the denominator. Optional participants remain
visible in diagnostics and do not change progress or block `Ready`.

The technical range cannot publish successful `100%`. The startup path
explicitly completes its reserved technical boundary before readiness begins.
A failure, invalidation or cancellation retains the last valid progress value
and never publishes successful `100%`.

The framework does not fabricate continuous progress inside one participant.
One aggregate Required participant produces one readiness increment. Multiple
independent increments require multiple independent Required participants.

## Transition-mode compatibility

```text
ObserveOnly + Seamless/Fade/FadeWithLoading
  valid

WaitVisible + Seamless/Fade/FadeWithLoading
  valid

WaitCovered + Fade
  valid; cover remains until Ready

WaitCovered + FadeWithLoading
  valid; determinate participant-aware progress when supported

WaitCovered + Seamless
  invalid; no authored visual cover satisfies the policy
```

The framework does not silently replace `Seamless` or strengthen an authored
gate.

## Capability-gate compatibility

Both waiting policies require the authored transition gate to block input,
interaction and gameplay until `Ready`. An insufficient gate is a blocking
validation issue. Route Startup Activity validation evaluates the Route gate
because the Route operation owns that entry envelope.

Lifecycle requests remain blocked only while the transient operation is active.
After a committed-destination failure, the transient gate ends and a scoped
recovery blocker preserves unsafe capabilities while allowing an explicit
recovery Route or Activity request.

## Failure behavior

A failed Required participant is terminal for the initial entry wait. It is not
converted to `Ready` and does not silently reveal or release gameplay.

For `WaitCovered`:

```text
visual cover remains active
Loading retains the last valid progress snapshot
normal input, interaction and gameplay remain blocked
request returns a typed committed-destination failure
```

For `WaitVisible`:

```text
target content remains visible
normal input, interaction and gameplay remain blocked
request returns a typed committed-destination failure
```

Automatic rollback and automatic timeout remain outside this decision.

## Authoring surface

The Activity Inspector exposes:

```text
Activity Entry Readiness
  Policy
    Observe Only
    Wait Covered
    Wait Visible
```

The author also selects a compatible visual transition and transition gate.
Readiness participants are authored in the explicit Route/Activity content
scope. Technical components remain inspectable through Advanced/Debug
surfaces.

## Diagnostics

Runtime and Loading diagnostics expose evidence equivalent to:

```text
Activity and occurrence identity
entry-readiness policy
aggregate readiness state
Required total/completed/pending/failed/released
Optional total/completed/pending/failed/released
technical and readiness ranges
last normalized Loading progress
terminal completion/failure evidence
Loading hidden
reveal completed
transition-gate and recovery-blocker state
```

## FIRSTGAME reference proof

`planet-devourer` Demo 01 Activity Readiness proves both waiting shapes:

```text
Wait Visible
  visible preparation while capabilities remain gated

Wait Covered + FadeWithLoading
  four independent Required participants
  one Optional participant kept pending
  participant-aware determinate Loading progress
  100% only after 4/4 Required complete
  Loading Hide before reveal
  clean exit to Intermission and reentry
```

The consumer owns the Chicken-to-condition mapping. The package owns readiness,
progress projection, presentation ordering and diagnostics.

## Implemented scope

The initial decision is implemented for:

```text
ActivityEntryReadinessPolicy authoring
ObserveOnly, WaitVisible and WaitCovered orchestration
occurrence-scoped event-driven waiting
Required/Optional aggregation
transition-after retention
capability-gate retention and scoped recovery blocker
Route Startup Activity policy consumption
Game Application Startup Activity parity
typed Ready, Failed, Invalidated and Cancelled results
participant-aware WaitCovered + FadeWithLoading progress
100% before Hide and reveal
runtime and Loading diagnostics
```

Package implementation evidence:

```text
322d395  Activity entry readiness policy authoring
89aa95d  occurrence-scoped Activity readiness waiting
f39c6e5  reveal and capability-gate orchestration
bd79dd5  cancellation and recovery ownership
f5620ef  stable Activity identity comparison
2a9cb1e  Required/Optional completion evidence
78405ef  operation-scoped Loading progress envelope
99893aa  WaitCovered determinate progress integration
c423d4c  canonical host-path wiring and retained diagnostics
72a6d9d  explicit startup technical-boundary completion
```

## Exclusions

```text
participant-authored weights
continuous percentage from one participant
time-based simulated readiness progress
scene polling
Activity content resolving the persistent Loading surface
automatic timeout or retry
automatic runtime re-gating after initial release
automatic rollback after target authority commit
```
