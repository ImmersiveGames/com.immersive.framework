# 04 — Player Passive Binding Foundation

Status: **canonical after F49M consolidation**.

This document summarizes the F49 passive player binding foundation. It is the current quick reference for the player topology, view and control readiness lane.

## Role of this foundation

F49 does not execute player binding. It establishes stable passive contracts, Unity-facing adapters, validators, readiness summaries and diagnostics so that a future binding implementation can start from explicit evidence instead of implicit runtime discovery.

```text
PlayerSlot
  -> PlayerEntry
  -> PlayerTopology
  -> PlayerView
  -> PlayerViewTopology
  -> PlayerControl
  -> PlayerControlTopology
  -> PlayerBindingReadiness
  -> PlayerBindingDiagnostics
```

## Closed F49 cuts

| Cut | Status | Scope |
|---|---|---|
| F49A | PASS | ADR and boundary normalization for the player lane. |
| F49B | PASS | Passive Actor readiness contracts. |
| F49B-QA | PASS | Actor readiness contract smoke. |
| F49C | PASS | Actor readiness Unity adapter. |
| F49C-QA | PASS | Actor readiness behaviour smoke. |
| F49D | PASS | `PlayerEntry` passive model. |
| F49D-QA | PASS | `PlayerEntry` passive smoke. |
| F49E | PASS | `PlayerEntryBehaviour` Unity adapter. |
| F49E-QA | PASS | `PlayerEntryBehaviour` smoke. |
| F49F | PASS | `PlayerTopology` passive validation. |
| F49F-QA | PASS | `PlayerTopology` smoke. |
| F49G | PASS | `PlayerView` passive contract and Unity adapter. |
| F49G-QA | PASS | `PlayerView` passive smoke. |
| F49H | PASS | `PlayerViewTopology` validation. |
| F49H-QA | PASS | `PlayerViewTopology` smoke. |
| F49I | PASS | `PlayerControl` passive contract and Unity adapter. |
| F49I-QA | PASS | `PlayerControl` passive smoke. |
| F49J | PASS | `PlayerControlTopology` validation. |
| F49J-QA | PASS | `PlayerControlTopology` smoke. |
| F49K | PASS | `PlayerBindingReadinessSummary`. |
| F49K-QA | PASS | Binding readiness smoke. |
| F49L | PASS | `PlayerBindingDiagnosticReport`. |
| F49L-QA | PASS | Binding diagnostics smoke. |
| F49M | Documentation-only | Consolidates the passive foundation and next-phase boundary. |

## Canonical responsibilities

| Surface | Responsibility | Explicitly not responsible for |
|---|---|---|
| `ActorReadiness` | States whether an Actor is ready for view or control. | It does not own PlayerEntry, input, camera or movement. |
| `PlayerEntry` | Connects `PlayerSlotId`, `ActorId`, lifecycle state and Actor readiness evidence. | It does not join players, spawn actors, bind view, bind control or move actors. |
| `PlayerTopologyValidator` | Validates authored coherence between slots, occupancies and entries. | It does not change occupancy or create entries. |
| `PlayerView` | Declares passive view evidence for a slot. | It does not activate a camera or choose priority. |
| `PlayerViewTopologyValidator` | Validates view evidence against player topology. | It does not call a CameraDirector or Cinemachine. |
| `PlayerControl` | Declares passive control evidence for a slot. | It does not activate input or movement. |
| `PlayerControlTopologyValidator` | Validates control evidence against player topology. | It does not route InputActions or switch action maps. |
| `PlayerBindingReadinessSummarizer` | Aggregates topology readiness for future binding. | It does not execute binding. |
| `PlayerBindingDiagnosticReporter` | Produces human-readable readiness diagnostics. | It does not mutate runtime state. |

## Passive boundary

All F49A-F49M surfaces must preserve this boundary:

```text
viewBinding = false
controlBinding = false
cameraActivation = false
inputActivation = false
movement = false
actorSpawning = false
```

Any future implementation that changes one of those fields from diagnostic false to real behavior must be a new explicit binding cut.

## Readiness rules

### View readiness

A player is ready for future view binding only when:

```text
PlayerTopology is valid
PlayerViewTopology is valid
there is at least one participating PlayerView
there are no blocking view-related issues
```

### Control readiness

A player is ready for future control binding only when:

```text
PlayerTopology is valid
PlayerControlTopology is valid
there is at least one participating PlayerControl
there are no blocking control-related issues
```

### Full binding readiness

Full binding readiness requires both view and control readiness.

```text
IsReadyForFullBinding = IsReadyForViewBinding && IsReadyForControlBinding
```

## Rules preserved from lower layers

`PlayerEntryState.Active` requires Actor readiness for view. Control readiness builds on top of that; it does not weaken the view-readiness requirement.

`ActorReadinessState.ReadyForControl` implies `ReadyForView`.

## Current QA evidence

The QA project owns synthetic smoke evidence under:

```text
Assets/ImmersiveFrameworkQA/Player/
Assets/ImmersiveFrameworkQA/Documentation/
```

The current player QA lane has verified:

```text
Actor readiness contracts and behaviour
PlayerEntry model and behaviour
PlayerTopology validation
PlayerView contract and topology validation
PlayerControl contract and topology validation
Binding readiness aggregation
Binding diagnostic reporting
```

## Next implementation block

The next block should not add more passive taxonomy unless a missing invariant is discovered. The recommended next block is a **binding planning gate** that selects one implementation lane:

```text
Option A — PlayerView binding adapter
Option B — PlayerControl binding adapter
Option C — Editor authoring validator for the complete player binding chain
```

Recommended order:

```text
1. Authoring validator for the complete chain.
2. PlayerView binding adapter.
3. PlayerControl binding adapter.
4. Optional Unity PlayerInput bridge.
5. FIRSTGAME usability proof after QA is clean.
```

## FIRSTGAME rule

Do not move this lane to FIRSTGAME until the selected binding implementation passes QA. QA proves technical correctness. FIRSTGAME proves practical usability.
