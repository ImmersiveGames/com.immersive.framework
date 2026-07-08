# 05 — Player Binding Authoring Validation

Status: **current F50A-F50D authoring validation surface**.

## Summary

F50 adds a passive authoring validation layer on top of the F49 Player passive binding foundation.

It validates whether authored Player binding evidence is coherent before any real binding adapter is allowed to activate camera, input, movement or spawning.

## Implemented cuts

| Cut | Status | Result |
|---|---|---|
| F50A | PASS | `PlayerBindingAuthoringValidator` and `PlayerBindingAuthoringValidationReport`. |
| F50B | PASS | Editor-only validation surface for active scene, selected root and explicit root. |
| F50C | PASS | Root-cause/derived issue cleanup for readable authoring diagnostics. |
| F50D | Documentation-only | Usage guide and current-state note. |

## Canonical validation chain

```text
Authoring components
  -> PlayerTopologyValidationResult
  -> PlayerViewTopologyValidationResult
  -> PlayerControlTopologyValidationResult
  -> PlayerBindingReadinessSummary
  -> PlayerBindingDiagnosticReport
  -> PlayerBindingAuthoringValidationReport
```

## Required authored evidence

```text
PlayerSlotDeclaration
PlayerSlotOccupancy
ActorReadinessBehaviour
PlayerEntryBehaviour
PlayerViewBehaviour
PlayerControlBehaviour
```

## Editor surface

Menu:

```text
Immersive Framework > Player Binding > Authoring Validation
```

Supported operations:

```text
Validate Active Scene
Validate Selected Root
Validate Root Field
```

## Diagnostic policy

The authoring report separates:

```text
RootCauseIssues
DerivedIssues
```

Root cause issues are the normal actionable output. Derived issues remain available for technical traceability and detailed diagnostics.

## Passive boundary

F50 authoring validation must not execute binding behavior.

Expected boundary:

```text
viewBinding='False'
controlBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Validation evidence

F50A, F50B and F50C passed in QAFramework.

F50D is documentation-only and has no Unity smoke.

## Next candidate

The next implementation block can start the first explicit PlayerView binding adapter, but only after the target scene/root passes the F50 authoring validation surface.
