# F24-ADR-UNITY-BUILD-001 - Unity Build Surface / Lifecycle Wiring

Status: Accepted / F24A Planned  
Phase: F24 - Unity Build Surface / Lifecycle Wiring  
Type: Framework Unity Build Surface / Lifecycle Wiring Boundary  
Last updated: 2026-06-26

## Context

F23 is closed as Pause intent/requirement-only. The framework now needs a phase that proves existing contracts through minimal real Unity wiring before optional adapter modules begin.

The gap is:

```text
contract exists
synthetic smoke can pass
but the real Unity lifecycle path is not wired yet
```

That gap belongs to F24, not to F25 adapter modules.

## Decision

F24 is Unity Build Surface / Lifecycle Wiring.

F24 comes after F23 and before later Activity scene operation, loading progress and adapter module planning work.

F24 owns minimal Unity surfaces that prove framework contracts through real lifecycle wiring. It does not build product gameplay or broad subsystem adapters.

## Critical Ordering

F24B must be the first technical cut:

```text
F24B - Transition <-> GameFlow Runtime Integration
```

Reason: `Transition` exists as framework language, but `RouteRequestTrigger` / `GameFlow` must pass through a real `TransitionPlan` before curtain, loading screen or pause overlay visuals become meaningful.

## F24 Plan

| Cut | Name | Objective |
|---|---|---|
| F24A | Unity Build / Lifecycle Wiring ADR Plan | Lock the F24 boundary and ordering. |
| F24B | Transition <-> GameFlow Runtime Integration | Wire the real route/gameflow transition path. |
| F24C | Transition Curtain Unity Build | Build the minimal curtain surface after lifecycle wiring is real. |
| F24D | Loading Screen Unity Adapter Build | Build the minimal loading screen adapter surface. |
| F24E | Pause Overlay Unity Build | Build the minimal pause overlay Unity surface from F23 intent contracts. |
| F24F | Save Moment Authoring Boundary | Define save moment authoring without gameplay payload ownership. |
| F24G | Preferences Authoring Boundary | Define preferences authoring without progression slot ownership. |
| F24H | Closure + Usage Guide | Close F24 and document usage. |

## Exclusions

F24 does not create:

- gameplay adapters
- camera/audio/input adapter modules
- actor/player/NPC modules
- inventory/combat/projectile/damage modules
- full UI system
- product gameplay
- parallel lifecycle pipeline
- replacement for Route, Activity, Gate, Transition, Loading, Pause or Save ownership

F24A specifically does not implement runtime code, asmdef changes, GameObjects, prefabs, scene objects, ScriptableObjects, UI or smoke execution.

## Consequences

F24 prevents framework contracts from jumping directly from synthetic documentation/runtime primitives into gameplay adapter work.

F25 can start only after the framework has proven the real Unity lifecycle/build surfaces needed by adapter modules.
