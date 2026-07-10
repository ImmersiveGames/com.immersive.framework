using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Route content binding that supplies a camera rig to a FrameworkCameraDirector.
    /// An optional explicit Cinemachine output can be applied after the legacy Route path.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Route Camera Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public sealed class FrameworkRouteCameraBinding : RouteContentBehaviour
    {
        private const string LogPrefix = "[FRAMEWORK_CAMERA]";
        private const string CinemachineAppliedCode = "route-cinemachine-output-applied";
        private const string CinemachineBlockedCode = "route-cinemachine-output-blocked";
        private const string CinemachineSkippedCode = "route-cinemachine-output-skipped";

        [SerializeField] private GameObject routeCameraRig;
        [SerializeField] private FrameworkCameraAnchorHost routeAnchors;
        [SerializeField] private FrameworkCameraDirector director;
        [SerializeField] private FrameworkActivityCameraBinding startupActivityCameraBinding;
        [SerializeField] private Cinemachine.FrameworkCinemachineCameraOutputSource cinemachineOutputSource;

        public GameObject RouteCameraRig => routeCameraRig;

        public FrameworkCameraAnchorHost RouteAnchors => routeAnchors;

        public FrameworkCameraDirector Director => director;

        public FrameworkActivityCameraBinding StartupActivityCameraBinding => startupActivityCameraBinding;

        public Cinemachine.FrameworkCinemachineCameraOutputSource CinemachineOutputSource => cinemachineOutputSource;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Route Camera binding requires a FrameworkCameraDirector.", this);
                return;
            }

            ActivityAsset startupActivity = context.Route != null && context.Route.HasStartupActivity
                ? context.Route.StartupActivity
                : null;

            bool hasStartupActivity = startupActivity != null;
            director.SetRouteCamera(routeCameraRig, routeAnchors, hasStartupActivity);
            ApplyCinemachineOutput(context.RouteName);

            if (!hasStartupActivity)
            {
                return;
            }

            if (startupActivityCameraBinding != null
                && startupActivityCameraBinding.TryApplyStartupActivityCamera(director, startupActivity, context.RouteName))
            {
                return;
            }

            Debug.LogWarning(
                $"{LogPrefix} Route has Startup Activity but no valid explicit Startup Activity Camera binding was assigned. route='{context.RouteName}' startupActivity='{FormatActivity(startupActivity)}'. Route camera fallback will be applied.",
                this);
            director.Refresh();
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Route Camera binding requires a FrameworkCameraDirector.", this);
                return;
            }

            director.ClearRouteCamera(routeCameraRig);
        }

        private void ApplyCinemachineOutput(string routeName)
        {
            if (cinemachineOutputSource == null)
            {
                return;
            }

            if (!cinemachineOutputSource.TryCreateOutput(out Cinemachine.CinemachineCameraOutput output, out Cinemachine.CinemachineCameraOutputDiagnostic creationDiagnostic))
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

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<none>";
        }
    }
}
