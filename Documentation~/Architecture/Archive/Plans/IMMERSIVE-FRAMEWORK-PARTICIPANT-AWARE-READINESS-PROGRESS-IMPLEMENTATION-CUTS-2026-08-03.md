# Immersive Framework — Participant-Aware Activity Readiness Loading Progress
## Implementation Cuts

**Date:** 2026-08-03  
**Status:** Proposed implementation sequence  
**Normative decision:** `IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress`  
**Repositories:** `com.immersive.framework`, `QAFramework`, `planet-devourer`

## Audited baselines

```text
com.immersive.framework
  f5620efa8ddd1046e6ecb7f3194a2ee562db6dd5

QAFramework
  3a2daf56518f5dc84890466bcce28dc3d981d65c

planet-devourer
  0311cda65d4730d8c613ef99c5ee04f044d893c2
```

Operational rule:

```text
repositories remain read-only;
each implementation is delivered as a ZIP;
each ZIP contains created, edited and removed files;
each ZIP includes CHANGESET.md, source SHAs, validation steps and limitations.
```

---

# Executive sequence

```text
IF-DOC-READY-PROGRESS-00
  Canonicalize IF-ADR-011 and reconcile IF-ADR-007
    ↓
IF-READY-PROGRESS-01
  Required/Optional completion evidence
    ↓
IF-READY-PROGRESS-02
  Loading progress envelope contracts
    ↓
IF-READY-PROGRESS-03
  WaitCovered runtime integration
    ↓
QA-READY-PROGRESS-01
  Positive participant progression and ordering
    ↓
QA-READY-PROGRESS-02
  Failure, invalidation and startup parity
    ↓
FIRSTGAME-M03-READY-PROGRESS-01
  WaitCovered product demonstration
    ↓
IF-DOC-READY-PROGRESS-01
  Usage and diagnostic closure
```

`M07` remains paused until the M03 WaitCovered proof is complete.

---

# Cut 0 — IF-DOC-READY-PROGRESS-00
## Canonical ADR integration

### Type

```text
documentation / architecture
```

### Objective

Integrate the accepted participant-aware Loading decision into the package without an ADR number collision and explicitly amend the previous IF-ADR-007 Loading-progress rule.

### Scope

```text
rename the decision to IF-ADR-011;
preserve IF-ADR-009 Activity Local Visibility Rules;
preserve IF-ADR-010 Editor and Inspector Product Surface Authority;
state that IF-ADR-011 refines IF-ADR-007 only for:
  WaitCovered + FadeWithLoading + determinate progress;
replace the old “participant counts must not produce percentage” rule
with the participant-aware reserved-range rule;
record that implementation is still pending.
```

### Out of scope

```text
runtime code;
QA;
FIRSTGAME assets;
Loading prefab changes;
public API.
```

### Files created

```text
Documentation~/Architecture/ADRs/
  IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md
```

### Files altered

```text
Documentation~/Architecture/ADRs/
  IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md
```

If the current package contains an ADR listing outside this folder, include its exact existing index file in the ZIP after inspecting the source SHA. Do not invent a new parallel index.

### Files removed

```text
none
```

### Product surface affected

```text
architecture documentation only
```

### Expected flow

```text
reader opens IF-ADR-007
→ sees the general readiness/reveal policy
→ follows IF-ADR-011 for determinate WaitCovered progress
→ sees that participant progress is framework-owned
→ sees that implementation is not yet claimed complete
```

### Technical smoke

```text
Markdown links resolve;
no duplicate IF-ADR number;
no contradictory Loading-progress statements remain;
package contents unchanged outside documentation.
```

### Technical acceptance

```text
IF-ADR-009 remains Activity Local Visibility Rules;
IF-ADR-010 remains unchanged;
new decision is IF-ADR-011;
IF-ADR-007 references IF-ADR-011;
no runtime claim exceeds implementation.
```

### Product acceptance

```text
a developer can identify the normative decision;
Required and Optional denominator rules are unambiguous;
WaitVisible and ObserveOnly behavior remain unchanged.
```

### Architectural gain

```text
one canonical decision;
no conflicting ADR identity;
clear refinement boundary over IF-ADR-007.
```

### Usability gain

```text
future implementation and documentation use one vocabulary and one progress formula.
```

### Suggested commit

```text
docs(adr): freeze participant-aware readiness loading progress
```

---

# Cut 1 — IF-READY-PROGRESS-01
## Required/Optional completion evidence

### Type

```text
technical / package contract
```

### Objective

Expose the occurrence-scoped counts required to calculate participant-aware progress without changing Loading behavior.

### Scope

Add explicit evidence for:

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
```

Preserve the frozen participant set and occurrence matching.

Introduce a compact immutable progress projection:

```text
ActivityReadinessProgressSnapshot
  occurrence
  required total
  required completed
  required pending
  required failed
  required released
  optional diagnostic counts
  readiness ratio
  aggregate Ready
  terminal failure
```

The snapshot is evidence. It does not update Loading.

### Out of scope

```text
Loading ranges;
GameFlow orchestration;
presentation;
public API;
participant weights;
continuous progress inside one participant.
```

### Files created

```text
Runtime/ActivityFlow/
  ActivityReadinessProgressSnapshot.cs
```

### Files altered

```text
Runtime/ActivityFlow/
  ActivityReadinessOccurrenceState.cs
  ActivityReadinessState.cs
  ActivityReadinessRecomposer.cs
```

Alter `ActivityReadinessUpdate.cs` only if its current diagnostic projection duplicates counts instead of carrying `ActivityReadinessState`.

### Files removed

```text
none
```

### Product surface affected

```text
Advanced / Debug and internal diagnostics only;
no Inspector authoring change.
```

### Expected flow

```text
occurrence captures participants
→ Required/Optional identities and requiredness remain frozen
→ participant changes state
→ occurrence recomputes separated counts
→ aggregate readiness is recomposed
→ progress snapshot can be created deterministically
```

### Technical smoke

```text
package compiles;
existing ObserveOnly, WaitVisible and WaitCovered behavior is unchanged;
existing QA Foundation 20 passes;
QA-01 18 passes;
QA-02 26 passes;
QA-03 42 passes.
```

### Technical acceptance

```text
RequiredCompletedCount never includes Optional;
Optional completion never changes Required ratio;
released Required is terminal evidence;
count invariants are validated;
no scene scan, polling or global lookup;
reentry creates a new snapshot.
```

### Product acceptance

```text
diagnostics can explain “3 of 4 Required completed”;
Optional remains visibly diagnostic without appearing blocking.
```

### Architectural gain

```text
Loading integration consumes a typed readiness projection instead of reverse-engineering aggregate counts.
```

### Usability gain

```text
technical debug can explain readiness progress in product terms.
```

### Suggested commit

```text
feat(activity-flow): expose required readiness completion evidence
```

---

# Cut 2 — IF-READY-PROGRESS-02
## Loading progress envelope contracts

### Type

```text
technical / package foundation
```

### Objective

Create a reusable, scoped and monotonic progress envelope capable of reserving a final readiness range, without wiring it into requests yet.

### Scope

Introduce internal contracts equivalent to:

```text
ActivityEntryLoadingProgressPlan
  technical step count
  readiness phase unit count
  technical range
  readiness range
  applicability reason

ActivityEntryLoadingProgressEnvelope
  root reporter
  technical reporter
  last accepted progress
  report readiness snapshot
  terminal completion issued
  terminal failure observed
```

Rules:

```text
technical range = technicalSteps / (technicalSteps + readinessUnit);
readiness range occupies the final unit;
technical reporter cannot reach 1 when readiness is reserved;
readiness increments are equal by Required participant;
1.0 requires aggregate Ready;
progress is monotonic;
Failed / Released / Invalidated / Cancelled never produce 1.0;
non-applicable operations use a no-op or normal technical reporter.
```

### Out of scope

```text
GameFlow method changes;
Activity/Route startup wiring;
Loading UI;
QA scene fixtures;
FIRSTGAME.
```

### Files created

```text
Runtime/Loading/
  ActivityEntryLoadingProgressPlan.cs
  ActivityEntryLoadingProgressEnvelope.cs
```

Add a small range value type only if it is not already represented by an existing Loading range contract:

```text
Runtime/Loading/
  FrameworkLoadingProgressRange.cs
```

### Files altered

```text
none, unless the existing reporter interface requires a narrow extension
```

Do not broaden `ILoadingSurfaceAdapter`. The envelope wraps the existing `IFrameworkLoadingProgressReporter`.

### Files removed

```text
none
```

### Product surface affected

```text
none
```

### Expected flow

```text
caller creates a plan before execution
→ technical work reports through mapped technical reporter
→ technical completion stops at readiness-range start
→ readiness snapshots advance inside the final range
→ Ready may publish 100%
```

### Technical smoke

```text
package compiles;
pure contract tests or deterministic assertions cover mapping;
existing runtime behavior remains byte-for-byte equivalent because no request path is wired.
```

### Technical acceptance

```text
zero technical steps is valid;
zero Required participants requires aggregate Ready;
ratio is clamped and finite;
regression cannot lower progress;
duplicate snapshot does not emit duplicate update;
terminal 100% is idempotent;
no timer or frame dependency.
```

### Product acceptance

```text
not applicable; foundation is intentionally internal.
```

### Architectural gain

```text
one scoped progress authority;
no Loading-surface knowledge in Activity content;
no duplicated percentage formula across request paths.
```

### Usability gain

```text
none directly; enables coherent Loading presentation in the next cut.
```

### Suggested commit

```text
feat(loading): add activity entry progress envelope
```

---

# Cut 3 — IF-READY-PROGRESS-03
## WaitCovered runtime integration

### Type

```text
technical / package runtime
```

### Objective

Wire participant-aware progress into every initially accepted WaitCovered entry path and enforce final ordering.

### Scope

Operation coverage:

```text
direct Activity request;
Route request with Startup Activity;
Game Application startup with Startup Activity.
```

Runtime behavior:

```text
detect WaitCovered + visible progress-capable Loading;
calculate technical step count before execution;
create one operation-scoped envelope;
pass only the mapped technical reporter into lifecycle work;
capture the target occurrence returned by materialization;
forward typed readiness updates to the readiness range;
publish 100% only after Ready;
publish 100% before Loading Hide;
Hide before Transition After/reveal;
release gate only after successful Ready;
retain explicit failure semantics without fabricated 100%.
```

Preserve:

```text
ObserveOnly;
WaitVisible;
WaitCovered + Fade without Loading percentage;
Optional non-blocking behavior;
current committed-destination failure and recovery gate semantics.
```

### Out of scope

```text
participant weights;
continuous per-participant progress;
retry UI;
new authoring fields;
FIRSTGAME.
```

### Files created

```text
none expected beyond Cut 2 contracts
```

### Files altered

```text
Runtime/ApplicationLifecycle/
  FrameworkRuntimeHost.cs

Runtime/GameFlow/
  GameFlowRuntime.cs

Runtime/ActivityFlow/
  ActivityFlowRuntime.cs
  ActivityFlowStartResult.cs

Runtime/RouteLifecycle/
  RouteLifecycleRuntime.cs
  RouteLifecycleStartResult.cs
```

Alter the existing Loading diagnostics/result file that owns `FrameworkLoadingDiagnostics` so it records:

```text
technical range end;
readiness range start/end;
required total/completed/pending/failed;
last progress;
100% issued;
Loading hidden;
reveal completed.
```

Do not create a second diagnostics authority.

### Files removed

```text
none
```

### Product surface affected

```text
Loading presentation behavior for WaitCovered + FadeWithLoading;
Advanced / Debug request diagnostics.
```

### Expected flow

```text
request begins
→ Loading Show at 0%
→ technical lifecycle advances below 100%
→ target occurrence becomes available
→ 0/N Required at readiness-range start
→ each Required completion advances Loading
→ aggregate Ready
→ Loading Update 100%
→ Loading Hide
→ Transition After
→ reveal
→ capability gate release
→ request success
```

### Technical smoke

```text
package compiles;
existing QA exact counts remain passing;
WaitVisible still reveals before Ready;
ObserveOnly remains unchanged;
WaitCovered + Fade still works without determinate Loading;
host-owned surfaces finish hidden on success.
```

### Technical acceptance

```text
all three entry paths use the same envelope contract;
technical progress never reaches 1 before Ready when applicable;
Optional updates do not alter percentage;
Required failure/release/invalidation/cancellation does not emit 1;
old occurrence cannot advance a replacement;
final update precedes Hide;
Hide precedes reveal completion;
no fallback or polling.
```

### Product acceptance

```text
a progress-capable Loading surface never displays 100% while the covered Activity remains Preparing.
```

### Architectural gain

```text
Loading remains presentation;
ActivityFlow remains readiness authority;
GameFlow remains ordering authority;
FrameworkRuntimeHost remains surface adapter.
```

### Usability gain

```text
100% regains a reliable product meaning: covered target Activity is ready to reveal.
```

### Suggested commit

```text
feat(game-flow): project required readiness into loading progress
```

---

# Cut 4 — QA-READY-PROGRESS-01
## Positive progression and ordering

### Type

```text
technical QA
```

### Objective

Prove participant-granular determinate progress and presentation ordering through the official host-owned path.

### Scope

Fixture:

```text
WaitCovered;
FadeWithLoading;
InputInteractionAndGameplay gate;
4 Required participants;
1 Optional participant;
host-owned Transition and Loading surfaces;
progress-capable Loading adapter.
```

Evidence:

```text
technical completion remains below 100%;
0/4 at readiness range start;
1/4 first increment;
2/4 second increment;
3/4 third increment;
4/4 + aggregate Ready = 100%;
Optional pending does not alter progress;
Optional failed does not alter progress;
100% precedes Hide;
Hide precedes reveal;
gate releases after Ready;
request succeeds;
cleanup restores initial authority.
```

### Out of scope

```text
failure paths;
Route startup;
Game Application startup;
FIRSTGAME visuals.
```

### Files created

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaParticipantAwareReadinessLoadingProgressRegression.cs

Assets/ImmersiveFrameworkQA/Documentation/
  QA-READY-PROGRESS-01-2026-08-03.md
```

Create a dedicated fixture helper only if the runner would otherwise duplicate participant setup:

```text
Assets/ImmersiveFrameworkQA/GameFlow/Internal/
  QaParticipantAwareReadinessFixture.cs
```

### Files altered

Reuse without weakening:

```text
QaOwnedAsyncOperation<TResult>
QaEvidenceCheckpoint
QaCausalSignalJoin<TFirst,TSecond>
QaLoadingPresentationEvidenceGrammar
QaFailureCollector
QaCaseRegistry
```

Alter `QaLoadingPresentationEvidenceGrammar.cs` only if its current variable-update grammar cannot express exact normalized progression and ordering.

Do not change QA-03's exact 42-case baseline.

### Files removed

```text
none
```

### Product surface affected

```text
QA menu only
```

### Expected smoke

```text
Run Participant-Aware Readiness Loading Progress Regression
→ exact case count passes
→ original authority restored
→ surfaces hidden
→ no stale fixture or gate blocker
```

### Technical acceptance

```text
typed progress evidence;
strictly increasing evidence sequence;
exact normalized values within tolerance;
no Task.Delay;
no timeout;
no frame polling;
no log parsing;
no global object lookup.
```

### Product acceptance

```text
not applicable; QA proves contract, not consumer UX.
```

### Architectural gain

```text
the central positive contract is protected independently of the broader policy regression.
```

### Usability gain

```text
none directly.
```

### Suggested commit

```text
test(qa): prove participant-aware readiness loading progress
```

---

# Cut 5 — QA-READY-PROGRESS-02
## Terminal paths and startup parity

### Type

```text
technical QA / negative regression
```

### Objective

Prove that failure states never fabricate completion and that direct Activity, Route Startup Activity and Game Application Startup Activity share the accepted semantics.

### Scope

Negative cases:

```text
Required participant fails;
Required participant is released before completion;
occurrence invalidated by replacement;
wait cancelled through owned operation unwind;
late completion from old occurrence;
duplicate terminal observation.
```

Required evidence:

```text
last progress < 1;
no 100% update;
typed terminal result;
destination authority evidence preserved;
recovery gate ownership preserved;
Loading/Transition terminal state diagnostic;
cleanup restores safe baseline.
```

Parity cases:

```text
direct Activity request;
Route request with WaitCovered Startup Activity;
Game Application startup with WaitCovered Startup Activity.
```

### Out of scope

```text
FIRSTGAME;
retry UI;
automatic timeout;
continuous participant progress.
```

### Files created

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaParticipantAwareReadinessLoadingTerminalRegression.cs

Assets/ImmersiveFrameworkQA/Documentation/
  QA-READY-PROGRESS-02-2026-08-03.md
```

### Files altered

```text
shared QA fixture/helper only when required for parity setup
```

### Files removed

```text
none
```

### Product surface affected

```text
QA menu only
```

### Expected smoke

```text
all negative branches terminate explicitly;
no branch publishes successful 100%;
all parity paths pass the same positive ordering;
final restore passes.
```

### Technical acceptance

```text
no silent success;
no stuck hidden in-flight Task;
no stale gate owner;
no old occurrence release;
no leaked scene or participant;
all original readiness regressions remain passing.
```

### Product acceptance

```text
not applicable.
```

### Architectural gain

```text
failure and startup paths cannot drift from the direct Activity implementation.
```

### Usability gain

```text
failures become diagnosable instead of presenting misleading 100%.
```

### Suggested commit

```text
test(qa): cover readiness progress terminals and startup parity
```

---

# Cut 6 — FIRSTGAME-M03-READY-PROGRESS-01
## WaitCovered product demonstration

### Type

```text
UX/product + real integration
```

### Objective

Complete the Demo 01 Activity Readiness model with a production-like WaitCovered case that teaches participant-aware Loading progress.

### Scope

Preserve:

```text
Observe Only;
Wait Visible;
Intermission;
shared Activity Readiness Scenario prefab.
```

Add:

```text
Wait Covered Activity;
FadeWithLoading;
InputInteractionAndGameplay gate;
progress-capable persistent Loading surface;
4 independent Required readiness participants;
1 Optional participant kept pending;
menu button;
Build Settings entry;
short usage explanation.
```

The framework counts participants, not chickens.

Consumer mapping:

```text
Chicken 01 reaches Target 01
  -> Required participant 01 completes

Chicken 02 reaches Target 02
  -> Required participant 02 completes

Chicken 03 reaches Target 03
  -> Required participant 03 completes

Chicken 04 reaches Target 04
  -> Required participant 04 completes
```

The same scenario remains valid in ObserveOnly and WaitVisible.

### Out of scope

```text
M07;
M08;
package runtime changes;
custom participant weights;
direct access from Activity scripts to Loading;
new global manager.
```

### Files created

```text
Assets/_Project/Demo 01 - Routes and Activities/
  Data/Activity Readiness/Activities/
    ActivityReadiness_WaitCovered.asset
    ActivityReadiness_WaitCovered.asset.meta

  Data/Activity Readiness/Activities/Profiles/
    ActivityContent_ReadinessWaitCovered.asset
    ActivityContent_ReadinessWaitCovered.asset.meta

  Scenes/Activity Readiness/ActivitiesContent/
    Activity_Readiness_WaitCovered.unity
    Activity_Readiness_WaitCovered.unity.meta
```

Create one game-owned bridge component only if the existing area component cannot complete one participant per subject cleanly:

```text
Assets/_Project/Demo 01 - Routes and Activities/Scripts/Activity Readiness/
  ReadinessSubjectParticipantBridge.cs
  ReadinessSubjectParticipantBridge.cs.meta
```

The bridge must observe one explicit subject/target condition and call only its assigned `ActivityReadinessParticipant`.

### Files altered

```text
Assets/_Project/Demo 01 - Routes and Activities/
  Prefabs/Activity Readiness/Activity Readiness Scenario.prefab
  Prefabs/Activity Readiness/Ui/Canvas_ActivityReadinessNavigation.prefab

ProjectSettings/
  EditorBuildSettings.asset
```

Alter the existing persistent presentation prefab or scene only after auditing the current `Persistent Presentation` hierarchy. The final scene must contain exactly the intended Loading adapter set; do not add a duplicate Loading authority.

Likely affected existing composition:

```text
Assets/_Project/Scenes/Shared/Shared_PersistentContent.unity
and/or its referenced Persistent Presentation prefab
```

The exact existing prefab path must be recorded in the ZIP `CHANGESET.md` after source inspection.

### Files removed

```text
none expected
```

### Product surface affected

```text
Demo 01 > Activity Readiness;
persistent Loading presentation;
Activity Readiness navigation.
```

### Expected flow

```text
enter Activity Readiness Route
→ Observe Only demonstrates observational readiness
→ Wait Visible demonstrates visible preparation with retained gate
→ Intermission clears the occurrence
→ Wait Covered starts FadeWithLoading
→ technical phase stops below 100%
→ each Required chicken participant advances the final range
→ Optional stays pending without changing percentage
→ fourth Required completes
→ Activity becomes Ready
→ Loading reaches 100%
→ Loading hides
→ cover reveals the completed scenario
→ Intermission unloads it
→ reentry starts from 0 with a new occurrence.
```

### Technical smoke

```text
no blocking validation issue;
one WaitCovered scene instance;
4 Required + 1 Optional captured;
monotonic Loading updates;
no Missing Script/reference;
Intermission unloads owned scene;
reentry creates a clean occurrence;
no stale participant or gate.
```

### Technical acceptance

```text
WaitCovered uses FadeWithLoading;
gate is InputInteractionAndGameplay;
Activity scripts never resolve Loading;
100% occurs only after Ready;
Optional does not affect denominator;
no duplicate Loading adapter.
```

### Product acceptance

```text
developer can compare the three policies in one Route;
Loading progression visibly follows Required participant completion;
the scene is revealed only when complete;
panel explains policy and participant counts;
the demonstration remains manually authorable and inspectable.
```

### Architectural gain

```text
FIRSTGAME consumes the official package contract without becoming its implementation authority.
```

### Usability gain

```text
the developer sees exactly how multiple Required participants produce readiness progress.
```

### Suggested commit

```text
feat(firstgame): demonstrate participant-aware wait-covered readiness
```

---

# Cut 7 — IF-DOC-READY-PROGRESS-01
## Usage and diagnostics closure

### Type

```text
documentation / product closure
```

### Objective

Document the completed feature only after package, QA and FIRSTGAME evidence pass.

### Scope

Document:

```text
when to use ObserveOnly;
when to use WaitVisible;
when to use WaitCovered;
Fade versus FadeWithLoading;
Required denominator;
Optional diagnostics;
one aggregate participant versus several independent participants;
failure behavior;
Advanced / Debug evidence;
FIRSTGAME reference path.
```

Update IF-ADR-011 Current implementation coverage from pending to implemented with exact commit evidence.

### Out of scope

```text
new runtime behavior;
new authoring fields;
new sample.
```

### Files altered

At minimum:

```text
Documentation~/Architecture/ADRs/
  IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md
  IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md
```

Update the current usage guide that owns Activity authoring after locating its canonical path in the package. Do not add a second competing guide.

### Files created

```text
none expected
```

### Files removed

```text
none
```

### Product surface affected

```text
package documentation
```

### Expected flow

```text
developer reads Activity readiness usage
→ chooses policy
→ understands Loading requirements
→ authors Required/Optional participants
→ validates configuration
→ uses Advanced / Debug when needed
→ opens FIRSTGAME M03 as reference.
```

### Technical smoke

```text
all documented menu names, asset fields and sample paths exist;
no claim is based only on QA;
no obsolete “indeterminate only” statement remains.
```

### Technical acceptance

```text
docs match runtime and QA evidence;
FIRSTGAME path is current;
failure semantics are explicit.
```

### Product acceptance

```text
a new framework consumer can build the feature without reverse-engineering internal contracts.
```

### Architectural gain

```text
ADR, runtime, QA and consumer proof close around the same terminology.
```

### Usability gain

```text
the capability becomes teachable as a product feature rather than a hidden runtime behavior.
```

### Suggested commit

```text
docs(readiness): document participant-aware wait-covered progress
```

---

# Package acceptance matrix

| Contract | Cut that introduces it | Cut that proves it |
|---|---|---|
| Required/Optional completed counts | IF-READY-PROGRESS-01 | QA-READY-PROGRESS-01 |
| Frozen occurrence denominator | existing + IF-READY-PROGRESS-01 diagnostics | QA-READY-PROGRESS-01/02 |
| Stable reserved readiness range | IF-READY-PROGRESS-02 | QA-READY-PROGRESS-01 |
| Direct Activity integration | IF-READY-PROGRESS-03 | QA-READY-PROGRESS-01 |
| Route Startup parity | IF-READY-PROGRESS-03 | QA-READY-PROGRESS-02 |
| Game Application Startup parity | IF-READY-PROGRESS-03 | QA-READY-PROGRESS-02 |
| No 100% on terminal failure | IF-READY-PROGRESS-03 | QA-READY-PROGRESS-02 |
| 100% before Hide/reveal | IF-READY-PROGRESS-03 | QA-READY-PROGRESS-01 |
| Real consumer UX | FIRSTGAME-M03-READY-PROGRESS-01 | FIRSTGAME Play Mode |
| Final documentation | IF-DOC-READY-PROGRESS-01 | manual documentation audit |

---

# Frozen exclusions for the program

```text
no participant weights;
no continuous percentage supplied by one participant;
no Optional progress contribution;
no time-based progress;
no scene polling;
no Loading lookup from Activity content;
no new singleton or service locator;
no silent gate strengthening;
no automatic timeout;
no retry authoring;
no M07 or M08 work inside these ZIPs.
```

---

# First implementation ZIP

The first code ZIP should be:

```text
IF-READY-PROGRESS-01-readiness-completion-evidence.zip
```

It contains only:

```text
Runtime/ActivityFlow/
  ActivityReadinessProgressSnapshot.cs
  ActivityReadinessOccurrenceState.cs
  ActivityReadinessState.cs
  ActivityReadinessRecomposer.cs

CHANGESET.md
```

It must not contain:

```text
GameFlowRuntime changes;
FrameworkRuntimeHost changes;
Loading surface changes;
QA changes;
FIRSTGAME changes.
```

This is the smallest causal foundation for every later cut.
