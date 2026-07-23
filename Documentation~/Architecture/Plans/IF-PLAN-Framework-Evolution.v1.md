# IF-PLAN — Framework Evolution

Status: Accepted / Immutable
Version: v1
Last updated: 2026-07-23
Supersedes: fragmented roadmaps and mutable implementation plans
Superseded by: none

## Purpose

Define the stable route for evolving `com.immersive.framework` without turning
plans into execution diaries or reopening frozen technical packages.

## Tracks

1. Framework Core: settings, bootstrap, scoped runtime authority and lifecycle.
2. Product authoring: Profiles, Recipes, Authoring/Composer and Apply/Rebuild.
3. Player: participation, Actor lifecycle, input and camera eligibility.
4. Camera: rig authoring, request arbitration and output ownership.
5. Optional adapters: framework-owned semantics over sibling/third-party packages.
6. Interaction foundations: InputMode, Pause, Gate and Reset.
7. Operations: loading, transitions, persistence and diagnostics.

## Planned order

For each newly selected product cut:

1. Reconcile current source, package boundary and active consumer evidence.
2. Decide public concept, Inspector language and runtime owner in an ADR.
3. Define a complete authoring-to-runtime cut and explicit non-goals.
4. Implement inside `com.immersive.framework`; consume technical packages.
5. Validate statically, then obtain Unity compile/import and focused QA evidence.
6. Prove real-game usability in FIRSTGAME when the surface is product-facing.
7. Update only the tracker and affected ADR/guide.

Only one active product cut should be selected at a time unless dependencies are
independent and explicitly planned.

## Gates

- Package/module owner is explicit.
- Required configuration fails fast.
- No static lookup, service locator, singleton shortcut or silent fallback.
- No new abstraction without at least two concrete use cases.
- Public/serialized changes include a migration strategy.
- Product features document create, author, apply/rebuild, runtime and debug flow.
- Unity validation is recorded only from actual user/CI evidence.

## Non-goals

- Recreating Base/NewScripts architecture.
- Splitting internal modules into packages without independent reuse/versioning.
- Mutating frozen technical-package baselines.
- Tracking micro-cuts, commit diaries or smoke transcripts in this plan.

## Validation policy

Static checks may verify boundaries and references. Unity compile, import,
Play Mode, smoke and consumer proof must be executed outside documentation-only
work and recorded honestly in the tracker.

## Change policy

Progress is not tracked here. Use
`../Tracking/IF-TRACK-Framework.md`.

Material route changes require a new plan version. This file may then receive
only a `Superseded by` metadata update.
