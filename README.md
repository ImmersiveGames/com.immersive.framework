# Immersive Framework

`com.immersive.framework` is the official Unity package for framework runtime,
authoring, diagnostics and validation.

Current version: `1.0.0-preview.15`.

## Current product surfaces

```text
PlayerRecipe (optional) -> PlayerComposer -> Validate -> Apply/Rebuild
GameApplication -> UIGlobal Host Registration -> LocalPlayerProvisioningAuthoring -> canonical P3 runtime lane
CameraRigRecipe -> CameraRigComposer -> Validate -> Apply/Rebuild
```

P3 is the only Player runtime lane. Session join assigns `PlayerSlotId` to the
stable `LocalPlayerHostAuthoring`; Player prefabs and `PlayerComposer` do not
pre-author Slot identity. The former passive F49/F51/F52 graph is removed.

Camera authoring and physical output ownership remain separate. Route, Activity,
Local Player and Session publish typed requests into the explicit output context.

## Next active lane

Manual clean P3 regression, followed by FIRSTGAME consumer migration.

## Start reading

- [Documentation index](Documentation~/README.md)
- [Current State](Documentation~/Current/00-Current-State.md)
- [Player Architecture Flow](Documentation~/Guides/Player-Architecture-Flow.md)
- [Camera Product Usage](Documentation~/Guides/Camera-Product-Usage.md)

QAFramework owns synthetic technical validation. FIRSTGAME owns minimal real-game
proof. Consumer assets and old project architecture do not belong in this package.
