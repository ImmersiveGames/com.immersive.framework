using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.Pause;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// Immutable read-only development diagnostics for the current application runtime.
    /// It carries no request methods, registries, services or runtime implementation objects.
    /// </summary>
    internal readonly struct FrameworkRuntimeDiagnosticsSnapshot
    {
        internal FrameworkRuntimeDiagnosticsSnapshot(
            string applicationName,
            RouteAsset startupRoute,
            string currentRouteName,
            string currentActivityName,
            int contentAnchorBindingCount,
            bool hasPauseSnapshot,
            PauseState pauseState,
            int pauseGateBlockerCount)
        {
            ApplicationName = applicationName ?? string.Empty;
            StartupRoute = startupRoute;
            CurrentRouteName = currentRouteName ?? string.Empty;
            CurrentActivityName = currentActivityName ?? string.Empty;
            ContentAnchorBindingCount = contentAnchorBindingCount;
            HasPauseSnapshot = hasPauseSnapshot;
            PauseState = pauseState;
            PauseGateBlockerCount = pauseGateBlockerCount;
        }

        internal string ApplicationName { get; }

        internal RouteAsset StartupRoute { get; }

        internal string CurrentRouteName { get; }

        internal string CurrentActivityName { get; }

        internal int ContentAnchorBindingCount { get; }

        internal bool HasPauseSnapshot { get; }

        internal PauseState PauseState { get; }

        internal int PauseGateBlockerCount { get; }
    }
}
