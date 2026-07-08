# Player Binding Authoring Validation Guide

Status: **F50D documentation-only guide**  
Audience: framework implementers, QA operators and gameplay integrators preparing Player binding scenes.  
Scope: explains the passive Player Binding Authoring Validation surface delivered by F50A-F50C.

## Purpose

The Player Binding Authoring Validator answers one question before any real binding is executed:

```text
Is this authored hierarchy coherent enough for future Player view/control binding?
```

It validates authored evidence, topology, readiness and diagnostics. It does **not** activate camera, input, movement, spawning or runtime lifecycle.

## Current passive chain

The expected foundation is:

```text
PlayerSlotDeclaration
PlayerSlotOccupancy
ActorReadinessBehaviour
PlayerEntryBehaviour
PlayerViewBehaviour
PlayerControlBehaviour
  -> PlayerTopology
  -> PlayerViewTopology
  -> PlayerControlTopology
  -> PlayerBindingReadinessSummary
  -> PlayerBindingDiagnosticReport
  -> PlayerBindingAuthoringValidationReport
```

## Minimal authored hierarchy

A minimal valid authored hierarchy should contain one coherent Player binding root with the following evidence:

```text
Player Binding Root
  PlayerSlotDeclaration
  PlayerSlotOccupancy
  ActorReadinessBehaviour
  PlayerEntryBehaviour
  PlayerViewBehaviour
  PlayerControlBehaviour
```

The exact GameObject layout may vary, but the validation scope must contain the full chain.

## Required authoring evidence

| Evidence | Why it matters |
|---|---|
| `PlayerSlotDeclaration` | Declares the stable framework PlayerSlot identity. |
| `PlayerSlotOccupancy` | Connects the PlayerSlot to an Actor identity. |
| `ActorReadinessBehaviour` | Provides Actor readiness evidence for view/control eligibility. |
| `PlayerEntryBehaviour` | Provides passive PlayerEntry evidence. |
| `PlayerViewBehaviour` | Provides passive PlayerView evidence for future view binding. |
| `PlayerControlBehaviour` | Provides passive PlayerControl evidence for future control binding. |

## How to run from QA Hub

Use the QA Hub for technical smoke evidence.

```text
Immersive Framework QA > Hub > Create or Refresh Hub and Player QA Scenes
```

Then run:

```text
Player Binding Authoring Validator QA
Player Binding Authoring Issue Cleanup QA
```

Expected pass lines:

```text
[F50A_PLAYER_BINDING_AUTHORING_VALIDATOR_QA] status='Succeeded'
[F50C_PLAYER_BINDING_AUTHORING_ISSUE_CLEANUP_QA] status='Succeeded'
```

## How to run from the Editor surface

Open:

```text
Immersive Framework > Player Binding > Authoring Validation
```

Use one of the validation buttons:

```text
Validate Active Scene
Validate Selected Root
Validate Root Field
```

### Use cases

| Button | Use when |
|---|---|
| `Validate Active Scene` | You want a broad pass over the currently open scene. |
| `Validate Selected Root` | You selected the intended Player binding root in the Hierarchy. |
| `Validate Root Field` | You dragged a specific root into the window field. |

## Reading the report

The report has two layers:

```text
RootCauseIssues
DerivedIssues
```

### Root cause issues

Root cause issues are the first actionable problems. They are what a user should fix first.

Example:

```text
rootIssue[0]='MissingPlayerSlotDeclaration'
derivedIssuesSuppressed='27'
```

This means the root problem is missing `PlayerSlotDeclaration`. The other 27 issues are downstream noise caused by that missing declaration.

### Derived issues

Derived issues are still preserved for technical debugging. They come from topology, readiness or diagnostic layers.

Use them when:

```text
- root causes are fixed but binding is still not ready;
- a validator or topology layer seems wrong;
- QA needs full traceability.
```

The normal summary intentionally suppresses derived details. Use detailed diagnostics when the full chain is needed.

## Common issues and fixes

| Root cause | Meaning | Fix |
|---|---|---|
| `MissingValidationRoot` | No validation scope was provided. | Select a root or validate an active scene. |
| `MissingPlayerSlotDeclaration` | No stable PlayerSlot identity exists. | Add/configure `PlayerSlotDeclaration`. |
| `MissingPlayerSlotOccupancy` | PlayerSlot is not connected to an Actor. | Add/configure `PlayerSlotOccupancy`. |
| `MissingActorReadinessBehaviour` | No Actor readiness evidence exists in scope. | Add/configure `ActorReadinessBehaviour`. |
| `MissingPlayerEntryBehaviour` | No PlayerEntry evidence exists in scope. | Add/configure `PlayerEntryBehaviour`. |
| `MissingPlayerViewBehaviour` | No PlayerView evidence exists in scope. | Add/configure `PlayerViewBehaviour`. |
| `MissingPlayerControlBehaviour` | No PlayerControl evidence exists in scope. | Add/configure `PlayerControlBehaviour`. |
| `PlayerViewTopologyIssue` | PlayerView evidence conflicts with topology. | Check slot id, entry state and duplicate PlayerView per slot. |
| `PlayerControlTopologyIssue` | PlayerControl evidence conflicts with topology. | Check slot id, entry state, control readiness and duplicate PlayerControl per slot. |

## Interpreting readiness flags

| Flag | Meaning |
|---|---|
| `readyForViewBinding='True'` | Passive evidence is coherent enough for a future view binding adapter. |
| `readyForControlBinding='True'` | Passive evidence is coherent enough for a future control binding adapter. |
| `readyForFullBinding='True'` | Both view and control readiness are true. |

These flags do **not** mean real binding has happened.

## Passive boundary

A successful report must still preserve the passive boundary:

```text
viewBinding='False'
controlBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

If any of these become `True` in this validation lane, the cut has crossed into behavior and must be rejected or moved to an explicit binding adapter cut.

## Validation order before binding real behavior

Use this order:

```text
1. Run F50A/F50C QA smokes.
2. Run the Editor Authoring Validation surface on the intended scene/root.
3. Fix root cause issues first.
4. Inspect derived issues only after root causes are resolved.
5. Start binding adapter work only when the report is ready and passive boundary remains false.
```

## What remains future work

The following remain out of scope for F50A-F50D:

```text
CameraDirector integration
Cinemachine activation
PlayerInput bridge
InputAction routing
movement enable/disable
control lifecycle
runtime spawning
FIRSTGAME usability proof
```

The recommended next technical block is the first explicit PlayerView binding adapter, after authoring validation is consistently clean.
