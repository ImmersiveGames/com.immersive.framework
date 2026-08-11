# IF-ADR-017 — Application Frame Rate Execution Plan

**Date:** 2026-08-11  
**Type:** architecture + product authoring + runtime + QA  
**ADR:** IF-ADR-017 — Application Frame Rate and Session Preference Authority  
**Package baseline inspected:** `cd1b6ffb1df707a63ca6f67f3905de9510a78e9c` (`master`, read-only)

## Objective

Move the existing frame-rate feature from `GameApplicationAsset` to the canonical
project-level Immersive Framework settings, establish one typed runtime authority,
and leave a safe extension point for Session override and future Preferences
persistence.

## Scope

### Stage A — Project baseline authority

```text
Project Settings
  -> required Frame Rate policy
  -> boot validation
  -> runtime application
  -> diagnostics
```

### Stage B — Session override

```text
Session
  -> typed override request
  -> runtime applies effective policy
  -> clear override restores project baseline
```

### Stage C — Future Preferences integration

```text
Preferences storage
  -> load persisted user choice
  -> resolve session preference
  -> issue typed Session override
```

Stage C is blocked on the separate Persistence Foundation Disposition decision.

## Out of scope

- dynamic adaptive FPS;
- Route/Activity overrides;
- thermal policy;
- XR refresh selection;
- quality presets;
- save-game integration;
- a global manager/service locator;
- Preferences architecture redesign in Stage A.

## Current source to change in Stage A

Expected package files:

```text
EDIT Runtime/Authoring/ImmersiveFrameworkSettingsAsset.cs
EDIT Editor/Settings/ImmersiveFrameworkSettingsProvider.cs
EDIT Runtime/Bootstrap/FrameworkBootValidator.cs
EDIT Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs
EDIT Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs
EDIT Runtime/ApplicationLifecycle/FrameworkRuntimeHost.FrameRate.cs

EDIT Runtime/Authoring/GameApplicationAsset.cs
EDIT Editor/Authoring/GameApplicationAssetEditor.cs

PRESERVE Runtime/Performance/ApplicationFrameRatePolicy.cs
PRESERVE Runtime/Performance/ApplicationFrameRatePolicyApplier.cs
PRESERVE Runtime/Performance/ApplicationFrameRateApplicationResult.cs
PRESERVE Runtime/Performance/ApplicationFrameRateApplicationStatus.cs
PRESERVE Runtime/Performance/ApplicationFrameRateMode.cs

EDIT Documentation~/Guides/Application-Frame-Rate-Usage.md
```

Additional validation/editor utility files may be edited if the existing frame-rate
validator is moved from Game Application validation to Project Settings validation.

## Desired Project Settings surface

```text
Immersive Framework

Application
  Active Game Application

Performance
  Frame Rate
    Mode
    Target Frame Rate
    VSync Count
    Allow Session Override

Logging
  Logging Config

Configuration
  Validate Configuration
```

Frame Rate should not remain duplicated in the Game Application Inspector.

## Required authored state

The project must always have a frame-rate policy object.

The default serialized policy is:

```text
Mode = UseUnityDefaults
Allow Session Override = false
```

Therefore an untouched project is valid and explicit.

"Required" means the policy exists and is valid, not that the framework must force an
FPS value.

## Boot flow after Stage A

```text
Load ImmersiveFrameworkSettingsAsset
  ↓
FrameworkBootValidator
  validates:
    Active Game Application
    Startup Route
    Primary Scene
    Project Frame Rate Policy
  ↓
resolve immutable boot configuration
  ↓
FrameworkRuntimeHost.TryCreate(...)
  receives project frame-rate baseline
  ↓
StartAsync
  ↓
Application Frame Rate Runtime applies baseline
  ↓
normal startup continues
```

A cleaner implementation should avoid having `FrameworkRuntimeHost` reach back into a
Resources settings asset after creation.

Prefer passing resolved configuration into the runtime host.

## Runtime shape

Stage A may keep the existing applier as the low-level Unity mutation adapter.

Recommended conceptual split:

```text
ApplicationFrameRatePolicy
  authored/immutable intent

ApplicationFrameRateRuntime
  application-lifetime effective-policy authority

ApplicationFrameRatePolicyApplier
  Unity mutation adapter
```

Do not create a globally accessible service.

The runtime may be owned directly by `FrameworkRuntimeHost` or by a narrow module
attached to it.

## Stage B — Session override contract

Recommended public/narrow operation:

```text
RequestFrameRateOverride(policy, source, reason)
ClearFrameRateOverride(source, reason)
TryGetFrameRateSnapshot(out snapshot)
```

Possible result statuses:

```text
Applied
AppliedNoChange
RejectedOverrideDisabled
RejectedInvalidPolicy
RejectedNoActiveSession
RestoredProjectBaseline
RestoredProjectBaselineNoChange
PlatformLimited
```

Names may change during implementation; the semantic distinction must remain.

## Override lifecycle

```text
Application starts
  -> project baseline applied

Session begins
  -> no override by default

valid allowed override requested
  -> effective policy becomes SessionOverride

override updated
  -> new valid policy replaces previous override

override cleared
  -> project baseline reapplied

Session ends
  -> any session override is cleared
  -> project baseline is authoritative again
```

No override may survive into a later Session unless a future Preferences cut loads it
again explicitly.

## Future Preferences contract

Do not implement this in Stage A.

Future flow:

```text
Preferences
  key: presentation.frameRate (illustrative only)
  ↓
typed preference resolver
  ↓
FrameRatePreference
  ↓
Session override request
```

The storage representation must not become the runtime API.

Missing persisted preference is a normal condition:

```text
no stored preference
  -> no Session override
  -> project baseline remains effective
```

Corrupt/invalid persisted preference must be explicit and diagnosable. The future
Persistence ADR decides whether it blocks preference loading or is ignored with an
explicit typed result.

## Migration plan

Current source has:

```text
GameApplicationAsset.frameRatePolicy
```

Stage A must remove the dual authority.

Preferred migration:

1. Add project-level policy with `UseUnityDefaults`.
2. Add an explicit Editor migration action when an active Game Application contains a
   non-default existing policy.
3. Migration previews the old and target values.
4. User confirms explicit copy.
5. Game Application frame-rate authoring is removed.
6. Validation reports any unresolved legacy state during the transition.
7. No runtime reads both sources.

If a compatibility migration is not worth preserving in preview, the alternative is:

```text
new Project Settings policy becomes authoritative
legacy GameApplication field removed
user configures Project Settings explicitly
```

Do not merge the two policies.

## QA plan

### ADR017-QA-01 — Project baseline TargetFrameRate

Prove:

```text
Project policy = TargetFrameRate
boot succeeds
VSync becomes 0
target frame rate becomes configured value
typed source = ProjectBaseline
```

### ADR017-QA-02 — Project baseline VerticalSync

Prove:

```text
target frame rate becomes -1
VSync becomes configured interval
platform limitation is explicit when applicable
```

### ADR017-QA-03 — UseUnityDefaults

Prove:

```text
existing Unity values preserved
operation succeeds as explicit no-override policy
```

### ADR017-QA-04 — Invalid project policy

Prove:

```text
boot fails explicitly
Unity values are not partially mutated
```

### ADR017-QA-05 — no dual GameApplication authority

Prove:

```text
GameApplication cannot independently override project frame rate
```

### ADR017-QA-06 — Session override disabled

Stage B:

```text
override request rejected explicitly
project baseline remains effective
```

### ADR017-QA-07 — Session override apply / replace / clear

Stage B:

```text
baseline -> override A -> override B -> clear -> baseline
```

with typed source/effective diagnostics after every transition.

### ADR017-QA-08 — Session teardown

Stage B:

```text
session override active
session ends
override removed
project baseline restored
```

## FIRSTGAME product validation

After Stage A:

```text
open Project Settings
find Performance / Frame Rate immediately
configure baseline without opening GameApplication
enter Play Mode
observe expected frame pacing
inspect diagnostic result
```

After Stage B:

```text
expose a temporary real-game settings control
request Session override through public package contract
confirm runtime change
clear/reset to project baseline
```

After Stage C:

```text
change user preference
restart Session/application as defined by Persistence ADR
confirm preference is loaded and applied through Session override
```

FIRSTGAME must not directly write Unity frame pacing values as a workaround.

## Technical acceptance

Stage A:

```text
compiles
Project Settings policy is required and valid
GameApplication is no longer frame-rate authority
boot validates project policy
runtime receives resolved policy explicitly
invalid policy has no partial mutation
diagnostics identify ProjectBaseline
QA Stage A passes
```

Stage B:

```text
typed Session override exists
override permission is explicit
no static/global lookup
clear/end Session restores baseline
QA Stage B passes
```

Stage C:

```text
only after Persistence architecture is decided
Preferences stores intent
runtime authority remains Application Frame Rate Runtime
```

## Product acceptance

Stage A:

```text
user configures Frame Rate in Project Settings
default is understandable
UseUnityDefaults is explicit
no duplicate GameApplication control
validation is visible in the Project Settings flow
```

Stage B:

```text
consumer can expose a gameplay settings option without touching framework internals
runtime result is inspectable
```

## Architectural gain

```text
before:
GameApplicationAsset
  -> owns frame pacing despite being application graph authoring

after:
Project Settings
  -> owns required project baseline

Application Frame Rate Runtime
  -> owns effective process-level frame pacing

Session
  -> may request temporary override

Preferences
  -> may later persist user intent only
```

This preserves one authored baseline, one runtime authority and a clean future
persistence boundary.

## Suggested implementation sequence

```text
ADR017-A1  move authoring authority to Project Settings
ADR017-A2  update boot resolution/runtime input
ADR017-A3  remove GameApplication duplicate surface
ADR017-A4  update validation/docs
ADR017-A5  QA project baseline

ADR017-B1  add scoped Session override contract
ADR017-B2  runtime effective-policy snapshot/diagnostics
ADR017-B3  QA override lifecycle
ADR017-B4  FIRSTGAME real settings control

Persistence disposition decision

ADR017-C1  Preferences integration only if retained
```

## Suggested commit messages

Architecture cut:

```text
docs(architecture): propose project frame rate authority
```

Stage A package:

```text
refactor(performance): move frame rate policy to project settings
```

Stage B package:

```text
feat(performance): add session frame rate override
```

Future Preferences integration:

```text
feat(preferences): persist frame rate session preference
```
