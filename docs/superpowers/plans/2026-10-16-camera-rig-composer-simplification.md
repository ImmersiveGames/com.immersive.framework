# Camera Rig Composer Simplification Implementation Plan

> **For agentic workers:** Execute this plan inline in the current task; the request explicitly disallows intermediate approval gates. Steps use checkbox syntax for tracking.

**Goal:** Let a designer preview a Follow rig, adjust canonical camera settings from the Composer Inspector, and persist them through one safe Save Configuration operation.

**Architecture:** Keep Behavior Definitions and captured Composer lens/pose as the only persistent authorities. Editor-only Preview controls edit the real Cinemachine components transiently; save validates, captures those values into the existing authorities, and applies only supported authored prefab overrides. Preview output remains temporary and the original camera continues driving it.

**Tech Stack:** Unity 6000.5 Editor APIs, Cinemachine 3.1.7, NUnit Editor tests.

**Spec:** `C:/Users/renat/.codex/attachments/abbbc450-f5c2-4bfe-bbbf-7c41d715bac1/Texto colado.txt`

## Global Constraints

- Change only `com.immersive.framework`; do not modify PEGA or Session Camera.
- Preserve local work and do not change package version, commit, tag, or release.
- Do not add duplicate serialized framing fields or change Stable API without necessity.
- Do not remove external/unknown Cinemachine controls.
- Do not run Unity build, tests, Play Mode, smoke, or batchmode.

## Review Focus

- A preview adjustment is discarded by Disable unless Save Configuration succeeds.
- Shared Behavior Definition writes are explicit and warn about other rigs.
- Prefab persistence applies only relevant authored values, never preview bindings or unrelated overrides.
- Failed save leaves the preview and designer edits available for recovery.
- FixedOffset and ScreenComposer continue to use their existing canonical values and ownership evidence.

---

### Task 1: Preview-session Follow controls and framing conversion

**Files:** `Editor/CameraAuthoring/CameraRigComposerEditor.cs`, `Editor/CameraAuthoring/CameraRigPreviewController.cs`, `Tests/Editor/CameraAuthoring/CameraRigPreviewTests.cs`.

- [x] Add Editor tests for Distance/Pitch/Yaw conversion, live component edits in both Follow modes, and discard on Disable.
- [x] Add Inspector controls that read/write canonical-backed Cinemachine controls only while Preview is active; show derived values outside the session and keep technical fields in Advanced.
- [x] Parent the DontSave preview output under the selected rig while preserving world pose; keep preview-only Target/channel state out of prefab persistence.

### Task 2: Save Configuration transaction

**Files:** `Editor/CameraAuthoring/CameraRigPreviewController.cs`, `Editor/CameraAuthoring/CameraRigComposerApplyRebuildUtility.cs`, tests above.

- [x] Add regressions for save capture, shared-definition references, Save Configuration undo/redo, prefab save filtering, and no serialized preview Target.
- [x] Implement one Save Configuration entry point: preflight, snapshot, capture into existing authorities, reconcile with Apply/Rebuild, selectively persist prefab properties, and update the restore baseline only on success.
- [x] Preserve unsaved edits and keep Preview active on pre-persistence failure; preserve ownership gates and do not remove external components.

### Task 3: Play Mode, Inspector hierarchy, and documentation

**Files:** `Editor/CameraAuthoring/CameraRigPreviewController.cs`, `Editor/CameraAuthoring/CameraRigComposerEditor.cs`, `Documentation~/Guides/Camera-Usage.md`, tests above.

- [x] Add coverage for Save & Play, Discard & Play, Cancel, FixedOffset/ScreenComposer, and post-save Validate/Apply/Rebuild. Cleanup-failure handling remains a Unity manual check because the policy disallows running Editor tests here.
- [x] Offer Save & Play / Discard & Play / Cancel; move Validate and Apply/Rebuild to Advanced while retaining clear status and conflict messages in the primary surface.
- [x] Document Enable Preview → Adjust → Save Configuration → Disable Preview and prefab/shared-asset behavior; remove obsolete Capture/Apply Overrides instructions.

### Validation

- [x] Run non-Unity static checks only and inspect the full diff/status.
- [x] Leave all Unity Editor tests and Unity compile/import/Play Mode steps for manual validation under the workspace rule; no Unity PASS is claimed.
