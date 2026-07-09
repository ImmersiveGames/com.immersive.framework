# PlayerComposer FIRSTGAME Evidence

Status: Consumer validation evidence
Date: 2026-07-09
Package: `com.immersive.framework`
Consumer: FIRSTGAME / `planet-devourer`

## Summary

FIRSTGAME validated the official package `PlayerComposer` flow on the real player prototype.

The accepted flow is now:

```text
PlayerComposer official package component
Inspector Apply/Rebuild
Composer diagnostics
```

The old local FIRSTGAME player-binding facade was removed after the official Composer path passed.

## Validated Consumer Object

```text
PlayerPrototype
```

Configured intent:

```text
actorId = player.actor
playerSlotId = player.1
gameplayActionMap = Player
hasPlayerInput = True
resetEnabled = False
resetParticipantPolicy = None
```

## Evidence Logs

Composer intent configured in FIRSTGAME:

```text
[FIRSTGAME][PlayerComposerPilot] Configured official PlayerComposer intent. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1' actionMap='Player' hasPlayerInput='True'. Use the PlayerComposer Inspector Apply/Rebuild button to materialize framework bindings.
```

Apply/Rebuild was idempotent with no blocking issues:

```text
[Immersive.Framework][PlayerComposer] Apply/Rebuild completed. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1' created='0' repaired='0' alreadyValid='13' skippedByPolicy='2' blocked='0' resetEnabled='False' resetParticipantPolicy='None'
```

Validation succeeded:

```text
[Immersive.Framework][PlayerComposer] Validation succeeded. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1'
```

Second Apply/Rebuild remained idempotent:

```text
[Immersive.Framework][PlayerComposer] Apply/Rebuild completed. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1' created='0' repaired='0' alreadyValid='13' skippedByPolicy='2' blocked='0' resetEnabled='False' resetParticipantPolicy='None'
```

## Product Conclusions

- FIRSTGAME validated the official package Composer, not a local replacement facade.
- `PlayerPrototype` can be configured through `PlayerComposer` intent.
- Apply/Rebuild can be run repeatedly without creating duplicates or repairing stable materialization.
- `blocked='0'` confirms no materialization blocker was reported in the validated path.
- Reset stayed optional by default: `resetEnabled='False'` and `resetParticipantPolicy='None'`.
- `skippedByPolicy='2'` is expected for optional reset materialization in this configuration.
- The local FIRSTGAME canonical player-binding facade was removed to prevent regression to consumer-only authoring.

## Current Limits

This evidence does not prove:

- QAFramework coverage;
- runtime gameplay authority;
- runtime input routing;
- movement;
- actor spawning;
- save, preferences or progression;
- `PlayerRecipe`;
- `PlayerRuntimeContext`.

QAFramework has not entered this validation path yet.

## Acceptance Meaning

This is consumer evidence that `PlayerComposer` is usable as the first official product surface in a real game project.

It should not be interpreted as proof that every future Player feature belongs inside the Composer. Future recurring intent should be formalized through package-owned product surfaces such as `PlayerRecipe`, Composer improvements, runtime contexts or samples when those scopes are explicitly approved.
