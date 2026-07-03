# F37 ADR TRANSITION 001 - Transition Gate Policy

Status: Accepted

## Context

Route and Activity transitions can run visual fade windows while scene-authored UI remains clickable. A repeated click during an Activity fade can reach `ActivityRequestTrigger` and be reported as an already-active or in-flight request, even though the user intent should be blocked by the transition window.

Pause already owns `Global/Pause`, `PauseRuntime`, Gate blockers for Pause capabilities and the `Time.timeScale = 0` simulation effect. Transition must not reuse Pause state, duplicate Pause input maps, or use `Time.timeScale`.

## Decision

`com.immersive.framework` adds an explicit `TransitionGateMode` policy for Route and Activity authoring:

- `None`: visual transition only.
- `LifecycleRequestsOnly`: blocks concurrent lifecycle/gameflow requests.
- `InputAndInteraction`: blocks lifecycle requests, input acceptance and interaction acceptance.
- `InputInteractionAndGameplay`: blocks lifecycle requests, input acceptance, interaction acceptance and gameplay actions.

`RouteAsset` owns Route transition gate policy. `ActivityAsset` owns Activity transition gate policy. Runtime translates the policy into passive `GateBlocker` entries through `TransitionGateBlockerPolicy` and keeps a `GateSnapshot` active only during the transition/lifecycle window.

The gate is applied before `TransitionBefore` and released after `TransitionAfter`. A `finally` release protects against stuck gates when a transition or lifecycle operation fails.

## Consequences

- Transition Gate is not Pause.
- Transition Gate does not use `Time.timeScale`.
- `Global/Pause` remains the single canonical Pause input action.
- `PauseKeepUiActionMap` is not canonical framework behavior.
- For First Game fades, set Route and Activity `Transition Gate` to `InputInteractionAndGameplay`.
- `UnityFadeCurtainEffectAdapter` remains the minimal UI-blocking surface: when visible and configured with `Block Raycasts When Visible`, repeated UI clicks do not reach request triggers.

## Validation

Manual smoke should confirm that clicking `Go Activity B` repeatedly during fade does not produce `IgnoredAlreadyActive`, and that buttons work again after the transition closes.
