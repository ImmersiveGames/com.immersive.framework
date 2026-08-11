# Application Frame Rate

Status: Current Stage A product surface; ADR-017 QA certification pending  
Architecture: `IF-ADR-017 — Application Frame Rate Project Authority`

## Purpose

Application Frame Rate defines one required project-level frame pacing baseline.

The canonical authoring surface is:

```text
Edit
  Project Settings
    Immersive Framework
      Performance
        Frame Rate
```

The backing asset is `ImmersiveFrameworkSettingsAsset`.

`GameApplicationAsset` does not own frame-rate policy.

The framework applies the validated project policy at the beginning of
`FrameworkRuntimeHost.StartAsync`, before startup scene composition.

The feature replaces scene-local frame limiter components. A game does not need a
`MonoBehaviour` in every scene and does not need a second persistent manager.

## Required does not mean forced FPS

Every Framework project has an explicit frame-rate policy.

The default is:

```text
Mode = Use Unity Defaults
```

This is a valid authored decision:

```text
framework owns the decision not to override Unity frame pacing
```

It is not missing configuration.

## Authoring

Open:

```text
Project Settings
  Immersive Framework
    Performance
      Frame Rate
        Mode
        Target Frame Rate
        VSync Count
```

### Use Unity Defaults

```text
Application.targetFrameRate
QualitySettings.vSyncCount
```

remain unchanged.

### Target Frame Rate

The framework applies:

```text
QualitySettings.vSyncCount = 0
Application.targetFrameRate = configured target
```

### Vertical Sync

The framework applies:

```text
Application.targetFrameRate = -1
QualitySettings.vSyncCount = configured interval
```

Supported authored VSync Count values are `1` through `4`.

Mobile platforms may ignore `QualitySettings.vSyncCount`. Project Settings validation
reports this as a warning rather than silently changing the selected policy.

XR providers may control refresh rate through platform-specific APIs; this cut does
not replace those APIs.

## Boot and runtime authority

The canonical path is:

```text
ImmersiveFrameworkSettingsAsset
  project authored baseline
        ↓
FrameworkBootValidator
  validates complete policy
        ↓
ImmersiveFrameworkBootstrap
  passes the validated policy explicitly
        ↓
FrameworkRuntimeHost
  retains the project baseline for this application lifetime
        ↓
ApplicationFrameRatePolicyApplier
  mutates Unity frame pacing
```

`FrameworkRuntimeHost` does not rediscover Frame Rate from Resources and does not read
it from `GameApplicationAsset`.

Invalid policy does not partially mutate Unity values.

## Diagnostics

Runtime evidence includes:

```text
source = ProjectSettings
status
requested mode
requested target frame rate
requested VSync count
previous target frame rate
previous VSync count
applied target frame rate
applied VSync count
runtime platform
message
```

Summary diagnostics use Info or Warning. Invalid policy uses Error. Detailed evidence
is emitted at Debug level.

## Validation

Project Settings validation checks:

```text
policy exists
mode is defined
Target Frame Rate is greater than zero
VSync Count is between 1 and 4
mobile Vertical Sync limitation is visible
```

Boot performs the blocking validation independently of the Inspector.

No invalid value is normalized to a different policy.

## Future Session / Preferences direction

The current Stage A cut does **not** implement mutable Session override or preference
persistence.

The intended later relationship is:

```text
Project Settings
  baseline
     ↓
Frame Rate runtime authority
     ↑
Session override request
     ↑
future Preferences resolution
```

Preferences may persist user intent in a later cut, but must not directly mutate
`Application.targetFrameRate` or `QualitySettings.vSyncCount`.

## Scope exclusions

This cut does not implement:

```text
Session frame-rate override
Preferences persistence
dynamic adaptive FPS
Route- or Activity-specific FPS policies
Adaptive Performance
thermal management
XR refresh-rate selection
benchmarking
an on-screen FPS counter
```
