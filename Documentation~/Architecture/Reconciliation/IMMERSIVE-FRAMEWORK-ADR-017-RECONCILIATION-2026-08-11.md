# Immersive Framework — ADR-017 Reconciliation

**Date:** 2026-08-11  
**ADR:** IF-ADR-017 — Application Frame Rate Project Authority  
**Type:** technical reconciliation / Stage A certification  
**Package baseline:** `dbae01cf2fce27d4cd7311233e32fa1dc034e057`  
**Package commit:** `refactor(performance): move frame rate policy to project settings`

## Final disposition

```text
Architecture:          ACCEPTED / RECONCILED
Package Runtime:       IMPLEMENTED
Product Surface:       IMPLEMENTED
Technical QA:          CERTIFIED
Package divergence:    NONE
Technical remaining:   0%
Stage A:               CLOSED / 100%
```

The accepted Stage A boundary is the project-level Frame Rate baseline only.

Session-scoped Frame Rate override and persisted Preferences integration remain future
scope and are not Stage A gaps.

## Reconciled product authority

Canonical authoring:

```text
Project Settings
  Immersive Framework
    Performance
      Frame Rate
```

Backing authority:

```text
ImmersiveFrameworkSettingsAsset.FrameRatePolicy
```

Runtime path:

```text
Project Settings baseline
  -> FrameworkBootValidator
  -> explicit bootstrap handoff
  -> FrameworkRuntimeHost
  -> ApplicationFrameRatePolicyApplier
  -> Application.targetFrameRate / QualitySettings.vSyncCount
```

`GameApplicationAsset` has no serialized Frame Rate field and no Frame Rate API
authority.

There is no legacy fallback, migration source, runtime merge or second authored
authority.

## Technical QA certification

ADR017-A5 used a one-shot preboot sentinel:

```text
Application.targetFrameRate = 47
QualitySettings.vSyncCount = 2
```

before the framework `AfterSceneLoad` bootstrap.

The official runtime host had to report that sentinel as its previous values.

### Edit validation

```text
[ADR017_QA_EDIT]
status='Passed'
cases='13'
invalidProjectPolicy='RejectedBeforeMutation'
invalidApplier='RejectedWithoutPartialMutation'
projectSettingsAuthority='Present'
gameApplicationSerializedAuthority='Absent'
gameApplicationApiAuthority='Absent'
restored='True'
```

### TargetFrameRate

```text
[ADR017_QA_TARGET]
status='Passed'
cases='13'
source='ProjectSettings'
mode='TargetFrameRate'
previousTargetFrameRate='47'
previousVSyncCount='2'
appliedTargetFrameRate='73'
appliedVSyncCount='0'
runtimeStatus='Applied'
platform='WindowsEditor'
gameApplicationFrameRateAuthority='Absent'
```

### VerticalSync

```text
[ADR017_QA_VSYNC]
status='Passed'
cases='13'
source='ProjectSettings'
mode='VerticalSync'
previousTargetFrameRate='47'
previousVSyncCount='2'
appliedTargetFrameRate='-1'
appliedVSyncCount='3'
runtimeStatus='Applied'
platform='WindowsEditor'
gameApplicationFrameRateAuthority='Absent'
```

The successful run was followed by automatic fixture restoration. The earlier manual
restore was an explicit QA recovery action before this successful run and is not
package divergence.

### UseUnityDefaults

```text
[ADR017_QA_DEFAULTS]
status='Passed'
cases='13'
source='ProjectSettings'
mode='UseUnityDefaults'
previousTargetFrameRate='47'
previousVSyncCount='2'
appliedTargetFrameRate='47'
appliedVSyncCount='2'
runtimeStatus='SkippedUnityDefaults'
platform='WindowsEditor'
gameApplicationFrameRateAuthority='Absent'
```

This proves `UseUnityDefaults` is an explicit valid no-override policy and not a
silent fallback.

## Final QA matrix

```text
Edit Validation       PASS — 13/13
TargetFrameRate       PASS — 13/13
VerticalSync          PASS — 13/13
UseUnityDefaults      PASS — 13/13
```

No focused gate remains open.

## Technical acceptance

```text
PASS  Project Settings owns Frame Rate
PASS  project policy is required and validated
PASS  GameApplication has no Frame Rate field/property
PASS  boot receives the project policy explicitly
PASS  official host applies only the supplied baseline
PASS  invalid policy fails before mutation
PASS  invalid applier performs no partial mutation
PASS  TargetFrameRate semantics proven E2E
PASS  VerticalSync semantics proven E2E
PASS  UseUnityDefaults preservation proven E2E
PASS  fixture restoration proven
PASS  no silent fallback
```

## Product acceptance

The Stage A product surface was manually validated in Unity before focused QA:

```text
Project Settings > Immersive Framework > Performance > Frame Rate
```

The Game Application Inspector no longer duplicates the control.

The current boundary does not require a separate FIRSTGAME certification gate because
it is project-level application configuration rather than a gameplay-facing Session
preference.

A future Session override/player-facing preference is a separate product cut and
should receive FIRSTGAME proof when accepted.

## Architectural gain

```text
before:
GameApplicationAsset
  -> Frame Rate policy
  -> FrameworkRuntimeHost

after:
ImmersiveFrameworkSettingsAsset
  -> required project baseline
  -> boot validation
  -> explicit runtime handoff
  -> FrameworkRuntimeHost
  -> Unity frame pacing
```

This establishes one authored project authority and one runtime application path.

## Reopen conditions

Reopen ADR-017 Stage A only if:

```text
a focused regression reproduces current-contract divergence
the project-level authority contract changes
a second authored Frame Rate authority is introduced
boot/runtime handoff semantics change
supported frame-pacing mode semantics change
```

Do not reopen Stage A merely because Session override or Preferences persistence is
later accepted as a new scope.

## Suggested commit

```text
docs(architecture): certify ADR-017 stage A frame rate authority
```
