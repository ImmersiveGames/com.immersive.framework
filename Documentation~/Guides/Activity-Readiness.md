# Activity Readiness

Last updated: **2026-09-30**

Canonical focused guide for Activity entry readiness. For Route/Activity ownership,
navigation requests and the relationship between navigation and readiness, see
[Game Flow](Game-Flow.md).

Activity readiness is the occurrence-scoped post-materialization contract used to decide when an Activity is safe to reveal and release for normal use.

`ActivityFlowRuntime` remains the authority for Activity identity, participant lifecycle and readiness. Consumer authoring contributes preparation evidence; it does not assign global Activity state or control persistent Loading directly.

## Choose the product behavior first

Open the `ActivityAsset` and configure **Activity Entry Readiness > Policy**.

| Policy | Reveal behavior | Capability behavior | Loading readiness progress |
|---|---|---|---|
| **Observe Only** | normal transition release | normal operation release | none; readiness remains observational |
| **Wait Visible** | reveal after materialization | input, interaction and gameplay stay blocked until Ready | none; preparation is intentionally visible |
| **Wait Covered** | keep visual cover until Ready | input, interaction and gameplay stay blocked until Ready | determinate only with Fade With Loading and a progress-capable Loading adapter |

### Observe Only

Use when content may be shown before every preparation contribution finishes. This is the compatibility default.

### Wait Visible

Use for visible assembly, staged introductions or didactic samples. The target is visible while unsafe capabilities remain blocked.

### Wait Covered

Use when an incomplete Activity must never be shown. Pair it with:

```text
Visual Transition = Fade
or
Visual Transition = Fade With Loading

Block During Transition = Input Interaction And Gameplay
```

`Wait Covered + Seamless` is invalid. The framework reports the mismatch and does not silently add a fade or strengthen the gate.

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

Participants are discovered only from explicit Route-owned and matching Activity-owned loaded scenes. Discovery includes roots, descendants and inactive GameObjects.

Package Player readiness contribution is resolved from Activity configuration and the current Session projection **before** Activity content lifecycle executes, so a Required Player participant is present when the readiness occurrence begins.

Therefore:

```text
disabling a participant GameObject does not exclude it from readiness discovery
```

A legacy participant left inactive may still be captured, begin preparation and block a waiting policy. Remove it from the explicit scope, remove the component or repurpose it as a valid Required/Optional participant. Do not rely on inactive state as authoring exclusion.

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

Use one aggregate Required participant when the product condition is one compound unit:

```text
all four objects prepared
→ aggregate participant completes once
→ one readiness progress increment
```

Use several independent Required participants when separate progress increments are meaningful:

```text
object 01 prepared → Required 01 completes
object 02 prepared → Required 02 completes
object 03 prepared → Required 03 completes
object 04 prepared → Required 04 completes
```

The framework counts completed Required participants. It does not count gameplay objects directly.

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

The technical step count is known before the operation. The Required participant count is captured after target materialization and subdivides only the reserved readiness range.

For four Required participants, the Loading surface receives four monotonic readiness increments. An Optional participant may remain pending without changing the denominator.

The exact global percentages depend on the number of technical operation steps. Do not assume the readiness increments are globally `25/50/75/100`.

## Persistent Loading requirements

The application should expose one explicit persistent Loading adapter. A typical Unity surface uses:

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

The Activity scene and readiness participants must not hold references to this adapter. Game Flow supplies Loading requests and progress snapshots through the canonical runtime path.

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

## Commit versus Ready

Navigation commit and Activity readiness are separate truths.

A Route request may successfully commit the Route while its startup Activity ends in a blocking readiness state:

```text
Route Request = Succeeded
current Activity = target Activity
ActivityState = Active
ActivityReadiness = NotReady
ActivityTransition = CommittedNotReady
blockingIssues > 0
```

Do not read Route success as implicit startup-Activity readiness success.

When the target Activity is already current, its Activity-owned `RuntimeContent` root remains valid until Activity exit/release even if a required Player contextual admission failed. Player rollback must not destroy the current Activity scope merely to satisfy a zero-root assertion.

This is especially important when diagnosing SceneProvided negative paths:

```text
Activity scope present
!=
Player contextual admission succeeded
!=
physical Player handed off
```

## Player readiness levels and limits

Player-specific readiness levels are ordered:

```text
None
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
```

Keep these meanings distinct:

```text
LogicalActorsPrepared
  required physical Player Actor occurrences are selected/prepared for the Activity projection

GameplayReady
  current contextual gameplay projection is established over retained prepared Session Players
```

`GameplayReady` is a Framework lifecycle/readiness contract. It does **not** by itself prove that game-owned Presentation content is functionally complete.

For example, readiness success alone does not certify:

```text
locomotion implemented
concrete gameplay input consumer authored
camera rig/provider authored
character visuals complete
```

Those remain gameplay-owned composition requirements. A game may require `GameplayReady` before using those systems without redefining `GameplayReady` as proof that the systems exist.

For Actor occurrence identity and preparation boundaries, see [Player Usage](Player-Usage.md) and [IF-ADR-038](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md).

Player Actor occurrence identity is likewise a preparation concern: reusable `PlayerActorDeclaration` templates may have an empty authored occurrence ID, and typed runtime `ActorId` becomes valid only when physical Player Actor preparation establishes the occurrence identity. See [IF-ADR-023A](../Architecture/Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md).

## Failure and interruption

The following never publish successful `100%`:

```text
Required failure
Required release before completion
occurrence invalidation
wait cancellation
runtime disposal
```

For a committed `Wait Covered` destination, visual cover and the last valid Loading progress remain visible while an explicit recovery blocker keeps unsafe capabilities blocked. A typed failure result is returned; there is no silent fallback or automatic rollback.

Stopping Play Mode while the operation is pending produces a typed cancellation such as `GameFlowRuntimeDisposed`. That is interruption evidence, not a successful readiness result.

These are Framework robustness contracts. Their negative, invalid, interrupted and terminal paths belong to technical QA/certification. A game-facing Sample or FIRSTGAME scenario should prove valid authored gameplay behavior and should not fabricate a failure button, invalid participant or forced cancellation solely to duplicate QA coverage. Only add a consumer recovery scenario if the game itself intentionally exposes such recovery as a real player-facing product behavior.

## Player-specific failure evidence

Player failures must be observed at the layer that owns them.

A SceneProvided authoring/adoption failure may be canonically exposed through Activity content participant failure and readiness blocking before a public Player admission operation/result exists. Do not fabricate a public terminal admission result merely to make the failure observable.

Likewise, `Contextual=Absent` does not prove physical Actor destruction. Session-owned physical truth comes from Session/occurrence preparation evidence.

## Present local readiness without polling

Add **Immersive Framework/Activity Readiness Events** in the same explicit Activity scope. Wire `Preparing`, `Ready` and `Not Ready` to a local presenter. The presenter may update visuals, text or enabled content, but must not:

```text
change readiness authority
resolve FrameworkRuntimeHost
update persistent Loading
poll global objects
parse logs as a command path
```

Local UI progress may explain a game-specific condition, but it must not replace the framework Loading authority.

`LastSnapshot` identifies both the committed `Activity` and its readiness
`Occurrence`. `Revision` orders public presentation changes; it is not an
occurrence identity.

For `Wait Visible`, the target Activity is committed as the canonical current
Activity before the request finishes waiting for readiness. Its public observer
therefore receives `Preparing` for that committed occurrence while the request
remains pending, followed by `Ready` for the same occurrence. The observer does
not publish a future target before Activity commit.

## Repeatable isolated comparison pattern

When a consumer wants to compare `Wait Visible` and `Wait Covered` using the **same Activity-owned preparation scene**, use a neutral baseline Activity between test entries if each test is expected to exercise a fresh materialization.

A valid pattern is:

```text
Baseline
  Observe Only
  no ActivityContentProfile

Baseline -> Wait Visible -> Baseline
Baseline -> Wait Covered -> Baseline
```

The waiting Activities may share one `ActivityContentProfile` when the previous waiting Activity is fully exited and its Activity-owned scene is released before the next test begins.

This prevents a demonstration from accidentally becoming:

```text
Wait Visible -> Wait Covered
  shared scene already loaded
  -> no fresh scene-load work
  -> Loading evidence no longer represents the intended comparison
```

The baseline is not a hidden readiness reset API. It is an ordinary Activity request whose authored content boundary causes the previous Activity-owned scene to release normally.

A Route-owned menu may use `ActivityVisibilityRule` to show only the controls valid for the current demonstration state:

```text
Baseline active
  show Wait Visible / Wait Covered requests

waiting Activity active
  hide direct cross-policy requests
  show return-to-Baseline request
```

`ActivityVisibilityRule` controls presentation only. `ActivityRequestTrigger` / Game Flow remains the request path and `ActivityFlowRuntime` remains readiness authority. It does not make the Route-owned menu Activity-owned.

## Advanced and runtime diagnostics

Useful evidence includes:

```text
activityReadiness
activityReadinessReason
occurrence transition sequence
Activity transition terminal phase
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
Activity RuntimeContent owner
Player contextual admission evidence
Session physical preparation evidence
activityContentExecution
activityContentExecutionEnter
activityContentEnterDiagnostic
```

`activityContentEnterDiagnostic` is the canonical detailed Activity Content Enter projection used by Boot/Route/Activity request diagnostics. It preserves the participant result details needed to distinguish typed lifecycle failure from an exception thrown by a participant.

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
→ Required participants complete
→ Loading reaches 100 and hides
→ reveal completes
→ exit to another Activity
→ tracked participants release
→ Activity-owned readiness content releases when authored to release on change
→ reenter Wait Covered
→ a fresh occurrence starts
→ the same contributions complete again
```

Old occurrence updates must not advance the replacement occurrence.

## Related architecture

- [IF-ADR-007 — Activity entry readiness and reveal gating](../Architecture/ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md)
- [IF-ADR-011 — Participant-aware Activity readiness/loading progress](../Architecture/ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md)
- [IF-ADR-012 — Activity Player participation and readiness compatibility](../Architecture/ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md)
## Current limits

```text
no participant weights
no continuous percentage from one participant
no automatic timeout
no automatic retry or rollback
no post-release automatic re-gating
inactive participant components remain discoverable in explicit scope
```
