# P3C.4 — Model Readiness Integration and QA Handoff

## Objective

Close the package-side P3C authoring foundation by including Player participation authoring in the canonical Project Settings Model Readiness report and by defining the corresponding QA handoff.

## Type

```text
Product authoring closure
Editor validation integration
Technical QA handoff
```

## Package files

### Modified

```text
Editor/Settings/ImmersiveFrameworkSettingsProvider.cs
Editor/PlayerParticipation/PlayerParticipationAuthoringValidator.cs
```

### Created

```text
Documentation~/Product/P3C4-MODEL-READINESS-AND-QA-HANDOFF-MANIFEST.md
```

## Product surface

```text
Project Settings
  > Immersive Framework
    > Model Readiness
      > Run Model Readiness Check
```

The report now aggregates:

```text
existing framework model readiness
ordered Game/Application Local Player Slots
missing and repeated Profile references
configured duplicate PlayerSlotId values
project-wide PlayerSlotProfile validation
project-wide PlayerSlotId uniqueness
PlayerParticipationRequirementsProfile validation
```

## Validation behavior

The integration is report-only.

It does not:

```text
create Player Profiles
repair Game/Application
insert a fallback Player 1
change array order
mutate Profile assets
create runtime participation state
```

The Game Application Inspector still includes detailed configured Profile validation. Model Readiness validates ordered configuration and then performs one project-wide Profile pass, avoiding duplicate configured-Profile detail messages.

## QA handoff

The matching QAFramework cut provides:

```text
Immersive Framework
  > QA
    > Player
      > P3C Run Player Profile Authoring Smoke
```

Expected QA evidence:

```text
identity normalization
ordered configuration preservation
valid configuration acceptance
explicit None requirements
non-mutating validation
missing reference rejection
repeated Profile rejection
duplicate PlayerSlotId rejection
empty identity rejection
official complete template creation
```

## Out of scope

```text
Activity participation projection
Activity references to requirements Profiles
Session Slot roster
join capacity
PlayerInputManager provisioning
Actor selection
Actor materialization
occupancy
FIRSTGAME integration
```

## Acceptance

Technical:

```text
package compiles
Model Readiness includes Player participation findings
QA smoke passes
no runtime assembly depends on Editor
no validator mutates authoring assets
no missing required Slot is replaced by fallback
```

Product:

```text
designer finds Player participation errors in the central readiness flow
ordered seats remain visible in Game/Application
Profile templates remain explicit project assets
None remains an explicit reusable Profile
```

## Suggested commits

Package:

```text
P3C.4 — integrate Player participation into Model Readiness
```

QAFramework:

```text
P3C.4 — add Player Profile authoring QA smoke
```
