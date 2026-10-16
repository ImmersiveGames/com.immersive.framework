# Original Rig Camera Preview Implementation Plan

> **For agentic workers:** Implement task-by-task in this session. The user authorized native execution and prohibited commits/tags.

**Goal:** Run Edit Mode Camera Rig Preview through the rig's original `CinemachineCamera`, with only a temporary Unity Camera and CinemachineBrain, and keep Capture/Disable safe.

**Architecture:** Reuse the materializer's ownership preflight in a read-only mode, then bind a session to the existing camera and controls. The session temporarily changes only target/output isolation state and owns one non-persistent camera output; Capture synchronizes supported fields to authoring data, while Disable restores transient state and discards uncaptured rig edits when restoration is safe.

**Tech Stack:** Unity Editor 6000.5.0f1, Cinemachine 3.1.7, C#, NUnit Editor tests.

**Spec:** User brief “Immersive Framework — Refatorar Camera Rig Preview para usar o rig original”.

## Global Constraints

- Work only in `com.immersive.framework`; inspect PEGA read-only.
- Do not change `package.json`, runtime camera authority, Session Camera Assignment, Player Session, or Game Flow.
- Do not create persistent Camera Outputs, commits, tags, or releases.
- Preserve all pre-existing worktree changes.
- Never remove or adopt ExternalOrUnknown components.
- Do not run Unity build, import, tests, Play Mode, smoke, or batchmode.
- Static checks may run; Unity validation remains a manual user step.

## Review Focus

- Existing external `CinemachineOrbitalFollow` conflicts with Follow `FixedOffset`; preflight must block without mutation and retain the source component.
- `FixedOffset` and `ScreenComposer` must preview through the same original camera with exactly one compatible Body controller.
- Scene and Prefab Stage target references must be temporary and restored before serialization.
- Disable without Capture must restore pre-session serialized camera/control values while preserving baseline edits and never delete externally added components.
- Capture followed by Disable and Apply/Rebuild must retain captured behavior, camera pose and lens values.

---

### Task 1: Read-only materialization preflight

**Files:**
- Modify: `Editor/Camera/Cinemachine/CinemachineRigMaterializer.cs`
- Modify: `Editor/CameraAuthoring/CameraRigComposerApplyRebuildUtility.cs`
- Test: `Tests/Editor/CameraAuthoring/CameraRigPreviewTests.cs`

**Interfaces:**
- Add `CinemachineRigMaterializer.ValidateExistingComposition(CinemachineRigMaterializationRequest request, bool requireMaterializedControls)` returning the existing materialization report/evidence shape without creating, configuring, deleting, or adopting components.
- Add an internal request builder/preflight entry point in `CameraRigComposerApplyRebuildUtility` so Preview validates the same desired pipeline and ownership references used by Apply/Rebuild.

- [x] Add a focused test proving an `OrbitalFollow` on the original camera is reported as `ExternalOrUnknown` conflict and remains attached and unchanged.
- [x] Implement validation-only flow by sharing `ValidateRequest`, desired-pipeline resolution and `PreflightStage`; when Preview requires a materialized controller, block if a required stage has no compatible existing control.
- [x] Confirm the Preview request sets both component-create flags false and that this path performs no mutation by source component identity and serialized-state assertions.

### Task 2: Preview session uses the original rig

**Files:**
- Modify: `Editor/CameraAuthoring/CameraRigPreviewController.cs`
- Test: `Tests/Editor/CameraAuthoring/CameraRigPreviewTests.cs`

**Interfaces:**
- Replace clone-oriented session fields with `SourceCamera`, the source owned controls/group, temporary output objects, original `CameraTarget`, output channel, priority and Brain masks, plus pre-session serialized snapshots for supported rig components.
- Replace `TryComposePreview` with `TryEnablePreview`; replace `TryRemovePreview` with `TryDisablePreview` while retaining safe internal call sites for Editor lifecycle cleanup.

- [x] Add tests for FixedOffset and ScreenComposer asserting the preview camera is the exact original instance, no duplicate CinemachineCamera/Composer/controller exists, and the temporary root contains only Unity Camera and Brain output.
- [x] Create only a `DontSave` output root in the source rig's loaded scene/Prefab Stage; bind the original camera to an unused channel and temporarily mask that channel off on other Brains.
- [x] Require an explicit Transform in the owner's same loaded scene/Prefab Stage; restore the original `CameraTarget` and channel on every exit path and before scene/prefab serialization.
- [x] For Group, temporarily project the selected member into the original Group Target Group and restore temporary membership without cloning the camera or Group component.
- [x] Handle owner/target destruction, scene changes, Prefab Stage close, assembly reload, Editor quit, errors and Play Mode through the existing lifecycle hooks.

### Task 3: Capture and Disable transactions

**Files:**
- Modify: `Editor/CameraAuthoring/CameraRigPreviewController.cs`
- Modify: `Editor/CameraAuthoring/CameraRigComposerApplyRebuildUtility.cs` only if needed to preserve captured projection after Disable
- Test: `Tests/Editor/CameraAuthoring/CameraRigPreviewTests.cs`

**Interfaces:**
- Capture reads directly from `SourceCamera` and its original controls.
- Disable restores temporary `CameraTarget`, output channel and Brain masks in all cases; when Capture has not occurred, restore pre-session serialized rig state for existing snapshotted objects only. Never delete components added by another operation.

- [x] Test Capture updates Behavior Definition and CameraRigComposer, preserves the original camera instance and values through Disable and Apply/Rebuild, and keeps Undo/Redo functional.
- [x] Test Disable without Capture restores pre-preview camera/control/lens state, target, channel and Brain masks; assert no scene target is persisted.
- [x] Warn and require explicit confirmation when Capture writes to a persistent Behavior Definition asset, stating that other rigs referencing it will also observe the change; do not duplicate the asset.
- [x] If a snapshotted source component is missing or restoration cannot be completed, retain the rig state, disable/remove only the temporary output, and expose a resolvable cleanup warning rather than deleting or adopting components.

### Task 4: Inspector language, docs and evidence

**Files:**
- Modify: `Editor/CameraAuthoring/CameraRigComposerEditor.cs`
- Modify: `Documentation~/Guides/Camera-Usage.md`
- Modify: `Documentation~/API/Public-API.md`
- Modify: `CHANGELOG.md`
- Test: `Tests/Editor/CameraAuthoring/CameraRigPreviewTests.cs`

- [x] Rename controls and diagnostics from Compose/Remove to Enable/Disable and explain which values Capture synchronizes versus which transient target/channel values are restored.
- [x] Preserve the Play Mode decision flow for Capture & Disable, Disable Without Capture, and Cancel; keep Play Mode canceled if capture or cleanup fails. Verify the choices manually because Editor lifecycle tests are not run here.
- [x] Document the Enable → adjust original camera → Capture → Disable → Validate → Apply/Rebuild sequence and Prefab Asset/Prefab Stage limits.
- [x] Run static diff checks only, inspect all changed paths and Git status, and report authored tests separately from Unity execution (not run per repository rules).

## Self-review

- All requested behavior is assigned to a task: source-camera identity/output ownership (Task 2), Capture/Disable and shared assets (Task 3), FixedOffset/ScreenComposer and external conflict (Tasks 1–2), Play Mode and cleanup (Tasks 2 and 4), Apply/Rebuild retention (Task 3), docs (Task 4), PEGA evidence (read-only task investigation).
- Materialization preflight is read-only; Apply/Rebuild remains the only authoring projection operation.
- No runtime authority, package metadata, or consumer project changes are planned.
- Test methods will be authored but not run because the package's validation instructions prohibit Unity execution in this task.
