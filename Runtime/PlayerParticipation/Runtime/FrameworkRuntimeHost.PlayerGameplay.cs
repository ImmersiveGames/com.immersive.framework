using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;

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
