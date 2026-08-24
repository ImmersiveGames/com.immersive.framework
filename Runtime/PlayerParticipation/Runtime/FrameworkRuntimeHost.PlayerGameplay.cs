using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.GameFlow.Diagnostics;
using Immersive.Framework.RouteLifecycle;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private CameraOutputSessionBinding playerGameplayCameraOutputSession;
        private IActivityPlayerLifecycleAdmissionRuntime
            playerActivityLifecycleAdmissionRuntime;

        internal void SetPlayerGameplayCameraOutputSession(
            CameraOutputSessionBinding outputSession)
        {
            playerGameplayCameraOutputSession = outputSession;
        }


        internal void SetActivityPlayerLifecycleAdmissionRuntime(
            IActivityPlayerLifecycleAdmissionRuntime runtime)
        {
            playerActivityLifecycleAdmissionRuntime = runtime;
            ApplyPlayerActivityLifecycleAdmissionRuntime();
        }

        internal bool SetRoutePlayerSpatialEntryParticipant(
            IRoutePlayerSpatialEntryLifecycleParticipant participant,
            out string issue)
        {
            issue = string.Empty;
            if (_gameFlowRuntime == null)
            {
                issue = "FrameworkRuntimeHost has no canonical Game Flow lifecycle authority.";
                return false;
            }

            return _gameFlowRuntime.SetRoutePlayerSpatialEntryParticipant(
                participant,
                out issue);
        }

        internal bool TryInstallGameFlowDiagnosticFaultPlan(
            IGameFlowDiagnosticFaultPlan plan,
            out string issue)
        {
            issue = string.Empty;
            if (plan == null)
            {
                issue = "Game Flow diagnostic fault plan is required.";
                return false;
            }

            if (_gameFlowRuntime == null)
            {
                issue = "FrameworkRuntimeHost has no canonical Game Flow lifecycle authority.";
                return false;
            }

            if (_gameFlowRuntime.HasLifecycleRequestInFlight)
            {
                issue = "Game Flow diagnostic faults cannot be installed during an active lifecycle request or transaction.";
                return false;
            }

            _gameFlowRuntime.SetDiagnosticFaultPlan(plan);
            return true;
        }

        internal void ClearGameFlowDiagnosticFaultPlan(IGameFlowDiagnosticFaultPlan plan)
        {
            if (_gameFlowRuntime == null)
                return;

            _gameFlowRuntime.SetDiagnosticFaultPlan(NoOpGameFlowDiagnosticFaultPlan.Instance);
        }

        private void ApplyPlayerActivityLifecycleAdmissionRuntime()
        {
            _gameFlowRuntime?.SetActivityPlayerLifecycleAdmissionRuntime(
                playerActivityLifecycleAdmissionRuntime);
        }

        internal bool TryGetPlayerGameplayCameraOutputSession(
            out CameraOutputSessionBinding outputSession,
            out string issue)
        {
            outputSession = playerGameplayCameraOutputSession;
            if (outputSession == null)
            {
                issue =
                    "FrameworkRuntimeHost has no current CameraOutputSessionBinding for Player gameplay camera publication.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
