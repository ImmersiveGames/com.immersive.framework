using Immersive.Audio.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Route content binding that supplies BGM to a FrameworkBgmDirector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Audio/Route BGM Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F47C optional framework-owned BGM adapter.")]
    public sealed class FrameworkRouteBgmBinding : RouteContentBehaviour
    {
        private const string LogPrefix = "[FRAMEWORK_BGM]";

        [SerializeField] private AudioBgmCueAsset routeBgm;
        [SerializeField] private FrameworkBgmDirector director;
        [SerializeField] private FrameworkActivityBgmBinding startupActivityBgmBinding;

        public AudioBgmCueAsset RouteBgm => routeBgm;

        public FrameworkBgmDirector Director => director;

        public FrameworkActivityBgmBinding StartupActivityBgmBinding => startupActivityBgmBinding;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Route BGM binding requires a FrameworkBgmDirector.", this);
                return;
            }

            ActivityAsset startupActivity = context.Route != null && context.Route.HasStartupActivity
                ? context.Route.StartupActivity
                : null;

            bool hasStartupActivity = startupActivity != null;
            director.SetRouteBgm(routeBgm, hasStartupActivity);

            if (!hasStartupActivity)
            {
                return;
            }

            if (startupActivityBgmBinding != null
                && startupActivityBgmBinding.TryApplyStartupActivityBgm(director, startupActivity, context.RouteName))
            {
                return;
            }

            Debug.LogWarning(
                $"{LogPrefix} Route has Startup Activity but no valid explicit Startup Activity BGM binding was assigned. route='{context.RouteName}' startupActivity='{FormatActivity(startupActivity)}'. Route BGM fallback will be applied.",
                this);
            director.Refresh();
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Route BGM binding requires a FrameworkBgmDirector.", this);
                return;
            }

            director.ClearRouteBgm(routeBgm);
        }

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<none>";
        }
    }
}
