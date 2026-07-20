using Immersive.Framework.Diagnostics;
using Immersive.Framework.Pause;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost : IFrameworkRuntimeDiagnosticsPort
    {
        FrameworkRuntimeDiagnosticsSnapshot
            IFrameworkRuntimeDiagnosticsPort.CreateFrameworkRuntimeDiagnosticsSnapshot()
        {
            FrameworkRuntimeState state = State;
            var application = state.GameApplication;
            bool hasPauseSnapshot =
                TryGetPauseSnapshot(out PauseSnapshot pauseSnapshot);

            return new FrameworkRuntimeDiagnosticsSnapshot(
                application != null
                    ? application.ApplicationName
                    : string.Empty,
                application != null
                    ? application.StartupRoute
                    : null,
                state.CurrentRouteName,
                state.CurrentActivityName,
                ContentAnchorBindingCount,
                hasPauseSnapshot,
                hasPauseSnapshot
                    ? pauseSnapshot.State
                    : PauseState.Unknown,
                PauseGateSnapshot.BlockerCount);
        }
    }
}
