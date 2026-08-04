# Activity Readiness

Activity readiness is the occurrence-scoped post-materialization contract used
to decide when an Activity is safe to reveal and release for normal use.

`ActivityFlowRuntime` remains the authority for Activity identity, participant
lifecycle and readiness. Consumer authoring contributes preparation evidence; it
does not assign global Activity state or control persistent Loading directly.

## Choose the product behavior first

Open the `ActivityAsset` and configure **Activity Entry Readiness > Policy**.

| Policy | Reveal behavior | Capability behavior | Loading readiness progress |
|---|---|---|---|
| **Observe Only** | normal transition release | normal operation release | none; readiness remains observational |
| **Wait Visible** | reveal after materialization | input, interaction and gameplay stay blocked until Ready | none; preparation is intentionally visible |
| **Wait Covered** | keep visual cover until Ready | input, interaction and gameplay stay blocked until Ready | determinate only with Fade With Loading and a progress-capable Loading adapter |

### Observe Only

Use when content may be shown before every preparation contribution finishes.
This is the compatibility default.

### Wait Visible

Use for visible assembly, staged introductions or didactic samples. The target is
visible while unsafe capabilities remain blocked.

### Wait Covered

Use when an incomplete Activity must never be shown. Pair it with:

```text
Visual Transition = Fade
or
Visual Transition = Fade With Loading

Block During Transition = Input Interaction And Gameplay
```

`Wait Covered + Seamless` is invalid. The framework reports the mismatch and
does not silently add a fade or strengthen the gate.

## Add a readiness participant

1. Add **Immersive Framework/Activity Readiness Participant** inside the explicit Route/Activity content scope.
2. Set a stable **Participant Id**. Object names and hierarchy paths are not identity.
3. Choose **Required** when the contribution must complete before `Ready`; choose **Optional** for diagnostic-only work.
4. Wire **Preparation Started** to local preparation or observation.
5. Call `CompletePreparation()` on that same participant when its condition succeeds.
6. Call `FailPreparation(reason)` for explicit failure.
7. Use **Preparation Released** to cancel or release local work on exit or replacement.

The framework does not provide a timer or fabricated completion coroutine.

## Discovery scope and inactive objects

Participants are discovered only from explicit Route-owned and matching
Activity-owned loaded scenes. Discovery includes roots, descendants and inactive
GameObjects.

Therefore:

```text
disabling a participant GameObject does not exclude it from readiness discovery
```

A legacy participant left inactive may still be captured, begin preparation and
block a waiting policy. Remove it from the explicit scope, remove the component or
repurpose it as a valid Required/Optional participant. Do not rely on inactive
state as authoring exclusion.

## Required and Optional semantics

```text
Required Preparing
  blocks Ready

Required Completed
  contributes to Ready and participant-aware progress

Required Failed
  produces terminal failure

Required Released before completion
  produces terminal failure evidence

Optional any state
  remains diagnostic
  does not enter the progress denominator
  does not block Ready
```

Participant IDs must be unique inside the captured occurrence.

## One aggregate participant or several independent participants

Use one aggregate Required participant when the product condition is one compound
unit:

```text
all four objects prepared
→ aggregate participant completes once
→ one readiness progress increment
```

Use several independent Required participants when separate progress increments
are meaningful:

```text
object 01 prepared → Required 01 completes
object 02 prepared → Required 02 completes
object 03 prepared → Required 03 completes
object 04 prepared → Required 04 completes
```

The framework counts completed Required participants. It does not count gameplay
objects directly.

## Wait Covered Loading progress

Participant-aware determinate progress requires all of:

```text
Policy = Wait Covered
Visual Transition = Fade With Loading
persistent Loading surface exists
Loading adapter reports progress support
captured readiness occurrence is valid
```

The runtime allocates one stable operation envelope:

```text
technical range
→ readiness range
→ terminal 100% only after aggregate Ready
```

The technical step count is known before the operation. The Required participant
count is captured after target materialization and subdivides only the reserved
readiness range.

For four Required participants, the Loading surface receives four monotonic
readiness increments. An Optional participant may remain pending without changing
the denominator.

The exact global percentages depend on the number of technical operation steps.
Do not assume the readiness increments are globally `25/50/75/100`.

## Persistent Loading requirements

The application should expose one explicit persistent Loading adapter. A typical
Unity surface uses:

```text
UnityLoadingSurfaceAdapter
Canvas Group / visual root
progress root
Image fill and/or Slider
Apply Hidden State On Awake
Show Progress When Visible
Hide Progress When Hidden
Reset Progress On Hide
```

The Activity scene and readiness participants must not hold references to this
adapter. Game Flow supplies Loading requests and progress snapshots through the
canonical runtime path.

## Ordering contract

Successful `Wait Covered + Fade With Loading` entry follows:

```text
Loading Show
→ technical work
→ technical boundary below 100%
→ participant readiness increments
→ aggregate Ready
→ Loading 100%
→ Loading Hide
→ transition reveal
→ capability gate release
→ request success
```

`100%` never means “technical scenes loaded but readiness still pending.”

## Failure and interruption

The following never publish successful `100%`:

```text
Required failure
Required release before completion
occurrence invalidation
wait cancellation
runtime disposal
```

For a committed `Wait Covered` destination, visual cover and the last valid
Loading progress remain visible while an explicit recovery blocker keeps unsafe
capabilities blocked. A typed failure result is returned; there is no silent
fallback or automatic rollback.

Stopping Play Mode while the operation is pending produces a typed cancellation
such as `GameFlowRuntimeDisposed`. That is interruption evidence, not a successful
readiness result.

## Present local readiness without polling

Add **Immersive Framework/Activity Readiness Events** in the same explicit
Activity scope. Wire `Preparing`, `Ready` and `Not Ready` to a local presenter.
The presenter may update visuals, text or enabled content, but must not:

```text
change readiness authority
resolve FrameworkRuntimeHost
update persistent Loading
poll global objects
parse logs as a command path
```

Local UI progress may explain a game-specific condition, but it must not replace
the framework Loading authority.

## Advanced and runtime diagnostics

Useful evidence includes:

```text
activityReadiness
activityReadinessReason
occurrence transition sequence
Required total/completed/pending/failed/released
Optional total/completed/pending/failed/released
loadingProgressSupported
loadingProgressMode
loadingProgressValue
loadingProgressPercent
loadingProgressPhase
loadingProgressMessage
Loading hidden
reveal completed
blockingIssues
```

Expected successful terminal evidence for a four-Required/one-Optional case:

```text
activityReadiness = Ready
Required completed = 4
Required total = 4
Required pending = 0
Optional total = 1
Optional pending = 1
loadingProgressMode = Determinate
loadingProgressPhase = ActivityReadiness
loadingProgressPercent = 100
blockingIssues = 0
```

## Reentry checklist

Validate at least:

```text
enter Wait Covered
→ four Required participants complete independently
→ Loading reaches 100 and hides
→ reveal completes
→ exit to another Activity
→ all tracked participants release
→ reenter Wait Covered
→ a fresh occurrence starts
→ the same four contributions complete again
```

Old occurrence updates must not advance the replacement occurrence.

## FIRSTGAME reference

Repository:

```text
ImmersiveGames/planet-devourer
```

Demo:

```text
Assets/_Project/Demo 01 - Routes and Activities/
```

Primary reference assets:

```text
Data/Activity Readiness/Activities/ActivityReadiness_WaitCovered.asset
Data/Activity Readiness/Activities/Profiles/ActivityContent_ReadinessWaitCovered.asset
Scenes/Activity Readiness/ActivitiesContent/Activity_Readiness_WaitCovered.unity
Prefabs/Activity Readiness/Activity Readiness Scenario - Wait Covered.prefab
Prefabs/Activity Readiness/Ui/Canvas_ActivityReadinessNavigation.prefab
```

The scenario proves:

```text
4 independent Required participants
1 Optional participant kept pending
Fade With Loading
Input Interaction And Gameplay gate
100% after Ready
Loading Hide before reveal
Intermission exit and clean reentry
```

FIRSTGAME is a consumer proof, not the authority for the progress formula.

## Current limits

```text
no participant weights
no continuous percentage from one participant
no automatic timeout
no automatic retry or rollback
no post-release automatic re-gating
inactive participant components remain discoverable in explicit scope
```
