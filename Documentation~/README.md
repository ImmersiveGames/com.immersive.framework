# Immersive Framework Documentation

## Operational entry points

```text
Current/05-Execution-Status.md
  canonical operational state and H2.4 validation handoff

Current/00-Current-State.md
  supported runtime-authority boundary

Current/01-Roadmap.md
  H2 closure and validation gate

Current/02-Usage-Map.md
  designer-first product surfaces
```

## H2.4 closure

H2 is closed and Unity-validated in `1.0.0-preview.16`.

```text
GameApplication bootstrap
-> stateless FrameworkRuntimeHost factory
-> explicit feature runtime ports
-> authoring / Unity adapter bindings
```

The package has no static current-host field or lookup API. QA-only host
resolution is confined to the QA friend-assembly harness and requires exactly
one loaded candidate; it is not a package service locator or runtime fallback.

The H2.4 Play Mode smoke evidence is approved (`Passed`, 10 cases). Read
`Current/05-Execution-Status.md` for the recorded delivery state.

## Camera status

Camera C9 is closed at the current single-output product level.

Canonical authoring:

```text
CameraRigRecipe
-> CameraRigComposer
-> Validate
-> Apply/Rebuild virtual Cinemachine rig
```

Canonical runtime:

```text
persistent UIGlobal output
-> CameraOutputSession
-> CameraOutputContext
-> Player / Activity / Route / Session requests
-> selected Cinemachine rig
```

Default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

Read:

- `Guides/Camera-Product-Usage.md`
- `Guides/Camera-Architecture-Flow.md`
- `Current/Camera-Delivery-Reconciliation.md`
- `ADRs/Product/ADR-PROD-0006-camera-requests-output-contexts.md`

## Pause status

Canonical Pause composition is:

```text
PausePlayerInputBinding
-> PauseProductBindingRuntimeContext
-> InputMode transaction
-> UnityPlayerInputGateAdapter
-> UnityPlayerInputStateWriter
```

Running enables `Global + configured gameplay action map`, whose default name is
`Player`; paused enables `Global`. QA may configure that gameplay map as
`Gameplay`. The earlier
`PauseInputModeUnityPlayerInputRuntimeBridge` and
`PauseInputActionRuntimeBridgeTrigger` topology is removed. Superseded Input ADRs
remain historical records only.

## FRAMEWORK-HYGIENE-1 reconciliation

Commit `fe90949e401a5d01c9f12a75dbc989ce0d8ac02e` modified 18 files and
removed 130 files. It removed the superseded Pause/InputMode bridge family and
the superseded UnityInputTarget family without adding wrappers, aliases or a
parallel runtime authority.

The source cut is closed, but its release gate is pending: package compile,
post-migration QA compile and focused regression PASS results have not been
supplied. Until all three are confirmed, the documented and manifest version
remains `1.0.0-preview.16`.

## Next selection

No post-H2 implementation cut is active. The next product lane may now be
selected without reopening H2.4.
