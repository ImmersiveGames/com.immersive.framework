# IF-ADR-017 — Stage A Project Frame Rate Cut

Date: 2026-08-11  
Type: architecture + product authoring + runtime  
State: implementation prepared; QA pending

## Objective

Move Application Frame Rate from `GameApplicationAsset` to the canonical project
settings surface and make the project policy an explicit boot input.

## Scope

```text
A1 Project Settings owns authored Frame Rate
A2 Boot validates/resolves the project policy
A3 GameApplication loses the duplicate authority
A4 Validation, diagnostics and Guide align
A5 Focused QA remains
```

## Out of scope

```text
legacy migration
legacy compatibility fields
Session override
Preferences persistence
dynamic FPS
Adaptive Performance
```

## Files changed

```text
EDIT Runtime/Authoring/ImmersiveFrameworkSettingsAsset.cs
EDIT Runtime/Authoring/GameApplicationAsset.cs
EDIT Runtime/Bootstrap/FrameworkBootValidator.cs
EDIT Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs
EDIT Runtime/ApplicationLifecycle/FrameworkRuntimeHost.FrameRate.cs
EDIT Editor/Validation/ApplicationFrameRateAuthoringValidator.cs
EDIT Editor/Settings/ImmersiveFrameworkSettingsProvider.cs
EDIT Editor/Authoring/GameApplicationAssetEditor.cs
EDIT Documentation~/Guides/Application-Frame-Rate-Usage.md

CREATE Documentation~/Architecture/ADRs/
  IF-ADR-017-Application-Frame-Rate-Project-Authority.md

CREATE Documentation~/Architecture/Plans/
  IF-ADR-017-STAGE-A-PROJECT-FRAME-RATE-CUT-2026-08-11.md
```

## Removed authored surface

```text
GameApplicationAsset.frameRatePolicy
GameApplicationAsset.FrameRatePolicy
Game Application Inspector > Performance > Frame Rate
```

No replacement legacy field is retained.

## Expected product flow

```text
Project Settings
  Immersive Framework
    Performance
      Frame Rate
        configure
        validate

Validate Configuration
  ↓
Play Mode
  ↓
boot
  ↓
ProjectSettings policy applied
```

## Technical smoke expected

Stage A QA should prove:

```text
ADR017-QA-01 TargetFrameRate baseline
ADR017-QA-02 VerticalSync baseline
ADR017-QA-03 UseUnityDefaults preserves values
ADR017-QA-04 invalid policy blocks before mutation
ADR017-QA-05 GameApplication cannot independently affect Frame Rate
```

## Technical acceptance

```text
compiles
no GameApplication Frame Rate field/property
Project Settings policy exists by default
boot rejects null/invalid project policy
host receives policy explicitly
no runtime Resources lookup for Frame Rate
no silent fallback
no partial invalid mutation
runtime diagnostics show source=ProjectSettings
```

## Product acceptance

```text
Frame Rate is discoverable in Project Settings
default is understandable
UseUnityDefaults is explicit
GameApplication no longer duplicates the control
validation is embedded in the Project Settings flow
```

## Architectural gain

```text
before:
GameApplicationAsset -> Frame Rate -> FrameworkRuntimeHost

after:
ImmersiveFrameworkSettingsAsset
  -> validated project baseline
  -> explicit bootstrap handoff
  -> FrameworkRuntimeHost
  -> Unity frame pacing
```

## Suggested commit

```text
refactor(performance): move frame rate policy to project settings
```
