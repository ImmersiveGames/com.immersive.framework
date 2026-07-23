using Immersive.Audio.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RouteLifecycle;
using Immersive.Logging.Records;
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
        [SerializeField] private AudioBgmCueAsset routeBgm;
        [SerializeField] private FrameworkBgmDirector director;
        [SerializeField] private FrameworkActivityBgmBinding startupActivityBgmBinding;
        private FrameworkLogger logger;

        public AudioBgmCueAsset RouteBgm => routeBgm;

        public FrameworkBgmDirector Director => director;

        public FrameworkActivityBgmBinding StartupActivityBgmBinding => startupActivityBgmBinding;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Error("Route BGM binding requires a FrameworkBgmDirector.");
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

            Warning(
                "Route has Startup Activity but no valid explicit Startup Activity BGM binding was assigned. Route BGM fallback will be applied.",
                LogFields.Of(
                    LogFields.Field("route", context.RouteName),
                    LogFields.Field("startupActivity", FormatActivity(startupActivity))));
            director.Refresh();
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Error("Route BGM binding requires a FrameworkBgmDirector.");
                return;
            }

            director.ClearRouteBgm(routeBgm);
        }

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<none>";
        }

        private void Warning(string message, params LogField[] fields)
        {
            EnsureLogger();
            logger.Warning(message, fields);
        }

        private void Error(string message, params LogField[] fields)
        {
            EnsureLogger();
            logger.Error(message, fields);
        }

        private void EnsureLogger()
        {
            logger ??= FrameworkLogger.Create<FrameworkRouteBgmBinding>();
        }
    }
}
