# 05 — Execution Status

Status: **canonical operational source**

Last reconciled evidence: **P2G FIRSTGAME PASS — 11/11**

This document answers only three questions:

```text
What is closed?
What is active?
What comes next?
```

Detailed design remains in `Planning/Plano de Firstgame.md`. Architectural constraints remain in ADRs.

## Current position

```text
R0 — Documentation and Roadmap Reconciliation
  Closed at the previous baseline; this reconciliation updates it with P2 evidence.

P2 — Player Control Product
  Closed at the accepted current shape.

G1 — Minimal Playable Loop
  Active.

P3 — Player Spawn / Runtime Materialization
  Next after G1.

C9 — Camera Output Lifetime / Release
  Ordered after P3.

S1 — Progression Save Runtime
  Ordered after C9 and only when FIRSTGAME has meaningful state to persist.
```

## P2 accepted outcome

P2 did not finish with the exact original runtime-context proposal.

The accepted implementation/evidence is:

```text
PlayerRecipe / PlayerComposer
  designer-first control authoring and materialization

PlayerSlotDeclaration
  stable player-slot identity

PlayerInput
  explicit Unity Input System reference

UnityPlayerInputGateAdapter
  Gate/Pause/Transition-driven action-map availability

game-owned movement component
  reads gameplay actions and executes movement
```

### Closed evidence

| Cut/evidence | Status | Result |
|---|---|---|
| P2A | Closed | Player control boundary audited. |
| P2B | Closed | Control authoring integrated into PlayerRecipe/PlayerComposer. |
| P2C original binding/runtime proposal | Rejected and reverted | It introduced authority/binding complexity not justified by the proven slice. |
| P2D accepted QA baseline | Closed | QA proved real PlayerInput topology and runtime readiness, 13/13. |
| P2E Gate runtime proof | Closed | QA proved Transition, Pause, block and restoration, 14/14. |
| P2F | Absorbed | Technical coverage was provided by P2D/P2E rather than a duplicate smoke. |
| P2G | Closed | FIRSTGAME proved real Move input and game-owned displacement, 11/11. |

### Frozen boundary

```text
Framework owns:
  Player authoring intent
  explicit PlayerInput reference
  PlayerSlot identity evidence
  Gate/Pause/Transition availability
  diagnostics

Consumer game owns:
  action semantics
  reading Move/Jump/Fire
  movement implementation and tuning
  gameplay rules
```

Do not add a Player runtime context, binding facade or generic movement controller unless a later real requirement proves that the existing boundary is insufficient.

## Active block — G1

### Goal

Prove the existing systems together as a minimal coherent game loop:

```text
Bootstrap
-> Startup Route
-> Startup Activity
-> Player available
-> control active
-> CameraComposer active
-> simple objective
-> Pause / Resume
-> Transition or controlled re-entry
-> Activity Restart
-> Reset
-> playable initial state restored
```

### First cut

```text
G1A — FIRSTGAME Minimal Playable Loop Audit
```

G1A must inspect the real FIRSTGAME before creating new systems.

Required questions:

```text
Which existing object can act as the simple objective?
Which existing interaction/trigger changes an observable state?
Which state already participates in Reset?
How is Activity Restart requested?
What proves the Player and objective returned to the initial state?
Does CameraComposer remain correct after restart?
Does Pause/Resume preserve the loop?
```

### G1 scope

```text
one simple objective
one interaction or trigger
one resettable state
one Activity Restart
one controlled return/re-entry
```

### G1 out of scope

```text
combat system
inventory
save
final UI
multiplayer
mission framework
generic interaction product
```

## Ordered continuation

After G1 is explicitly closed:

```text
P3 — Player Spawn and runtime materialization
C9 — Camera output lifetime, release and restoration
S1 — Progression save runtime
```

Do not start P3, C9 or S1 in parallel unless fixing a critical blocker directly required by G1.

## Repository roles

```text
com.immersive.framework
  Official product, contracts, runtime, tooling and docs.

QAFramework
  Technical contracts, regressions and negative cases.

FIRSTGAME / planet-devourer
  Real usability and playable integration.
```

Git repositories are read-only inputs. All changes are delivered as ZIP deltas.
