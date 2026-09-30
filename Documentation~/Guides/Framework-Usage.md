# Framework Usage

This page is a cross-cutting orientation, not a second getting-started tutorial. For the concrete first-use sequence, see [Getting Started](Getting-Started.md). For supported types, see the [Public API Reference](../API/Public-API.md).

## Authoring principles

- Use explicit component, asset, Project Settings, template or composer surfaces documented for the feature.
- Use a Profile when reusable intent is needed. Use Apply/Rebuild only where the owning guide documents materialization.
- Validate through the owning authoring surface; authoring validation and runtime readiness are separate evidence.
- Required configuration fails explicitly. Do not rely on hidden lookup, implicit fallback, a service locator or global runtime access.
- Keep optional features absent when the game does not need them.

The Framework runtime host is an internal composition root, not a consumer service locator. Architecture and historical status live under [Architecture](../Architecture/README.md).
