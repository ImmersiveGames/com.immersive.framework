# 02 — Usage Map

For the current execution block, read `05-Execution-Status.md`.

## Camera rig and output authoring

| Need | Current surface |
|---|---|
| Reusable presentation intent | `CameraRigRecipe` |
| Materialize a virtual rig | `CameraRigComposer` → Validate → Apply/Rebuild |
| Author Follow framing | `Follow Offset` in Recipe/Composer |
| Physical single-player output | `CameraOutputSessionBinding` with explicit Unity Camera and `CinemachineBrain` in `UIGlobal` |
| Normal Player presentation | `LocalPlayerCameraRequestBinding` plus explicit eligibility |
| Temporary Activity override | `ActivityCameraOverrideBinding` → `RequestOverride()` / `ReleaseOverride()` |
| Temporary Route override | `RouteCameraOverrideBinding` → `RequestOverride()` / `ReleaseOverride()` |
| Transition presentation | `SessionCameraOverrideBinding` operated by the transition orchestrator |
| Inspect arbitration | binding diagnostics and `CameraOutputContext` snapshot |

`CameraRigComposer` materializes only a local Cinemachine rig and its target
pipeline. It does not create the physical output and never chooses the active
rig.

```text
Player request / Activity override / Route override / Session override
-> typed publisher
-> CameraOutputSession
-> CameraOutputContext
-> CameraOutputRigApplicator
-> selected materialized CinemachineCamera
```

Default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

Framework Core injects the persistent `UIGlobal` output into Route, Activity and
Player consumers. Do not author cross-scene output references.

Do not use Unity Camera enable/disable, Cinemachine priority competition,
object-name lookup, `Camera.main`, singleton or global manager as selection
policy.

## Consumer Route loop

The current G1 product proof is built from existing application-flow surfaces:

| Need | Existing surface |
|---|---|
| Enter application | Bootstrap + Startup Route |
| Show menu | Menu Route |
| Start gameplay | explicit Route request |
| Present loading/transition | canonical loading and transition surfaces |
| Block input during transition | Transition Gate + `UnityPlayerInputGateAdapter` |
| Preserve camera continuity | Session override during transition; Player restored afterward |
| Finish or leave gameplay | explicit request to Ending Route or Menu Route |
| Re-enter | another explicit Route request |

The framework owns Route lifecycle, transitions, loading, input availability,
camera authority and diagnostics. It does not own the gameplay reason for
requesting the next Route.

A gameplay objective, interaction, win condition or resettable object is
consumer content. It is optional evidence, not a prerequisite for proving the
framework Route loop.
