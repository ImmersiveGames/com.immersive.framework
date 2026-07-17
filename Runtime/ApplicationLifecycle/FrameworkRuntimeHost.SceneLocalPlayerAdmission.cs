using System;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        /// <summary>
        /// Composes Scene Local Player admission directly from the Session Player participation
        /// authority, then wraps the canonical Player Activity lifecycle with phase-aware Scene
        /// admission ordering. This path remains independent from PlayerInputManager provisioning.
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
                    out SceneLocalPlayerAdmissionRuntimeHostModule sceneModule,
                    out string issue))
            {
                throw new InvalidOperationException(
                    "Scene Local Player admission runtime composition failed. " + issue);
            }

            PlayerActorPreparationRuntimeHostModule preparation =
                GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (preparation == null ||
                !preparation.TryComposeSceneLocalPlayerAdmissionLifecycle(
                    sceneModule,
                    out issue))
            {
                throw new InvalidOperationException(
                    "Scene Local Player Activity lifecycle composition failed. " + issue);
            }
        }
    }
}
