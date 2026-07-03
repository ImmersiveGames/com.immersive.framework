
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Resolves InputMode application preview to project-owned Unity action map evidence.
    /// It never calls PlayerInput.SwitchCurrentActionMap and never mutates Unity Input.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B InputMode Unity action map preview evaluator.")]
    public static class InputModeUnityActionMapPreviewEvaluator
    {
        public static InputModeUnityActionMapPreviewResult Preview(
            InputModeUnityApplicationPreviewResult applicationPreview,
            UnityInputActionMapEvidence actionMapEvidence,
            IEnumerable<InputModeUnityActionMapBinding> bindings,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityActionMapPreviewEvaluator));
            var issues = new List<InputModeUnityActionMapPreviewIssue>();

            InputModeKind requestedMode = applicationPreview == null ? InputModeKind.Unknown : applicationPreview.RequestedMode;
            if (applicationPreview == null || !applicationPreview.Succeeded)
            {
                issues.Add(InputModeUnityActionMapPreviewIssue.BlockingIssue(
                    InputModeUnityActionMapPreviewIssueKind.ApplicationPreviewNotSucceeded,
                    requestedMode,
                    UnityInputActionMapName.From(string.Empty),
                    normalizedSource,
                    "InputMode Unity action map preview requires a successful Unity application preview."));

                return CreateResult(
                    InputModeUnityActionMapPreviewStatus.FailedApplicationPreview,
                    requestedMode,
                    UnityInputActionMapName.From(string.Empty),
                    false,
                    false,
                    actionMapEvidence is { HasActionAsset: true },
                    actionMapEvidence == null ? 0 : actionMapEvidence.ActionMapCount,
                    issues,
                    normalizedSource,
                    reason);
            }

            InputModeUnityActionMapBinding binding;
            if (!TryGetBinding(bindings, requestedMode, out binding))
            {
                issues.Add(InputModeUnityActionMapPreviewIssue.BlockingIssue(
                    InputModeUnityActionMapPreviewIssueKind.UnsupportedInputMode,
                    requestedMode,
                    UnityInputActionMapName.From(string.Empty),
                    normalizedSource,
                    "No InputMode-to-Unity-action-map binding exists for the requested mode."));

                return CreateResult(
                    InputModeUnityActionMapPreviewStatus.FailedUnsupportedMode,
                    requestedMode,
                    UnityInputActionMapName.From(string.Empty),
                    false,
                    false,
                    actionMapEvidence is { HasActionAsset: true },
                    actionMapEvidence == null ? 0 : actionMapEvidence.ActionMapCount,
                    issues,
                    normalizedSource,
                    reason);
            }

            if (!binding.ActionMapRequired)
            {
                return CreateResult(
                    InputModeUnityActionMapPreviewStatus.Succeeded,
                    requestedMode,
                    binding.ActionMapName,
                    false,
                    true,
                    actionMapEvidence is { HasActionAsset: true },
                    actionMapEvidence == null ? 0 : actionMapEvidence.ActionMapCount,
                    issues,
                    normalizedSource,
                    reason);
            }

            bool hasActionAsset = actionMapEvidence is { HasActionAsset: true };
            bool actionMapAvailable = actionMapEvidence != null && actionMapEvidence.Contains(binding.ActionMapName);

            if (!hasActionAsset)
            {
                issues.Add(InputModeUnityActionMapPreviewIssue.BlockingIssue(
                    InputModeUnityActionMapPreviewIssueKind.MissingActionMapEvidence,
                    requestedMode,
                    binding.ActionMapName,
                    normalizedSource,
                    "Required InputMode action-map preview has no Unity InputActionAsset evidence."));
            }
            else if (!actionMapAvailable)
            {
                issues.Add(InputModeUnityActionMapPreviewIssue.BlockingIssue(
                    InputModeUnityActionMapPreviewIssueKind.MissingRequiredActionMap,
                    requestedMode,
                    binding.ActionMapName,
                    normalizedSource,
                    "Required Unity action map was not found in the provided action-map evidence."));
            }

            return CreateResult(
                issues.Count == 0
                    ? InputModeUnityActionMapPreviewStatus.Succeeded
                    : InputModeUnityActionMapPreviewStatus.FailedActionMapEvidence,
                requestedMode,
                binding.ActionMapName,
                true,
                actionMapAvailable,
                hasActionAsset,
                actionMapEvidence == null ? 0 : actionMapEvidence.ActionMapCount,
                issues,
                normalizedSource,
                reason);
        }

        private static bool TryGetBinding(
            IEnumerable<InputModeUnityActionMapBinding> bindings,
            InputModeKind requestedMode,
            out InputModeUnityActionMapBinding binding)
        {
            binding = default;
            if (bindings == null)
            {
                return false;
            }

            foreach (InputModeUnityActionMapBinding candidate in bindings)
            {
                if (!candidate.IsValid || candidate.InputMode != requestedMode)
                {
                    continue;
                }

                binding = candidate;
                return true;
            }

            return false;
        }

        private static InputModeUnityActionMapPreviewResult CreateResult(
            InputModeUnityActionMapPreviewStatus status,
            InputModeKind requestedMode,
            UnityInputActionMapName actionMapName,
            bool actionMapRequired,
            bool actionMapAvailable,
            bool hasActionAsset,
            int availableActionMapCount,
            List<InputModeUnityActionMapPreviewIssue> issues,
            string source,
            string reason)
        {
            return new InputModeUnityActionMapPreviewResult(
                status,
                requestedMode,
                actionMapName,
                actionMapRequired,
                actionMapAvailable,
                hasActionAsset,
                availableActionMapCount,
                issues == null ? null : issues.ToArray(),
                source,
                reason);
        }
    }
}
