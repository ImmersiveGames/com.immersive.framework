# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: Accepted
Last updated: 2026-07-25
Supersedes: legacy baseline, lifecycle, scene, runtime-content and host-authority ADR fragments
Superseded by: none

## Context

The framework needs one owner for application/session composition, Route and
Activity lifecycle, scene/content ownership and feature runtime bindings without
turning that owner into globally discoverable mutable state.

Session-scoped participation can outlive Route and Activity changes. In particular,
a Logical Player may exist as a Session participant before a Route or Activity is
active and may remain valid after contextual gameplay content is released.

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
  -> session-scoped authorities and participants
     -> Logical Players
  -> Route
     -> Activity
        -> contextual projection, readiness and materialization
```

A Logical Player is a Session participant associated with a typed
`PlayerSlotId`. Its existence does not imply an Actor, materialization,
presentation or gameplay readiness.

Route and Activity do not own the identity or lifetime of a Session Logical Player.
They may:

```text
project eligible Logical Players
require progressive participation evidence
prepare or adopt contextual Actor content
enable contextual input, Camera and gameplay
release only the contextual parts they own
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
- Session-scoped Logical Player participation independent of Route/Activity lifetime.
- Scene loading/composition and explicit content ownership.
- Runtime materialization with request, result, handle and ordered release.
- Explicit narrow runtime ports and fail-fast required configuration.
- Structured facts distinct from human log text.

## Rejected scope

- Static host registry, service locator, singleton shortcut or name lookup.
- Silent fallback for required modules.
- Technical packages owning framework lifecycle.
- Camera, audio, Player or gameplay rules becoming Route/Activity identity.
- Requiring every Logical Player to originate inside a Route or Activity.
- Destroying or invalidating a Session Logical Player only because Route or Activity exits.
- Strings, hierarchy paths or `GameObject.name` fabricating identity.

## Consequences

Feature modules remain internal architectural units of one distributed package.
Unity adapters may be components, but runtime authority remains scoped and
explicit. QA-only host resolution is test harness infrastructure and is not a
production access path.

Session participation and contextual gameplay lifetime remain separate. Route and
Activity can consume a Logical Player without becoming its Session authority or
physical owner.

## Current implementation coverage

The internal host, explicit feature ports, bootstrap, Route/Activity runtimes,
scene lifecycle, content ownership and typed identity primitives exist. H2.4 and
the subsequent hygiene cut removed static host authority and superseded
compatibility paths; their Unity evidence is recorded in the tracker.

`PlayerParticipationRuntimeContext` already represents Session-scoped participation.
The Manager-Provisioned and Scene-Provided Logical Player sources exist. The
Session-Persistent Logical Player source is an accepted architectural gap and is
not documented as implemented by this ADR update.

The more explicit Activity transition vocabulary separating authority, phase,
readiness and previous-Activity finalization remains only partially represented
and must not be documented as complete.

## Pending decisions

- Final public/internal transaction snapshot for Activity authority commit and
  previous-Activity finalization.
- Cancellation and compensation policy before Activity authority commit.
- Concrete authoring and runtime contract for the Session-Persistent Logical Player source.
