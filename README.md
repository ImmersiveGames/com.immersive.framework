# Immersive Framework

`com.immersive.framework` is the official Unity package for Immersive Framework runtime, authoring, diagnostics and validation surfaces.

Current package version: `1.0.0-preview.12`

## Current State

The package has the supported Preview 12 baseline for application boot, route/activity flow, scene lifecycle, transition/loading surfaces, pause, reset and activity restart. It consumes technical primitives from `com.immersive.foundation`, `com.immersive.logging` and other package dependencies instead of reimplementing them here.

## Project Roles

- `com.immersive.framework`: official framework package and public documentation.
- QA Harness: technical smoke coverage for package behavior.
- FIRSTGAME: real consumer project for usability and practical game-start validation.

FIRSTGAME is not the primary technical QA harness, and QA assets should not be copied into consumer projects as canonical setup.

## Current Surface

- Game Application boot and startup route.
- Route and Activity lifecycle.
- Scene lifecycle for route/activity flow.
- RuntimeContent handles and ContentAnchor materialization surfaces.
- Pause runtime, pause input and pause surface.
- Transition and Loading surfaces.
- Reset subjects, reset participants and reset execution.
- Object reset, reset group and Activity Restart.
- Runtime logging with `Info`, `Debug` and `Trace` levels.

## Start Reading

- User-facing guide: [`Documentation~/Guides/Usage/index.html`](Documentation~/Guides/Usage/index.html)
- Guide notes and current preview semantics: [`Documentation~/Guides/Usage/README.md`](Documentation~/Guides/Usage/README.md)
- Package documentation index: [`Documentation~/README.md`](Documentation~/README.md)

## Logging Policy

- `Info`: operational summaries.
- `Debug`: technical diagnostics.
- `Trace`: waiting, retry and polling noise.

## Do Not

- Do not copy QA Harness assets into a consumer project as the canonical setup.
- Do not use FIRSTGAME as the main technical QA harness.
- Do not copy old `ProjectSettings`, scenes, configs or runtime architecture from older projects.
- Do not depend on paid assets as canonical package setup.
- Do not edit installed package files inside a consumer project; update the package source instead.
