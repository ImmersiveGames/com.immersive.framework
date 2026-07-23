# ADR-PROD-0004 — First Reference Product Surface

Status: Superseded for Local Player implementation  
Date: 2026-07-09

> Historical decision. The product-surface principles remain valid, but the
> concrete `PlayerRecipe` / `PlayerComposer` proposal is not current
> implementation guidance. The current Local Player model separates the
> `PlayerInputManager`-provisioned technical host, its `Actor Mount`, the
> contextual Logical Actor materialized from `ActorProfile`, and the scoped
> runtime contexts that own participation and Actor preparation.

## Context

The product transition needs a concrete first reference surface. Without a first slice, the new direction remains abstract and future work can regress to loose components, validators, and smokes.

The package has strong runtime foundations in GameApplication, Route, Activity, Global UI, Pause, Loading, Transition, and Reset. FIRSTGAME proves those pieces can run a minimal real game.

FIRSTGAME also exposed the largest usability leak around the player. A minimal real player currently requires many technical declarations, input bindings, reset adapters, camera anchors, and validation/repair tools.

## Decision

The following records the original reference-surface proposal and is retained
for historical traceability.

The first reference product surface is:

```text
Player Recipe / Player Composer
```

The first slice must prove that a user can create and configure a framework-ready player without manually understanding every internal contract.

The player surface becomes the reference model for later product surfaces such as Camera, Global UI, Resettable Object, Route/Activity, Input, and Content.

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
