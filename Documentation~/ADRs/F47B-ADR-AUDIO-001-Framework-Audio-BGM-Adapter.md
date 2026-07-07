# F47B-ADR-AUDIO-001 - Framework Audio BGM Adapter

Status: Accepted / Planning ADR
Phase: F47B - Framework Audio/BGM Adapter ADR + API Plan
Type: Optional Framework + Audio Adapter Boundary
Last updated: 2026-07-07

---

## Context

F46 moved Camera Route/Activity behavior out of QA and FIRSTGAME-specific components into framework-owned Camera components with an optional concrete adapter for Cinemachine.

F47A found the same ownership problem in FIRSTGAME BGM:

```text
FirstGameBgmDirector
FirstGameRouteBgmBinding
FirstGameActivityBgmBinding
FirstGameActivityBgmPolicy
```

Those classes contain reusable framework semantics:

```text
Route BGM
Activity BGM
Startup Activity BGM
Activity retention until Route exit
Route fallback
Silence / stop policy
RouteContentBehaviour binding
ActivityContentBehaviour binding
```

The actual playback primitives already belong to `com.immersive.audio`:

```text
AudioBgmCueAsset
AudioDefaultsAsset
AudioRuntimeHost
AudioBgmService / IAudioBgmService
AudioPlaybackResult / AudioPlaybackStatus
fade / play / stop / failure model
```

FIRSTGAME should keep only concrete authored content such as `MenuBgm`, `GameplayRouteBgm`, `ActivityBgm`, scenes, buttons and game flow.

---

## Decision

Create an optional framework-owned BGM adapter in a separate assembly inside `com.immersive.framework`, not in the framework base runtime assembly and not inside `com.immersive.audio`.

Proposed assembly:

```text
Immersive.Framework.Audio
```

Proposed namespace:

```text
Immersive.Framework.Audio
```

This assembly depends on:

```text
Immersive.Framework.Runtime
Immersive.Audio.Runtime
Immersive.Audio.Unity
```

The base `Immersive.Framework.Runtime` assembly must not depend on `com.immersive.audio`.

This follows the same boundary shape used by Camera:

```text
Framework core owns Route/Activity selection semantics.
Optional adapter assembly owns concrete third-party or sibling-package integration.
```

---

## Alternatives Considered

### A. Put BGM adapter in `com.immersive.framework` base runtime

Rejected.

It would make audio an implicit dependency of the framework base, reducing compile safety for framework consumers that do not install `com.immersive.audio`.

### B. Put Route/Activity BGM adapter in `com.immersive.audio`

Rejected.

`com.immersive.audio` is a technical package. It must not know `Route`, `Activity`, framework lifecycle, FIRSTGAME, QA, or game flow.

### C. Create a new optional package `com.immersive.framework.audio`

Deferred.

This is architecturally clean if the adapter needs a separate release cycle. For F47, the smaller and safer cut is an optional assembly inside `com.immersive.framework`. A package split can remain a future release decision.

### D. Create an optional assembly inside `com.immersive.framework`

Accepted.

This keeps the adapter near the framework Route/Activity ownership model while preserving compile safety and avoiding a new package before there is evidence of independent versioning needs.

---

## Public API Draft

Proposed public types:

```text
FrameworkBgmDirector
FrameworkRouteBgmBinding
FrameworkActivityBgmBinding
FrameworkBgmActivityPolicy
```

Rejected names:

```text
FirstGame*
Qa*
BgmManager
BgmCoordinator
BgmProcessor
AudioManager
```

The names mirror Camera deliberately:

```text
FrameworkCameraDirector -> FrameworkBgmDirector
FrameworkRouteCameraBinding -> FrameworkRouteBgmBinding
FrameworkActivityCameraBinding -> FrameworkActivityBgmBinding
FrameworkCameraActivityPolicy -> FrameworkBgmActivityPolicy
```

Draft serialized fields:

```text
FrameworkBgmDirector
- audioRuntimeHost
- logTransitions

FrameworkRouteBgmBinding
- routeBgm
- director
- startupActivityBgmBinding

FrameworkActivityBgmBinding
- assignedActivity
- activityBgm
- policy
- director
```

Draft diagnostics prefix:

```text
[FRAMEWORK_BGM]
```

---

## Policy Semantics

Accepted enum draft:

```csharp
public enum FrameworkBgmActivityPolicy
{
    UseOwnOrRoute = 0,
    UseOwnOrRetainActivityUntilRouteExit = 1,
    UseRoute = 2,
    Silence = 3
}
```

Policy meanings:

```text
UseOwnOrRoute
Activity BGM wins when assigned; otherwise Route BGM is used.

UseOwnOrRetainActivityUntilRouteExit
Activity BGM wins when assigned and is retained for the current Route after Activity exit. If no retained Activity BGM exists, Route BGM is used.

UseRoute
Ignore Activity BGM for this Activity and use Route BGM.

Silence
Explicitly stop BGM for this Activity scope. This is not the same as UseRoute.
```

Terminology decision:

```text
Silence is the Inspector-facing policy name.
StopBgm is implementation behavior.
```

Reason:

`Silence` describes user intent in authoring. `StopBgm` describes a concrete operation and reads less naturally in the Inspector.

---

## Precedence

Official precedence:

```text
Activity own BGM
Retained Activity BGM for current Route
Route BGM
Silence
```

`Silence` is an explicit policy result, not a missing fallback.

Retention is cleared:

```text
on Route exit
when a new Route BGM is set
when an explicit Silence policy applies
when a new Activity BGM is retained for the same Route
```

Activity exit behavior:

```text
UseOwnOrRoute
- clears current Activity BGM; Route BGM becomes effective.

UseOwnOrRetainActivityUntilRouteExit
- clears current Activity BGM but may keep retained Activity BGM until Route exit.

UseRoute
- clears Activity BGM state; Route BGM remains effective.

Silence
- clears Activity BGM and retained Activity BGM for the current Route, then calls StopBgm while active.
```

---

## Dependency / Assembly Strategy

The adapter assembly should be compile-safe when `com.immersive.audio` is absent.

Accepted strategy:

```text
Optional assembly inside com.immersive.framework.
Compile only when com.immersive.audio is installed.
Use versionDefines / defineConstraints similar to the Camera Cinemachine adapter.
```

Proposed define:

```text
IMMERSIVE_FRAMEWORK_AUDIO
```

The adapter should reference `Immersive.Audio.Unity` because its public authoring/runtime types are Unity-facing:

```text
AudioBgmCueAsset
AudioRuntimeHost
IAudioBgmService
```

`Immersive.Audio.Runtime` alone is insufficient because BGM cue assets and the host live in the Unity assembly.

Preferred consumption API:

```text
AudioRuntimeHost.PlayBgm(AudioBgmCueAsset)
AudioRuntimeHost.StopBgm()
```

Reason:

The host is the stable scene-authored composition surface. It owns composition of settings, routing, listener and BGM service without requiring the adapter to compose audio services directly.

`IAudioBgmService` can remain a future injection seam if an explicit service-binding adapter is needed.

---

## Logging

Use local simple Unity logging in the optional adapter:

```text
[FRAMEWORK_BGM]
```

Do not open internal framework logger APIs for this cut.

Reason:

The adapter is Unity-facing and experimental, and Camera already uses a local diagnostic prefix. Opening logger internals only for convenience would expand public API without a real architectural need.

---

## QA Strategy

F47D should add a QA fixture that proves the technical adapter, not FIRSTGAME content.

Minimum QA cases:

```text
QA Route BGM
QA Activity BGM
Startup Activity BGM pre-apply
Activity B retained Activity BGM
Activity C Route fallback
Activity D Silence
Clear Activity returns to Route or retained policy result
Route switch clears retained Activity BGM
```

The fixture should use synthetic QA `AudioBgmCueAsset` assets and generated clips, not FIRSTGAME assets.

Expected QA evidence:

```text
[FRAMEWORK_BGM] Route BGM set
[FRAMEWORK_BGM] Activity BGM set
[FRAMEWORK_BGM] BGM applied
[FRAMEWORK_BGM] Silence policy applied
AudioPlaybackResult status is explicit
```

The existing Audio QA remains valid for `com.immersive.audio` package behavior, but it does not prove Route/Activity BGM ownership.

---

## FIRSTGAME Migration Strategy

F47E should migrate FIRSTGAME after the adapter exists and QA proves it.

Expected migration:

```text
FG_Menu.unity
FG_Gameplay.unity
FirstGameBgmDirector -> FrameworkBgmDirector
FirstGameRouteBgmBinding -> FrameworkRouteBgmBinding
FirstGameActivityBgmBinding -> FrameworkActivityBgmBinding
FirstGameActivityBgmPolicy -> FrameworkBgmActivityPolicy
```

Preserve concrete consumer assets:

```text
MenuBgm
GameplayRouteBgm
ActivityBgm
FirstGameAudioDefaults
placeholder WAV clips
scene roots and authored flow
```

Remove after migration:

```text
FirstGameBgmDirector.cs
FirstGameRouteBgmBinding.cs
FirstGameActivityBgmBinding.cs
FirstGameActivityBgmPolicy.cs
their .meta files
```

No wrapper, alias, temporary compatibility class, or permanent `FirstGameBgm*` rail is accepted in the final result.

---

## Risks

```text
GUID / scene references
Missing Script during migration
Audio package dependency absent
asmdef reference drift
semantic mismatch between Stop and Silence
accidental framework base dependency on audio
runtime host reference missing
startup Activity pre-apply order
Route exit cleanup
retained Activity BGM leaking across Routes
```

Mitigations:

```text
Create adapter with stable .meta GUIDs before scene migration.
Use explicit serialized references, not scene search.
Validate no m_Script fileID 0 in QA and FIRSTGAME scopes.
Run MSBuild for framework runtime, optional adapter, QA and FIRSTGAME assemblies.
Run Unity smoke manually before declaring PASS.
```

---

## Acceptance Criteria

F47B:

```text
No runtime implemented.
No scene changed.
No FIRSTGAME changed.
No QA changed.
No audio package runtime changed.
ADR created and indexed.
Package / assembly decision is explicit.
Public names are explicit.
Silence policy is explicit.
Compile-safety strategy is explicit.
F47C-F47F sequence is defined.
```

F47C:

```text
Optional adapter assembly compiles only with com.immersive.audio present.
Framework base does not depend on audio.
Audio package does not depend on framework.
No service locator.
No silent fallback.
```

---

## Next Cuts

```text
F47C - Implement Optional Framework BGM Adapter
F47D - QA Route/Activity BGM Adapter Fixture
F47E - FIRSTGAME BGM Migration To Framework Adapter
F47F - Unity Smoke and Migration Closure
```

