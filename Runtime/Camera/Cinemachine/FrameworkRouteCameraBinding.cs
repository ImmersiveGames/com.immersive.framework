using Immersive.Framework.ApiStatus;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Canonical Route binding for one explicit Cinemachine camera output.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Route Camera Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "C8B4C canonical Route Cinemachine binding.")]
    public sealed class FrameworkRouteCameraBinding : RouteContentBehaviour
    {
        private const string LogPrefix = "[FRAMEWORK_CAMERA]";
        private const string CinemachineAppliedCode = "route-cinemachine-output-applied";
        private const string CinemachineBlockedCode = "route-cinemachine-output-blocked";
        private const string CinemachineSkippedCode = "route-cinemachine-output-skipped";
        private const string CinemachineMissingCode = "route-cinemachine-output-missing";

        [SerializeField] private Cinemachine.FrameworkCinemachineCameraOutputSource cinemachineOutputSource;

        public Cinemachine.FrameworkCinemachineCameraOutputSource CinemachineOutputSource => cinemachineOutputSource;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            ApplyCinemachineOutput(context.RouteName);
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            Debug.Log(
                $"{LogPrefix} code='route-cinemachine-output-exit-deferred' route='{context.RouteName}'. Canonical output priority is not cleared in this cut.",
                this);
        }

        private void ApplyCinemachineOutput(string routeName)
        {
            if (cinemachineOutputSource == null)
            {
                Debug.LogError(
                    $"{LogPrefix} code='{CinemachineMissingCode}' route='{routeName}'. Route camera binding requires an explicit FrameworkCinemachineCameraOutputSource; no fallback is available.",
                    this);
                return;
            }

            if (!cinemachineOutputSource.TryCreateOutput(
                    out Cinemachine.CinemachineCameraOutput output,
                    out Cinemachine.CinemachineCameraOutputDiagnostic creationDiagnostic))
            {
                LogCinemachineDiagnostic(
                    creationDiagnostic.IsSkipped ? CinemachineSkippedCode : CinemachineBlockedCode,
                    routeName,
                    creationDiagnostic);
                return;
            }

            Cinemachine.CinemachineCameraOutputDiagnostic diagnostic = Cinemachine.FrameworkCinemachineOutputApplier.Apply(
                output,
                explicitBrainScope: new[] { output.Brain });
            LogCinemachineDiagnostic(
                diagnostic.IsBlocked ? CinemachineBlockedCode : diagnostic.IsSkipped ? CinemachineSkippedCode : CinemachineAppliedCode,
                routeName,
                diagnostic);
        }

        private void LogCinemachineDiagnostic(
            string bindingCode,
            string routeName,
            Cinemachine.CinemachineCameraOutputDiagnostic diagnostic)
        {
            string message = $"{LogPrefix} code='{bindingCode}' route='{routeName}' outputId='{diagnostic.OutputId}' status='{diagnostic.Status}' diagnostic='{diagnostic.Code}' message='{diagnostic.Message}'.";
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
