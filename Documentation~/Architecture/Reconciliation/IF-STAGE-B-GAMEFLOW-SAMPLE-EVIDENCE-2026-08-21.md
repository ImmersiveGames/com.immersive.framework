# IF Stage B — Game Flow Sample Consumer Evidence — 2026-08-21

Status: **RECORDED — scoped Stage B consumer evidence; Game Flow positive consumer proof closed 2026-08-22**  
Consumer repository: `ImmersiveGames/planet-devourer`  
Consumer baseline: `34c01be29daaef62a428547b07e9818d0f8c4a41` (`Readiness Separate`)  
Demonstration: `Assets/_Sample/GameFlow/GameFlowShowcase/`  
Related decisions: IF-ADR-006, IF-ADR-007, IF-ADR-008, IF-ADR-009, IF-ADR-011, IF-ADR-013

## Purpose

Record real-consumer Play Mode evidence produced by the Game Flow Showcase without relabeling technical QA or claiming contracts the Sample has not exercised.

This record is Stage B consumer evidence. It does not replace Stage A certification records and it does not make FIRSTGAME/Sample an exceptional-path laboratory.

The consumer lane proves valid authored gameplay behavior and product usability. Negative, invalid, interrupted and terminal-failure robustness remains technical QA responsibility unless a future game feature intentionally exposes a player-facing recovery flow as a product behavior.

## Current consumer topology

```text
GameApplication_GameFlow
  Persistent Content -> SCN_GameFlow_Persistence
  Startup Route -> Route_Hub

Route_Hub
  no Startup Activity
  BGM -> explicit Silence

HUB topics
  -> Route_BasicFlow
  -> Route_ReadinessShowcase
```

Persistent presentation is explicit consumer composition:

```text
SCN_GameFlow_Persistence
  UnityFadeCurtainEffectAdapter
  UnityLoadingSurfaceAdapter
```

### Basic Flow

```text
Route_BasicFlow
  Primary Scene -> SCN_GameFlow_Basic
  Startup Activity -> Activity_Basic_A

Activity_Basic_A
  ActivityContentProfile -> ActivityContent_Basic_A
  Activity-owned scene -> SCN_GameFlow_Basic_A
  Visual Transition -> Seamless
  BGM -> BGM_Floresta

Activity_Basic_B
  ActivityContentProfile -> ActivityContent_Basic_B
  Activity-owned scene -> SCN_GameFlow_Basic_B
  Visual Transition -> Seamless
  BGM -> BGM_Gelo

Activity_Basic_C
  ActivityContentProfile -> None
  Activity-owned scene -> None
  Visual Transition -> Fade
  no new BGM intent
```

### Readiness Showcase

```text
Route_ReadinessShowcase
  Primary Scene -> SCN_GameFlow_Basic_Readiness
  Startup Activity -> Activity_Basic_C

Activity_Basic_C
  Observe Only
  no ActivityContentProfile
  no Activity-owned scene
  role in this Route -> neutral readiness baseline

Activity_Basic_D
  ActivityContentProfile -> ActivityContentReadiness
  Activity-owned scene -> SCN_GameFlow_Content_Readiness
  Entry Readiness -> Wait Visible
  Visual Transition -> Fade With Loading
  Gate -> Input Interaction And Gameplay

Activity_Basic_E
  ActivityContentProfile -> ActivityContentReadiness
  Activity-owned scene -> SCN_GameFlow_Content_Readiness
  Entry Readiness -> Wait Covered
  Visual Transition -> Fade With Loading
  Gate -> Input Interaction And Gameplay

SCN_GameFlow_Content_Readiness
  sample preparation controller
  one Required ActivityReadinessParticipant
```

D and E intentionally share the same preparation content. The variable under demonstration is the entry-readiness policy.

The readiness Activity content is authored to release on Activity change. The Route-owned menu uses `ActivityContentBinding` only to expose valid sample controls:

```text
C active
  D / E request controls visible
  return-to-C control hidden

D or E active
  D / E request controls hidden
  return-to-C control visible
```

The return control is a normal `ActivityRequestTrigger` targeting C. It is not Reset/Restart authority.

Canonical readiness cycles:

```text
C -> D -> C
C -> E -> C
```

This prevents direct D/E cross-entry from reusing an already-loaded shared Activity scene when the purpose is to compare fresh Loading/readiness behavior.

## Persistent Content / presentation resolution

Observed boot evidence:

```text
Persistent Content loaded
scene='SCN_GameFlow_Persistence'
transitionAdapterCount='1'
loadingAdapterCount='1'

Loading surface resolved
adapterCount='1'

Transition surface resolved
adapterCount='1'
```

This is consumer evidence that optional Transition and Loading presentation can be explicitly composed under the accepted Persistent Content lifetime rather than created through hidden runtime fallback.

## Route Transition + Loading proof

Observed Route requests complete with the persistent presentation surface:

```text
transition='SucceededWithUnitySurface'
loading='SucceededWithUnitySurface'
blockingIssues='0'
```

For `Route_Hub -> Route_BasicFlow`, destination Activity A settles `Ready`. For `Route_Hub -> Route_ReadinessShowcase`, destination Activity C settles `Ready` with no Activity scene composition.

The baseline Route presentation envelope remains:

```text
Fade cover
  -> Route lifecycle / scene composition
  -> Loading presentation during Route work
  -> destination Activity Ready when one exists
  -> Fade reveal
```

The sample also exercises reverse Route switches to the HUB.

## Basic Activity presentation proof

### Seamless A <-> B

A/B use `ActivityVisualTransitionMode.Seamless`.

Observed Activity Request evidence includes:

```text
transition='SkippedByActivityPolicy'
loadingPresentation='SkippedByActivityPolicy'
activitySceneComposition='Succeeded'
activityScenesLoaded='1'
activitySceneRelease='Succeeded'
activityScenesReleased='1'
blockingIssues='0'
```

Therefore Activity-owned scene load/release is independent from requiring visual cover: scene side effects occur while presentation remains intentionally Seamless.

### Fade A/B -> C

C uses `ActivityVisualTransitionMode.Fade`.

Observed target-C evidence includes:

```text
currentActivity='Basic Flow C'
activityReadiness='Ready'
transition='SucceededWithUnitySurface'
loadingPresentation='SkippedByActivityPolicy'
activitySceneComposition='NotRequested'
activityScenesLoaded='0'
activitySceneRelease='Succeeded'
activityScenesReleased='1'
blockingIssues='0'
```

This closes the baseline distinction:

```text
Activity target Seamless
  -> no visual Transition

Activity target Fade
  -> Fade cover/reveal
  -> no canonical Loading presentation
```

## Content-less Activity / negative visibility proof

Activity C intentionally has no `ActivityContentProfile` and no Activity-owned scene.

Observed result while C is active:

```text
activitySceneComposition='NotRequested'
activitySceneCompositionProfile=''
activitySceneCompositionScenes='0'
activityReadiness='Ready'
blockingIssues='0'
```

In Basic Flow, Activity-local visibility diagnostics show A/B-scoped objects deactivated under C. Therefore C is a valid active Activity without owned content and A/B content does not leak into it.

In Readiness Showcase, the same content-less Activity is a neutral baseline between readiness tests. This reuse does not create new readiness authority; it uses ordinary Activity lifecycle to release the previous Activity-owned readiness scene before the next test.

## Readiness success-path consumer proof

### Route entry baseline

Observed `Route_Hub -> Route_ReadinessShowcase` result:

```text
currentRoute='Readiness Showcase'
scene='SCN_GameFlow_Basic_Readiness'
currentActivity='Basic Flow C'
activitySceneComposition='NotRequested'
activityReadiness='Ready'
blockingIssues='0'
```

This proves the Route enters a neutral baseline rather than automatically executing D or E.

### Wait Visible — C -> D

D uses:

```text
Entry Readiness = Wait Visible
Visual Transition = Fade With Loading
Gate = Input Interaction And Gameplay
```

Observed consumer evidence:

```text
activitySceneComposition='Succeeded'
activitySceneCompositionLoaded='1'
activitySceneCompositionAlreadyLoaded='0'
activityTransition terminal='CommittedNotReady'
final activityReadiness='Ready'
transition='SucceededWithUnitySurface'
loadingPresentation='SucceededWithUnitySurface'
blockingIssues='0'
```

The target commits while its Required readiness contribution is still preparing, is revealed according to `Wait Visible`, then settles to `Ready` before the capability gate is released and the request completes.

The final Loading progress phase is the Activity transition rather than a readiness-held covered phase, which is expected for `Wait Visible`.

### D -> C release / neutralization

Observed return to C:

```text
SceneReleasing
  scene='SCN_GameFlow_Content_Readiness'
  reason='scene-unload'

activitySceneRelease='Succeeded'
activityScenesReleased='1'
activitySceneComposition='NotRequested'
currentActivity='Basic Flow C'
activityReadiness='Ready'
blockingIssues='0'
```

This is the reset boundary for the demonstration: it is normal Activity replacement and Activity-scene release, not a Framework Reset API.

### Wait Covered — C -> E

E uses:

```text
Entry Readiness = Wait Covered
Visual Transition = Fade With Loading
Gate = Input Interaction And Gameplay
```

Observed consumer evidence:

```text
activitySceneComposition='Succeeded'
activitySceneCompositionLoaded='1'
activitySceneCompositionAlreadyLoaded='0'
activityTransition terminal='CommittedNotReady'
final activityReadiness='Ready'
transition='SucceededWithUnitySurface'
loadingPresentation='SucceededWithUnitySurface'
loadingProgressSupported='True'
loadingProgressMode='Determinate'
loadingProgressPhase='ActivityReadiness'
Required completed='1'
Required total='1'
Required pending='0'
blockingIssues='0'
```

This is direct consumer proof that the Loading operation remains governed by Activity readiness until the Required participant completes, then reaches terminal progress and permits reveal.

### Reentry / fresh occurrence

The same runtime path is repeatable:

```text
D -> C -> D
E -> C -> E
```

Returning to C releases the shared readiness Activity scene. Reentry therefore loads `SCN_GameFlow_Content_Readiness` again instead of taking an `AlreadyLoaded` path and starts a fresh readiness occurrence.

The temporary occurrence trace used during diagnosis also showed consistent Create -> Publish -> Read identity on successful waiting entries. The earlier `InitialOccurrenceUnavailable` symptom was traced to invalid Sample authoring, not an occurrence propagation defect in the package runtime.

No package behavior correction was required for that investigation.

## BGM real-consumer proof

The Basic Flow sample exercises explicit Play, no-request Preserve and explicit Silence under real Route/Activity and scene lifetime.

Explicit Play path:

```text
Route_Hub
  -> explicit Silence

Activity A
  -> BGM_Floresta Applied

Activity B
  -> BGM_Gelo Applied
```

No-request preservation through content-less Activity C:

```text
A -> C
  Activity A owner exit
  confirmedBgm='BGM_Floresta'
  no explicit provider intent from C
  confirmed presentation preserved

B -> C
  Activity B owner exit
  confirmedBgm='BGM_Gelo'
  no explicit provider intent from C
  confirmed presentation preserved
```

Observed preservation diagnostic:

```text
BGM presentation preserved because no explicit provider intent exists.
reason='Activity owner exit does not mutate confirmed BGM.'
```

Returning to HUB proves explicit destination Silence:

```text
operation='Release'
outcome='Released'
requestedBgm='<none>'
requestedExplicitSilence='True'
confirmedBgm='<none>'
confirmedExplicitSilence='True'
```

This closes the Sample/FIRSTGAME real-consumer integration gate for the accepted IF-ADR-013 BGM intent boundary:

```text
Play(cue)   -> proven
No Request  -> Preserve proven
Silence     -> proven
owner exit  -> Preserve proven
persistent authority across transient Route/Activity scenes -> proven
```

This evidence does not itself change API maturity annotations. ADR-013 remains `Experimental` until an explicit product-maturity promotion cut updates the supported API status consistently.

## ADR disposition from this evidence

| ADR | Stage B disposition after this Sample proof |
|---|---|
| IF-ADR-006 | **GAME FLOW CONSUMER PASS** — persistent Transition/Loading, Route cover/reveal, Activity Fade/Seamless/FadeWithLoading and readiness-governed successful covered waiting are consumer-proven. Negative/terminal robustness remains technical QA responsibility and is not a FIRSTGAME/Sample completion gate. |
| IF-ADR-007 | **GAME FLOW CONSUMER PASS** — real `ObserveOnly`, `WaitVisible` and `WaitCovered` valid authoring, reveal behavior, capability blocking until Ready and clean reentry are consumer-proven. Negative, interrupted and terminal-failure semantics remain QA-owned. |
| IF-ADR-008 | **CONSUMER EVIDENCE ADDED** — explicit Persistent Content successfully hosts optional Transition/Loading presentation; current ADR-008 technical/product baseline was already closed. |
| IF-ADR-009 | **CONSUMER EVIDENCE ADDED** — A/B positive visibility plus C negative isolation proven; Readiness menu visibility also uses the same explicit binding model. ADR-009 technical boundary was already closed. |
| IF-ADR-011 | **CORE CONSUMER PROOF PASS** — participant-aware determinate readiness Loading progress is exercised with one Required participant, including release and fresh reentry. Broader technical QA remains the certification authority for the full matrix, including negative paths. |
| IF-ADR-013 | **FIRSTGAME/SAMPLE CONSUMER GATE PASS** — Play, Preserve/NoRequest, owner-exit continuity and explicit Silence are exercised in the real Sample topology. |

## Game Flow Stage B consumer closure

The intended positive Game Flow consumer proof is closed by the current Showcase:

```text
Basic Route / Activity navigation
Route-owned + Activity-local + Activity-owned composition
content-less Activity
Route Fade + Loading
Activity Seamless
Activity Fade
Activity Fade With Loading
Observe Only
Wait Visible
Wait Covered
Required readiness contribution
participant-aware determinate Loading progress
release / fresh reentry
contextual BGM Play / Preserve / Silence
blockingIssues = 0 on valid demonstrated paths
```

There is no remaining mandatory Game Flow Sample scenario solely to provoke failure, cancellation, invalidation, supersession or recovery. Those behaviors are Framework robustness contracts and remain under technical QA/certification.

Activity Restart, Camera, Player, Pause, Progression Save and additional Audio are feature-owned or evolutionary consumer demonstrations. They may be added when they teach a distinct positive product contract, but they do not block this Game Flow consumer closure.

## Scope boundary

This record now claims consumer proof for:

- baseline Route Transition/Loading presentation;
- Activity Seamless and Fade presentation;
- Activity `FadeWithLoading` on the readiness path;
- `WaitVisible` successful entry;
- `WaitCovered` successful entry;
- one Required participant readiness contribution;
- participant-aware determinate Loading progress;
- clean Activity-scene release and fresh reentry;
- content-less Activity and Activity-local visibility;
- BGM Play / Preserve / Silence.

This record intentionally does **not** claim consumer demonstrations of:

- Required readiness failure;
- interruption/cancellation/invalidation/supersession;
- terminal readiness recovery;
- invalid authoring paths.

Those are QA-owned robustness cases, not pending FIRSTGAME/Sample completion gates.

Player participation, Pause, broader Camera consumer proof, UPM Package Manager import proof and ADR-013 Stable API promotion remain separately governed by their own feature/product lanes.