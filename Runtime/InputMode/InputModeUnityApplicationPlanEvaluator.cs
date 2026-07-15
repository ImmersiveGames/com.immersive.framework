using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Builds a side-effect-free Unity Input application plan from F32A/F32B previews.
    /// This dry run reports intent only; it never calls PlayerInput or PlayerInputManager behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32C InputMode Unity application dry-run plan evaluator.")]
    public static class InputModeUnityApplicationPlanEvaluator
    {
        public static InputModeUnityApplicationPlanResult BuildPlan(
            InputModeUnityApplicationPreviewResult applicationPreview,
            InputModeUnityActionMapPreviewResult actionMapPreview,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityApplicationPlanEvaluator));
            var issues = new List<InputModeUnityApplicationPlanIssue>();

            InputModeKind requestedMode = ResolveRequestedMode(applicationPreview, actionMapPreview);
            UnityInputActionMapName actionMapName = actionMapPreview == null
                ? UnityInputActionMapName.From(string.Empty)
                : actionMapPreview.ActionMapName;

            if (applicationPreview == null || !applicationPreview.Succeeded)
            {
                issues.Add(InputModeUnityApplicationPlanIssue.BlockingIssue(
                    InputModeUnityApplicationPlanIssueKind.ApplicationPreviewNotSucceeded,
                    requestedMode,
                    actionMapName,
                    normalizedSource,
                    "Unity Input application plan requires a successful InputMode Unity application preview."));

                return CreateResult(
                    InputModeUnityApplicationPlanStatus.FailedApplicationPreview,
                    requestedMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    applicationPreview == null ? UnityInputTargetRole.Unknown : applicationPreview.TargetRole,
                    actionMapName,
                    actionMapPreview is { ActionMapRequired: true },
                    actionMapPreview is { ActionMapAvailable: true },
                    applicationPreview is { PlayerActorRequired: true },
                    applicationPreview is { LocalPlayerProvisioningRequired: true },
                    issues,
                    normalizedSource,
                    reason);
            }

            if (actionMapPreview == null || !actionMapPreview.Succeeded)
            {
                issues.Add(InputModeUnityApplicationPlanIssue.BlockingIssue(
                    InputModeUnityApplicationPlanIssueKind.ActionMapPreviewNotSucceeded,
                    requestedMode,
                    actionMapName,
                    normalizedSource,
                    "Unity Input application plan requires a successful InputMode Unity action-map preview."));

                return CreateResult(
                    InputModeUnityApplicationPlanStatus.FailedActionMapPreview,
                    requestedMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    applicationPreview.TargetRole,
                    actionMapName,
                    actionMapPreview is { ActionMapRequired: true },
                    actionMapPreview is { ActionMapAvailable: true },
                    applicationPreview.PlayerActorRequired,
                    applicationPreview.LocalPlayerProvisioningRequired,
                    issues,
                    normalizedSource,
                    reason);
            }

            if (applicationPreview.RequestedMode != actionMapPreview.RequestedMode)
            {
                issues.Add(InputModeUnityApplicationPlanIssue.BlockingIssue(
                    InputModeUnityApplicationPlanIssueKind.PreviewModeMismatch,
                    requestedMode,
                    actionMapPreview.ActionMapName,
                    normalizedSource,
                    "Application preview and action-map preview must target the same InputMode."));

                return CreateResult(
                    InputModeUnityApplicationPlanStatus.FailedPreviewMismatch,
                    requestedMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    applicationPreview.TargetRole,
                    actionMapPreview.ActionMapName,
                    actionMapPreview.ActionMapRequired,
                    actionMapPreview.ActionMapAvailable,
                    applicationPreview.PlayerActorRequired,
                    applicationPreview.LocalPlayerProvisioningRequired,
                    issues,
                    normalizedSource,
                    reason);
            }

            InputModeUnityApplicationPlanOperation operation;
            if (!TryResolveOperation(actionMapPreview.RequestedMode, actionMapPreview.ActionMapRequired, out operation))
            {
                issues.Add(InputModeUnityApplicationPlanIssue.BlockingIssue(
                    InputModeUnityApplicationPlanIssueKind.UnsupportedInputMode,
                    actionMapPreview.RequestedMode,
                    actionMapPreview.ActionMapName,
                    normalizedSource,
                    "InputMode has no Unity Input application plan operation."));

                return CreateResult(
                    InputModeUnityApplicationPlanStatus.FailedUnsupportedInputMode,
                    actionMapPreview.RequestedMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    applicationPreview.TargetRole,
                    actionMapPreview.ActionMapName,
                    actionMapPreview.ActionMapRequired,
                    actionMapPreview.ActionMapAvailable,
                    applicationPreview.PlayerActorRequired,
                    applicationPreview.LocalPlayerProvisioningRequired,
                    issues,
                    normalizedSource,
                    reason);
            }

            return CreateResult(
                InputModeUnityApplicationPlanStatus.Succeeded,
                actionMapPreview.RequestedMode,
                operation,
                applicationPreview.TargetRole,
                actionMapPreview.ActionMapName,
                actionMapPreview.ActionMapRequired,
                actionMapPreview.ActionMapAvailable,
                applicationPreview.PlayerActorRequired,
                applicationPreview.LocalPlayerProvisioningRequired,
                issues,
                normalizedSource,
                reason);
        }

        private static bool TryResolveOperation(
            InputModeKind requestedMode,
            bool actionMapRequired,
            out InputModeUnityApplicationPlanOperation operation)
        {
            operation = InputModeUnityApplicationPlanOperation.NoOperation;

            switch (requestedMode)
            {
                case InputModeKind.Gameplay:
                case InputModeKind.PauseOverlay:
                case InputModeKind.FrontendMenu:
                    operation = actionMapRequired
                        ? InputModeUnityApplicationPlanOperation.SelectActionMap
                        : InputModeUnityApplicationPlanOperation.NoOperation;
                    return true;
                case InputModeKind.InputLocked:
                    operation = InputModeUnityApplicationPlanOperation.LockInput;
                    return true;
                default:
                    return false;
            }
        }

        private static InputModeKind ResolveRequestedMode(
            InputModeUnityApplicationPreviewResult applicationPreview,
            InputModeUnityActionMapPreviewResult actionMapPreview)
        {
            if (applicationPreview != null && applicationPreview.RequestedMode != InputModeKind.Unknown)
            {
                return applicationPreview.RequestedMode;
            }

            return actionMapPreview == null ? InputModeKind.Unknown : actionMapPreview.RequestedMode;
        }

        private static InputModeUnityApplicationPlanResult CreateResult(
            InputModeUnityApplicationPlanStatus status,
            InputModeKind requestedMode,
            InputModeUnityApplicationPlanOperation operation,
            UnityInputTargetRole targetRole,
            UnityInputActionMapName actionMapName,
            bool actionMapRequired,
            bool actionMapAvailable,
            bool playerActorRequired,
            bool localPlayerProvisioningRequired,
            List<InputModeUnityApplicationPlanIssue> issues,
            string source,
            string reason)
        {
            return new InputModeUnityApplicationPlanResult(
                status,
                requestedMode,
                operation,
                targetRole,
                actionMapName,
                actionMapRequired,
                actionMapAvailable,
                playerActorRequired,
                localPlayerProvisioningRequired,
                issues == null ? null : issues.ToArray(),
                source,
                reason);
        }
    }
}
