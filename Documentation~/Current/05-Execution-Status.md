# 05 — Execution Status

Status: **canonical operational source**  
Last reconciled evidence: **C9R QA PASS 11 cases + FIRSTGAME runtime/visual PASS**  
Date: **2026-07-12**

This document answers:

```text
What is closed?
What is active?
What comes next?
```

## Current position

```text
R0 — Documentation and Roadmap Reconciliation
  Closed at the accepted baseline.

P2 — Player Control Product
  Closed at the accepted consumer-owned movement boundary.

C9 — Camera Requests, Output Contexts and Override Authority
  Closed at the current single-output product level.

G1 — Consumer Route Loop
  Active; scope must be locked with the user's additional FIRSTGAME requirements.

P3 — Player Spawn / Runtime Materialization
  Ordered after G1.

S1 — Progression Save Runtime
  Ordered only after FIRSTGAME contains meaningful state worth persisting.
```

## C9 accepted outcome

```text
CameraRigRecipe / CameraRigComposer
  designer-first reusable rig intent and idempotent virtual-rig materialization

CameraOutputSessionBinding
  one persistent session-owned physical output in UIGlobal

CameraOutputContext
  single winner-selection authority for that output

LocalPlayerCameraRequestBinding
  normal eligible Player request

ActivityCameraOverrideBinding
  explicit temporary Activity override

RouteCameraOverrideBinding
  explicit temporary Route override

SessionCameraOverrideBinding
  transition-scoped Session override

CameraOutputSessionInjectionRuntime
  typed output injection into loaded Route/Activity/Player consumers
```

Default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

### Closed evidence

| Evidence | Result |
|---|---|
| C9I Route/Activity lifecycle QA | closed, eight cases |
| C9L Player arbitration QA | PASS, ten cases |
| C9O teardown QA | Activity released before Route unload |
| C9Q Follow Pipeline QA | PASS, four cases |
| C9R Camera Override Authority QA | PASS, eleven cases |
| FIRSTGAME installer | persistent output and Session application reference validated |
| FIRSTGAME transition | Session requested at 300 and released after transition |
| FIRSTGAME manual overrides | Activity 100, Route 200 and Session 300 win and restore Player 50 |
| FIRSTGAME visual inspection | accepted |

Camera C9 requires no additional runtime, smoke or consumer patch for closure.

## Active block — G1

### Accepted name

```text
G1 — Consumer Route Loop
```

The earlier “minimal playable loop” framing implied gameplay requirements that
the framework does not own. That framing is superseded for operational
execution.

### Goal

Prove the application lifecycle through real consumer Routes:

```text
Bootstrap
-> Menu Route
-> Gameplay Route
-> Ending Route or Menu Route
-> controlled return/re-entry
```

### Framework-owned proof

```text
Route request admission
Route exit/entry
scene composition and release
loading and transition presentation
Transition Gate application/release
PlayerInput availability restoration
Session camera request/release
normal Player camera restoration
Pause/Resume compatibility when included in the selected flow
diagnostics without blocking issues
```

### Consumer-owned content

```text
why gameplay ends
objective and victory rules
interactions
combat
mission state
movement semantics
visual/gameplay tuning
```

These consumer concerns may participate in FIRSTGAME, but G1 must not create
generic framework gameplay authority merely to demonstrate the Route loop.

### Next cut

```text
G1A — FIRSTGAME Route Loop Audit and Scope Lock
```

Before implementation, G1A must incorporate the additional requirements the user
wants for G1 and determine whether the existing Menu → Gameplay → Menu/Ending
flow already closes the block without new framework behavior.

## Ordered continuation

```text
G1 closed
-> P3 Player Spawn / Runtime Materialization

Meaningful persistent game state exists
-> S1 Progression Save Runtime
```

Do not execute P3 or S1 in parallel with G1 unless resolving a critical blocker
directly required by the selected Route loop.

## Repository roles

```text
com.immersive.framework
  official product, contracts, runtime, tooling and documentation

QAFramework
  technical contracts, regressions and negative cases

FIRSTGAME / planet-devourer
  real usability, Route flow and game-owned content
```

Git repositories are read-only inputs. Changes are delivered as ZIP deltas.
