using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Non-MonoBehaviour service that applies one already-authorized resident
    /// InputMode transaction through the Pause and Unity PlayerInput pipeline.
    /// It owns no state and never reconstructs InputMode posture implicitly.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC2 Pause/InputMode apply service consuming resident state evidence.")]
    internal sealed class PauseInputModeApplyService
    {
        internal PauseInputModeApplyResult Apply(
            PauseInputModeApplyRequest request)
        {
            if (request == null)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedConfiguration,
                    PauseInputModeApplyStage.InvalidRequest,
                    PauseRequestKind.Unknown,
                    PauseState.Unknown,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    UnityInputActionMapName.From(string.Empty),
                    nameof(PauseInputModeApplyService),
                    string.Empty,
                    "Pause/InputMode apply request is missing.");
            }

            string source = request.Source.NormalizeTextOrFallback(
                nameof(PauseInputModeApplyService));
            string reason = request.Reason.NormalizeText();
            UnityInputActionMapName previousActionMap =
                UnityInputActionMapName.From(
                    CurrentActionMapName(request.PlayerInput));

            if (!Enum.IsDefined(
                    typeof(PauseRequestKind),
                    request.RequestKind) ||
                request.RequestKind == PauseRequestKind.Unknown)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedConfiguration,
                    PauseInputModeApplyStage.InvalidRequest,
                    request.RequestKind,
                    PauseState.Unknown,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "Pause/InputMode apply request kind must be explicit.");
            }

            if (request.RuntimeHost == null)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedRuntimeUnavailable,
                    PauseInputModeApplyStage.MissingRuntimeHost,
                    request.RequestKind,
                    PauseState.Unknown,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "FrameworkRuntimeHost is unavailable.");
            }

            if (!request.RuntimeHost.TryGetPauseSnapshot(
                    out PauseSnapshot pauseSnapshot) ||
                pauseSnapshot.State == PauseState.Unknown)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedRuntimeUnavailable,
                    PauseInputModeApplyStage.MissingPauseRuntime,
                    request.RequestKind,
                    PauseState.Unknown,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "Pause runtime snapshot is unavailable.");
            }

            if (request.PlayerInput == null)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedConfiguration,
                    PauseInputModeApplyStage.MissingPlayerInput,
                    request.RequestKind,
                    pauseSnapshot.State,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "Pause/InputMode apply requires an explicit PlayerInput reference.");
            }

            if (request.PlayerInput.actions == null)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedConfiguration,
                    PauseInputModeApplyStage.MissingActionMap,
                    request.RequestKind,
                    pauseSnapshot.State,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "Pause/InputMode apply requires PlayerInput.actions before " +
                    "applying InputMode.");
            }

            if (request.RequireLocalPlayerProvisioning &&
                request.LocalPlayerProvisioningValidation != null &&
                request.LocalPlayerProvisioningValidation.Failed)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedConfiguration,
                    PauseInputModeApplyStage.PreflightRejected,
                    request.RequestKind,
                    pauseSnapshot.State,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    request.LocalPlayerProvisioningValidation
                        .ToDiagnosticString());
            }

            if (!request.CurrentInputModeState.IsValid)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedPreflight,
                    PauseInputModeApplyStage.PreflightRejected,
                    request.RequestKind,
                    pauseSnapshot.State,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "Pause/InputMode apply requires the exact current state " +
                    "from the resident InputMode authority.");
            }

            InputModeKind expectedCurrentMode =
                MapPauseStateToInputMode(pauseSnapshot.State);
            if (request.CurrentInputModeState.CurrentKind !=
                expectedCurrentMode)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedPreflight,
                    PauseInputModeApplyStage.PreflightRejected,
                    request.RequestKind,
                    pauseSnapshot.State,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "Pause/InputMode apply rejected state drift. " +
                    $"inputMode='{request.CurrentInputModeState.CurrentKind}' " +
                    $"revision='{request.CurrentInputModeState.Revision}' " +
                    $"pause='{pauseSnapshot.State}'.");
            }

            if (!TryValidatePersistentActionMaps(
                    request,
                    source,
                    reason,
                    out InputModeUnityApplicationPlanResult persistentMapFailure))
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedPreflight,
                    PauseInputModeApplyStage.PreflightRejected,
                    request.RequestKind,
                    pauseSnapshot.State,
                    PauseState.Unknown,
                    default,
                    persistentMapFailure,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "Persistent action-map preflight failed before " +
                    "submitting the Pause request.");
            }

            PauseRequest pauseRequest = CreatePauseRequest(
                request.RequestKind,
                request.RequestId,
                source,
                reason);
            PauseState targetPauseState = PauseRequest.ResolveTargetState(
                request.RequestKind,
                pauseSnapshot.State);
            PauseResult anticipatedPauseResult =
                targetPauseState == pauseSnapshot.State
                    ? PauseResult.IgnoredNoChangeResult(
                        pauseRequest,
                        pauseSnapshot.State,
                        "Pause/InputMode apply preflight detected no Pause " +
                        "state change.")
                    : PauseResult.AppliedResult(
                        pauseRequest,
                        pauseSnapshot.State,
                        targetPauseState,
                        "Pause/InputMode apply preflight detected a Pause " +
                        "state change.");

            InputModeState currentInputModeState =
                request.CurrentInputModeState;
            InputModeUnityApplicationPlanResult preflightPlan =
                BuildPreflightPlan(
                    anticipatedPauseResult,
                    currentInputModeState,
                    request,
                    source,
                    reason);

            if (preflightPlan != null && preflightPlan.Failed)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedPreflight,
                    PauseInputModeApplyStage.PreflightRejected,
                    request.RequestKind,
                    pauseSnapshot.State,
                    targetPauseState,
                    default,
                    preflightPlan,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    "Pause/InputMode apply preflight failed before " +
                    "submitting the Pause request.");
            }

            PauseResult pauseResult;
            try
            {
                pauseResult = request.RuntimeHost.RequestPause(pauseRequest);
            }
            catch (Exception exception)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedPauseRequest,
                    PauseInputModeApplyStage.PauseRequestFailed,
                    request.RequestKind,
                    pauseSnapshot.State,
                    targetPauseState,
                    default,
                    preflightPlan,
                    null,
                    previousActionMap,
                    source,
                    reason,
                    exception.Message);
            }

            PauseInputModeUnityPlayerInputApplicationResult applicationResult =
                PauseInputModeUnityPlayerInputApplication.Apply(
                    pauseResult,
                    currentInputModeState,
                    request.TargetSet,
                    request.PlayerActorSet,
                    request.LocalPlayerProvisioningValidation,
                    request.ActionMapEvidence,
                    request.ActionMapBindings,
                    request.PlayerInput,
                    source,
                    reason,
                    request.PersistentActionMapNames);

            PauseInputModeUnityPlayerInputRuntimeBridgeStatus status =
                applicationResult.Succeeded
                    ? PauseInputModeUnityPlayerInputRuntimeBridgeStatus.Succeeded
                    : applicationResult.Ignored
                        ? PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                            .IgnoredInputModeRequest
                        : PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                            .FailedInputModePlayerInputApplication;

            PauseInputModeApplyStage failedStage = applicationResult.Failed
                ? PauseInputModeApplyStage.AdapterApplyFailed
                : PauseInputModeApplyStage.None;

            return CreateResult(
                status,
                failedStage,
                request.RequestKind,
                pauseSnapshot.State,
                targetPauseState,
                pauseResult,
                preflightPlan,
                applicationResult,
                previousActionMap,
                source,
                reason,
                $"InputMode PlayerInput application " +
                $"{applicationResult.Status}.");
        }

        private static InputModeUnityApplicationPlanResult BuildPreflightPlan(
            PauseResult anticipatedPauseResult,
            InputModeState currentInputModeState,
            PauseInputModeApplyRequest request,
            string source,
            string reason)
        {
            InputModeRequest inputModeRequest =
                PauseInputModeRequestMapper.CreateRequest(
                    anticipatedPauseResult,
                    source,
                    reason.NormalizeTextOrFallback(
                        "pause-inputmode-apply-preflight"));

            InputModeRequestResult requestPreview =
                InputModeRequestEvaluator.Preview(
                    currentInputModeState,
                    inputModeRequest,
                    source);
            if (requestPreview.Ignored)
            {
                return null;
            }

            if (!requestPreview.Succeeded)
            {
                return new InputModeUnityApplicationPlanResult(
                    InputModeUnityApplicationPlanStatus.FailedPreviewMismatch,
                    inputModeRequest.TargetMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    UnityInputTargetRole.Unknown,
                    UnityInputActionMapName.From(string.Empty),
                    false,
                    false,
                    false,
                    false,
                    new[]
                    {
                        InputModeUnityApplicationPlanIssue.BlockingIssue(
                            InputModeUnityApplicationPlanIssueKind
                                .PreviewModeMismatch,
                            inputModeRequest.TargetMode,
                            UnityInputActionMapName.From(string.Empty),
                            source,
                            "InputMode request preflight did not succeed.")
                    },
                    source,
                    reason);
            }

            InputModeUnityApplicationPreviewResult applicationPreview =
                InputModeUnityApplicationPreviewEvaluator.Preview(
                    requestPreview,
                    request.TargetSet,
                    request.PlayerActorSet,
                    request.LocalPlayerProvisioningValidation,
                    source,
                    reason);
            if (!applicationPreview.Succeeded)
            {
                return new InputModeUnityApplicationPlanResult(
                    InputModeUnityApplicationPlanStatus
                        .FailedApplicationPreview,
                    inputModeRequest.TargetMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    applicationPreview.TargetRole,
                    UnityInputActionMapName.From(string.Empty),
                    false,
                    false,
                    applicationPreview.PlayerActorRequired,
                    applicationPreview.LocalPlayerProvisioningRequired,
                    new[]
                    {
                        InputModeUnityApplicationPlanIssue.BlockingIssue(
                            InputModeUnityApplicationPlanIssueKind
                                .ApplicationPreviewNotSucceeded,
                            inputModeRequest.TargetMode,
                            UnityInputActionMapName.From(string.Empty),
                            source,
                            "InputMode Unity application preflight did not " +
                            "succeed.")
                    },
                    source,
                    reason);
            }

            InputModeUnityActionMapPreviewResult actionMapPreview =
                InputModeUnityActionMapPreviewEvaluator.Preview(
                    applicationPreview,
                    request.ActionMapEvidence,
                    request.ActionMapBindings,
                    source,
                    reason);
            if (!actionMapPreview.Succeeded)
            {
                return new InputModeUnityApplicationPlanResult(
                    InputModeUnityApplicationPlanStatus
                        .FailedActionMapPreview,
                    inputModeRequest.TargetMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    applicationPreview.TargetRole,
                    actionMapPreview.ActionMapName,
                    actionMapPreview.ActionMapRequired,
                    actionMapPreview.ActionMapAvailable,
                    applicationPreview.PlayerActorRequired,
                    applicationPreview.LocalPlayerProvisioningRequired,
                    new[]
                    {
                        InputModeUnityApplicationPlanIssue.BlockingIssue(
                            InputModeUnityApplicationPlanIssueKind
                                .ActionMapPreviewNotSucceeded,
                            inputModeRequest.TargetMode,
                            actionMapPreview.ActionMapName,
                            source,
                            "InputMode Unity action map preflight did not " +
                            "succeed.")
                    },
                    source,
                    reason);
            }

            return InputModeUnityApplicationPlanEvaluator.BuildPlan(
                applicationPreview,
                actionMapPreview,
                source,
                reason);
        }

        private static bool TryValidatePersistentActionMaps(
            PauseInputModeApplyRequest request,
            string source,
            string reason,
            out InputModeUnityApplicationPlanResult failure)
        {
            failure = null;
            UnityInputActionMapName[] persistent =
                request.PersistentActionMapNames;
            if (persistent == null || persistent.Length == 0)
            {
                return true;
            }

            for (int index = 0; index < persistent.Length; index++)
            {
                UnityInputActionMapName mapName = persistent[index];
                bool available = mapName.IsValid &&
                    request.ActionMapEvidence != null &&
                    request.ActionMapEvidence.Contains(mapName);
                if (available)
                {
                    continue;
                }

                InputModeKind requestedMode =
                    request.CurrentInputModeState.IsValid
                        ? request.CurrentInputModeState.CurrentKind
                        : InputModeKind.Unknown;
                failure = new InputModeUnityApplicationPlanResult(
                    InputModeUnityApplicationPlanStatus
                        .FailedActionMapPreview,
                    requestedMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    UnityInputTargetRole.Unknown,
                    mapName,
                    true,
                    false,
                    false,
                    false,
                    new[]
                    {
                        InputModeUnityApplicationPlanIssue.BlockingIssue(
                            InputModeUnityApplicationPlanIssueKind
                                .ActionMapPreviewNotSucceeded,
                            requestedMode,
                            mapName,
                            source,
                            "Required persistent Unity action map is missing. " +
                            "No fallback or implicit duplication was applied.")
                    },
                    source,
                    reason);
                return false;
            }

            return true;
        }

        private static PauseRequest CreatePauseRequest(
            PauseRequestKind kind,
            string requestId,
            string source,
            string reason)
        {
            switch (kind)
            {
                case PauseRequestKind.Pause:
                    return PauseRequest.Pause(
                        requestId,
                        source,
                        reason);
                case PauseRequestKind.Resume:
                    return PauseRequest.Resume(
                        requestId,
                        source,
                        reason);
                case PauseRequestKind.Toggle:
                    return PauseRequest.Toggle(
                        requestId,
                        source,
                        reason);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Pause/InputMode apply request kind must be explicit.");
            }
        }

        private static InputModeKind MapPauseStateToInputMode(
            PauseState state)
        {
            switch (state)
            {
                case PauseState.Running:
                    return InputModeKind.Gameplay;
                case PauseState.Paused:
                    return InputModeKind.PauseOverlay;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(state),
                        state,
                        "Pause/InputMode apply requires an explicit Pause state.");
            }
        }

        private static PauseInputModeApplyResult CreateResult(
            PauseInputModeUnityPlayerInputRuntimeBridgeStatus status,
            PauseInputModeApplyStage failedStage,
            PauseRequestKind requestKind,
            PauseState previousPauseState,
            PauseState targetPauseState,
            PauseResult pauseResult,
            InputModeUnityApplicationPlanResult preflightPlanResult,
            PauseInputModeUnityPlayerInputApplicationResult applicationResult,
            UnityInputActionMapName previousActionMapName,
            string source,
            string reason,
            string message)
        {
            return new PauseInputModeApplyResult(
                status,
                failedStage,
                requestKind,
                previousPauseState,
                targetPauseState,
                pauseResult,
                preflightPlanResult,
                applicationResult,
                previousActionMapName,
                source,
                reason,
                message);
        }

        private static string CurrentActionMapName(PlayerInput playerInput)
        {
            return playerInput != null &&
                   playerInput.currentActionMap != null
                ? playerInput.currentActionMap.name
                : string.Empty;
        }
    }
}
