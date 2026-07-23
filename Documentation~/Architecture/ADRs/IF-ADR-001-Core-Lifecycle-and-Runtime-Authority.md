# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: Accepted
Last updated: 2026-07-23
Supersedes: legacy baseline, lifecycle, scene, runtime-content and host-authority ADR fragments
Superseded by: none

## Context

The framework needs one owner for application/session composition, Route and
Activity lifecycle, scene/content ownership and feature runtime bindings without
turning that owner into globally discoverable mutable state.

## Decision

`com.immersive.framework` owns framework-specific lifecycle and product modules.
It consumes technical primitives from `com.immersive.foundation`,
`com.immersive.logging` and `com.immersive.pooling`; it does not reimplement
them or push Route, Activity, Player or framework lifecycle into those packages.

`FrameworkRuntimeHost` is the internal application/session composition root. Its
factory is stateless: there is no static current-host field or lookup API.
Authoring and Unity adapters receive narrow typed runtime ports from bootstrap,
scene composition or the owning runtime module. Missing required bindings fail
explicitly.

The ownership hierarchy is:

```text
Game Application / Session
  -> Route
    -> Activity
      -> scoped content, participants and runtime materialization
```

Route owns its identity, primary/additive scene intent and local lifecycle.
Activity is a playable/contextual step within Route and owns contextual
readiness. Route switches exit the current Route before entering the next.
Release frees owned content; Reset reconfigures active state and is a separate
operation.

Functional identities are typed and domain-specific. Names, paths and strings
may appear in diagnostics but are not cross-domain functional keys.

## Accepted scope

- Framework settings, bootstrap, module composition and diagnostics.
- Session, Route and Activity lifecycle.
- Scene loading/composition and explicit content ownership.
- Runtime materialization with request, result, handle and ordered release.
- Explicit narrow runtime ports and fail-fast required configuration.
- Structured facts distinct from human log text.

## Rejected scope

- Static host registry, service locator, singleton shortcut or name lookup.
- Silent fallback for required modules.
- Technical packages owning framework lifecycle.
- Camera, audio, Player or gameplay rules becoming Route/Activity identity.
- Strings, hierarchy paths or `GameObject.name` fabricating identity.

## Consequences

Feature modules remain internal architectural units of one distributed package.
Unity adapters may be components, but runtime authority remains scoped and
explicit. QA-only host resolution is test harness infrastructure and is not a
production access path.

## Current implementation coverage

The internal host, explicit feature ports, bootstrap, Route/Activity runtimes,
scene lifecycle, content ownership and typed identity primitives exist. H2.4 and
the subsequent hygiene cut removed static host authority and superseded
compatibility paths; their Unity evidence is recorded in the tracker.

The more explicit Activity transition vocabulary separating authority, phase,
readiness and previous-Activity finalization remains only partially represented
and must not be documented as complete.

## Pending decisions

- Final public/internal transaction snapshot for Activity authority commit and
  previous-Activity finalization.
- Cancellation and compensation policy before Activity authority commit.
