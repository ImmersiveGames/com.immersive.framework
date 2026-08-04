# Application Frame Rate

Status: Experimental product surface  
Cut: `IF-APPLICATION-FRAME-RATE-01`

## Purpose

Application Frame Rate defines one application-level frame pacing policy on the active `GameApplicationAsset`.
The framework applies that policy at the beginning of `FrameworkRuntimeHost.StartAsync`, before startup scene composition.

The feature replaces scene-local frame limiter components. A game does not need a `MonoBehaviour` in every scene and does not need a second persistent manager.

## Authoring

Open the active `GameApplicationAsset` and configure:

```text
Performance
└── Frame Rate
    ├── Mode
    ├── Target Frame Rate
    └── VSync Count
```

### Use Unity Defaults

```text
Application.targetFrameRate
QualitySettings.vSyncCount
```

remain unchanged.

This is the compatibility default for existing Game Application assets.

### Target Frame Rate

The framework applies:

```text
QualitySettings.vSyncCount = 0
Application.targetFrameRate = configured target
```

Use this mode when an explicit FPS target is the intended authority.

### Vertical Sync

The framework applies:

```text
Application.targetFrameRate = -1
QualitySettings.vSyncCount = configured interval
```

Supported authored VSync Count values are `1` through `4`.

Mobile platforms may ignore `QualitySettings.vSyncCount`. The Inspector validation reports this as a warning rather than silently changing the selected policy.
XR providers may control refresh rate through platform-specific APIs; this cut does not replace those APIs.

## Runtime behavior

The policy is:

```text
validated completely
→ previous Unity values captured
→ both effective values applied
→ typed result stored on the FrameworkRuntimeHost
→ summary and Advanced diagnostics logged
→ normal framework startup continues
```

Invalid policy does not partially mutate Unity values. Framework startup returns an explicit failed result.

Repeated startup application is idempotent. When the current Unity values already match the policy, the result is `AppliedNoChange` or `AppliedNoChangePlatformLimited` when the selected platform limitation also applies.

## Diagnostics

Runtime evidence includes:

```text
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

Summary diagnostics use Info or Warning. Invalid policy uses Error. Detailed evidence is emitted at Debug level.

## Validation

The Game Application validation flow checks:

```text
policy exists
mode is defined
Target Frame Rate is greater than zero
VSync Count is between 1 and 4
mobile Vertical Sync limitation is visible
```

No invalid value is normalized to a different policy.

## Scope exclusions

This cut does not implement:

```text
dynamic FPS changes
Route- or Activity-specific FPS policies
Adaptive Performance
thermal management
XR refresh-rate selection
benchmarking
an on-screen FPS counter
```
