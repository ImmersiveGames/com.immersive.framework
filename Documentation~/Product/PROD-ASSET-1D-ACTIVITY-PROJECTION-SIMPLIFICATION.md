# PROD-ASSET-1D — Activity-owned Player Participation Projection

## Objective

Remove the external `ActivityParticipationProjectionProfile` asset and make the owning
`ActivityAsset` the single authoring surface for contextual Player Slot projection.

## Type

UX/product + runtime contract migration + QA migration + documentation.

## Scope

```text
ActivityAsset owns:
  Projection Mode
  Zero Participant Policy
  ordered explicit PlayerSlotProfile references

ActivityAsset still produces ActivityParticipationProjectionDescriptor.
PlayerParticipationRequirementsProfile remains external in this cut.
```

## Removed product surface

```text
Activity Participation Projection Profile asset
Projection Profile Inspector
Activity Projection template set
Projection Profile reference in Activity Inspector
```

## Expected use

```text
Open Activity
-> select Slot Projection
-> select Zero Participants policy
-> configure Explicit Slots only when applicable
-> assign Requirements Profile
```

`NoSlots + Allowed` is the serialized default. Invalid enum values and contradictory
combinations remain blocking validation errors; no runtime fallback is introduced.

## Runtime preservation

```text
ActivityParticipationProjectionDescriptor remains immutable.
Explicit slots remain PlayerSlotProfile references.
Admission evaluation and Session state remain outside ActivityAsset.
```

## QA migration

Existing QA Activities receive values equivalent to their previously referenced Profiles.
Fixture builders and authoring smokes configure the Activity fields directly. The removed
Projection assets and their metadata are deleted. FIRSTGAME is not migrated because its assets
will be recreated.

## Acceptance

```text
framework and QA compile
no ActivityParticipationProjectionProfile code reference remains
no removed Projection GUID remains in QA
Activity Inspector is the complete Projection authoring surface
ordered explicit Slot references are preserved
invalid combinations remain diagnostic
```

## Suggested commits

```text
Framework:
refactor(player): move participation projection into activity

QA:
test(qa): migrate activity projection to inline authoring
```
