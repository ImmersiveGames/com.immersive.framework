# ADR-PROD-0003 — Domain Runtime Context Policy

Status: Accepted  
Date: 2026-07-09

## Context

Some framework features are purely authoring or contractual. Others operate real behavior in Play Mode.

When real runtime behavior exists, the framework needs explicit runtime authority. Without it, runtime ownership leaks into scattered scene components, ad-hoc facades, or eventually into global managers.

The product direction allows scoped runtime contexts, sessions, or services, but not implicit global access.

## Decision

Runtime authority must be domain-scoped, typed, explicit, and diagnostic.

Preferred shape:

```text
Route Runtime Context
Activity Runtime Context
Player Runtime Context
Input Runtime Context
Camera Runtime Context
Reset Runtime Context
Global UI Runtime Context
Content Runtime Context
Save Runtime Context
```

A generic Session may exist only as a lifetime/coordinator envelope for typed domain contexts. It must not become a universal registry, service locator, or manager.

### Domain identity

`RouteId` and `ActivityId` are the only functional identities for their respective domains. They are stable authored values and participate in ownership, equality, hashing, lookup and binding.

`RouteName` and `ActivityName` are presentation and observability data. Scene paths locate and compose content. Names and scene paths never act as identity or as fallback identity.

Changing a Route name or Primary Scene must not change Route identity. Changing an Activity name must not change Activity identity. Missing, malformed or duplicate authored IDs are explicit validation errors and are never repaired at runtime.

### Domain identity

`RouteId` and `ActivityId` are the only functional identities for their respective domains. They are stable authored values and participate in ownership, equality, hashing, lookup and binding.

`RouteName` and `ActivityName` are presentation and observability data. Scene paths locate and compose content. Names and scene paths never act as identity or as fallback identity.

Changing a Route name or Primary Scene must not change Route identity. Changing an Activity name must not change Activity identity. Missing, malformed or duplicate authored IDs are explicit validation errors and are never repaired at runtime.

## Rules

Runtime Contexts / Sessions / Services are acceptable only when they have:

```text
clear domain ownership
explicit lifetime
typed access
explicit dependencies
fail-fast required configuration
diagnostic results/logs
no implicit global lookup
```

They must not use:

```text
static Instance access
generic service locator access
Find/first object fallback as primary resolution
silent fallback for required state
name/path-based identity as functional binding
name or scene-path fallback for Route/Activity identity
name or scene-path fallback for Route/Activity identity
```

## Consequences

- Features with Play Mode authority must identify the owning domain context.
- Composer/Authoring should not become runtime gameplay ownership by accident.
- Runtime contexts should operate behavior; authoring components should compose, apply, validate, and diagnose.
- Existing runtime systems such as Route, Activity, Reset, Pause, Loading, and Transition should be formalized before being duplicated.
- Player, Input, Camera, Content, and Save require careful domain ownership before becoming larger runtime products.

## Non-goals

- This ADR does not create any runtime context.
- This ADR does not define final APIs for Player, Input, Camera, Content, or Save.
- This ADR does not approve a generic `PlayerManager`, `GameManager`, or global Session.

## Affected systems

Route, Activity, Player, Input, Camera, Reset, Global UI, Content, Save, and future runtime-facing modules.
