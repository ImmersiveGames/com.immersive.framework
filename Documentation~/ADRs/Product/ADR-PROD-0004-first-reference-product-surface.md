# ADR-PROD-0004 — First Reference Product Surface

Status: Superseded for the Local Player shape by `ADR-PROD-0007`, `ADR-PROD-0008`, and `ADR-PROD-0010`  
Date: 2026-07-09

> Historical direction only. This ADR proposed the first product-surface experiment; it does not describe an implemented `PlayerRecipe` or `PlayerComposer`. Current Local Player architecture is defined by the related Player participation, Actor materialization, and manual-join ADRs.

## Context

The product transition needs a concrete first reference surface. Without a first slice, the new direction remains abstract and future work can regress to loose components, validators, and smokes.

The package has strong runtime foundations in GameApplication, Route, Activity, Global UI, Pause, Loading, Transition, and Reset. FIRSTGAME proves those pieces can run a minimal real game.

FIRSTGAME also exposed the largest usability leak around the player. A minimal real player currently requires many technical declarations, input bindings, reset adapters, camera anchors, and validation/repair tools.

## Historical decision

The first reference product surface was proposed as:

```text
Player Recipe / Player Composer
```

The intended first slice was meant to prove that a user could create and configure a framework-ready player without manually understanding every internal contract.

That proposal remains useful as product-direction history, but the named `PlayerRecipe` / `PlayerComposer` shape is not current implementation guidance.

## Product goals

The first reference slice should prove:

```text
user creates a framework-ready player through a product surface
intent lives in Player Recipe or equivalent authoring surface
Player Composer configures the concrete scene/prefab instance
Apply/Rebuild materializes required bindings/adapters idempotently
technical components are moved to _Framework/_Bindings or Advanced/Debug evidence
basic Inspector speaks in domain terms: Player, Slot, Input, Camera Target, Reset
diagnostics confirm results but do not replace creation
FIRSTGAME can use the official surface without copying local facades as final API
```

## Scope boundary

The first slice may cover:

```text
player identity
player slot
PlayerInput reference
gameplay action map reference
camera target/look-at references or anchors
reset subject/participant materialization
technical binding materialization
diagnostics for applied player surface
```

## Out of scope

The first slice must not include:

```text
generic multiplayer join
generic spawn system
framework-owned movement
gameplay command execution
save/progression
full Camera Composer
full Route/Activity Composer
generic Session
copying FIRSTGAME paths, names, IDs, or local editor scripts
```

The Player Composer may accept gameplay components such as movement components as references or modules, but it must not become the owner of gameplay execution.

## Consequences

- Player becomes the first proof of the new product direction.
- FIRSTGAME is the real usability proof, not the source to copy into the package.
- QA technical coverage should follow the official product surface, not precede it as a substitute for UX.
- The player slice must define a reusable pattern for Recipe, Composer, Apply/Rebuild, materialization, runtime boundary, and diagnostics.

## Non-goals

- This ADR does not implement PlayerRecipe, PlayerComposer, PlayerRuntimeContext, or samples.
- This ADR does not decide final class names or serialized fields.
- This ADR does not remove existing player contracts, validators, or FIRSTGAME tools.

## Affected systems

Player, Actor, Slot, Input, Camera anchors, Reset, FIRSTGAME integration, QAFramework follow-up, and package documentation.
