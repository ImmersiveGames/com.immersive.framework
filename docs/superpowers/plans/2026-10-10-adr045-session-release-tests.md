# IF-ADR-045 D9 Session Release Test Plan

**Goal:** Add package-level regression tests for the internal Session composition release contract and record D7/D8/D9 evidence without changing runtime or QAFramework.

**Architecture:** Exercise `SceneLifecycleRuntime`'s internal release protocol in Edit Mode tests, reuse existing feature composition tests for authored bindings, and record the unavailable `FrameworkRuntimeHost.OnDestroy` integration seam as pending. Preserve all runtime/public boundaries.

**Tech Stack:** Unity Edit Mode, NUnit, C#, Markdown, Git static checks.

**Spec:** User request IF-ADR-045-D9 (2026-10-10).

## Global Constraints

- Read-only QAFramework; do not change its 6/7 terminal.
- Do not change public APIs or runtime behavior.
- Do not run Unity, Play Mode, builds, commits, or pushes.
- Preserve existing local changes.

## Review Focus

- Release must call every participant and retain failure diagnostics.
- Successful release must clear the discovered Session participant set.
- Release must not destroy Persistent Content roots.
- The real host shutdown callback must not be claimed covered without a supported seam and execution evidence.

### Task 1: Session release protocol regression tests

**Files:**
- Create `Tests/Editor/PersistentContent/SessionCompositionReleaseTests.cs` and its Unity `.meta`.
- Modify `Tests/Editor/Audio/FrameworkBgmDirectorCompositionTests.cs`.

- [x] Test a failed Session release invokes each participant and aggregates diagnostics, then a retry succeeds with the same owner and roots.
- [x] Extend the real discovered BGM Session participant test to assert roots survive release and the second release sees no residual participants.
- [x] Do not execute Unity during D9; at that point, leave test execution pending.

### Task 2: D7/D8/D9 evidence record

**Files:**
- Update `Documentation~/Architecture/ADRs/IF-ADR-045-Persistent-Content-Scene-Reference-Player-Builds.md`.
- Update the IF-ADR-045 row in `Documentation~/Architecture/Tracking/IF-TRACK-Framework.md`.

- [x] Record D7's six runtime checkpoints and public-boundary block, D8's `PACKAGE-LEVEL` decision, and D9 existing/new/pending package tests without claiming test execution.
- [x] Verify scope with `git diff --check`, static inspection, and Git status.

## D10 evidence reconciliation

After D9, the user reported a Unity Test Runner Edit Mode result of 194 passed,
0 failed and 0 ignored, with `SessionCompositionReleaseTests: PASS`, Audio
5/5 and `PersistentContentSceneReferenceTests` 12/12 separately identified.
This is user-reported execution evidence, not an execution performed by the D9
agent. Full `FrameworkRuntimeHost.OnDestroy → ReleaseSessionScope` integration
remains pending; the D7 QAFramework result remains 6/7 BLOCKED.
