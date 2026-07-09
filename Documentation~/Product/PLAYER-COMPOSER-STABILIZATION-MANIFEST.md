# Player Composer Stabilization Manifest

Status: Documentation stabilization cut
Date: 2026-07-09
Package: `com.immersive.framework`

## Cut

Consolidate `PlayerComposer` as the first Product Surface validated in a real consumer project.

## Objective

Document the official `PlayerComposer` usage flow, register FIRSTGAME evidence and preserve the validated behavior/defaults without adding new functionality.

## Files Created

```text
Documentation~/Product/PlayerComposer-QuickStart.md
Documentation~/Product/PlayerComposer-FIRSTGAME-Evidence.md
Documentation~/Product/PLAYER-COMPOSER-STABILIZATION-MANIFEST.md
```

## Files Changed

```text
none
```

Reviewed and intentionally unchanged:

```text
Runtime/PlayerAuthoring/PlayerComposer.cs
Editor/PlayerAuthoring/PlayerComposerEditor.cs
```

The existing Inspector copy already states that `PlayerComposer` is a designer-first authoring surface, that Apply/Rebuild materializes technical framework bindings, and that the component does not execute gameplay or act as a `PlayerManager`.

## Files Removed

```text
none
```

## Out Of Scope

```text
PlayerRecipe
PlayerRuntimeContext
QAFramework smoke
FIRSTGAME changes
runtime behavior changes
asmdef changes
new facade
old menu recreation
additional component moves
reset default changes
Unity build, Play Mode, smoke or batchmode execution
```

## Evidence Used

FIRSTGAME configured the official Composer on `PlayerPrototype`:

```text
[FIRSTGAME][PlayerComposerPilot] Configured official PlayerComposer intent. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1' actionMap='Player' hasPlayerInput='True'. Use the PlayerComposer Inspector Apply/Rebuild button to materialize framework bindings.
```

FIRSTGAME validated idempotent Apply/Rebuild:

```text
[Immersive.Framework][PlayerComposer] Apply/Rebuild completed. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1' created='0' repaired='0' alreadyValid='13' skippedByPolicy='2' blocked='0' resetEnabled='False' resetParticipantPolicy='None'
```

FIRSTGAME validated the Composer intent:

```text
[Immersive.Framework][PlayerComposer] Validation succeeded. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1'
```

The local FIRSTGAME player-binding facade was removed after the official flow passed.

QAFramework has not entered this validation path yet.

## Acceptance Criteria

This cut is PASS if:

```text
- PlayerComposer-QuickStart.md exists;
- PlayerComposer-FIRSTGAME-Evidence.md exists;
- PLAYER-COMPOSER-STABILIZATION-MANIFEST.md exists;
- docs explain the official Composer flow;
- docs explain diagnostic breakdown;
- docs register FIRSTGAME PASS evidence;
- no FIRSTGAME facade is reintroduced;
- validated defaults are unchanged;
- no asmdef is changed;
- no runtime behavior is changed;
- no code is changed except possible Inspector copy, which this cut did not require;
- Unity continues compiling;
- PlayerComposer keeps validating and Apply/Rebuild remains idempotent on PlayerPrototype.
```

## Suggested Commit Message

```text
Docs: stabilize PlayerComposer product surface
```
