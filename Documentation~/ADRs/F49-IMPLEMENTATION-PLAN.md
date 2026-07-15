# F49-IMPLEMENTATION-PLAN — Player Topology, Entry and View Ownership

Status: **Active / package-first implementation plan**  
Phase: **F49 — Player Topology, Player Entry and PlayerView Ownership**  
Last updated: **2026-07-07**  
Repositories:
- Framework package: `ImmersiveGames/com.immersive.framework`
- QA Harness: `rinnocenti/QAFramework`
- FIRSTGAME consumer: `ImmersiveGames/planet-devourer`

---

## 1. Goal

F49 turns the F49 ADR set into incremental, testable cuts.

The phase creates the player-facing foundation from:

```text
Actor readiness
-> PlayerEntry vocabulary
-> topology validation
-> transition evaluation
-> PlayerView ownership
-> camera precedence
-> control binding diagnostics
-> optional Unity PlayerInput / PlayerInputManager bridge
-> FIRSTGAME validation
```

F49 does **not** make the framework a movement system, character-selection system, custom input manager, split-screen implementation, online authority layer or FIRSTGAME-specific runtime.

---

## 2. Mandatory execution order

```text
1. com.immersive.framework
   Implement product contracts, runtime/editor tooling and official docs.

2. QAFramework
   Validate technical behavior with synthetic smokes, negative cases and regression surfaces.

3. planet-devourer / FIRSTGAME
   Validate practical usability only after QA is technically clean.
```

FIRSTGAME must not be used as the primary laboratory for new framework contracts.

---

## 3. Hard boundaries

- Use namespace `Immersive.Framework.*`.
- Do not use `_ImmersiveGames.NewScripts` in new framework code.
- Do not create singleton/service-locator runtime coordinators without a separate ADR.
- Do not create fallback-silent states for required configuration.
- Do not create runtime lifecycle during passive-contract cuts.
- Do not make `PlayerInput` a player identity.
- Do not make `PlayerSlotOccupancy` a control permission.
- Do not make Actor identity mean Actor readiness.
- Do not make PlayerView win camera precedence unless it is `Bound + Active`.
- Do not implement movement in framework core.

---

## 4. Cut sequence

| Cut | Scope | Primary repo |
|---|---|---|
| F49A | ADR normalization and package boundary cleanup | `com.immersive.framework` |
| F49B | Actor Readiness passive contracts | `com.immersive.framework` |
| F49C | Actor Readiness Unity adapter + QA smoke | package + QA |
| F49D | PlayerEntry passive model | package |
| F49E | PlayerTopology policy contracts + validator foundation | package + QA |
| F49F | PlayerEntry transition rules, no coordinator yet | package + QA |
| F49G | PlayerView passive declaration + camera precedence contract | package |
| F49H | CameraDirector integration point for Active PlayerView | package + QA |
| F49I | ControlBinding boundary + permission diagnostics | package + QA |
| F49J | Optional PlayerInput / PlayerInputManager bridge | package + QA |
| F49K | FIRSTGAME validation pass | FIRSTGAME |
| F49L | Documentation, ADR acceptance and next-phase handoff | package |

---

## 5. F49A — ADR normalization and package boundary cleanup

### Objective

Freeze the correct implementation lane before runtime expansion.

### Scope

- Mark F49 as active lane in current roadmap.
- Add this implementation plan.
- Keep package/QA/FIRSTGAME role split explicit.
- Confirm that F49 starts with passive contracts, not a coordinator.

### Out of scope

- No runtime behavior.
- No FIRSTGAME changes.
- No QA scene changes.
- No PlayerEntryCoordinator.
- No PlayerView registry.
- No PlayerInputManager bridge.

### Acceptance criteria

- Roadmap points to F49 as active.
- Plan is package-first and QA-first.
- FIRSTGAME appears only as later usability validation.
- No namespace `_ImmersiveGames.NewScripts` is introduced.

---

## 6. F49B — Actor Readiness passive contracts

### Objective

Separate Actor identity from Actor readiness.

### Scope

Create passive runtime contracts:

```text
Runtime/Actors/IActorReadiness.cs
Runtime/Actors/ActorReadinessState.cs
Runtime/Actors/ActorReadinessSnapshot.cs
Runtime/Actors/ActorReadiness.cs
```

### Rules

- `ReadyForControl` implies `ReadyForView`.
- `Released` cannot become ready again without an explicit new cycle.
- Failed readiness must carry explicit diagnostic reason.
- Contract is pure runtime; no `MonoBehaviour`.
- No event-driven coordinator yet.
- No Unity adapter yet.

### Out of scope

- No ActorInitializer.
- No PlayerEntry.
- No PlayerView.
- No ControlBinding.
- No PlayerInput bridge.
- No FIRSTGAME changes.

### Expected smoke

Package compile plus synthetic QA later:

```text
Actor starts NotReady.
Actor can become ReadyForView.
Actor can become ReadyForControl.
ReadyForControl without ReadyForView is rejected.
Failed state requires a reason.
Released state blocks readiness mutation.
BeginNewCycle explicitly returns to NotReady.
```

---

## 7. Later cuts summary

### F49C — Unity adapter + QA smoke

Add `ActorReadinessBehaviour` or equivalent only if scene-authored QA needs Unity-facing readiness evidence.

### F49D — PlayerEntry passive model

Add state vocabulary:

```text
Configured
Joined
Assigned
Instantiated
ActorReady
ViewBound
Active
Suspended
Released
```

`Suspended` must always carry a reason.

### F49E — PlayerTopology policy contracts

Add topology-aware validation for:

```text
SinglePlayer
LocalMultiplayer
Online
Hybrid
```

### F49F — PlayerEntry transition evaluator

Add deterministic transition evaluation without a runtime coordinator.

### F49G/F49H — PlayerView + camera precedence

Preserve:

```text
Explicit Cinematic Override
> Active PlayerView Camera
> Activity Camera
> Route Camera
> Default Camera
```

### F49I — ControlBinding diagnostics

Framework evaluates permission/readiness; game/model still owns movement.

### F49J — Optional Unity PlayerInput bridge

`PlayerInput` and `PlayerInputManager` remain Unity operational evidence, not identity.

### F49K — FIRSTGAME validation

Only after QA is clean.

### F49L — Closeout

Promote ADR statuses and document usage/gaps.

---

## 8. Commit message pattern

```text
F49A-F49B: add actor readiness contracts and active F49 plan
```
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
