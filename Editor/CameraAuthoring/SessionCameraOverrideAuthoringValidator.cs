using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal sealed class SessionCameraOverrideAuthoringValidationResult
    {
        private readonly List<string> _blockingIssues;

        internal SessionCameraOverrideAuthoringValidationResult(
            List<string> blockingIssues)
        {
            _blockingIssues = blockingIssues ?? new List<string>();
        }

        internal bool IsValid => _blockingIssues.Count == 0;
        internal int BlockingIssueCount => _blockingIssues.Count;
        internal IReadOnlyList<string> BlockingIssues => _blockingIssues;
    }

    /// <summary>
    /// Explicit, button-driven validation for one Session Camera Override.
    /// It does not initialize runtime services, publish requests, repair references
    /// or modify the authored scene.
    /// </summary>
    internal static class SessionCameraOverrideAuthoringValidator
    {
        internal static SessionCameraOverrideAuthoringValidationResult Validate(
            SessionCameraOverride binding)
        {
            var issues = new List<string>();

            if (binding == null)
            {
                issues.Add(
                    "Session Camera Override validation requires a target component.");
                return new SessionCameraOverrideAuthoringValidationResult(issues);
            }

            ValidateIdentity(binding, issues);
            if (!binding.RequestedOutputId.IsValid)
            {
                issues.Add("Assign the exact Camera Output ID that receives this request.");
            }
            ValidateRig(binding.RigComposer, issues);

            if (binding.TargetSource == null)
            {
                issues.Add(
                    "Assign the explicit Target used by the Session camera request.");
            }

            return new SessionCameraOverrideAuthoringValidationResult(issues);
        }

        private static void ValidateIdentity(
            SessionCameraOverride binding,
            ICollection<string> issues)
        {
            if (string.IsNullOrWhiteSpace(binding.ScopeId))
            {
                issues.Add(
                    "Generate or assign a Session Scope ID.");
            }

            if (string.IsNullOrWhiteSpace(binding.RequestIdText))
            {
                issues.Add(
                    "Generate or assign a Camera Request ID.");
            }

            if (string.IsNullOrWhiteSpace(binding.TieBreakerId))
            {
                issues.Add(
                    "Generate or assign a Tie Breaker ID.");
            }
        }

        private static void ValidateRig(
            CameraRigComposer composer,
            ICollection<string> issues)
        {
            if (composer == null)
            {
                issues.Add(
                    "Assign the Camera Rig Composer used by this request.");
                return;
            }

            if (!composer.TryValidateForApply(out string issue))
            {
                issues.Add(
                    $"Camera Rig Composer is not ready: {issue}");
                return;
            }

            CameraTargetResolveResult targets =
                composer.ResolveCameraTargets(
                    composer.FollowRequirement,
                    composer.LookAtRequirement);

            if (targets.IsBlocked)
            {
                issues.Add(
                    $"Camera Rig Composer target resolution is blocked: {targets.BlockingIssue}");
            }
        }
    }
}
