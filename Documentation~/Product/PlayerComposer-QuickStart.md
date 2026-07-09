# PlayerComposer QuickStart

Status: Product surface guide
Date: 2026-07-09
Package: `com.immersive.framework`

## What It Is

`PlayerComposer` is the official first product surface for authoring a framework-ready Player.

It is a Unity component added to the player GameObject. Its job is to hold designer intent and expose Inspector actions that validate and materialize the technical framework bindings needed by the current Player MVP.

It is the authoring surface. The technical components it creates or repairs are implementation details.

## What It Does

`PlayerComposer`:

- stores the intended `ActorId`, `PlayerSlotId`, `PlayerInput`, gameplay action map and camera targets;
- validates required Player setup before materialization;
- creates or repairs framework declarations and binding components;
- materializes technical bindings under `_Framework/_Bindings`;
- reports Apply/Rebuild diagnostics with `created`, `repaired`, `alreadyValid`, `skippedByPolicy` and `blocked`;
- keeps reset materialization optional by default.

## What It Does Not Do

`PlayerComposer` does not:

- move the player;
- execute gameplay commands;
- spawn actors;
- join players at runtime;
- switch action maps at runtime;
- save preferences or progression;
- act as a `PlayerManager`;
- replace future `PlayerRecipe` or `PlayerRuntimeContext`.

## Add It To A Player

1. Select the Player GameObject.
2. Add `Immersive Framework/Player/Player Composer`.
3. Fill the Designer fields.
4. Run `Validate`.
5. Run `Apply/Rebuild`.
6. Run `Apply/Rebuild` again to confirm idempotence.

The component can also be added by consumer editor tooling, as FIRSTGAME does through its Player Composer Pilot. The resulting official flow is still the package `PlayerComposer` Inspector.

## Minimum Fields

The minimum expected fields are:

```text
Actor Id
Player Slot Id
Player Input
Gameplay Action Map
Camera Target
Look At Target
```

Current defaults:

```text
actorId = player.actor
playerSlotId = player.1
gameplayActionMap = Player
cameraTarget = this transform when empty
lookAtTarget = cameraTarget when empty
resetEnabled = false
resetParticipantPolicy = None
```

`ActorId` and `PlayerSlotId` are stable framework identities. They are not `GameObject.name`, not hierarchy paths and not `PlayerInput.playerIndex`.

## Validate

Use the Inspector `Validate` button before applying.

Validation checks the active Composer intent. In Standard mode it blocks when required information is missing, including:

- empty `ActorId`;
- empty `PlayerSlotId`;
- missing `PlayerInput` when input binding is required;
- missing `Gameplay Action Map` when input binding is required;
- missing `PlayerInput.actions`;
- action map not found in the assigned input action asset;
- missing camera/look-at target when camera binding is required;
- missing bindings root when automatic root creation is disabled.

Successful validation logs:

```text
[Immersive.Framework][PlayerComposer] Validation succeeded.
```

Failed validation writes the issue into Debug and logs a warning.

## Apply/Rebuild

Use the Inspector `Apply/Rebuild` button to materialize the Composer intent.

The action is editor-only and idempotent. It creates missing technical structure, repairs existing serialized references when they drift, and leaves already-valid materialization untouched.

`Apply/Rebuild` does not remove gameplay scripts, does not delete consumer components and does not execute runtime gameplay.

## Confirm Idempotence

Run `Apply/Rebuild` twice on the same valid Player.

Expected pattern:

```text
first run: created and/or repaired may be greater than 0
second run: created=0 and repaired=0 when nothing drifted
blocked=0
alreadyValid increases or remains high
```

FIRSTGAME validated the stable path with:

```text
created='0' repaired='0' alreadyValid='13' skippedByPolicy='2' blocked='0'
```

## Diagnostic Breakdown

`created` means Apply/Rebuild created a missing GameObject or component.

`repaired` means an existing component was present but one or more serialized fields were corrected.

`alreadyValid` means the object/component already matched the Composer intent.

`skippedByPolicy` means the Composer intentionally skipped an optional item. Examples include reset being disabled, reset participant policy being `None`, camera binding being disabled or an optional type not existing.

`blocked` means Apply/Rebuild could not safely complete part of the materialization. A valid product flow should keep this at `0`.

When `blocked` is greater than `0`, inspect `Last Blocking Issue` in Debug and the console warning/log.

## Reset

Reset is optional by default.

```text
resetEnabled = false
resetParticipantPolicy = None
```

With reset disabled, Apply/Rebuild skips reset subject and participant materialization by policy. This is expected and should appear under `skippedByPolicy`, not `blocked`.

To materialize reset support explicitly:

1. Enable `Reset Enabled`.
2. Choose the desired `Reset Participant Policy`.
3. Use `Transform` only when a transform reset participant is intended.
4. Run `Apply/Rebuild`.

The Composer does not make reset mandatory for every Player.

## `_Framework/_Bindings`

`_Framework/_Bindings` is technical materialization created under the Player when automatic root creation is enabled.

It may contain components such as:

```text
PlayerControlBindingTargetBehaviour
UnityPlayerInputBridgeTargetBehaviour
UnityPlayerInputActivationTargetBehaviour
PlayerSlotOccupancy
FrameworkCameraAnchorHost
UnityTransformResetParticipant, only when reset policy enables it
```

Designers should normally work on `PlayerComposer`, not directly on these components.

Use `Select Technical Bindings` only when inspecting or debugging the generated materialization.

## Advanced And Debug

Use `Advanced` when you need to change policies:

- whether the Composer may create `_Framework/_Bindings`;
- whether anchors may be created;
- whether input and camera bindings are required;
- reset scope and reset participant policy;
- whether optional slot occupancy or passive entry/view/control components are materialized;
- whether Apply/Rebuild diagnostics are logged.

Use `Debug` to inspect resolved values and the latest validation/materialization result:

- resolved actor id;
- resolved player slot id;
- resolved `PlayerInput`;
- action map found;
- bindings root;
- camera/look-at targets;
- last status;
- last blocking issue;
- last materialization summary.

Debug is evidence. It is not the main authoring workflow.
