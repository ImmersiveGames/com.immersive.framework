# Immersive Framework

`com.immersive.framework` is the official Unity package for framework runtime,
authoring, diagnostics and validation.

Current version: `1.0.0-preview.16`.

## H2 closure

H2.4 closes the explicit runtime-port migration in the package source. Runtime
authoring and Unity adapters bind the narrow port they require from the
composition root; they do not discover a global `FrameworkRuntimeHost`.

`FrameworkRuntimeHost` remains the application/session composition root. Its
factory is stateless: it does not retain a static current-host reference or
offer a static lookup API. QA resolves a host only inside its friend-assembly
harness, from loaded scenes, and rejects anything other than exactly one
candidate.

The H2.4 Unity evidence is approved: the focused smoke passed with 10 cases.
See the current execution status below.

## Current product surfaces

```text
PlayerRecipe (optional) -> PlayerComposer -> Validate -> Apply/Rebuild
GameApplication -> UIGlobal Host Registration -> LocalPlayerProvisioningAuthoring -> canonical P3 runtime lane
CameraRigRecipe -> CameraRigComposer -> Validate -> Apply/Rebuild
PausePlayerInputBinding -> PauseProductBindingRuntimeContext -> InputMode transaction -> UnityPlayerInputStateWriter
```

P3 is the only Player runtime lane. Session join assigns `PlayerSlotId` to the
stable `LocalPlayerHostAuthoring`; Player prefabs and `PlayerComposer` do not
pre-author Slot identity. The former passive F49/F51/F52 graph is removed.

Camera authoring and physical output ownership remain separate. Route, Activity,
Local Player and Session publish typed requests into the explicit output context.

Pause has one product path. While running it enables `Global + Player`; while
paused it enables `Global`. The retired Pause/InputMode bridge APIs are not part
of the package surface.

## Current delivery state

H2 is closed and Unity-validated at `1.0.0-preview.16`. No subsequent
implementation lane is selected yet.

## Start reading

- [Documentation index](Documentation~/README.md)
- [Current State](Documentation~/Current/00-Current-State.md)
- [Player Architecture Flow](Documentation~/Guides/Player-Architecture-Flow.md)
- [Camera Product Usage](Documentation~/Guides/Camera-Product-Usage.md)
- [H2 execution status](Documentation~/Current/05-Execution-Status.md)

QAFramework owns synthetic technical validation. FIRSTGAME owns minimal real-game
proof. Consumer assets and old project architecture do not belong in this package.
