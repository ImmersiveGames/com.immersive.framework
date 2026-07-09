# PlayerComposer QA Readiness Manifest

Status: Documentation cut
Date: 2026-07-09
Package: `com.immersive.framework`
Cut: P1G

## Cut

`P1G - PlayerComposer QA Readiness Plan`

## Objective

Define the minimum QA coverage expected for the official `PlayerComposer` Product Surface after package implementation and FIRSTGAME validation.

## Files created

```text
Documentation~/Product/PlayerComposer-QA-Readiness-Plan.md
Documentation~/Product/PLAYER-COMPOSER-QA-READINESS-MANIFEST.md
```

## Files altered

```text
none
```

## Files removed

```text
none
```

## Out of scope

```text
C# code
runtime changes
editor tooling changes
asmdef changes
QAFramework implementation
FIRSTGAME changes
PlayerRecipe
PlayerRuntimeContext
smokes
validators
new facade
removed FIRSTGAME player binding facade
```

## Evidence used

FIRSTGAME validated the official package `PlayerComposer` flow:

```text
Configured official PlayerComposer intent.
Validation succeeded.
Apply/Rebuild completed with blocked='0'.
Second Apply/Rebuild completed with created='0' repaired='0'.
Reset remained optional by default.
Legacy local player binding facade was removed.
```

## Decisions registered

- QA should validate the official `PlayerComposer` surface after FIRSTGAME proof.
- QA should not define the UX.
- QA should not use FIRSTGAME assets as its required base.
- QA should not recreate the removed FIRSTGAME facade.
- Minimum QA should cover positive materialization, idempotency, missing PlayerInput, and reset optional default.
- Optional reset transform policy should be covered when stable enough.

## Acceptance criteria

This documentation cut is PASS if:

- QA readiness plan is created;
- manifest is created;
- no code is changed;
- no asmdef is changed;
- no FIRSTGAME file is changed;
- no QAFramework file is changed;
- planned QA cases distinguish product authoring from diagnostics;
- planned QA cases use the official `PlayerComposer` surface;
- planned QA cases do not depend on removed FIRSTGAME facade.

## Commit message suggested

```text
Docs: define PlayerComposer QA readiness
```
