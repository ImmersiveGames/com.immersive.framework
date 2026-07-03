using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RouteLifecycle;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// Development-only receiver for validating that Route Content local callbacks are actually dispatched.
    /// Place this under a RouteContentBinding root in QA scenes. It is not product API.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/QA/Route Content Lifecycle Smoke Probe")]
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "QA receiver used by F3F to smoke-test Route Content enter/exit callbacks.")]
    public sealed class RouteContentLifecycleSmokeProbe : RouteContentBehaviour
    {
        private static int _totalEnteredCount;
        private static int _totalExitedCount;

        private FrameworkLogger _logger;

        [SerializeField]
        private string probeName = "Route Content Smoke Probe";

        public string ProbeName => probeName.NormalizeTextOrFallback("Route Content Smoke Probe");

        public int EnteredCount { get; private set; }

        public int ExitedCount { get; private set; }

        public static int TotalEnteredCount => _totalEnteredCount;

        public static int TotalExitedCount => _totalExitedCount;

        public static void ResetGlobalCounters()
        {
            _totalEnteredCount = 0;
            _totalExitedCount = 0;
        }

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            EnteredCount++;
            _totalEnteredCount++;
            LogProbeEvent("Entered", context, EnteredCount);
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            ExitedCount++;
            _totalExitedCount++;
            LogProbeEvent("Exited", context, ExitedCount);
        }

        private void LogProbeEvent(string phase, RouteContentLifecycleContext context, int localCount)
        {
            EnsureLogger();
            string routeName = context.Route != null ? context.Route.RouteName : "<none>";
            string sceneName = context.Binding != null ? context.Binding.SceneName : "<no-scene>";
            string objectName = context.Binding != null ? context.Binding.ObjectName : "<missing>";

            _logger.Debug(
                "Route Content Smoke Probe callback.",
                LogFields.Of(
                    LogFields.Field("phase", FormatValue(phase)),
                    LogFields.Field("probe", FormatValue(ProbeName)),
                    LogFields.Field("route", FormatValue(routeName)),
                    LogFields.Field("scene", FormatValue(sceneName)),
                    LogFields.Field("object", FormatValue(objectName)),
                    LogFields.Field("localCount", localCount)));
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<RouteContentLifecycleSmokeProbe>();
            }
        }

        private static string FormatValue(string value)
        {
            return value.NormalizeTextOrFallback("<none>");
        }
    }
}
#endif
