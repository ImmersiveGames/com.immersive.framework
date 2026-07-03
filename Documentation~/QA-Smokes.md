# QA Smokes

The package exposes QA smoke runners through `FrameworkQaCanvas` and internal diagnostics. This page lists the smoke groups that matter for package validation.

## Validation policy

- Run Unity compile/import before relying on smoke results.
- Run the relevant smoke group after changing runtime, authoring, diagnostics or package setup.
- Do not update smoke text only to hide drift.
- Documentation-only changes do not require Unity smokes unless they claim runtime behavior changed.

## Core smoke groups

| Area | Relevant smokes |
| --- | --- |
| Standard package health | Standard Smoke |
| Route/Activity lifecycle | Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke, Composite Lifecycle Release Smoke, Scope Tail Operation Synthetic Smoke. No direct lifecycle operation evidence smoke button exists; validate `lifecycleOperation*`, `lifecycleContent*` and `lifecycleReadiness*` through Standard/Route/Activity logs. |
| Loading | Loading Result Smoke, Loading Readiness Smoke, Loading Progress Smoke, Loading Screen Adapter Smoke. Loading Result Smoke also validates `LoadingSurfaceAdapterEvidence` count, names/statuses, applied/skipped/failed counts, blocking issue count, progress support and explicit no-op/failure evidence. |
| Transition | Transition Smoke, Transition Effect Smoke, Transition Effect Unity Fade Curtain Smoke, Transition Gate Blocker Smoke |
| Pause | Pause Smoke, Pause Runtime Request Smoke, Pause Gate Blocker Smoke |
| Pause input | Pause InputMode Request Boundary Smoke, Pause InputMode Unity PlayerInput Runtime Bridge Smoke, Pause InputAction Runtime Bridge Trigger Smoke |
| InputMode | InputMode Contract Smoke, Unity Input Official Component Evidence Smoke, InputMode Unity PlayerInput application smokes |
| RuntimeContent / ContentAnchor | Runtime prefab materialization, Content Anchor diagnostics, Activity Content Anchor diagnostics, composite lifecycle release/materialization smokes |
| Reset/Object/Cycle | CycleReset and ObjectReset QA smoke runners when those surfaces change |

F46 is a decision-only lifecycle readiness cut. There is no direct F46 smoke button. Use the existing Route/Activity lifecycle smokes and request logs when validating later lifecycle evidence changes.

F47 is an ADR-only GameFlow request envelope boundary cut. F48 implements the first passive runtime diagnostics shell and does not add a smoke button. Validate F48 with Unity import/compile, Standard Smoke and the existing Route/Activity lifecycle smokes, then inspect Route and Activity request logs for `gameFlowEnvelope*` alongside preserved `lifecycleOperation*`, `lifecycleContent*`, `lifecycleReadiness*` and `loadingAdapterEvidence*`.

F49 is a decision-only ownership cut. It keeps envelope creation as `FrameworkRuntimeHost` diagnostics projection and does not require a new smoke beyond the F48 owner-validated baseline.

F50 is a decision-only Route/Activity trigger migration ADR. It approves local FlowTrigger helper adoption but does not change runtime, logs, serialized fields or smoke buttons. No new smoke is required for F50.

F51 migrates `RouteRequestTrigger` and `ActivityRequestTrigger` local state/diagnostics to the existing FlowTrigger helper without changing request semantics or adding smoke buttons. Validate F51 with Unity import/compile, Standard Smoke, Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke when present, Composite Lifecycle Release Smoke when present, Activity Content Anchor Diagnostics Smoke when present and Content Anchor Diagnostics Smoke when present. Pause/InputMode smokes are required only if the shared FlowTrigger helper changes.

F52 is a decision-only GameFlow request API cut. It keeps `FrameworkRuntimeHost` as the current request API boundary and does not add public/internal GameFlow request API, runtime code or smoke buttons. No Unity smoke is required for F52. Future runtime API work must rerun Unity import/compile and the affected Route/Activity smoke group.

F53 is a decision-only architecture consolidation next-track cut. It selects Transition Surface / Effects Hardening as the next track and does not add runtime code, public API, serialized assets or smoke buttons. No Unity smoke is required for F53. Future Transition contract/runtime cuts should use Unity import/compile plus the affected Route/Activity and Transition smoke groups: Standard Smoke, Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke, Transition Smoke, Transition Effect Smoke, Transition Effect Unity Fade Curtain Smoke and Transition Gate Blocker Smoke.

F54 is a decision-only Transition Surface / Effects Contract cut. It accepts the contract and does not add runtime code, public API, serialized assets, log fields or smoke buttons. No Unity smoke is required for F54. F55 runtime hardening should reuse existing smokes: Standard Smoke, Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke, Composite Lifecycle Release Smoke when applicable, Transition Smoke, Transition Effect Smoke, Transition Effect Unity Fade Curtain Smoke and Transition Gate Blocker Smoke.

F55 adds Transition runtime evidence and additive Route/Activity log fields; it does not add smoke buttons. Validate F55 after Unity import/compile with Standard Smoke, Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke and Composite Lifecycle Release Smoke when available. Also inspect Route/Activity logs for preserved `transition*` fields plus `transitionEffectAdapterEvidence*` fields. Transition-specific smokes remain useful evidence but no new smoke is required by F55.

F56 is a documentation-only first practical flow authoring cut. It creates no runtime code, serialized assets, scenes, prefabs or QA Canvas buttons, so no Unity smoke is required for the cut itself. A project following the guide should validate with Unity import/compile, Standard Smoke, Activity Baseline Smoke, Route Scene Composition Smoke, Route Release Smoke and the Transition smoke group, then inspect Route/Activity logs for `transition*` and `transitionEffectAdapterEvidence*` fields.

F57 is an ADR-only Model/Authorship boundary cut. It creates no runtime code, editor code, serialized assets, scenes, prefabs or QA Canvas buttons, so no Unity smoke is required for the cut itself.

F58 adds Editor-only Model Readiness validation and a Project Settings entry point. Validate F58 with Unity import/compile, then run Project Settings > Immersive Framework > Model Readiness > `Run Model Readiness Check`. Runtime smokes are not required by F58 unless a later fix touches runtime paths; if that happens, run Standard Smoke, Activity Baseline Smoke and Route Scene Composition Smoke.

F59 prepares Git package readiness. Validate F59 with `package.json` JSON validation, Unity import/compile in the current project, and Model Readiness with zero blocking issues. Runtime smokes are required only if a runtime, asmdef or package-boundary fix changes runtime behavior.

F60 synchronizes the package into the dedicated source repository at `https://github.com/ImmersiveGames/com.immersive.framework`. Validate F60 with package-root structure checks, `package.json` JSON validation, forbidden project artifact scans, path scans, `git diff --check` in the package repository and a clear package repository `git status`. Clean consumer install validation remains the next gate before tagging.

## QA Canvas expectations

`FrameworkQaCanvas` should expose current validation buttons, not every historical proof. Obsolete intermediate buttons should remain out of the visible primary QA path unless a future diagnostics mode intentionally restores them.

## Manual checklist

1. Open the QA scene/canvas used by the project.
2. Confirm the package imports without compile errors.
3. Run Standard Smoke.
4. Run the smoke group for the touched surface.
5. Confirm logs report explicit success/failure fields.
6. Confirm disabled or retired paths are not presented as the current validation path.

## Documentation-only checklist

For documentation-only cuts:

1. Confirm this package README points to `Documentation~/README.md`.
2. Confirm the documentation index is clear.
3. Confirm setup, QA and troubleshooting pages match the current runtime surface names.
4. Confirm historical ADRs, closeouts, roadmaps and phase notes are not primary package reading.
