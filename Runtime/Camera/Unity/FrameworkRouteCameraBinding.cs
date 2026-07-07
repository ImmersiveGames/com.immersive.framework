using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Route content binding that supplies a camera rig to a FrameworkCameraDirector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Route Camera Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public sealed class FrameworkRouteCameraBinding : RouteContentBehaviour
    {
        private const string LogPrefix = "[FRAMEWORK_CAMERA]";

        [SerializeField] private GameObject routeCameraRig;
        [SerializeField] private FrameworkCameraAnchorHost routeAnchors;
        [SerializeField] private FrameworkCameraDirector director;
        [SerializeField] private FrameworkActivityCameraBinding startupActivityCameraBinding;

        public GameObject RouteCameraRig => routeCameraRig;

        public FrameworkCameraAnchorHost RouteAnchors => routeAnchors;

        public FrameworkCameraDirector Director => director;

        public FrameworkActivityCameraBinding StartupActivityCameraBinding => startupActivityCameraBinding;

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

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<none>";
        }
    }
}
