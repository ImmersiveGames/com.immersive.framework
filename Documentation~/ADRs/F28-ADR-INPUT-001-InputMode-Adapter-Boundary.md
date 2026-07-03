# F28-ADR-PLAN-001 — Roadmap Reconciliation and Adapter Module Spine

Status: Accepted / F28A-F28F closed / F29 selected  
Phase: F28 — Roadmap Reconciliation and Adapter Module Spine  
Type: Framework Planning / Adapter Module Boundary / Input Positioning

## Context

F27 validated the deferred Pause UIGlobal surface and the narrow PauseToggle input path. The next attempted direction pushed Gate checks into individual input consumers.

That direction was rejected because it treated a missing ownership problem as a local input-consumer problem. The framework still needs a clear completion map for adapter modules, Player/Actor ownership, Unity Input target ownership, InputMode semantics and Pause integration.

The current baseline is frozen at F27D. F27E is cancelled.

## Decision

F28 is not an InputMode implementation phase.

F28 is the roadmap correction phase that defines the adapter/module spine from the stable framework core to future product-facing modules.

The canonical ordering is:

```text
Framework lifecycle core
  -> Unity build surface evidence
  -> adapter/module ownership map
  -> Player/Actor/Input target ownership
  -> typed InputMode semantics
  -> Pause requests InputMode
  -> later Camera/Audio/Save/RuntimeSpawned/Gameplay adapters
```

Runtime implementation resumes only after this planning phase identifies the next concrete owner, dependency chain, package/asset placement and smoke target.

## Positive Scope

F28 owns these planning outputs:

```text
completion dependency map
adapter module taxonomy
module placement rules
PlayerInput ownership decision path
InputMode position in the module graph
Pause/InputMode integration plan
next implementation phase entry criteria
```

## Canonical Tracks

| Track | Owns |
|---|---|
| Framework Core / Contracts | Existing lifecycle contracts, typed identities, state, diagnostics, policies and request/result language. |
| Unity Build Surface | Minimal Unity scenes, surfaces and QA wiring that prove framework contracts. |
| Adapter Modules | Optional modules that connect product/gameplay systems to framework contracts without redefining them. |
| Project Assets | Game-specific prefabs, scenes, UI art, player prefabs and concrete product configuration. |
| External Packages | Unity official modules, optional packages, third-party tools and project-specific assets consumed by adapters. |

## Adapter Module Families

F28 recognizes these future families as separate lanes:

```text
Player / Actor
Unity Input
InputMode
Pause Integration
Camera
Audio
Save / Progression
Runtime Spawned / Pooling
Gameplay Capabilities
```

Each family must declare owner, dependency order and placement before implementation.

## InputMode Position

InputMode remains a required future boundary, but it is downstream of Player/Input ownership decisions.

Candidate modes:

```text
Gameplay
PauseOverlay
FrontendMenu
InputLocked
```

`InputMode` is typed framework language. Unity action-map names are adapter configuration.

## Pause Direction

The accepted direction remains:

```text
Running:
  InputMode = Gameplay

Paused:
  InputMode = PauseOverlay
  UI input remains available
  PauseToggle / Cancel remains available
  gameplay command maps do not drive gameplay
```

Pause may request a mode change after InputMode exists. Pause does not own `PlayerInput`, action-map strings or player lifecycle.

## Gate Position

Gate remains passive admission and hard-lock language.

Gate should be used for:

```text
admission diagnostics
request guards
exceptional blocks
stale/foreign/in-flight safety checks
```

Gate is not the normal implementation path for pausing ordinary gameplay input.

## Consequences

- F27E remains cancelled.
- The next cut is documentation/planning, not contracts/runtime.
- Adapter module work is treated as a progressive roadmap lane, not as a single input patch.
- Player/Input ownership must be decided before Pause drives action-map behavior.
- Camera, Audio, Save, RuntimeSpawned and Gameplay adapters must not be pulled into core as side effects of solving Pause/Input.

## F28 Cut Order

| Cut | Name | Output |
|---|---|---|
| F28A | Frozen Baseline Reconciliation | Authoritative read of package docs, project docs, QA assets and the cancelled F27E path. |
| F28B | Completion Dependency Map | Closed: ordered dependency graph for remaining product-completion tracks. |
| F28C | Adapter Module Taxonomy | Closed: module families, owner kind, placement rule and dependency category. |
| F28D | Player / Actor / Input Ownership Plan | Closed: player object, `PlayerInput`, player/actor adapter placement and first target proof. |
| F28E | InputMode and Pause Integration Plan | Closed: typed InputMode semantics and Pause-driven mode requests after ownership is clear. |
| F28F | Next Implementation Closeout | Closed: selects F29 — Unity Input Target Ownership Proof as the next code phase. |


## F28A Closure

F28A closes the frozen baseline reconciliation.

Accepted baseline:

```text
F24-F27 are frozen evidence.
F27E is cancelled.
F28 is roadmap reconciliation before runtime implementation resumes.
Assets/ and Packages/com.immersive.framework/ are both source-of-truth boundaries for their respective scopes.
```

F28B is the next cut and must produce a dependency map before any adapter/module implementation starts.

## F28B Dependency Decision

F28B closes the completion dependency map.

The accepted order is:

```text
F28C adapter module taxonomy
  -> F28D Player / Actor / Unity Input ownership
  -> F28E InputMode and Pause integration
  -> F28F next implementation selection
```

InputMode is explicitly downstream of Unity Input target ownership. Pause integration is explicitly downstream of InputMode.

Camera, Audio, Save / Progression, RuntimeSpawned / Pooling and Gameplay Capabilities are separate adapter lanes. They may be planned in parallel after taxonomy, but implementation must respect their family-specific blockers.

F28B reference:

```text
Assets/_Documentation/Notes/F28B-Completion-Dependency-Map.md
```



## F28C Taxonomy Decision

F28C closes the adapter module taxonomy.

Every future adapter/module implementation must declare:

```text
Family
Owner kind
Dependency category
Placement
Evidence surface
First proof
Blocked by
Must not touch
```

Accepted owner kinds:

```text
Framework Core
Framework Unity Adapter
Optional Immersive Package
Project Integration
QA Evidence
External Tool Boundary
Sandbox Experiment
```

Accepted dependency categories:

```text
Framework Core
Official Unity Package
Optional Immersive Package
External Tool
Project Asset / Config
Personal Asset
QA Fixture
```

This decision separates official Unity modules, optional packages, external tools and personal/project assets before runtime implementation resumes.

F28D closed Player / Actor / Unity Input ownership planning. F28E closes typed InputMode and Pause integration semantics. It does not implement action-map behavior, Camera, Audio, Save, RuntimeSpawned or Gameplay modules.

F28C reference:

```text
Assets/_Documentation/Notes/F28C-Adapter-Module-Taxonomy.md
```


## F28D Ownership Decision

F28D closes Player / Actor / Unity Input ownership planning.

Accepted split:

```text
Project Integration owns concrete player prefabs, controllers, visuals and InputActionAssets.
Unity Input Adapter owns translation from Unity Input System targets to framework input language.
Framework Core owns typed InputMode language.
Pause Integration may request modes after InputMode exists, but does not own PlayerInput or action-map strings.
Runtime-spawned player/actor lifetime is deferred until RuntimeContentHandle, runtime roots and release policy exist.
```

The first future implementation proof should be a QA-authored Unity input target proof. It should validate explicit target ownership and missing/duplicate target diagnostics before implementing full InputMode or Player/Actor lifecycle.

F28E closes InputMode/Pause semantics without creating runtime code. F28F closes the planning gate and selects F29 — Unity Input Target Ownership Proof as the next implementation phase.

F28D reference:

```text
Assets/_Documentation/Notes/F28D-Player-Actor-Input-Ownership-Plan.md
```


## F28E InputMode / Pause Decision

F28E closes typed InputMode and Pause integration planning.

Accepted first mode vocabulary:

```text
Gameplay
PauseOverlay
FrontendMenu
InputLocked
```

Accepted semantics:

```text
InputMode is framework language for command execution posture.
InputMode is not a Unity action-map name, InputActionAsset path, PlayerInput owner, movement toggle, Gate substitute or Time.timeScale policy.
Unity action-map names are adapter/project configuration.
```

Accepted Pause integration:

```text
Running -> Gameplay
Pause accepted -> PauseOverlay
Pause released -> Gameplay

Pause may request mode changes after InputMode exists.
Pause does not own PlayerInput, action-map names, player/actor lifecycle, movement, Time.timeScale, camera, audio, save or gameplay adapters.
UI/Pause input remains available during PauseOverlay.
Gameplay commands stop driving gameplay during PauseOverlay.
```

F28F chooses F29 — Unity Input Target Ownership Proof as the next implementation path. The first code direction is F29A — Unity Input Target Declaration Proof.

F28E reference:

```text
Assets/_Documentation/Notes/F28E-InputMode-Pause-Integration-Plan.md
```


## F28F Closeout Decision

F28F closes the F28 planning gate.

Selected next implementation phase:

```text
F29 — Unity Input Target Ownership Proof
```

Selected first implementation cut:

```text
F29A — Unity Input Target Declaration Proof
```

F29 is selected because explicit Unity Input target ownership is the missing boundary before InputMode behavior or Pause-driven action-map behavior can be implemented.

F29A must prove:

```text
valid target set
missing required target diagnostics
duplicate target diagnostics
global UI / Pause intent target separate from gameplay command target
no action-map switching as part of the proof
```

F29A must not implement full InputMode, player/actor runtime spawning, movement, camera, audio, save, runtime-spawned gameplay or per-consumer Gate checks.

F28F references:

```text
Assets/_Documentation/Notes/F28F-Next-Implementation-Closeout.md
Assets/_Documentation/Plans/F29-PLAN-Unity-Input-Target-Ownership-Proof.md
```
