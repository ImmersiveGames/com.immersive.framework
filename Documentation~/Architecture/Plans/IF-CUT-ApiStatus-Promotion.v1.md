# IF-CUT — API Status Promotion

Status: Waves 0–D complete / Wave E deferred  
Version: v1  
Last updated: 2026-07-31  
Package baseline: `1.0.0-preview.17`  
Depends on: IF-PLAN-Framework-Evolution.v1, IF-TRACK-Framework, IF-ADR-001..008  
Related: IF-ADR-009 (identity authority, Proposed), IF-ADR-010 (editor status presentation)

### Wave execution log

| Wave | Status | Date | Result |
|---|---|---|---|
| 0 + B | **Done** | 2026-07-31 | Camera UNMARKED → 0; product types Stable; output authority Internal |
| A | **Done** | 2026-07-31 | 29 types → Stable (authoring + Game Flow + identity) |
| C | **Done** | 2026-07-31 | 36 types → Stable (Pause/Input/Gate single-player product) |
| D | **Done** | 2026-07-31 | 6 types → Stable (Scene-Provided Player product subset) |
| E | Deferred | — | Reset/Audio/Save/etc. not authorized by this cut |

## Purpose

Promote a narrow, product-facing subset of `FrameworkApiStatus` from
`Experimental`/`UNMARKED` to `Stable` (or correctly to `Internal`) without
claiming that whole modules or the preview package are stable.

This cut is a classification and contract freeze. It does not add features.

## Inventory snapshot (source of truth for this cut)

Counted on package source at cut authoring time:

| Status | Occurrences |
|---|---:|
| Experimental | ~572 |
| Internal | ~175 |
| DevelopmentTooling | ~32 |
| Deferred | 2 |
| Stable | 2 (`FrameworkApiStatus`, `FrameworkApiStatusAttribute` only) |

Notable gap:

- `Runtime/Camera/**` has **no** `[FrameworkApiStatus]` markers on any type.
- Product guides and IF-TRACK already treat several Camera / Pause / Scene-Provided
  Player surfaces as consumer baseline, while attributes still say Experimental
  or are missing.

## Promotion rule

A type may become `Stable` only when **all** are true:

1. Owning ADR is **Accepted** for the exposed contract.
2. IF-TRACK marks the track **Closed / approved / consumer baseline** for the
   declared boundary (not merely “Implemented”).
3. A product guide documents create / author / runtime / debug for the surface.
4. FIRSTGAME or equivalent consumer proof is recorded when the surface is
   designer- or game-facing.
5. Open ADR pending items do **not** rewrite the type’s public shape (extension
   outside the frozen boundary is allowed).
6. The attribute note states the **frozen boundary**, not “until roadmap stabilizes”.

`Stable` means: games may depend on it; breaking changes require ADR + migration.

`Internal` means: not game-facing; may change without game migration.

`Experimental` remains the default for unfinished product contracts.

## Explicit non-goals

- Promoting whole folders (`PlayerParticipation`, `ActivityFlow`, `Reset`, …).
- Changing runtime behavior, serialized fields, or public method shapes.
- Declaring the package non-preview (`1.0.0` release).
- Promoting Audio, ProgressionSave, Snapshot, Preferences, Transition stack,
  Session-Persistent Player, or Activity transaction/finalization.
- Using API status as a substitute for validation (IF-ADR-010 §16).

## Boundary notes (canonical text for attributes)

Use short, boundary-explicit notes. Prefer these templates:

| Wave | Note template |
|---|---|
| A | `Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.` |
| A | `Stable boundary identity for authored Route/Activity/PlayerSlot seats.` |
| A | `Stable authored Game Flow request trigger for Route/Activity navigation.` |
| B | `Stable single-output Camera product surface. Multi-output/split-screen is out of scope.` |
| C | `Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.` |
| D | `Stable Scene-Provided local Player product surface. Manager-Provisioned and Session-Persistent remain Experimental.` |
| Internal | `Runtime implementation detail; not game-facing API.` |

---

## Wave 0 — Classification hygiene (no Stable promotions)

**Status: Done (landed with Wave B on 2026-07-31).**

**Goal:** close the Camera metadata gap and mark obvious unmarked product types.  
**Must complete before Wave B.**

### 0.1 Mark every public type under `Runtime/Camera/**`

Current state: all UNMARKED.

| Target status | Types |
|---|---|
| **Stable** (apply in Wave B, but may be marked Experimental first if sequencing prefers two commits) | See Wave B product list |
| **Internal** | See Wave B internal list |

Recommended single-pass approach: apply the Wave B Stable/Internal classification
directly in one commit so Camera never sits as “Experimental for one day”.

### 0.2 Unmarked product/support types outside Camera

| Type | Path | Target |
|---|---|---|
| `PersistentContentComposition` | `Runtime/Authoring/PersistentContentComposition.cs` | **Stable** with Wave A |
| `PlayerInputActionMapReference` | `Runtime/UnityInput/PlayerInputActionMapReference.cs` | **Stable** with Wave C |

### 0.3 Acceptance

- No public type under `Runtime/Camera/**` remains UNMARKED.
- `PersistentContentComposition` and `PlayerInputActionMapReference` carry status.
- No behavior/serialized field changes.

---

## Wave A — Application authoring + Game Flow product envelope

**Status: Done (2026-07-31). Metadata only; no runtime behavior change.**

**Evidence:** IF-ADR-001/002/008 Accepted; Framework-Usage guide; host remains Internal.  
**Risk:** ADR-001 still has pending Activity transaction vocabulary; freeze only
the authoring/request envelope games already use, not Activity commit internals.

**Landed:** 29 types → `Stable` (Authoring assets/entries/policies, Route/Activity IDs,
Game Flow request triggers/events/bridges, identity primitives).

### A.1 Promote to Stable

| Type | Path |
|---|---|
| `GameApplicationAsset` | `Runtime/Authoring/GameApplicationAsset.cs` |
| `RouteAsset` | `Runtime/Authoring/RouteAsset.cs` |
| `ActivityAsset` | `Runtime/Authoring/ActivityAsset.cs` |
| `PersistentContentComposition` | `Runtime/Authoring/PersistentContentComposition.cs` |
| `RouteId` | `Runtime/Authoring/RouteId.cs` |
| `ActivityId` | `Runtime/Authoring/ActivityId.cs` |
| `ImmersiveFrameworkSettingsAsset` | `Runtime/Authoring/ImmersiveFrameworkSettingsAsset.cs` |
| `FrameworkValidationMode` | `Runtime/Authoring/FrameworkValidationMode.cs` |
| `FrameworkValidationModePolicy` | `Runtime/Authoring/FrameworkValidationModePolicy.cs` |
| `FrameworkEditorPlayModeStartup` | `Runtime/Authoring/FrameworkEditorPlayModeStartup.cs` |
| `ActivityContentProfileAsset` | `Runtime/Authoring/ActivityContentProfileAsset.cs` |
| `ActivityContentSceneEntry` | `Runtime/Authoring/ActivityContentSceneEntry.cs` |
| `ActivityContentSceneLoadMode` | `Runtime/Authoring/ActivityContentSceneLoadMode.cs` |
| `ActivityContentReleasePolicy` | `Runtime/Authoring/ActivityContentReleasePolicy.cs` |
| `ActivityVisualTransitionMode` | `Runtime/Authoring/ActivityVisualTransitionMode.cs` |
| `RouteContentProfileAsset` | `Runtime/Authoring/RouteContentProfileAsset.cs` |
| `RouteContentSceneEntry` | `Runtime/Authoring/RouteContentSceneEntry.cs` |
| `RouteRequestTrigger` | `Runtime/GameFlow/RouteRequestTrigger.cs` |
| `ActivityRequestTrigger` | `Runtime/GameFlow/ActivityRequestTrigger.cs` |
| `RouteRequestTriggerEvent` | `Runtime/GameFlow/RouteRequestTriggerEvent.cs` |
| `ActivityRequestTriggerEvent` | `Runtime/GameFlow/ActivityRequestTriggerEvent.cs` |
| `RouteRequestTriggerUnityEventBridge` | `Runtime/GameFlow/RouteRequestTriggerUnityEventBridge.cs` |
| `ActivityRequestTriggerUnityEventBridge` | `Runtime/GameFlow/ActivityRequestTriggerUnityEventBridge.cs` |
| `FlowRequestEventPhase` | `Runtime/GameFlow/FlowRequestEventPhase.cs` |
| `FlowRequestOutcome` | `Runtime/GameFlow/FlowRequestOutcome.cs` |
| `IFrameworkIdentity` | `Runtime/Identity/IFrameworkIdentity.cs` |
| `FrameworkIdentityValue` | `Runtime/Identity/FrameworkIdentityValue.cs` |
| `FrameworkIdentityKey` | `Runtime/Identity/FrameworkIdentityKey.cs` |
| `FrameworkIdentityDomain` | `Runtime/Identity/FrameworkIdentityDomain.cs` |

**Count:** ~29 types → Stable.

### A.2 Keep Experimental (not this wave)

| Type / area | Reason |
|---|---|
| Activity content execution / finalization stack under `ActivityFlow` | ADR-001 pending commit/finalization model |
| Route/Activity lifecycle runtimes | Internal authority, not product assets |
| Any `GameFlowRuntime` / ports | Already Internal; leave Internal |

### A.3 Confirm Internal (no promotion)

| Type | Path | Status |
|---|---|---|
| `FrameworkRuntimeHost` | `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs` | Internal |
| `GameFlowRuntime` | `Runtime/GameFlow/GameFlowRuntime.cs` | Internal |
| `ImmersiveFrameworkBootstrap` / validators | `Runtime/Bootstrap/**` | Internal |

### A.4 Acceptance

- Attribute notes no longer say “until the owning roadmap phase stabilizes it”
  for Wave A types.
- Guides continue to describe the same workflow; no API shape change.
- Tracker note: Wave A promoted.

---

## Wave B — Camera single-output product surface

**Status: Done (2026-07-31). Metadata only; no runtime behavior change.**

**Evidence:** IF-ADR-004 Accepted; IF-TRACK Camera **Closed** for single-output;
Camera-Usage guide; IF-ADR-002 marks Recipe→Composer complete.  
**Out of scope forever in this Stable claim:** multi-output, split-screen,
output reassignment across local Players.

**Landed counts under `Runtime/Camera` + `Runtime/CameraAuthoring`:**

| Status | Count |
|---|---:|
| Stable | 42 |
| Internal | 17 |
| UNMARKED files | 0 |

### B.1 Promote to Stable — designer / scene product surfaces

| Type | Path |
|---|---|
| `CameraRigComposer` | `Runtime/CameraAuthoring/CameraRigComposer.cs` |
| `ExplicitCameraTargetSourceAuthoring` | `Runtime/CameraAuthoring/ExplicitCameraTargetSourceAuthoring.cs` |
| `CameraOutputSessionBinding` | `Runtime/Camera/Bindings/CameraOutputSessionBinding.cs` |
| `SessionCameraOverrideBinding` | `Runtime/Camera/Bindings/SessionCameraOverrideBinding.cs` |
| `LocalPlayerCameraRequestBinding` | `Runtime/Camera/Bindings/LocalPlayerCameraRequestBinding.cs` |
| `ActivityCameraOverrideBinding` | `Runtime/Camera/Bindings/ActivityCameraOverrideBinding.cs` |
| `RouteCameraOverrideBinding` | `Runtime/Camera/Bindings/RouteCameraOverrideBinding.cs` |
| `ScopedCameraOverrideBinding` | `Runtime/Camera/Bindings/ScopedCameraOverrideBinding.cs` |
| `ICameraOutputSessionConsumer` | `Runtime/Camera/Bindings/ICameraOutputSessionConsumer.cs` |
| `ISessionCameraOverrideConsumer` | same file as above |
| `CameraOverrideOperationKind` | `Runtime/Camera/Bindings/CameraOverrideOperationKind.cs` |
| `CameraOverrideResult` | `Runtime/Camera/Bindings/CameraOverrideResult.cs` |

### B.2 Promote to Stable — product vocabulary used by authoring/bindings

| Type | Path |
|---|---|
| `CameraRigPresentationIntent` | `Runtime/Camera/Product/CameraRigPresentationIntent.cs` |
| `CameraTargetSourceKind` | `Runtime/Camera/Product/CameraTargetSourceKind.cs` |
| `CameraTargetRequirement` | `Runtime/Camera/Product/CameraTargetRequirement.cs` |
| `ICameraTargetSource` | `Runtime/Camera/Product/ICameraTargetSource.cs` |
| `CameraTargetSourceDescriptor` | `Runtime/Camera/Product/CameraTargetSourceDescriptor.cs` |
| `CameraResolvedTargets` | `Runtime/Camera/Product/CameraResolvedTargets.cs` |
| `CameraTargetResolveResult` | `Runtime/Camera/Product/CameraTargetResolveResult.cs` |
| `CameraOperationStatus` | `Runtime/Camera/Product/CameraOperationStatus.cs` |
| `CameraIssue` | `Runtime/Camera/Product/CameraIssue.cs` |
| `CameraIssueSeverity` | `Runtime/Camera/Product/CameraIssueSeverity.cs` |
| `CameraOutputId` | `Runtime/Camera/Requests/CameraOutputId.cs` |
| `CameraRequestId` | `Runtime/Camera/Requests/CameraRequestId.cs` |
| `CameraRequest` | `Runtime/Camera/Requests/CameraRequest.cs` |
| `CameraRequestCreateResult` | `Runtime/Camera/Requests/CameraRequestCreateResult.cs` |
| `CameraRequestOwner` | `Runtime/Camera/Requests/CameraRequestOwner.cs` |
| `CameraRequestOwnerKind` | `Runtime/Camera/Requests/CameraRequestOwnerKind.cs` |
| `CameraRequestLifetime` | `Runtime/Camera/Requests/CameraRequestLifetime.cs` |
| `CameraRequestLifetimeKind` | `Runtime/Camera/Requests/CameraRequestLifetimeKind.cs` |
| `CameraRequestReleaseCondition` | `Runtime/Camera/Requests/CameraRequestReleaseCondition.cs` |
| `CameraRequestPolicy` | `Runtime/Camera/Requests/CameraRequestPolicy.cs` |
| `CameraRigReference` | `Runtime/Camera/Requests/CameraRigReference.cs` |
| `ICameraRequestPublisher` | `Runtime/Camera/Publishing/ICameraRequestPublisher.cs` |
| `ScopedCameraRequestPublisher` | `Runtime/Camera/Publishing/ScopedCameraRequestPublisher.cs` |
| `SessionCameraRequestPublisher` | `Runtime/Camera/Publishing/SessionCameraRequestPublisher.cs` |
| `RouteCameraRequestPublisher` | `Runtime/Camera/Publishing/RouteCameraRequestPublisher.cs` |
| `ActivityCameraRequestPublisher` | `Runtime/Camera/Publishing/ActivityCameraRequestPublisher.cs` |
| `LocalPlayerCameraRequestPublisher` | `Runtime/Camera/LocalPlayerCameraRequestPublisher.cs` |
| `CameraRequestPublisherOperationKind` | `Runtime/Camera/Publishing/CameraRequestPublisherOperationKind.cs` |
| `CameraRequestPublisherResult` | `Runtime/Camera/Publishing/CameraRequestPublisherResult.cs` |
| `CameraRequestPublisherCreateResult` | `Runtime/Camera/Publishing/CameraRequestPublisherCreateResult.cs` |

### B.3 Mark Internal — output arbitration / injection (public today but not game API)

These types are public in source but are host/module authority, not designer
contracts. Prefer `Internal` status now; a later visibility cut may change C#
visibility separately.

| Type | Path |
|---|---|
| `CameraOutputContext` | `Runtime/Camera/Output/CameraOutputContext.cs` |
| `CameraOutputSession` | `Runtime/Camera/Output/CameraOutputSession.cs` |
| `CameraOutputRigApplicator` | `Runtime/Camera/Output/CameraOutputRigApplicator.cs` |
| `CameraOutputBinding` | `Runtime/Camera/Output/CameraOutputBinding.cs` |
| `CameraOutputApplyKind` | `Runtime/Camera/Output/CameraOutputApplyKind.cs` |
| `CameraOutputApplyResult` | `Runtime/Camera/Output/CameraOutputApplyResult.cs` |
| `CameraOutputContextChangeKind` | `Runtime/Camera/Output/CameraOutputContextChangeKind.cs` |
| `CameraOutputContextOperationKind` | `Runtime/Camera/Output/CameraOutputContextOperationKind.cs` |
| `CameraOutputContextResult` | `Runtime/Camera/Output/CameraOutputContextResult.cs` |
| `CameraOutputContextSnapshot` | `Runtime/Camera/Output/CameraOutputContextSnapshot.cs` |
| `CameraOutputSessionOperationKind` | `Runtime/Camera/Output/CameraOutputSessionOperationKind.cs` |
| `CameraOutputSessionResult` | `Runtime/Camera/Output/CameraOutputSessionResult.cs` |
| `CameraOutputInjectionRuntime` | `Runtime/Camera/Lifecycle/CameraOutputInjectionRuntime.cs` |
| `CameraOutputSessionInjectionRuntime` | `Runtime/Camera/Lifecycle/CameraOutputSessionInjectionRuntime.cs` |
| `SessionCameraTransitionOrchestrator` | `Runtime/Camera/Lifecycle/SessionCameraTransitionOrchestrator.cs` |
| `CameraRequestPublisherFactory` | `Runtime/Camera/Publishing/CameraRequestPublisherFactory.cs` |

**Count:** ~30 Stable product types + ~16 Internal authority types.

### B.4 Acceptance

- Zero UNMARKED types under `Runtime/Camera/**` and `Runtime/CameraAuthoring/**`.
- Product guide Camera-Usage still matches Stable surfaces.
- Multi-output remains explicitly rejected in notes and ADR.

---

## Wave C — Pause / Input / Gate single-player product surface

**Status: Done (2026-07-31). Metadata only; no runtime behavior change.**

**Evidence:** IF-ADR-005 Accepted; IF-TRACK Pause/Input/Gate **Closed** for
single-player; Pause-Usage / related guides.  
**Out of scope:** multiplayer Pause policy, multi-binding competition.

**Landed:** 36 types → `Stable`. Internals already `Internal` confirmed.
`PauseActivityBindingAuthoring*` remains Experimental.

### C.1 Promote to Stable — product MonoBehaviours and adapters games place

| Type | Path |
|---|---|
| `PauseRequestTrigger` | `Runtime/Pause/PauseRequestTrigger.cs` |
| `PausePlayerInputBinding` | `Runtime/Pause/PausePlayerInputBinding.cs` |
| `IPauseSurfaceAdapter` | `Runtime/Pause/IPauseSurfaceAdapter.cs` |
| `UnityPauseSurfaceAdapter` | `Runtime/Pause/UnityPauseSurfaceAdapter.cs` |
| `UnityPauseResidentSurfaceAdapter` | `Runtime/Pause/UnityPauseResidentSurfaceAdapter.cs` |
| `UnityPlayerInputGateAdapter` | `Runtime/UnityInput/UnityPlayerInputGateAdapter.cs` |
| `PlayerInputActionMapReference` | `Runtime/UnityInput/PlayerInputActionMapReference.cs` |

### C.2 Promote to Stable — product vocabulary / results consumed by games or Inspectors

| Type | Path |
|---|---|
| `PauseState` | `Runtime/Pause/PauseState.cs` |
| `PauseRequestKind` | `Runtime/Pause/PauseRequestKind.cs` |
| `PauseRequestStatus` | `Runtime/Pause/PauseRequestStatus.cs` |
| `PauseRequest` | `Runtime/Pause/PauseRequest.cs` |
| `PauseRequestId` | `Runtime/Pause/PauseRequestId.cs` |
| `PauseResult` | `Runtime/Pause/PauseResult.cs` |
| `PauseIssue` | `Runtime/Pause/PauseIssue.cs` |
| `PauseIssueSeverity` | `Runtime/Pause/PauseIssueSeverity.cs` |
| `PauseVisualSurfaceKind` | `Runtime/Pause/PauseVisualSurfaceKind.cs` |
| `PauseInputCommandKind` | `Runtime/Pause/PauseInputCommandKind.cs` |
| `PauseInputSourceKind` | `Runtime/Pause/PauseInputSourceKind.cs` |
| `PauseInputActionId` | `Runtime/Pause/PauseInputActionId.cs` |
| `PauseInputIntent` | `Runtime/Pause/PauseInputIntent.cs` |
| `PauseInputSignal` | `Runtime/Pause/PauseInputSignal.cs` |
| `PausePresentationIntent` | `Runtime/Pause/PausePresentationIntent.cs` |
| `PauseGateBlockerPolicy` | `Runtime/Pause/PauseGateBlockerPolicy.cs` |
| `GateScope` | `Runtime/Gate/GateScope.cs` |
| `GateDomain` | `Runtime/Gate/GateDomain.cs` |
| `GateDecisionStatus` | `Runtime/Gate/GateDecisionStatus.cs` |
| `GateDecision` | `Runtime/Gate/GateDecision.cs` |
| `GateBlocker` | `Runtime/Gate/GateBlocker.cs` |
| `GateEvaluationResult` | `Runtime/Gate/GateEvaluationResult.cs` |
| `GateSnapshot` | `Runtime/Gate/GateSnapshot.cs` |
| `InputModeKind` | `Runtime/InputMode/InputModeKind.cs` |
| `InputModeDefinitions` | `Runtime/InputMode/InputModeDefinitions.cs` |
| `InputModeRules` | `Runtime/InputMode/InputModeRules.cs` |
| `InputModeRequestStatus` | `Runtime/InputMode/InputModeRequestStatus.cs` |
| `InputModeRequestIssueKind` | `Runtime/InputMode/InputModeRequestIssueKind.cs` |
| `InputModeRequestResult` | `Runtime/InputMode/InputModeRequestResult.cs` |

### C.3 Keep Experimental

| Type | Reason |
|---|---|
| `PauseActivityBindingAuthoring` (+ intent/status/requiredness/validator) | Activity-scoped binding lane still thinner than core Pause product; promote only after explicit tracker proof |
| `InputModeRequestEvaluator` | evaluation helper; treat as Experimental until confirmed as product API |
| `InputModeRuntimeContext` / `InputModeRuntimeSnapshot` / runtime operation types | runtime authority shape; prefer future Internal if not game-facing |

### C.4 Confirm Internal

| Type | Path |
|---|---|
| `PauseRuntime` | `Runtime/Pause/PauseRuntime.cs` |
| `PauseSurfaceRuntime` | `Runtime/Pause/PauseSurfaceRuntime.cs` |
| `PauseTimeScaleRuntime` | `Runtime/Pause/PauseTimeScaleRuntime.cs` |
| `PauseProductBindingRuntimeContext` and related ports | `Runtime/Pause/**` |
| `GateRequestAdmission` | `Runtime/Gate/GateRequestAdmission.cs` |
| `UnityPlayerInputStateWriter` | `Runtime/UnityInput/UnityPlayerInputStateWriter.cs` |

### C.5 Acceptance

- Pause product components used by Persistent Content baseline are Stable.
- Notes explicitly say single-player boundary.
- No change to execution modes (`PlayerInputTransaction` vs `ApplicationOnly`).

---

## Wave D — Scene-Provided Player product subset only

**Status: Done (2026-07-31). Metadata only; no runtime behavior change.**

**Evidence:** IF-ADR-003 Accepted; IF-TRACK Scene-Provided **consumer baseline
approved** + FIRSTGAME; Player-Usage marks Scene-Provided validated.  
**Do not promote** Manager-Provisioned or Session-Persistent lanes.

**Landed:** 6 types → `Stable` (`PlayerSlotId`, `PlayerSlotProfile`,
`LocalPlayerHostAuthoring`, `SceneLocalPlayerAdmissionAuthoring`,
`SceneLogicalPlayerActorEvidence`, `PlayerGameplayCameraAuthoring`).
Manager-Provisioned authoring remains Experimental.

### D.1 Promote to Stable

| Type | Path |
|---|---|
| `PlayerSlotId` | `Runtime/PlayerSlots/PlayerSlotId.cs` |
| `PlayerSlotProfile` | `Runtime/PlayerParticipation/Authoring/PlayerSlotProfile.cs` |
| `LocalPlayerHostAuthoring` | `Runtime/PlayerParticipation/Authoring/LocalPlayerHostAuthoring.cs` |
| `SceneLocalPlayerAdmissionAuthoring` | `Runtime/PlayerParticipation/Authoring/SceneLocalPlayerAdmissionAuthoring.cs` |
| `SceneLogicalPlayerActorEvidence` | `Runtime/PlayerParticipation/Authoring/SceneLogicalPlayerActorEvidence.cs` |
| `PlayerGameplayCameraAuthoring` | `Runtime/PlayerParticipation/Authoring/PlayerGameplayCameraAuthoring.cs` |

### D.2 Keep Experimental (explicit)

| Type | Reason |
|---|---|
| `LocalPlayerProvisioningAuthoring` | Manager-Provisioned; FIRSTGAME pending |
| `LocalPlayerProvisioningHostRegistration` | Manager-Provisioned |
| `LocalPlayerActorSelectionRequestAuthoring` | selection request surface; not part of approved Scene-Provided freeze |
| Entire non-authoring `PlayerParticipation` runtime (~130 Experimental) | admission/runtime modules; promote only after dedicated API freeze cuts |

### D.3 Acceptance

- Scene-Provided composer + host + slot profile Stable.
- Manager-Provisioned remains Experimental in attributes and guides.
- PLAYER-DIAG-1 semantics unchanged.

---

## Wave E — Deferred (do **not** promote in this cut)

Hold until tracker + ADR pending close:

| Area | Why blocked |
|---|---|
| Audio BGM (`FrameworkBgm*`) | IF-ADR-007 explicitly waits for QA + real-game promotion |
| ProgressionSave / Snapshot / Preferences | Foundation; no product consumer proof |
| Loading / Transition / TransitionEffects | ADR-006: coverage varies; no Closed claim matching Camera/Pause |
| ActivityFlow execution / finalization | ADR-001 pending |
| Session-Persistent Player | Blocked package gap |
| Manager-Provisioned full lane | FIRSTGAME pending |
| Reset / ObjectReset / CycleReset / ActivityRestart | Implemented, but open unload `update-retry` finding; treat product triggers as next cut candidate after Reset cut |
| Diagnostics DevelopmentTooling | Correct as tooling; not Stable product API |
| IF-ADR-009 identity *authority migration* | ADR still Proposed; Wave A freezes current identity primitives only |

### E.1 Likely next cut after this one (not authorized yet)

After Reset unload finding is closed:

- `ActivityRestartTrigger` (+ event/result status)
- `ObjectResetTrigger` / `ObjectResetGroupTrigger` (+ events/bridges)
- selected Reset product ids/results used by those triggers

---

## Execution order

```text
Wave 0 + B together   (Camera mark+promote in one commit preferred)
Wave A                (authoring + Game Flow + identity)
Wave C                (Pause/Input/Gate single-player)
Wave D                (Scene-Provided Player subset)
```

Waves A/C/D are independent of each other after Wave 0/B hygiene and may be
stacked as one PR or separate PRs. Prefer **one PR per wave** for reviewability.

## Per-wave implementation checklist

For each wave:

1. Change only `[FrameworkApiStatus(...)]` and related `/// API status:` XML notes.
2. Do not change public members, serialization, or runtime logic.
3. Grep for remaining `until the owning roadmap phase stabilizes` on promoted types → must be zero.
4. Count status distribution before/after; record in IF-TRACK.
5. Update guide status lines only if a guide currently says Experimental for a
   promoted surface (e.g. Audio stays Experimental; Camera/Pause guides may say
   Stable for the frozen boundary).
6. No package version bump required for metadata-only promotion; optional note in
   changelog when shipping preview.

## IF-TRACK update template

After each wave lands, append under Validation log / a new “API status promotion”
section:

```text
API status Wave <X>
  date: YYYY-MM-DD
  types promoted to Stable: N
  types marked Internal: M
  boundary: <one line>
  evidence: ADR-... + TRACK row + guide
```

## Expected impact after Waves 0–D

Approximate, not a hard guarantee:

| Status | Before | After Waves 0–D (order of magnitude) |
|---|---:|---:|
| Stable | 2 | ~90–110 product types |
| Internal | ~175 | ~190+ (Camera authority marked) |
| Experimental | ~572 | ~470–500 |
| UNMARKED public Camera | 55 files | 0 |

Still majority Experimental overall — by design. This cut freezes **consumer
contracts**, not the whole package.

## Decision log

| Decision | Choice |
|---|---|
| Promote whole modules? | No — type-level product subsets only |
| Camera output context public types | Mark Internal (authority), do not Stable |
| Manager-Provisioned with Scene-Provided? | No — Scene-Provided only in Wave D |
| ActivityRestart / ObjectReset now? | No — wait Reset unload cut (Wave E next) |
| ADR-009 Proposed vs identity Stable | Freeze current identity primitives in Wave A; ADR-009 migration remains separate |
| Package leave preview? | Yes — preview remains until broader product freeze |

## Approval

- [ ] Architecture owner accepts wave list and boundaries
- [ ] Product owner confirms Scene-Provided / Camera / Pause match real games
- [ ] Execution PR(s) land metadata only
- [ ] IF-TRACK updated per wave

---

## Quick command helpers (execution)

Count after a wave:

```powershell
Get-ChildItem -Recurse -Filter *.cs Runtime,Editor |
  ForEach-Object {
    Select-String -Path $_.FullName -Pattern 'FrameworkApiStatus\.(Stable|Experimental|Internal|Deferred|DevelopmentTooling|Removed)' -AllMatches
  } |
  ForEach-Object { $_.Matches } |
  ForEach-Object { $_.Value } |
  Group-Object |
  Sort-Object Count -Descending
```

Find remaining UNMARKED public files under Camera:

```powershell
Get-ChildItem -Recurse -Filter *.cs Runtime\Camera,Runtime\CameraAuthoring |
  Where-Object { -not (Select-String -Path $_.FullName -Pattern 'FrameworkApiStatus' -Quiet) } |
  Select-Object -ExpandProperty FullName
```
