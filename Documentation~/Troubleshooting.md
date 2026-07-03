# Troubleshooting

Use this page when package setup or QA smokes do not behave as expected.

## Package docs point to old roadmap or ADRs

Use `Documentation~/README.md` as the package documentation entry point. Old `ADRs/`, `Planning/` and phase `Guides/` are historical source material, not primary package usage docs.

## Git package install fails

Check:

- Git is installed and available to Unity.
- The Git URL is reachable from the consumer machine.
- The requested tag exists, for example `v1.0.0-preview.1` after release validation.
- `com.immersive.foundation` and `com.immersive.logging` are installed directly in the consumer project when they are private Git packages.
- `com.immersive.framework/package.json` does not contain Git URLs as package dependencies.
- `Packages/packages-lock.json` is not pinning an older or missing revision.
- The consumer project has `com.unity.inputsystem` resolved.

If a private sibling package is missing, add it to the consumer project's `Packages/manifest.json` or configure a scoped registry that provides the required version. Do not solve this by adding a Git URL inside the framework package manifest.

## Package dependency does not resolve

`com.immersive.framework` declares package/version dependencies. Unity can resolve them from a registry or from packages already installed in the consumer project.

For private Git siblings, install in this order:

1. `com.immersive.foundation`
2. `com.immersive.logging`
3. `com.immersive.framework`

If Unity still resolves an older revision, delete or regenerate the consumer project's `Packages/packages-lock.json` after confirming the manifest points at the intended tags.

## Surface or adapter aggregate fails

Check the aggregate diagnostics before changing setup:

- domain `status`.
- `failedStage`, when present.
- `adapterResult` or the named original subsystem result.
- `blockingIssues` and non-blocking issues.
- `source`, `reason` and the short message.
- whether the result applied the required side effect or explicitly skipped/no-oped by policy.

Do not treat `Unknown`, `NotRequested` or an unexecuted stage as success. Required missing capability is a failure; optional no-op must say why it was skipped.

## Route or Activity request stage is unclear

For Route and Activity request logs, read the domain status first, then use `lifecycleOperation*` to locate the stage evidence:

- `lifecycleOperationStages`
- `lifecycleOperationBlockingIssues`
- `lifecycleOperationIssues`
- `lifecycleOperationSideEffects`
- `lifecycleOperationFailedStages`
- `lifecycleOperationSkippedStages`
- `lifecycleOperationStageNames`
- `lifecycleOperationStageStatuses`

These fields are a lifecycle-local projection over existing Route/Activity evidence. They help locate transition, loading, scene composition/release, content, runtime scope, readiness and ledger evidence, but they do not replace the original Route/Activity/Scene/Content statuses.

For content/readiness questions, read the dedicated F45 projections after the domain status:

- `lifecycleContentStatus`
- `lifecycleContentEnter`
- `lifecycleContentExit`
- `lifecycleContentEnterRequests`
- `lifecycleContentExitRequests`
- `lifecycleContentParticipants`
- `lifecycleContentParticipantSource`
- `lifecycleContentBlockingIssues`
- `lifecycleContentBlocksReadiness`
- `lifecycleContentHandles`
- `lifecycleContentDiagnostics`
- `lifecycleReadiness`
- `lifecycleReadinessReason`
- `lifecycleReadinessIssues`
- `lifecycleReadinessBlockedByContent`

Route request logs use `lifecycleContent*` for Route content enter/exit evidence and `lifecycleReadiness*` for the resulting startup Activity readiness when available. Activity request logs use `lifecycleContent*` for Activity content participant/lifecycle evidence and `lifecycleReadiness*` for Activity readiness. Older domain fields such as `activityContentExecution*`, `activityContentParticipant*`, `routeContentEnterReceivers`, `routeContentExitReceivers`, `activityReadiness*` and `activityContentHandles` remain valid and should be used when deeper domain detail is needed.

These lifecycle fields are diagnostics, not policy. A deferred content dispatch kernel, readiness kernel or orchestration kernel is not a bug by itself. Treat it as a bug only when an existing Route/Activity result contradicts the projected evidence, hides a blocking issue, or reports success for an unexecuted required step.

F48 adds a passive GameFlow request envelope diagnostics shell. F49 keeps envelope creation as `FrameworkRuntimeHost` log projection, not as stored state in `GameFlowRuntime` request results. When troubleshooting current GameFlow requests, read the Route/Activity domain result first, then use `gameFlowEnvelope*` as the request-level summary. The envelope fields should agree with the existing `kind`, target/source/reason, lifecycle and Loading diagnostics; they do not replace domain statuses.

Key envelope fields:

- `gameFlowEnvelopeKind`
- `gameFlowEnvelopeAdmission`
- `gameFlowEnvelopeSource`
- `gameFlowEnvelopeReason`
- `gameFlowEnvelopeTargetRoute`
- `gameFlowEnvelopePreviousRoute`
- `gameFlowEnvelopeTargetActivity`
- `gameFlowEnvelopePreviousActivity`
- `gameFlowEnvelopeTransitionStatus`
- `gameFlowEnvelopeLoadingStatus`
- `gameFlowEnvelopeValidationMode`
- `gameFlowEnvelopeDomainStatus`
- `gameFlowEnvelopeLifecycleOperationKind`
- `gameFlowEnvelopeLifecycleStages`
- `gameFlowEnvelopeLifecycleBlockingIssues`
- `gameFlowEnvelopeLifecycleFailedStages`
- `gameFlowEnvelopeLifecycleSkippedStages`
- `gameFlowEnvelopeContentStatus`
- `gameFlowEnvelopeContentBlockingIssues`
- `gameFlowEnvelopeContentHandles`
- `gameFlowEnvelopeReadiness`
- `gameFlowEnvelopeReadinessReason`
- `gameFlowEnvelopeReadinessIssues`
- `gameFlowEnvelopeLoadingAdapterEvidenceCount`
- `gameFlowEnvelopeLoadingAdapterBlockingIssues`

Use `lifecycleOperation*`, `lifecycleContent*`, `lifecycleReadiness*` and `loadingAdapterEvidence*` for detailed evidence. Treat a mismatch between envelope summary and existing domain fields as a diagnostics bug, not as permission to change Route/Activity behavior.

Current Route/Activity requests should enter through the supported host boundary: scene-authored triggers or QA/runtime callers resolve `FrameworkRuntimeHost` and call its Route/Activity request methods. `GameFlowRuntime` and `GameFlowRequestEnvelope` are internal implementation details. `gameFlowEnvelope*` fields are diagnostics, not a request API.

## UIGlobal scene does not load

Check:

- `GameApplicationAsset` has `UIGlobal` policy set correctly.
- The `UIGlobal` scene path/name is assigned when policy is required.
- The scene is included in Build Settings.
- Required Loading, Transition and Pause adapters exist in the scene when the project expects them.

If the policy is not configured, explicit no-op visual behavior is expected.

## Model Readiness reports blocking issues

Run the check from Project Settings > Immersive Framework > Model Readiness.

Interpret the counters this way:

- `errors`: blocking issues for package readiness.
- `warnings`: non-blocking drift or policy mismatch that should be reviewed.
- `optionalSkips`: intentionally absent optional model pieces, such as no Startup Activity on a menu route or no resident Pause adapter when no Pause-required policy exists.
- `info`: evidence and summary messages.

Do not fix readiness failures by adding fallback code. Fix the owning authoring asset, scene, Build Settings entry or adapter setup.

Common readiness blockers:

- Active Game Application missing.
- Startup Route missing.
- Route Primary Scene missing or outside Build Settings.
- Required `UIGlobal` scene missing or outside Build Settings.
- Required `UIGlobal` scene missing Transition or Loading adapters.
- Content profile scene entries missing, invalid or not build-loadable.
- Private package dependency missing after Git install.

Pause resident adapter absence is currently reported as an optional skip unless the project has an explicit product requirement for shared Pause presentation. There is no serialized Pause-required policy in the Model 1.0 boundary.

## Loading surface missing

Check:

- `UnityLoadingSurfaceAdapter` exists in `UIGlobal`.
- The Game Application actually loads `UIGlobal`.
- Loading diagnostics report `loadingBefore`, `loadingAfter`, `loadingAdapterCount`, `loadingBlockingIssues`, `loadingProgressSupported` and `loadingProgressMode`.
- The aggregate surface result and request-level route/activity Loading diagnostics report `adapterEvidence`, `adapterEvidenceApplied`, `adapterEvidenceSkipped`, `adapterEvidenceFailed` and `adapterEvidenceBlockingIssues`.
- Each adapter evidence entry reports adapter name, status, applied/skipped/failed state, issue count, blocking issue count and message.

Loading is not the same as transition fade. A fade curtain does not replace a loading surface.

If Loading is optional or no surface is configured, an explicit skipped/no-op result is expected. If the project requires Loading, missing surface capability should appear as a visible failure/blocking issue.

## Transition surface missing

Check:

- `UnityFadeCurtainEffectAdapter` or another transition effect adapter exists in `UIGlobal`.
- `GameApplicationAsset` is configured to load the canonical `UIGlobal` scene when the flow depends on shared Transition.
- Transition smokes report the expected transition/effect result.
- Route/activity visual policy is configured to request the transition where expected.

F55 adds named Transition Effect adapter evidence to Route/Activity logs. Diagnose Transition through the existing fields:

- `transition`
- `transitionScope`
- `transitionBefore`
- `transitionAfter`
- `transitionBlockingIssues`
- `transitionVisual`
- `transitionEffect`
- `transitionEffectBefore`
- `transitionEffectAfter`
- `transitionEffectBlockingIssues`
- `transitionEffectAdapterCount`

Then inspect the F55 additive fields:

- `transitionEffectAdapterEvidenceCount`
- `transitionEffectAdapterEvidenceApplied`
- `transitionEffectAdapterEvidenceSkipped`
- `transitionEffectAdapterEvidenceFailed`
- `transitionEffectAdapterEvidenceBlockingIssues`
- `transitionEffectAdapterEvidenceNames`
- `transitionEffectAdapterEvidenceStatuses`

If these fields expose missing required adapters, blocking issues, failed effect status or a required visual surface missing message, treat that as Transition setup/evidence to investigate.

For first playable flow setup, use `Documentation~/Guides/First-Practical-Flow-Transition.md`. In that flow, expected healthy evidence for one fade adapter is usually `transitionEffectAdapterCount='1'`, `transitionEffectAdapterEvidenceCount='2'`, `transitionEffectAdapterEvidenceFailed='0'` and `transitionEffectAdapterEvidenceStatuses='Succeeded, Succeeded'`.

If Loading works but Transition does not, keep the domains separate: `UnityLoadingSurfaceAdapter` proves Loading presentation only. It does not prove that `UnityFadeCurtainEffectAdapter` or another `ITransitionEffectAdapter` exists.

## Pause surface does not show

Check:

- Logical Pause state first: `PauseRuntime` should report a valid paused snapshot after the request.
- The Pause panel is resident in `UIGlobal`.
- `UnityPauseResidentSurfaceAdapter` points at the correct hierarchy.
- Pause runtime request smoke passes.
- Pause surface diagnostics report adapter count, supported adapters, applied adapters, failed adapters and visible/hidden state transitions.

If no Pause resident adapter is configured, visual presentation should be explicit skipped/no-op behavior while logical Pause remains available. If a project declares Pause visual presentation as required in a future cut, missing visual capability must fail visibly.

RuntimeContent/ContentAnchor Pause visual materialization is experimental/frozen. Treat materialization smokes as QA evidence only, not proof that a product/runtime Pause visual consumer is ready.

Pause visual is separate from InputMode apply. A visible Pause panel does not prove Unity `PlayerInput` action-map application, and a successful InputMode apply does not prove visual materialization readiness.

## Pause input changes Pause but not PlayerInput

Use the supported bridge path:

```text
PauseInputActionRuntimeBridgeTrigger
  -> PauseInputModeUnityPlayerInputRuntimeBridge
  -> PauseResult
  -> InputMode request
  -> Unity PlayerInput application
```

Check:

- The trigger has action evidence.
- The trigger resolves an explicit runtime bridge.
- The runtime bridge has a valid `PlayerInput`.
- The InputMode action map bindings match the project's Unity Input action maps.

Do not add new usage of the retired direct Pause input adapter. It is historical and should not be used for active setup.

## Trigger diagnostics look wrong

Check the trigger's local diagnostics before changing domain setup:

- `source`
- `reason`
- submitted/completed state
- ignored/failed/succeeded outcome
- blocking issue count
- domain result or bridge result

The shared FlowTrigger helper only records local bookkeeping. It does not prove that the domain request is valid, does not choose route/activity/Pause policy and does not apply `PlayerInput`.

For Route or Activity trigger issues, inspect the domain request result and lifecycle logs. For Pause InputAction trigger issues, inspect action evidence first, then the Pause/InputMode bridge result.

Route and Activity triggers use the shared FlowTrigger helper for local phase/outcome/reason/message and succeeded/ignored/failed diagnostics. Their in-flight guard, target selection, Activity clear semantics, request policy and domain result mapping remain local. Request logs still come from `FrameworkRuntimeHost`, and `gameFlowEnvelope*` remains host projection.

## RuntimeContent materializes but ContentAnchor placement fails

Check:

- RuntimeContent materialization result succeeded.
- The ContentAnchor declaration exists for the same owner/scope.
- The logical ContentAnchor binding succeeded.
- The Unity placement adapter/service has valid physical evidence.
- Release/rollback logs show whether physical or logical cleanup ran.

## ContentAnchor bridge set partially materializes

Check bridge set diagnostics before materialization:

- duplicate keys
- invalid authored bridge configuration
- missing runtime/anchor/adapter references
- pre-existing active bridge state

Bridge sets should roll back partial batch materialization when a later bridge fails.

## QA Canvas does not show an old smoke button

The QA Canvas should expose current validation buttons. Historical proof buttons may have been removed from the visible primary QA surface. Use the current smoke groups in `QA-Smokes.md`.
