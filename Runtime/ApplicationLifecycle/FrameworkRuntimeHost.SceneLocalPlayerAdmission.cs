using System;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        /// <summary>
        /// Composes Scene Local Player admission directly from the Session Player participation
        /// authority. This path is independent from PlayerInputManager provisioning.
        /// </summary>
        private void ApplySceneLocalPlayerAdmissionRuntime()
        {
            if (!this.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext participationContext))
            {
                throw new InvalidOperationException(
                    "Scene Local Player admission requires the initialized Session Player participation context.");
            }

            if (!SceneLocalPlayerAdmissionRuntimeHostModule.TryAttach(
                    this,
                    participationContext,
                    out _,
                    out string issue))
            {
                throw new InvalidOperationException(
                    "Scene Local Player admission runtime composition failed. " + issue);
            }
        }
    }
}
