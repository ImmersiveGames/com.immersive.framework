using Unity.Cinemachine;

namespace Immersive.Framework.Camera.Cinemachine
{
    /// <summary>
    /// Applies an explicit Cinemachine output. It does not create cameras, search the scene,
    /// select lifecycle state, enable GameObjects or toggle Unity Camera.enabled.
    /// </summary>
    public static class FrameworkCinemachineOutputApplier
    {
        public static CinemachineCameraOutputDiagnostic Validate(
            CinemachineCameraOutput output,
            bool requireFollowTarget = false,
            bool requireLookAtTarget = false,
            CinemachineBrain[] explicitBrainScope = null)
        {
            CinemachineCameraOutputDiagnostic brainScopeDiagnostic = ValidateExplicitBrainScope(output, explicitBrainScope);
            if (brainScopeDiagnostic.IsBlocked)
            {
                return brainScopeDiagnostic;
            }

            if (!output.HasAnyOutputEvidence)
            {
                return output.Required
                    ? CinemachineCameraOutputDiagnostic.Blocked(
                        output,
                        CinemachineCameraOutputDiagnostic.CameraOutputMissing,
                        "Required Cinemachine camera output is missing.")
                    : CinemachineCameraOutputDiagnostic.Skipped(
                        output,
                        CinemachineCameraOutputDiagnostic.OptionalOutputSkipped,
                        "Optional Cinemachine camera output is missing and was skipped.");
            }

            if (!output.HasCamera)
            {
                return output.Required
                    ? CinemachineCameraOutputDiagnostic.Blocked(
                        output,
                        CinemachineCameraOutputDiagnostic.CinemachineCameraMissing,
                        "Required CinemachineCamera reference is missing.")
                    : CinemachineCameraOutputDiagnostic.Skipped(
                        output,
                        CinemachineCameraOutputDiagnostic.OptionalOutputSkipped,
                        "Optional CinemachineCamera reference is missing and was skipped.");
            }

            if (!output.HasBrain)
            {
                return output.Required
                    ? CinemachineCameraOutputDiagnostic.Blocked(
                        output,
                        CinemachineCameraOutputDiagnostic.CinemachineBrainMissing,
                        "Required CinemachineBrain reference is missing.")
                    : CinemachineCameraOutputDiagnostic.Skipped(
                        output,
                        CinemachineCameraOutputDiagnostic.OptionalOutputSkipped,
                        "Optional CinemachineBrain reference is missing and was skipped.");
            }

            if (requireFollowTarget && !output.HasFollowTarget)
            {
                return CinemachineCameraOutputDiagnostic.Blocked(
                    output,
                    CinemachineCameraOutputDiagnostic.FollowTargetMissing,
                    "Required Cinemachine follow target is missing.");
            }

            if (requireLookAtTarget && !output.HasLookAtTarget)
            {
                return CinemachineCameraOutputDiagnostic.Blocked(
                    output,
                    CinemachineCameraOutputDiagnostic.LookAtTargetMissing,
                    "Required Cinemachine look-at target is missing.");
            }

            if (!output.HasFollowTarget || !output.HasLookAtTarget)
            {
                return CinemachineCameraOutputDiagnostic.SucceededWithWarnings(
                    output,
                    CinemachineCameraOutputDiagnostic.OutputApplied,
                    "Cinemachine camera output is valid with one or more optional targets missing.");
            }

            return CinemachineCameraOutputDiagnostic.Succeeded(output, "Cinemachine camera output is valid.");
        }

        public static CinemachineCameraOutputDiagnostic Apply(
            CinemachineCameraOutput output,
            bool requireFollowTarget = false,
            bool requireLookAtTarget = false,
            CinemachineBrain[] explicitBrainScope = null)
        {
            CinemachineCameraOutputDiagnostic validation = Validate(
                output,
                requireFollowTarget,
                requireLookAtTarget,
                explicitBrainScope);

            if (!validation.IsSucceeded)
            {
                return validation;
            }

            output.CinemachineCamera.Priority = output.Priority;
            output.CinemachineCamera.Target.TrackingTarget = output.FollowTarget;
            output.CinemachineCamera.Target.LookAtTarget = output.LookAtTarget;

            return CinemachineCameraOutputDiagnostic.Succeeded(output, "Cinemachine camera output applied.");
        }

        private static CinemachineCameraOutputDiagnostic ValidateExplicitBrainScope(
            CinemachineCameraOutput output,
            CinemachineBrain[] explicitBrainScope)
        {
            if (explicitBrainScope == null || explicitBrainScope.Length == 0)
            {
                return CinemachineCameraOutputDiagnostic.Succeeded(output, "No explicit CinemachineBrain scope was provided.");
            }

            int count = 0;
            CinemachineBrain scopedBrain = null;
            for (int i = 0; i < explicitBrainScope.Length; i++)
            {
                CinemachineBrain candidate = explicitBrainScope[i];
                if (candidate == null)
                {
                    continue;
                }

                count++;
                scopedBrain = candidate;
            }

            if (count > 1)
            {
                return CinemachineCameraOutputDiagnostic.Blocked(
                    output,
                    CinemachineCameraOutputDiagnostic.MultipleCinemachineBrains,
                    "Multiple CinemachineBrain references were provided in the explicit camera scope.");
            }

            if (count == 1 && output.Brain != null && output.Brain != scopedBrain)
            {
                return CinemachineCameraOutputDiagnostic.Blocked(
                    output,
                    CinemachineCameraOutputDiagnostic.BrainScopeMismatch,
                    "Cinemachine camera output brain does not match the explicit camera scope.");
            }

            return CinemachineCameraOutputDiagnostic.Succeeded(output, "Explicit CinemachineBrain scope is valid.");
        }
    }
}
