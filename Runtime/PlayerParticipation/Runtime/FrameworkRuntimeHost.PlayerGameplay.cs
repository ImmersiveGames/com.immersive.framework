using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.GameFlow.Diagnostics;
using Immersive.Framework.RouteLifecycle;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private CameraOutputAuthoring _playerGameplayCameraOutputSession;
        private IActivityPlayerLifecycleAdmissionRuntime
            _playerActivityLifecycleAdmissionRuntime;

        internal void SetPlayerGameplayCameraOutputSession(
            CameraOutputAuthoring outputSession)
        {
            _playerGameplayCameraOutputSession = outputSession;
        }


        internal void SetActivityPlayerLifecycleAdmissionRuntime(
            IActivityPlayerLifecycleAdmissionRuntime runtime)
        {
            _playerActivityLifecycleAdmissionRuntime = runtime;
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
                _playerActivityLifecycleAdmissionRuntime);
        }

        internal bool TryGetPlayerGameplayCameraOutputSession(
            out CameraOutputAuthoring outputSession,
            out string issue)
        {
            outputSession = _playerGameplayCameraOutputSession;
            if (outputSession == null)
            {
                issue =
                    "FrameworkRuntimeHost has no current CameraOutputAuthoring for Player gameplay camera publication.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
