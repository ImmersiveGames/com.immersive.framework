using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Canonical Activity binding for one explicit Cinemachine camera output.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Activity Camera Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "C8B4C canonical Activity Cinemachine binding.")]
    public sealed class FrameworkActivityCameraBinding : ActivityContentBehaviour
    {
        private const string LogPrefix = "[FRAMEWORK_CAMERA]";
        private const string CinemachineAppliedCode = "activity-cinemachine-output-applied";
        private const string CinemachineBlockedCode = "activity-cinemachine-output-blocked";
        private const string CinemachineSkippedCode = "activity-cinemachine-output-skipped";
        private const string CinemachineMissingCode = "activity-cinemachine-output-missing";
        private const string CinemachineUseRouteCode = "activity-cinemachine-output-use-route";

        [SerializeField] private FrameworkCameraActivityPolicy policy = FrameworkCameraActivityPolicy.UseOwn;
        [SerializeField] private Cinemachine.FrameworkCinemachineCameraOutputSource cinemachineOutputSource;

        public FrameworkCameraActivityPolicy Policy => policy;

        public Cinemachine.FrameworkCinemachineCameraOutputSource CinemachineOutputSource => cinemachineOutputSource;

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            ApplyCinemachineOutput(context.ActivityName);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            Debug.Log(
                $"{LogPrefix} code='activity-cinemachine-output-exit-deferred' activity='{context.ActivityName}'. Canonical output priority is not cleared in this cut.",
                this);
        }

        private void ApplyCinemachineOutput(string activityName)
        {
            if (policy == FrameworkCameraActivityPolicy.UseRoute)
            {
                Debug.Log(
                    $"{LogPrefix} code='{CinemachineUseRouteCode}' activity='{activityName}' outputId='{(cinemachineOutputSource != null ? cinemachineOutputSource.OutputId : "<none>")}' policy='{policy}'. Activity override was not applied.",
                    this);
                return;
            }

            if (cinemachineOutputSource == null)
            {
                Debug.LogError(
                    $"{LogPrefix} code='{CinemachineMissingCode}' activity='{activityName}'. Activity camera binding requires an explicit FrameworkCinemachineCameraOutputSource; no fallback is available.",
                    this);
                return;
            }

            if (!cinemachineOutputSource.TryCreateOutput(
                    out Cinemachine.CinemachineCameraOutput output,
                    out Cinemachine.CinemachineCameraOutputDiagnostic creationDiagnostic))
            {
                LogCinemachineDiagnostic(
                    creationDiagnostic.IsSkipped ? CinemachineSkippedCode : CinemachineBlockedCode,
                    activityName,
                    creationDiagnostic);
                return;
            }

            Cinemachine.CinemachineCameraOutputDiagnostic diagnostic = Cinemachine.FrameworkCinemachineOutputApplier.Apply(
                output,
                explicitBrainScope: new[] { output.Brain });
            LogCinemachineDiagnostic(
                diagnostic.IsBlocked ? CinemachineBlockedCode : diagnostic.IsSkipped ? CinemachineSkippedCode : CinemachineAppliedCode,
                activityName,
                diagnostic);
        }

        private void LogCinemachineDiagnostic(
            string bindingCode,
            string activityName,
            Cinemachine.CinemachineCameraOutputDiagnostic diagnostic)
        {
            string message = $"{LogPrefix} code='{bindingCode}' activity='{activityName}' outputId='{diagnostic.OutputId}' status='{diagnostic.Status}' diagnostic='{diagnostic.Code}' message='{diagnostic.Message}'.";
            if (diagnostic.IsBlocked)
            {
                Debug.LogError(message, this);
            }
            else if (diagnostic.IsSkipped)
            {
                Debug.LogWarning(message, this);
            }
            else
            {
                Debug.Log(message, this);
            }
        }
    }
}
