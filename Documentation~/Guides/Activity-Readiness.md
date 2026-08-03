# Activity Readiness

`ActivityFlowRuntime` remains the only authority that enters or exits an Activity. The authoring surface contributes preparation evidence; it never assigns global Activity state.

## Add a participant

1. Add **Immersive Framework/Activity Readiness Participant** to the Route primary scene or an Activity-owned loaded scene.
2. Set a stable **Participant Id**. It is required; object names and hierarchy paths are not identity.
3. Select **Required** when preparation must finish before the Activity can be ready. Select **Optional** when preparation is diagnostic only.
4. Wire **Preparation Started** to real local preparation. When that work ends, call `CompletePreparation()` on the same participant. Call `FailPreparation(reason)` for an explicit failure.
5. Use **Preparation Released** to cancel/release local work on Activity exit. No timer or coroutine is supplied by the framework.

Participants are discovered only from the explicit Route primary root and loaded scenes owned by the entering Activity. Discovery includes roots, descendants and inactive objects, removes object duplicates and never uses a global Unity lookup.

## Choose the Activity entry policy

Open the `ActivityAsset` and configure **Activity Entry Readiness > Policy**:

- **Observe Only** preserves the current post-transition behavior. Readiness remains observable, but it does not retain visual cover or the operation capability gate. Existing assets deserialize to this policy because it is enum value `0`.
- **Wait Covered** declares that the target must remain visually covered and that input, interaction and gameplay must remain blocked until the initial readiness occurrence reaches `Ready`. It requires **Fade** or **Fade With Loading**.
- **Wait Visible** declares that the target may be revealed after materialization while input, interaction and gameplay remain blocked until the initial readiness occurrence reaches `Ready`.

Both waiting policies require **Block During Transition = Input Interaction And Gameplay**. The framework reports incompatible authoring as an error and does not silently replace `Seamless` or strengthen the capability gate. A Route with a Startup Activity validates the Route transition gate against the Startup Activity policy because the Route operation owns that entry envelope.

IF-READY-02 adds the policy contract, Inspector guidance and validation only. Occurrence-scoped waiting and Game Flow reveal/gate orchestration are delivered by the following readiness runtime cuts; selecting a waiting policy in this cut does not yet change Play Mode sequencing.

## Present readiness without polling

Add **Immersive Framework/Activity Readiness Events** in the same explicit Activity scope. Wire its `Preparing`, `Ready` and `Not Ready` UnityEvents to a local presenter. The presenter may update text, visuals or enabled content, but must not change readiness or look up a runtime.

## Lifecycle and diagnostics

Required participants begin in `Preparing`. This keeps the aggregate Activity `NotReady`, but normal preparation is not a failure and does not add a blocking issue. A Required participant that completes contributes to `Ready`; a Required participant that fails produces explicit terminal blocking evidence. Optional participants remain diagnostic whether they are preparing, completed or failed and never block `Ready`.

Exit releases tracked participants. A completion after release is diagnosed as `LateCompletionRejected` and does not modify a later occurrence. Reentry creates a new participant occurrence.

Zero participants remain supported: the technical execution path preserves its existing `SucceededNoParticipants` behavior. The Inspector shows identity, requiredness and callbacks first; Advanced shows occurrence, state and last reason.
