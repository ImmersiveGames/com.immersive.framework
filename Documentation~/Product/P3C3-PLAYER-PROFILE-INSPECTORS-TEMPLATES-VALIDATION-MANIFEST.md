# P3C.3 — Player Profile Inspectors, Templates and Project Validation

Status: implementation cut; Unity compile/import and manual Inspector validation pending.

Package: `com.immersive.framework`

## Objective

Complete the designer-facing P3C Profile foundation without introducing runtime participation state, Activity projection or join behavior.

## Created

```text
Editor/PlayerParticipation/PlayerParticipationProfileEditors.cs
Editor/PlayerParticipation/PlayerParticipationProfileTemplateUtility.cs
Documentation~/Product/P3C3-PLAYER-PROFILE-INSPECTORS-TEMPLATES-VALIDATION-MANIFEST.md
```

## Modified

```text
Editor/PlayerParticipation/PlayerParticipationAuthoringValidator.cs
Editor/Authoring/GameApplicationAssetEditor.cs
```

## Product surface

### PlayerSlotProfile Inspector

The default Inspector now presents:

```text
Identity
Presentation
Authoring Validation
```

`Advanced / Debug` exposes normalized and typed identity evidence without becoming the primary editing surface.

### PlayerParticipationRequirementsProfile Inspector

The default Inspector presents:

```text
Designer metadata
Progressive requirement level
Authoring Validation
```

The Inspector states explicitly that `None` is an authored Profile and that `null` never means `None`.

### Official creation templates

Available from:

```text
Assets > Create > Immersive Framework > Player > Templates
```

Commands:

```text
Player Slot Profiles 1-4
Participation Requirements Set
Complete Local Player Profile Set
```

The generated assets are ordinary explicit project assets. Re-running a command creates unique asset paths and never mutates existing Profiles.

## Validation

The Player participation validator now covers:

```text
empty or invalid PlayerSlotId
empty designer-facing names
same Profile repeated in Game/Application
same PlayerSlotId repeated in Game/Application
duplicate PlayerSlotId across project Profiles
invalid participation requirement enum value
missing project Slot Profiles
missing requirements Profiles as an optional pre-P3D skip
```

The Game Application Inspector aggregates:

```text
existing framework authoring validation
ordered local Slot configuration validation
project Profile validation
```

Validation reports only. It does not repair, create or silently choose assets.

## Out of scope

```text
Activity requirements reference
Activity participation projection
runtime Slot roster
join capacity
PlayerInputManager provisioning
Actor selection
Actor materialization
QAFramework assets or smokes
FIRSTGAME integration
```

## Expected Unity validation

```text
package compiles
custom Inspectors load without missing SerializedProperty errors
template commands create 4 Slot and 5 Requirements Profiles
re-running templates uses unique paths
invalid/duplicate PlayerSlotId is visible in Inspector validation
Game Application report includes project Profile findings
Profiles remain unchanged when entering Play Mode
```

## Suggested commit

```text
P3C.3 — add Player Profile inspectors templates and project validation
```
