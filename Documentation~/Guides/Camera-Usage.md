# Camera Usage

Status: Current for the single-output product  
Last updated: 2026-07-25

## Product model

The Camera product is separated into four responsibilities:

```text
CameraRigRecipe
  reusable presentation defaults

CameraRigComposer
  concrete rig authoring and idempotent materialization

Scoped Camera request binding
  Player, Activity, Route or Session intent publication

CameraOutputSessionBinding
  persistent physical output and winner application
```

`CameraRigComposer` does not own the physical output or Camera arbitration.
`CameraOutputContext` remains the only winner-selection authority.

## Create a reusable rig

1. Create a `CameraRigRecipe`.
2. Configure its Camera behavior and advanced materialization defaults.
3. Add `CameraRigComposer` to the virtual-rig root.
4. Optionally assign the Recipe and press `Apply Recipe Defaults`.
5. Select the Target Authoring Mode:
   - `Explicit Transforms`; or
   - `Target Source Component`.
6. Configure Follow/Look At requirements and Follow Offset.
7. Press `Validate Configuration`.
8. Press `Apply / Rebuild`.

The current product scope supports `Follow` presentation. A Target Source Component
must implement `ICameraTargetSource`. Explicit mode requires the concrete Follow and
Look At transforms directly on the Composer according to their configured
requirements.

`Apply Recipe Defaults` fills applicable defaults without intentionally replacing
existing authored values. The explicit overwrite action remains under
`Advanced Configuration`.

The Recipe provides reusable defaults only. It does not assign scene targets,
target-source components, a persistent output or a runtime Camera winner.

## Apply / Rebuild boundary

`Validate Configuration` resolves the authored configuration and reports blocking
issues without changing the Cinemachine rig.

`Apply / Rebuild` idempotently creates or repairs the local Cinemachine virtual rig
and target pipeline. It may materialize the configured `CinemachineCamera`, but it
does not create:

```text
Unity Camera
CinemachineBrain
AudioListener
CameraOutputSessionBinding
runtime Camera authority
```

Running `Apply / Rebuild` a second time must not duplicate valid objects or
components.

## Camera Rig Composer Inspector

The primary Inspector contains:

```text
Recipe
Camera Behavior
Materialization
Validation
```

`Advanced Configuration` contains editable technical materialization options,
including the explicit Recipe overwrite action.

`Advanced / Diagnostics` is read-only and contains:

```text
effective presentation intent
serialized target-source kind
last validation/apply status
blocking issue
target-resolution evidence
materialization evidence
resolved Follow/Look At targets
validation report
```

Changing the authored configuration marks the previous validation result as
outdated. Validation runs only when explicitly requested; Inspector repaint does not
resolve targets or execute materialization.

## Create the persistent output

Create the application Persistent Content Scene from the official Scene Template, or
author an equivalent scene manually.

The minimum Camera composition contains:

```text
Persistent Camera
  Camera Output
    Unity Camera
    CinemachineBrain
    CameraOutputSessionBinding
    SessionCameraOverrideBinding

  Session Camera Target

  Session Camera Rig
    CinemachineCamera
    CameraRigComposer
```

Use one explicit output ID. Gameplay scenes do not serialize references to this
persistent object; Framework Core injects the output session into scoped consumers.

## Configure the Session Camera Override

The primary `SessionCameraOverrideBinding` Inspector configures:

```text
Camera Output
Camera Rig
Target
Priority
```

Then press `Validate Configuration`.

Stable identity is under `Advanced / Diagnostics`:

```text
Session Scope ID
Camera Request ID
Tie Breaker ID
```

Use `Generate Missing IDs` to fill only empty identities. Existing IDs are never
replaced automatically. The same area exposes read-only runtime state and the last
explicit validation report.

The Session override references existing output, rig and target objects. It does not
create or discover them automatically.

## Publish Camera intent

- `PlayerGameplayCameraAuthoring` supplies the normal eligible Player request.
- `LocalPlayerCameraRequestBinding` is authoring/evidence; its scene
  auto-publisher is opt-in and must not duplicate gameplay admission publishing.
- `ActivityCameraOverrideBinding` and `RouteCameraOverrideBinding` publish only
  after explicit `RequestOverride()` and release with `ReleaseOverride()`.
- `SessionCameraOverrideBinding` supplies the Session-scoped override used by the
  persistent composition.

Default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

`CameraOutputContext` selects the winner. Owners do not toggle the physical Camera
or compete by editing Cinemachine priority.

## Diagnose

Use the relevant `Advanced / Diagnostics` foldout and the output snapshot to inspect:

```text
output
request
owner
scope
Player Slot
precedence
targets
rig
winner
last diagnostic
```

Use `Advanced Configuration` only for editable technical materialization settings;
it is not the runtime evidence surface.

Do not use `Camera.main`, name lookup, singleton, service locator or cross-scene
fallback.

## Manual validation

1. Compile Framework and QAFramework with Cinemachine installed.
2. Configure a `CameraRigComposer` through one explicit target-authoring mode.
3. Press `Validate Configuration`.
4. Run `Apply / Rebuild` twice; the second run must be idempotent.
5. Configure the persistent `SessionCameraOverrideBinding`.
6. Generate only missing IDs and press `Validate Configuration`.
7. Validate the Persistent Content Scene through the Game Application.
8. Confirm Player → Activity → Route → Session precedence and reverse restoration.
9. Exercise Route/Activity exit and Session transition release.
10. Confirm one persistent output and no duplicate publisher for a Player.
11. Validate framing and restoration visually in FIRSTGAME.
