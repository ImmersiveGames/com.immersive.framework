# P3D.2 — Activity Participation Templates and Model Readiness

## Objective

Complete the P3D authoring layer with valid reusable Projection templates, one canonical Model Readiness aggregation path and a technical QA handoff.

## Type

UX/product + Editor validation + technical QA handoff.

## Created

```text
Editor/Validation/FrameworkAuthoringModelReadinessAggregator.cs
Documentation~/Product/P3D2-ACTIVITY-PARTICIPATION-TEMPLATES-MODEL-READINESS-MANIFEST.md
```

## Altered

```text
Editor/PlayerParticipation/PlayerParticipationProfileTemplateUtility.cs
Editor/PlayerParticipation/ActivityParticipationProjectionAuthoringValidator.cs
Editor/Settings/ImmersiveFrameworkSettingsProvider.cs
```

## Product surface

```text
Assets > Create > Immersive Framework > Player > Templates
  Activity Projection Set
  Complete Local Player Profile Set

Project Settings > Immersive Framework > Model Readiness
```

## Official Projection templates

```text
Activity Participation — No Players
  NoSlots
  Zero Participants: Allowed

Activity Participation — All Joined (Zero Allowed)
  AllJoinedSlots
  Zero Participants: Allowed

Activity Participation — All Joined (At Least One)
  AllJoinedSlots
  Zero Participants: Rejected
```

An empty `ExplicitSlots` template is intentionally not created because it would be invalid. Explicit subsets must reference real project `PlayerSlotProfile` assets.

## Model Readiness

The canonical aggregator composes:

```text
base application/route/activity readiness
ordered GameApplication Local Player Slots
all PlayerSlotProfile assets
all Participation Requirements Profiles
all Activity Participation Projection Profiles
all ActivityAsset participation pairs
```

Validation is Editor-only, non-mutating and creates no fallback.

## Out of scope

```text
Session participation state
runtime projection evaluation
Activity admission gate
join
Actor selection
Actor materialization
FIRSTGAME integration
```

## Manual validation order

```text
1. Import and compile package.
2. Create Activity Projection Set in the QA Profiles folder.
3. Assign Projection + Requirements to every retained QA Activity.
4. Run P3D Activity Participation Authoring Smoke.
5. Run Project Settings > Immersive Framework > Model Readiness.
```

## Expected QA log

```text
[P3D_ACTIVITY_PARTICIPATION_AUTHORING_SMOKE] status='Passed'
```

## Suggested commit

```text
P3D.2 — add Activity projection templates and readiness aggregation
```
