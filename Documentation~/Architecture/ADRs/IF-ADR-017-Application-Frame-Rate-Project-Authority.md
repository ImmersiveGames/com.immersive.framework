# IF-ADR-017 — Application Frame Rate Project Authority

Status: **Accepted**  
Date: 2026-08-11  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-010  
Current implementation cut: ADR017-A1..A4  
QA certification: **CERTIFIED — ADR017-A5**

## Context

The package already implements Unity frame pacing through:

```text
ApplicationFrameRatePolicy
ApplicationFrameRatePolicyApplier
FrameworkRuntimeHost
```

The initial implementation placed the authored policy on `GameApplicationAsset`.
That made a project/process-level policy look like one part of the game application
graph and left the runtime decision without a normative ADR.

Frame pacing changes process-level Unity state:

```text
Application.targetFrameRate
QualitySettings.vSyncCount
```

It should therefore have one explicit project baseline and one runtime application
path.

## Decision

### Project authoring authority

`Project Settings > Immersive Framework` owns the required project Frame Rate policy.

The backing asset is:

```text
ImmersiveFrameworkSettingsAsset
```

The policy is always expected to exist and validate.

The default:

```text
UseUnityDefaults
```

is an explicit valid policy. Required configuration does not imply a forced FPS.

`GameApplicationAsset` is not a Frame Rate authority.

There is no legacy fallback, migration source or runtime merge between Project
Settings and Game Application.

### Boot boundary

The project policy is validated by the boot configuration before runtime mutation.

The bootstrap passes the validated policy explicitly to the application runtime host.

The runtime host does not rediscover Frame Rate through Resources after creation and
does not read Frame Rate from `GameApplicationAsset`.

### Runtime effect authority

The application-lifetime Framework runtime owns application of the effective project
Frame Rate policy.

The low-level Unity mutation remains isolated in
`ApplicationFrameRatePolicyApplier`.

No scene-local limiter, global singleton, service locator or second manager is
introduced.

### Supported modes

#### UseUnityDefaults

Preserve the current Unity values.

#### TargetFrameRate

```text
QualitySettings.vSyncCount = 0
Application.targetFrameRate = configured target
```

#### VerticalSync

```text
Application.targetFrameRate = -1
QualitySettings.vSyncCount = configured interval
```

Current authored VSync Count range remains `1..4`.

### Failure semantics

Invalid project policy blocks framework boot.

Validation occurs before Unity mutation.

No invalid policy may partially mutate Unity frame pacing state.

No invalid value is silently normalized to another policy.

Platform limitations are explicit diagnostics, not silent policy substitution.

## Product surface

```text
Edit
  Project Settings
    Immersive Framework
      Performance
        Frame Rate
          Mode
          Target Frame Rate
          VSync Count
```

The Game Application Inspector may point the user to Project Settings but does not
duplicate Frame Rate authoring.

## Diagnostics

Runtime evidence identifies:

```text
source = ProjectSettings
status
requested policy
previous Unity values
applied Unity values
platform
message
```

Diagnostics project runtime evidence and are not a second authority.

## Future extension direction — not current Stage A runtime

A later cut may allow a Session-scoped Frame Rate override.

A later Persistence/Preferences decision may allow a persisted user preference to
produce that Session override.

The intended direction is:

```text
Preferences
  persisted intent
      ↓
Session resolution
      ↓
typed Frame Rate override request
      ↓
application Frame Rate runtime authority
```

Preferences must not directly mutate Unity Frame Rate/VSync values.

The current ADR does not canonize the existing experimental Preferences subsystem and
does not define storage/corruption/cross-session semantics.

## Out of scope

```text
Session override implementation
Preferences integration
Route-specific FPS
Activity-specific FPS
Adaptive Performance
thermal management
XR refresh-rate authority
benchmarking
FPS counter
quality preset system
```

## Consequences

Positive:

```text
one project-level authored baseline
one runtime application path
GameApplication no longer duplicates Frame Rate authority
UseUnityDefaults remains explicit and valid
future preference support has a clean boundary
```

Tradeoffs:

```text
existing GameApplication serialized Frame Rate data is no longer consumed
projects must configure the Project Settings policy
Session override remains future work
```

## Stage A acceptance

```text
Project Settings owns Frame Rate
policy is required and validated
GameApplication has no Frame Rate field/property
bootstrap passes policy explicitly
runtime applies only the supplied project baseline
invalid policy fails before partial mutation
Project Settings exposes designer-first validation
Guide describes the canonical flow
focused QA certifies the current implementation
```


## Stage A certification

ADR017-A5 technical QA completed on 2026-08-11.

```text
Edit validation       PASS — 13/13
TargetFrameRate       PASS — 13/13
VerticalSync          PASS — 13/13
UseUnityDefaults      PASS — 13/13

Package divergence    NONE
Technical remaining   0%
Stage A               CLOSED / 100%
```

This certification records implementation evidence only. It does not expand the
normative boundary. Session-scoped override and Preferences persistence remain future
scope.
