# Editor Authoring Standard

Status: Current  
Last updated: 2026-08-09  
Decision source: `IF-ADR-002`, `IF-ADR-010`

## Purpose

Immersive Framework Inspectors are product surfaces. They let a designer
configure intent, understand invalid state and inspect runtime evidence without
reading internal runtime ports, registry handles or implementation details.

The standard does **not** require automation.

```text
Manual explicit authoring is the default.
```

## Semantic order

Use only the sections that are meaningful for the feature:

1. Product purpose / intent.
2. Primary authoring configuration.
3. Identity where explicit authored identity exists.
4. Request metadata where the component owns it.
5. Configuration Status / Validation.
6. Runtime Binding / Runtime Evidence in Play Mode.
7. Explicit supported actions.
8. Advanced / Debug technical evidence.

This is semantic consistency, not a requirement that every Custom Editor use the
same helper or identical visual layout.

## Direct authoring is valid

A canonical feature may simply be:

```text
Add Component
-> configure fields
-> Validate
-> Play Mode evidence
-> Advanced / Debug
```

No Profile, Composer, Wizard or Apply/Rebuild is required unless the feature
lifecycle actually needs it.

## Conditional product layers

### Profile / Recipe / Template
Use when authored intent is genuinely reusable or has useful standalone identity.

### Composer
Use when one authored instance coordinates meaningful technical composition that
would otherwise require internal framework knowledge.

### Apply / Rebuild
Use only for real derived technical materialization. It must be explicit,
idempotent, deterministic, Undo-aware, non-destructive and diagnostic.

### Wizard / Create action
Optional. Use only when it improves a concrete creation problem. A Wizard must
not invent gameplay intent or hide architecture.

## Identity, Source and Reason

Identity names a stable authored concept. Source identifies the public request
surface. Reason describes a particular request. They are not interchangeable.

Identity generation is explicit. A deterministic suggestion may fill an empty
value, but it does not run during repaint, replace populated values or silently
regenerate on rename/move/import.

## Validation and runtime evidence

Validation is non-mutating unless the user explicitly invokes a documented safe
remediation action.

Inspector repaint must not:

```text
open scenes
create assets/GameObjects
bind runtime ports
admit a Player
register Reset state
execute gameplay
repair gameplay configuration
```

Runtime diagnostics are read-only by default. Technical IDs, tokens, revisions,
occurrences and raw evidence belong under Advanced / Debug.

## Explicit remediation

Remediation is not required for compliance.

When provided it must be narrowly scoped, predictable, Undo-aware when mutating
Editor state and must not choose gameplay intent.

Avoid broad `Fix Everything` or complete-setup operations when multiple valid
intent choices exist.

## Play Mode actions

A supported runtime Inspector action is explicit, Play Mode-only and invokes the
same public product method used by consumer code/UnityEvents. It never bypasses
the official runtime authority.

## Authoring safety checklist

- Use current serialized property names.
- No reflection, scene search, fallback binding or implicit repair.
- No mutation during passive repaint.
- Use Undo/dirty/prefab override recording for explicit Editor writes.
- Reject unsupported multi-object/prefab contexts explicitly rather than partly
  applying a destructive operation.
- Keep Editor-only code in Editor assemblies; runtime does not depend on Editor.
- Keep operational detail in Advanced / Debug.

## QA boundary

There is no generic "UX smoke".

QA proves objective technical Editor contracts only when they exist, such as
serialization stability, idempotent materialization, ownership preservation,
Undo/Redo or Prefab Stage safety. Those tests belong to the owning feature.

FIRSTGAME real integration may reveal UX friction. UX evaluation is qualitative
and does not become a QA certification phase.
