using System.Collections.Generic;
using Immersive.Framework.Camera;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal sealed class CameraOutputSessionBindingAuthoringValidationResult
    {
        private readonly List<string> blockingIssues;

        internal CameraOutputSessionBindingAuthoringValidationResult(
            List<string> blockingIssues)
        {
            this.blockingIssues = blockingIssues ?? new List<string>();
        }

        internal bool IsValid => blockingIssues.Count == 0;
        internal int BlockingIssueCount => blockingIssues.Count;
        internal IReadOnlyList<string> BlockingIssues => blockingIssues;
    }

    /// <summary>
    /// Explicit, button-driven validation for one persistent Camera Output.
    /// It does not initialize runtime services, create components, discover
    /// references or repair the authored scene.
    /// </summary>
    internal static class CameraOutputSessionBindingAuthoringValidator
    {
        internal static CameraOutputSessionBindingAuthoringValidationResult Validate(
            CameraOutputSessionBinding binding)
        {
            var issues = new List<string>();

            if (binding == null)
            {
                issues.Add(
                    "Camera Output validation requires a target component.");
                return new CameraOutputSessionBindingAuthoringValidationResult(issues);
            }

            if (string.IsNullOrWhiteSpace(binding.OutputIdText))
            {
                issues.Add(
                    "Generate or assign a Camera Output ID.");
            }

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

            if (binding.UnityCamera != null &&
                binding.CinemachineBrain != null &&
                binding.UnityCamera.gameObject !=
                    binding.CinemachineBrain.gameObject)
            {
                issues.Add(
                    "The Unity Camera and Cinemachine Brain must be on the same GameObject.");
            }

            return new CameraOutputSessionBindingAuthoringValidationResult(issues);
        }
    }
}
