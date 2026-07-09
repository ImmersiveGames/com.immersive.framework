# PlayerComposer Editor Utility Manifest

Status: Package stabilization cut
Date: 2026-07-09
Package: `com.immersive.framework`

## Corte

Expose an editor-only `PlayerComposerApplyRebuildUtility` so the PlayerComposer Inspector and future QAFramework smokes use the same Apply/Rebuild implementation.

## Objetivo

Prepare the official PlayerComposer Product Surface for QA without requiring reflection or private Inspector method calls.

## Arquivos criados

```text
Editor/PlayerAuthoring/PlayerComposerApplyRebuildUtility.cs
Documentation~/Product/PLAYER-COMPOSER-EDITOR-UTILITY-MANIFEST.md
```

## Arquivos alterados

```text
Editor/PlayerAuthoring/PlayerComposerEditor.cs
```

## Arquivos removidos

```text
none
```

## Decisões

- `PlayerComposerApplyRebuildUtility` is editor-only.
- Inspector `Validate` and `Apply/Rebuild` now delegate to the utility.
- Future QAFramework smokes should call the utility directly.
- No runtime authority was added.
- No PlayerRecipe or PlayerRuntimeContext was added.
- No defaults or materialization policy were changed.

## Fora de escopo

```text
PlayerRecipe
PlayerRuntimeContext
QAFramework implementation
FIRSTGAME changes
runtime behavior changes
asmdef changes
new validators
new smokes
```

## Critérios de aceite

This cut is PASS if:

- Unity compiles;
- PlayerComposer Inspector Validate still works;
- PlayerComposer Inspector Apply/Rebuild still works;
- second Apply/Rebuild remains idempotent;
- future QA can call `PlayerComposerApplyRebuildUtility.ApplyOrRebuild`;
- no FIRSTGAME facade is reintroduced;
- no asmdef is changed.

## Commit message sugerida

```text
Product: expose PlayerComposer editor apply utility
```
