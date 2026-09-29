using System.Collections.Generic;
using Immersive.Framework.Camera;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal sealed class CameraOutputAuthoringValidationResult
    {
        private readonly List<string> _blockingIssues;

        internal CameraOutputAuthoringValidationResult(
            List<string> blockingIssues)
        {
            this._blockingIssues = blockingIssues ?? new List<string>();
        }

        internal bool IsValid => _blockingIssues.Count == 0;
        internal int BlockingIssueCount => _blockingIssues.Count;
        internal IReadOnlyList<string> BlockingIssues => _blockingIssues;
    }

    /// <summary>
    /// Explicit, button-driven validation for one Camera Output component.
    /// Session topology is validated by CameraSessionConfiguration.
    /// </summary>
    internal static class CameraOutputAuthoringValidator
    {
        internal static CameraOutputAuthoringValidationResult Validate(
            CameraOutputAuthoring binding)
        {
            var issues = new List<string>();

            if (binding == null)
            {
                issues.Add(
                    "Camera Output validation requires a target component.");
                return new CameraOutputAuthoringValidationResult(issues);
            }

            string identityIssue = binding.OutputDefinition == null
                ? "Assign an Output Definition asset."
                : CameraDefinitionIdentityEditorUtility.ValidateLocalIdentity(binding.OutputDefinition);
            if (identityIssue != null) issues.Add(identityIssue);

            if (binding.UnityCamera == null)
            {
                issues.Add(
                    "Assign the physical Unity Camera used by this output.");
            }

            if (binding.CinemachineBrain == null)
            {
                issues.Add(
                    "Assign the Cinemachine Brain used by this output.");
            }

            if (binding.FallbackCameraRig == null)
            {
                issues.Add(
                    "Assign the Output's explicit Fallback Camera, which must be available before a normal Camera Occurrence can be applied.");
            }

            if (binding.UnityCamera != null &&
                binding.CinemachineBrain != null &&
                binding.UnityCamera.gameObject !=
                    binding.CinemachineBrain.gameObject)
            {
                issues.Add(
                    "The Unity Camera and Cinemachine Brain must be on the same GameObject.");
            }

            return new CameraOutputAuthoringValidationResult(issues);
        }
    }
}
