# IF Stage B — Game Flow Sample Consumer Evidence — 2026-08-21

Status: **RECORDED — scoped Stage B consumer evidence**  
Consumer repository: `ImmersiveGames/planet-devourer`  
Consumer baseline: `3642fb2ad207b7dcfc0c230f657a475fdf67a27d` (`Activity C`)  
Demonstration: `Assets/_Sample/GameFlow/GameFlowShowcase/`  
Related decisions: IF-ADR-006, IF-ADR-007, IF-ADR-008, IF-ADR-009, IF-ADR-011, IF-ADR-013

## Purpose

Record the current real-consumer Play Mode evidence produced by the Game Flow Sample without relabeling technical QA or claiming contracts that the Sample has not exercised.

This record is Stage B evidence. It does not replace Stage A certification records and it does not make FIRSTGAME/Sample an exceptional-path laboratory.

## Consumer topology

```text
GameApplication_GameFlow
  Persistent Content -> SCN_GameFlow_Persistence
  Startup Route -> Route_Hub

Route_Hub
  no Startup Activity
  BGM -> explicit Silence

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

Persistent presentation is explicit consumer composition:

```text
SCN_GameFlow_Persistence
  UnityFadeCurtainEffectAdapter
  UnityLoadingSurfaceAdapter
```

## Persistent Content / presentation resolution

Observed boot evidence:

```text
Persistent Content loaded
scene='SCN_GameFlow_Persistence'
rootCount='4'
transitionAdapterCount='1'
loadingAdapterCount='1'

Loading surface resolved
adapterCount='1'

Transition surface resolved
adapterCount='1'
```

This is consumer evidence that optional Transition and Loading presentation can be explicitly composed under the accepted Persistent Content lifetime rather than created through hidden runtime fallback.

## Route Transition + Loading proof

Observed `Route_Hub -> Route_BasicFlow` result:

```text
kind='Succeeded'
transition='SucceededWithUnitySurface'
loading='SucceededWithUnitySurface'
activity='Basic Flow A'
activityReadiness='Ready'
blockingIssues='0'
```

Detailed evidence:

```text
transitionScope='Route'
transitionVisual='UnitySurface'
transitionEffect='Fade'
transitionEffectBefore='Succeeded'
transitionEffectAfter='Succeeded'
transitionEffectAdapterCount='1'
transitionEffectAdapterEvidenceApplied='2'
transitionEffectBlockingIssues='0'

LoadingBefore='Succeeded'
LoadingAfter='Succeeded'
loadingAdapterEvidenceApplied='2'
loadingAdapterBlockingIssues='0'

transitionGateMode='InputInteractionAndGameplay'
transitionGateApplied='True'
transitionGateReleased='True'
```

This proves real consumer authoring and execution of the baseline Route presentation envelope:

```text
Fade cover
  -> Route lifecycle / scene composition
  -> Loading presentation during Route work
  -> destination Activity Ready
  -> Fade reveal
```

The sample also exercises the reverse `Route_BasicFlow -> Route_Hub` Route-switch envelope.

## Activity presentation proof

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

Therefore Activity-owned scene load/release is independent from requiring visual cover: the scene side effects occur while presentation remains intentionally Seamless.

### Fade A/B -> C

C uses `ActivityVisualTransitionMode.Fade`.

Observed `B -> C` result:

```text
previousActivity='Basic Flow B'
targetActivity='Basic Flow C'
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

Detailed evidence confirms:

```text
transitionScope='Activity'
transitionVisual='UnitySurface'
transitionEffect='Fade'
transitionEffectBefore='Succeeded'
transitionEffectAfter='Succeeded'
transitionEffectBlockingIssues='0'
activityTransitionMode='Fade'
activityLoadingMode='ActivitySceneRelease'
```

The same target-C policy is exercised from A as well. Returning from C to A/B uses the target Activity's Seamless policy.

This consumer proof closes the baseline distinction:

```text
Activity target Seamless
  -> no visual Transition

Activity target Fade
  -> Fade cover/reveal
  -> no canonical Loading presentation
```

It does **not** prove `FadeWithLoading`, readiness-governed Loading or recovery.

## Content-less Activity / negative visibility proof

Activity C intentionally has no `ActivityContentProfile` and no Activity-owned scene.

Observed result while C is active:

```text
activitySceneComposition='NotRequested'
activitySceneCompositionProfile=''
activitySceneCompositionScenes='0'
activityContentHandles='0'
activityReadiness='Ready'
blockingIssues='0'
```

The Activity-local visibility diagnostics show A/B-scoped objects deactivated under C because there is no listed Activity match. Therefore C is a valid active Activity without owned content and A/B content does not leak into it.

This is additional real-consumer evidence for IF-ADR-009's explicit negative visibility semantics. ADR-009 was already technically closed; this evidence does not reopen or redefine that boundary.

## BGM real-consumer proof

The sample exercises explicit Play, no-request Preserve and explicit Silence under real Route/Activity and scene lifetime.

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

This closes the previously pending Sample/FIRSTGAME real-consumer integration gate for the accepted IF-ADR-013 BGM intent boundary:

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
| IF-ADR-006 | **PARTIAL PASS** — real Transition + Loading authoring and Route cover/reveal execution proven; readiness-governed wait/reveal, terminal recovery and participant-aware progress remain pending. |
| IF-ADR-007 | **NOT CLOSED BY THIS EVIDENCE** — current Activities use baseline `ObserveOnly`; `WaitCovered` / `WaitVisible` consumer proof remains pending. |
| IF-ADR-008 | **CONSUMER EVIDENCE ADDED** — explicit Persistent Content successfully hosts optional Transition/Loading presentation; current ADR-008 technical/product baseline was already closed. |
| IF-ADR-009 | **CONSUMER EVIDENCE ADDED** — A/B positive visibility plus C negative isolation proven; ADR-009 technical boundary was already closed. |
| IF-ADR-011 | **NOT CLOSED BY THIS EVIDENCE** — Player Session is disabled and participant-aware readiness progress is not exercised. |
| IF-ADR-013 | **FIRSTGAME/SAMPLE CONSUMER GATE PASS** — Play, Preserve/NoRequest, owner-exit continuity and explicit Silence are all exercised in the real Sample topology. |

## Remaining Game Flow Stage B proof

The next presentation/readiness proof should target a distinct contract rather than duplicate the baseline Transition demonstration:

```text
Activity Visual Transition = FadeWithLoading
Activity Entry Readiness = WaitCovered or WaitVisible
real Required readiness contribution
truthful progress remains below terminal 100% while not Ready
Ready -> terminal progress -> hide Loading -> reveal
terminal failure / recovery remains explicit
```

Restart / Recovery remains a separate later Game Flow proof.

## Scope boundary

This record does not claim:

- participant-aware Loading progress;
- `WaitCovered` / `WaitVisible` consumer proof;
- terminal readiness failure/recovery proof;
- Player participation proof;
- Pause consumer proof;
- broader Camera consumer proof;
- UPM Package Manager import proof;
- ADR-013 Stable API promotion.

Those remain separately governed.
