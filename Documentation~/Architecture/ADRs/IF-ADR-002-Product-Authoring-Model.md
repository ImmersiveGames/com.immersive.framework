# IF-ADR-002 — Product Authoring Model

Status: Accepted
Last updated: 2026-07-23
Supersedes: fragmented product-surface specifications and manifests
Superseded by: none

## Context

Framework features must be usable by Unity teams, not exposed only as contracts,
validators and QA menus. The public vocabulary must communicate user intent and
separate immutable authoring from scoped runtime state.

## Decision

Use these roles consistently:

```text
Profile
  immutable stable product identity or selectable option

Recipe
  reusable construction or materialization intent

Composer / Authoring
  concrete prefab or scene surface

Apply / Rebuild
  idempotent technical materialization from authored intent

Runtime Context / Session / Module
  mutable state with explicit lifetime and dependencies

Diagnostics / Validation
  evidence and problem reporting, not the primary product flow
```

Owner-local scalar or reference configuration stays on its owning asset unless
an independently reusable concept is demonstrated. A ScriptableObject is not
automatically a Profile. Profiles and Recipes are immutable runtime inputs;
mutable state never writes back to them.

Every recurring product feature should document how to create, author,
validate, apply/rebuild when applicable, run, diagnose and integrate it into a
real game. Inspector defaults are designer-facing; technical evidence belongs
under Advanced/Debug.

## Accepted scope

- `GameApplicationAsset`, `RouteAsset` and `ActivityAsset` as primary intent.
- Profiles for stable identities such as Player Slots and Actors.
- Recipes for reusable technical construction such as Camera rigs.
- Explicit authoring components for scene/prefab composition.
- Idempotent Apply/Rebuild where derived components are materialized.
- Validators and diagnostics supporting the product workflow.

## Rejected scope

- Manager/Coordinator/Processor as an unresolved ownership bucket.
- New abstraction without at least two concrete use cases.
- Validator/smoke as the only user experience.
- Runtime mutation of Profiles or Recipes.
- Hidden compatibility rails, reflection-based architecture or silent repair.
- Public terminology copied from old Base/NewScripts without product review.

## Consequences

The package may expose several product surfaces while keeping one runtime
authority per domain. Product authoring remains explicit and testable without
making technical materialization the user-facing concept.

## Current implementation coverage

Camera has the complete Recipe → Composer → Validate → Apply/Rebuild flow.
Scene Local Player Admission and Pause provide explicit authoring and
validation surfaces. Local Player provisioning is explicit but does not use a
`PlayerRecipe` or `PlayerComposer`; those obsolete claims were removed.

## Pending decisions

- Which product lane receives the next complete authoring workflow.
- Which mature workflows warrant distributed Samples.
