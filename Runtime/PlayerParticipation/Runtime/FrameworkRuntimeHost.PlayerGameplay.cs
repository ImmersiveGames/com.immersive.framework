using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private CameraOutputSessionBinding playerGameplayCameraOutputSession;

        internal void SetPlayerGameplayCameraOutputSession(
            CameraOutputSessionBinding outputSession)
        {
            playerGameplayCameraOutputSession = outputSession;
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
