using Immersive.Foundation.Common;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Cinemachine
{
    /// <summary>
    /// CameraComposer bridge to explicit Cinemachine output evidence.
    /// This extension does not add lifecycle authority to CameraComposer.
    /// </summary>
    public static class CameraComposerCinemachineOutputExtensions
    {
        public static bool TryCreateCinemachineOutput(
            this CameraComposer composer,
            out CinemachineCameraOutput output,
            bool required = true,
            CinemachineBrain explicitBrain = null,
            string outputId = null)
        {
            if (composer == null)
            {
                output = CinemachineCameraOutput.Missing(
                    outputId.NormalizeTextOrFallback("camera.composer.missing"),
                    "CameraComposer:missing",
                    required);
                return false;
            }

            CinemachineBrain brain = explicitBrain;
            if (brain == null && composer.UnityCamera != null)
            {
                brain = composer.UnityCamera.GetComponent<CinemachineBrain>();
            }

            Transform followTarget = null;
            Transform lookAtTarget = null;
            CameraTargetResolveResult targetResult = composer.ResolveCameraTargets(
                composer.FollowRequirement,
                composer.LookAtRequirement);

            if (targetResult.IsSucceeded)
            {
                followTarget = targetResult.Targets.FollowTarget;
                lookAtTarget = targetResult.Targets.LookAtTarget;
            }

            string composerName = composer.gameObject != null ? composer.gameObject.name : "CameraComposer";
            string normalizedName = composerName.NormalizeTextOrFallback("CameraComposer");
            string normalizedOutputId = outputId.NormalizeTextOrFallback($"camera.composer.{normalizedName}");
            string displayName = $"CameraComposer:{normalizedName}";

            output = new CinemachineCameraOutput(
                normalizedOutputId,
                displayName,
                composer.CinemachineCamera,
                brain,
                followTarget,
                lookAtTarget,
                composer.Priority,
                required);

            return output.IsMaterialized;
        }

    }
}
