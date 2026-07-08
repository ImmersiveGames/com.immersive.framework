# F49A-F49B — Actor Readiness Contracts Note

Status: **implementation package prepared / smoke pending**  
Phase: **F49**  
Cuts: **F49A + F49B**

---

## Objective

Prepare the F49 active lane and add the first passive runtime contracts for Actor readiness.

---

## Scope

### F49A

- Update package README version/current surface.
- Mark F49 as the active roadmap lane.
- Add the F49 implementation plan.

### F49B

Add pure runtime readiness contracts:

```text
Runtime/Actors/IActorReadiness.cs
Runtime/Actors/ActorReadinessState.cs
Runtime/Actors/ActorReadinessSnapshot.cs
Runtime/Actors/ActorReadiness.cs
```

---

## Out of scope

- No FIRSTGAME changes.
- No QA scene changes in this package.
- No `ActorReadinessBehaviour`.
- No `PlayerEntryCoordinator`.
- No `PlayerView`.
- No `ControlBinding`.
- No `PlayerInputManagerBridge`.
- No movement implementation.

---

## Expected smoke

After applying the package delta:

```text
1. Open QAFramework with the updated package.
2. Let Unity recompile.
3. Confirm no compile errors in Immersive.Framework.Runtime.
4. Add/execute synthetic actor-readiness checks:
   - initial state is NotReady;
   - MarkReadyForView produces ReadyForView;
   - MarkReadyForControl produces ReadyForControl;
   - SetReadiness(false, true) throws;
   - MarkFailed requires a reason;
   - Release blocks MarkReadyForView / MarkReadyForControl;
   - BeginNewCycle returns to NotReady explicitly.
```

---

## Acceptance criteria

- Package compiles.
- Runtime contract has no Editor dependency.
- No Unity lifecycle/runtime coordinator was introduced.
- No `NewScripts` namespace was introduced.
- Readiness invalid states fail explicitly.
- QA smoke can validate the contract without FIRSTGAME.

---

## Architectural gain

The framework now has a minimal, testable way to say:

```text
Actor exists != Actor is ready for view != Actor is ready for control
```

This is the required foundation before PlayerEntry, PlayerView and ControlBinding.
