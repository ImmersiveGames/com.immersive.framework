using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.GameFlow.Diagnostics;

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

            if (_gameFlowRuntime == null ||
                playerActivityLifecycleAdmissionRuntime is not ActivityPlayerLifecycleAdmissionRuntimeContext lifecycle)
            {
                issue = "FrameworkRuntimeHost has no canonical Game Flow lifecycle authority.";
                return false;
            }

            if (_gameFlowRuntime.HasLifecycleRequestInFlight || lifecycle.HasActiveTransaction)
            {
                issue = "Game Flow diagnostic faults cannot be installed during an active lifecycle request or transaction.";
                return false;
            }

            _gameFlowRuntime.SetDiagnosticFaultPlan(plan);
            lifecycle.SetDiagnosticFaultPlan(plan);
            return true;
        }

        internal void ClearGameFlowDiagnosticFaultPlan(IGameFlowDiagnosticFaultPlan plan)
        {
            if (_gameFlowRuntime == null ||
                playerActivityLifecycleAdmissionRuntime is not ActivityPlayerLifecycleAdmissionRuntimeContext lifecycle)
                return;

            _gameFlowRuntime.SetDiagnosticFaultPlan(NoOpGameFlowDiagnosticFaultPlan.Instance);
            lifecycle.SetDiagnosticFaultPlan(NoOpGameFlowDiagnosticFaultPlan.Instance);
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
