# Framework Usage

This page summarizes cross-feature authoring principles. For capability discovery, supported surfaces, guide navigation and evidence, use the [Documentation capability index](../README.md). For first-use setup, see [Getting Started](Getting-Started.md).

## Authoring principles

- Use explicit component, asset, Project Settings, template or composer surfaces documented for the feature.
- Use a Profile when reusable intent is needed. Use Apply/Rebuild only where the owning guide documents materialization.
- Validate through the owning authoring surface; authoring validation and runtime readiness are separate evidence.
- Required configuration fails explicitly. Do not rely on hidden lookup, implicit fallback, a service locator or global runtime access.
- Keep optional features absent when the game does not need them.
- Treat hierarchy as composition only where the owning feature explicitly defines a hierarchy boundary; do not use GameObject names or paths as identity.

The Framework runtime host is an internal composition root, not a consumer service locator. Architecture and historical status live under [Architecture](../Architecture/README.md).

## Validation vocabulary

Keep these states separate when recording feature or sample progress:

- **Implemented**: the code/authoring change exists.
- **Tested**: automated or focused tests were executed.
- **Integrated**: the feature is composed in a real consumer context.
- **Validated**: the intended observable behavior was proven with appropriate evidence.

A working Inspector configuration by itself is not runtime validation, and a runtime result does not by itself prove package-import or release readiness.
