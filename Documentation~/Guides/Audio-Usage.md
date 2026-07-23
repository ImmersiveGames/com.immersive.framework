# Audio BGM Usage

Status: Current / Experimental
Last updated: 2026-07-23

## Dependency

Install a compatible `com.immersive.audio` package. The optional
`Immersive.Framework.Audio` assembly is enabled by version define; Framework
Core remains independent from Audio.

## Author

1. Configure an `AudioRuntimeHost` using the Audio package.
2. Add `FrameworkBgmDirector` and assign that host explicitly.
3. Add `FrameworkRouteBgmBinding` to Route content and assign its Route cue and
   director.
4. Add `FrameworkActivityBgmBinding` to Activity content and assign the
   Activity, cue, policy and director.
5. When a Route has a Startup Activity, reference its explicit Activity BGM
   binding from the Route binding.

Policies:

| Policy | Behavior |
|---|---|
| `UseOwnOrRoute` | Use Activity cue, otherwise Route cue. |
| `UseOwnOrRetainActivityUntilRouteExit` | Retain the Activity cue within the Route after Activity exit. |
| `UseRoute` | Ignore the Activity cue and use Route cue. |
| `Silence` | Explicitly stop BGM for the active Activity. |

## Runtime flow

```text
Route/Activity lifecycle
-> framework BGM binding
-> FrameworkBgmDirector precedence/retention
-> AudioRuntimeHost
-> com.immersive.audio playback
```

Route exit clears Route and retained Activity state. Activity transitions may
defer refresh so the next Activity can apply without an unintended intermediate
fallback.

## Diagnose

Inspect the director's Route, Activity, retained and effective cues, current
policy and last `AudioPlaybackResult`. Logs use `[FRAMEWORK_BGM]`. A missing
`AudioRuntimeHost` is an explicit error; do not add scene search or a global
audio manager.

## Manual validation

1. Compile with and without `com.immersive.audio` installed as supported by the
   package dependency setup.
2. Prove Route cue, Activity cue, Route fallback, retention and Silence.
3. Prove Startup Activity pre-application.
4. Switch Routes and confirm retained Activity BGM does not leak.
5. Validate playback results and current real-game behavior before promotion
   from Experimental.
