# Activity Readiness

`ActivityFlowRuntime` remains the only authority that enters or exits an Activity. The authoring surface contributes preparation evidence; it never assigns global Activity state.

## Add a participant

1. Add **Immersive Framework/Activity Readiness Participant** to the Route primary scene or an Activity-owned loaded scene.
2. Set a stable **Participant Id**. It is required; object names and hierarchy paths are not identity.
3. Select **Required** when preparation must finish before the Activity can be ready. Select **Optional** when preparation is diagnostic only.
4. Wire **Preparation Started** to real local preparation. When that work ends, call `CompletePreparation()` on the same participant. Call `FailPreparation(reason)` for an explicit failure.
5. Use **Preparation Released** to cancel/release local work on Activity exit. No timer or coroutine is supplied by the framework.

Participants are discovered only from the explicit Route primary root and loaded scenes owned by the entering Activity. Discovery includes roots, descendants and inactive objects, removes object duplicates and never uses a global Unity lookup.

## Present readiness without polling

Add **Immersive Framework/Activity Readiness Events** in the same explicit Activity scope. Wire its `Preparing`, `Ready` and `Not Ready` UnityEvents to a local presenter. The presenter may update text, visuals or enabled content, but must not change readiness or look up a runtime.

## Lifecycle and diagnostics

Required participants begin pending and block readiness until completed. Optional participants use the existing non-blocking failure semantics. Exit releases tracked participants. A completion after release is diagnosed as `LateCompletionRejected` and does not modify a later occurrence. Reentry creates new participant state.

Zero participants remain supported: the technical execution path preserves its existing `SucceededNoParticipants` behavior. The Inspector shows identity, requiredness and callbacks first; Advanced shows occurrence, state and last reason.
