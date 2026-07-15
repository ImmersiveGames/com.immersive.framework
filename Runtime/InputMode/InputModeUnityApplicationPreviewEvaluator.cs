using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Pure preview evaluator that checks whether an InputMode request has enough Unity Input evidence to be applied later by an adapter.
    /// It never switches action maps, activates PlayerInput, calls PlayerInputManager join APIs or owns player input.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32A InputMode Unity application preview evaluator.")]
    public static class InputModeUnityApplicationPreviewEvaluator
    {
        public static InputModeUnityApplicationPreviewResult Preview(
            InputModeRequestResult inputModeResult,
            UnityInputTargetSet targetSet,
            PlayerActorSet playerActorSet,
            LocalPlayerProvisioningValidationResult localPlayerProvisioningValidation,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityApplicationPreviewEvaluator));
            var issues = new List<InputModeUnityApplicationPreviewIssue>();

            if (inputModeResult == null || !inputModeResult.Succeeded)
            {
                InputModeKind failedMode = inputModeResult == null ? InputModeKind.Unknown : inputModeResult.Request.TargetMode;
                issues.Add(InputModeUnityApplicationPreviewIssue.BlockingIssue(
                    InputModeUnityApplicationPreviewIssueKind.InputModeRequestNotSucceeded,
                    failedMode,
                    UnityInputTargetRole.Unknown,
                    normalizedSource,
                    "InputMode Unity application preview requires a successful InputMode request result."));

                return CreateResult(
                    InputModeUnityApplicationPreviewStatus.FailedInputModeRequest,
                    failedMode,
                    UnityInputTargetRole.Unknown,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    issues,
                    normalizedSource,
                    reason);
            }

            InputModeKind requestedMode = inputModeResult.Request.TargetMode;
            UnityInputTargetRole targetRole;
            bool targetRequired;
            bool playerActorRequired;
            bool localPlayerProvisioningRequired;
            if (!TryResolvePolicy(
                requestedMode,
                out targetRole,
                out targetRequired,
                out playerActorRequired,
                out localPlayerProvisioningRequired))
            {
                issues.Add(InputModeUnityApplicationPreviewIssue.BlockingIssue(
                    InputModeUnityApplicationPreviewIssueKind.UnsupportedInputMode,
                    requestedMode,
                    UnityInputTargetRole.Unknown,
                    normalizedSource,
                    "InputMode Unity application preview only supports explicit canonical InputMode kinds."));

                return CreateResult(
                    InputModeUnityApplicationPreviewStatus.FailedUnsupportedMode,
                    requestedMode,
                    UnityInputTargetRole.Unknown,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    issues,
                    normalizedSource,
                    reason);
            }

            bool targetAvailable = !targetRequired || HasRequiredTarget(targetSet, targetRole);
            if (!targetAvailable)
            {
                issues.Add(InputModeUnityApplicationPreviewIssue.BlockingIssue(
                    InputModeUnityApplicationPreviewIssueKind.MissingRequiredUnityInputTarget,
                    requestedMode,
                    targetRole,
                    normalizedSource,
                    "InputMode Unity application preview requires a valid Unity Input target for the resolved role."));
            }

            bool playerActorAvailable = !playerActorRequired || HasRequiredPlayerActor(playerActorSet);
            if (!playerActorAvailable)
            {
                issues.Add(InputModeUnityApplicationPreviewIssue.BlockingIssue(
                    InputModeUnityApplicationPreviewIssueKind.MissingRequiredPlayerActor,
                    requestedMode,
                    targetRole,
                    normalizedSource,
                    "Gameplay InputMode preview requires a framework-recognized PlayerActor with Unity PlayerInput evidence."));
            }

            bool localPlayerProvisioningAvailable = !localPlayerProvisioningRequired || HasRequiredLocalPlayerProvisioning(localPlayerProvisioningValidation);
            if (!localPlayerProvisioningAvailable)
            {
                issues.Add(InputModeUnityApplicationPreviewIssue.BlockingIssue(
                    InputModeUnityApplicationPreviewIssueKind.MissingRequiredLocalPlayerProvisioning,
                    requestedMode,
                    targetRole,
                    normalizedSource,
                    "Gameplay InputMode preview requires one Session-scoped Unity PlayerInputManager evidence component."));
            }

            InputModeUnityApplicationPreviewStatus status = issues.Count == 0
                ? InputModeUnityApplicationPreviewStatus.Succeeded
                : ResolveFailureStatus(issues[0].Kind);

            return CreateResult(
                status,
                requestedMode,
                targetRole,
                targetRequired,
                targetAvailable,
                playerActorRequired,
                playerActorAvailable,
                localPlayerProvisioningRequired,
                localPlayerProvisioningAvailable,
                issues,
                normalizedSource,
                reason);
        }

        private static bool TryResolvePolicy(
            InputModeKind mode,
            out UnityInputTargetRole targetRole,
            out bool targetRequired,
            out bool playerActorRequired,
            out bool localPlayerProvisioningRequired)
        {
            switch (mode)
            {
                case InputModeKind.Gameplay:
                    targetRole = UnityInputTargetRole.GameplayCommands;
                    targetRequired = true;
                    playerActorRequired = true;
                    localPlayerProvisioningRequired = true;
                    return true;
                case InputModeKind.PauseOverlay:
                case InputModeKind.FrontendMenu:
                    targetRole = UnityInputTargetRole.GlobalUiPause;
                    targetRequired = true;
                    playerActorRequired = false;
                    localPlayerProvisioningRequired = false;
                    return true;
                case InputModeKind.InputLocked:
                    targetRole = UnityInputTargetRole.Unknown;
                    targetRequired = false;
                    playerActorRequired = false;
                    localPlayerProvisioningRequired = false;
                    return true;
                default:
                    targetRole = UnityInputTargetRole.Unknown;
                    targetRequired = false;
                    playerActorRequired = false;
                    localPlayerProvisioningRequired = false;
                    return false;
            }
        }

        private static bool HasRequiredTarget(UnityInputTargetSet targetSet, UnityInputTargetRole role)
        {
            if (targetSet == null || targetSet.Failed || role == UnityInputTargetRole.Unknown)
            {
                return false;
            }

            return targetSet.TryGetSingle(role, out UnityInputTargetDescriptor descriptor) && descriptor.IsValid;
        }

        private static bool HasRequiredPlayerActor(PlayerActorSet playerActorSet)
        {
            return playerActorSet is { Succeeded: true, Count: > 0, PlayerInputEvidenceCount: > 0 };
        }

        private static bool HasRequiredLocalPlayerProvisioning(LocalPlayerProvisioningValidationResult evidence)
        {
            return evidence is { Succeeded: true, Required: true, Available: true, SurfaceCount: 1 };
        }

        private static InputModeUnityApplicationPreviewStatus ResolveFailureStatus(InputModeUnityApplicationPreviewIssueKind issueKind)
        {
            switch (issueKind)
            {
                case InputModeUnityApplicationPreviewIssueKind.MissingRequiredUnityInputTarget:
                case InputModeUnityApplicationPreviewIssueKind.InvalidUnityInputTargetEvidence:
                    return InputModeUnityApplicationPreviewStatus.FailedTargetEvidence;
                case InputModeUnityApplicationPreviewIssueKind.MissingRequiredPlayerActor:
                case InputModeUnityApplicationPreviewIssueKind.InvalidPlayerActorEvidence:
                    return InputModeUnityApplicationPreviewStatus.FailedPlayerActorEvidence;
                case InputModeUnityApplicationPreviewIssueKind.MissingRequiredLocalPlayerProvisioning:
                case InputModeUnityApplicationPreviewIssueKind.InvalidLocalPlayerProvisioning:
                    return InputModeUnityApplicationPreviewStatus.FailedLocalPlayerProvisioning;
                case InputModeUnityApplicationPreviewIssueKind.UnsupportedInputMode:
                    return InputModeUnityApplicationPreviewStatus.FailedUnsupportedMode;
                default:
                    return InputModeUnityApplicationPreviewStatus.FailedInputModeRequest;
            }
        }

        private static InputModeUnityApplicationPreviewResult CreateResult(
            InputModeUnityApplicationPreviewStatus status,
            InputModeKind requestedMode,
            UnityInputTargetRole targetRole,
            bool targetRequired,
            bool targetAvailable,
            bool playerActorRequired,
            bool playerActorAvailable,
            bool localPlayerProvisioningRequired,
            bool localPlayerProvisioningAvailable,
            List<InputModeUnityApplicationPreviewIssue> issues,
            string source,
            string reason)
        {
            return new InputModeUnityApplicationPreviewResult(
                status,
                requestedMode,
                targetRole,
                targetRequired,
                targetAvailable,
                playerActorRequired,
                playerActorAvailable,
                localPlayerProvisioningRequired,
                localPlayerProvisioningAvailable,
                issues == null ? Array.Empty<InputModeUnityApplicationPreviewIssue>() : issues.ToArray(),
                source,
                reason);
        }
    }
}
