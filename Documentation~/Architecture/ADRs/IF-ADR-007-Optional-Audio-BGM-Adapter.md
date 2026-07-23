# IF-ADR-007 — Optional Audio BGM Adapter

Status: Accepted
Last updated: 2026-07-23
Supersedes: F47B Audio/BGM planning ADR
Superseded by: none

## Context

Route and Activity BGM selection is framework lifecycle semantics, while cue
assets, playback, fades and the audio runtime belong to `com.immersive.audio`.
Neither Framework Core nor the technical Audio package should absorb the
other's responsibility.

## Decision

The framework owns an optional Unity adapter assembly:

```text
Immersive.Framework.Audio
```

It compiles only when `com.immersive.audio` is installed, through the
`IMMERSIVE_FRAMEWORK_AUDIO` version define. The base
`Immersive.Framework.Runtime` assembly does not reference the Audio package,
and `com.immersive.audio` does not know Route, Activity or framework lifecycle.

The product surface is:

```text
FrameworkBgmDirector
FrameworkRouteBgmBinding
FrameworkActivityBgmBinding
FrameworkBgmActivityPolicy
```

The director selects effective BGM and delegates playback to an explicit
`AudioRuntimeHost`. It does not compose or replace Audio services.

Activity policies are:

```text
UseOwnOrRoute
UseOwnOrRetainActivityUntilRouteExit
UseRoute
Silence
```

Activity BGM wins when selected; retained Activity BGM may persist only within
the current Route; Route BGM is the fallback. `Silence` is explicit user intent,
not a missing cue or silent fallback. Route exit clears retained Activity state.

## Accepted scope

- Optional adapter assembly inside `com.immersive.framework`.
- Route/Activity lifecycle bindings and startup-Activity pre-application.
- Explicit `AudioRuntimeHost` reference.
- Typed Audio playback result and `[FRAMEWORK_BGM]` diagnostics.
- Route-scoped retention and explicit Silence policy.

## Rejected scope

- Framework base runtime depending on `com.immersive.audio`.
- Route/Activity semantics inside the technical Audio package.
- New package before independent release/versioning evidence exists.
- Audio manager/service locator, scene search or implicit host lookup.
- FIRSTGAME-specific adapters or permanent compatibility aliases.

## Consequences

Games without `com.immersive.audio` keep the framework base compile-safe.
Games using it receive framework-owned BGM lifecycle semantics without
duplicating playback primitives.

## Current implementation coverage

The optional asmdef, director, Route binding, Activity binding and policy enum
exist and remain marked Experimental. The implementation delegates to
`AudioRuntimeHost.PlayBgm`/`StopBgm` and reports missing host explicitly.

## Pending decisions

- Evidence for a separate adapter package and independent version lifecycle.
- Promotion from Experimental after current QA and real-game proof.
