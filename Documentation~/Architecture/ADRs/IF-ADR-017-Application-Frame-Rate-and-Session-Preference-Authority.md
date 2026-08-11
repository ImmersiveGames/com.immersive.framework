# IF-ADR-017 — Application Frame Rate and Session Preference Authority

Status: **Proposed**  
Date: 2026-08-11  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-010  
Source finding: Package → ADR Reverse Audit RA-01

## Context

The package already implements application frame pacing through
`ApplicationFrameRatePolicy`, `ApplicationFrameRatePolicyApplier`,
`FrameworkRuntimeHost` and the `GameApplicationAsset` Inspector.

The existing implementation establishes real runtime policy:

```text
UseUnityDefaults
TargetFrameRate
VerticalSync
```

and mutates Unity process-level frame pacing values:

```text
Application.targetFrameRate
QualitySettings.vSyncCount
```

This behavior is currently documented by a product Guide but is not owned by an
accepted architecture decision.

The current policy is authored on `GameApplicationAsset`. That placement mixes two
different concerns:

```text
Game Application intent
  startup Route
  Player Session
  Persistent Content
  application graph

Project/runtime platform policy
  frame pacing
```

Frame pacing should exist for the project even before any particular gameplay Route
or Activity is considered.

The framework also contains an experimental `Preferences` foundation. A future
consumer-facing frame-rate option may persist a user's preferred frame pacing mode,
but persistence must not become runtime authority over Unity global frame pacing.

## Decision

### Project-level authored authority

`Project Settings > Immersive Framework` owns the required authored frame-rate
baseline for the project.

The backing authority is `ImmersiveFrameworkSettingsAsset`.

The project frame-rate policy is always present as valid authored intent.

`UseUnityDefaults` is an explicit valid policy. It means:

```text
framework owns the decision to not override Unity frame pacing
```

It is not equivalent to a missing configuration.

The frame-rate policy is removed from `GameApplicationAsset` as a second authoring
authority.

### Runtime authority

Unity frame pacing mutation is owned by one application-lifetime runtime authority
inside the framework runtime composition.

Conceptually:

```text
Project Frame Rate Policy
        ↓
boot validation / resolution
        ↓
Application Frame Rate Runtime
        ↓
Application.targetFrameRate
QualitySettings.vSyncCount
```

`Project Settings` is authoring authority, not mutable runtime authority.

No scene-local limiter component, singleton, service locator or second persistent
manager is introduced.

### Session override

The application-lifetime frame-rate runtime may accept an explicit session-scoped
override request.

The override changes the **effective runtime policy** for the current application
session. It does not rewrite Project Settings or the authored project baseline.

Conceptually:

```text
Project baseline
    ↓
Application Frame Rate Runtime
    ↑
Session Frame Rate Override
```

The project policy must explicitly state whether session override is allowed.

Default:

```text
Allow Session Override = false
```

A project that wants a player-facing frame-rate option enables this capability
explicitly.

When session override is disabled, override requests fail explicitly.

### Preferences boundary

`Preferences` may later persist a user's frame-rate preference.

Persistence is not frame-rate authority.

The future relationship is:

```text
Preferences Store
  persisted user choice
        ↓
Session preference resolution
        ↓
typed session override request
        ↓
Application Frame Rate Runtime
```

`Preferences` must not directly mutate:

```text
Application.targetFrameRate
QualitySettings.vSyncCount
```

The current ADR does not promote the entire experimental Preferences subsystem into a
canonical persistence architecture.

A future Preferences/Persistence decision must define storage, initialization,
corruption handling and cross-session lifetime before persistence is integrated.

### Effective-policy resolution

At runtime there is one effective policy.

Without an active session override:

```text
effective = project baseline
```

With a valid allowed session override:

```text
effective = session override
```

An invalid override is rejected explicitly.

The runtime does not silently normalize an invalid override to another mode/value.

Removal of a session override restores the authored project baseline through the same
runtime authority.

### Supported authored modes

#### UseUnityDefaults

Preserve the current Unity values.

#### TargetFrameRate

Apply:

```text
QualitySettings.vSyncCount = 0
Application.targetFrameRate = configured target
```

#### VerticalSync

Apply:

```text
Application.targetFrameRate = -1
QualitySettings.vSyncCount = configured interval
```

The current supported VSync interval remains `1..4`.

### Boot validation

The framework boot configuration is invalid when the required project frame-rate
policy cannot be resolved or validated.

Validation occurs before runtime mutation.

No invalid policy may partially mutate Unity frame pacing state.

### Diagnostics

The runtime exposes typed evidence for every application attempt:

```text
source
  ProjectBaseline
  SessionOverride
  ProjectBaselineRestored

status
requested mode
requested target frame rate
requested VSync count
previous target frame rate
previous VSync count
effective target frame rate
effective VSync count
platform
message
```

Platform limitations remain explicit diagnostics.

A platform limitation does not silently replace the selected policy.

## Product surface

Canonical project authoring:

```text
Edit
  Project Settings
    Immersive Framework
      Performance
        Frame Rate
          Mode
          Target Frame Rate
          VSync Count
          Allow Session Override
```

`GameApplicationAsset` no longer owns the Frame Rate section.

A future player-facing/session-facing preference surface is a separate product cut.

## Authority summary

| Concern | Authority |
|---|---|
| Required project baseline | `ImmersiveFrameworkSettingsAsset` |
| Project editing surface | `Project Settings > Immersive Framework` |
| Effective Unity frame pacing | scoped application runtime authority |
| Runtime override request | current Session |
| Persisted user choice | future `Preferences` integration |
| Unity global values | runtime output/effect, not authored authority |
| Diagnostics | projection only |

## Explicit non-goals

This ADR does not introduce:

```text
global Performance Manager
service locator
scene-local FPS limiter
Route-specific FPS
Activity-specific FPS
automatic adaptive performance
thermal management
XR refresh-rate authority
benchmarking
FPS counter
quality-preset system
persistence orchestration
save-game integration
```

## Migration

The existing `GameApplicationAsset.frameRatePolicy` is superseded by the project-level
policy.

Migration must be explicit and diagnostic.

Do not create two active authored authorities.

The implementation cut may either:

```text
A. migrate existing serialized value into Project Settings through an explicit Editor action
or
B. require the Project Settings value to be configured and mark the former field obsolete/removed
```

The package must not silently copy an arbitrary Game Application policy into project
authority at runtime.

## Consequences

Positive:

```text
frame pacing has one project-level authored authority
all games have an explicit baseline
runtime mutation remains scoped and typed
session customization is possible without changing project assets
Preferences can persist intent without owning performance runtime
no second manager/singleton is introduced
```

Tradeoffs:

```text
existing GameApplication assets require migration
runtime must distinguish authored baseline from effective override
future Preferences integration requires a separate persistence decision
```

## Acceptance conditions

The ADR can move from Proposed to Accepted when:

```text
Project Settings ownership is agreed
GameApplication ceases to be a second frame-rate authority
runtime effective-policy ownership is explicit
session override semantics are frozen
Preferences is explicitly storage/input, not runtime authority
migration behavior is explicit
no silent fallback path exists
```
