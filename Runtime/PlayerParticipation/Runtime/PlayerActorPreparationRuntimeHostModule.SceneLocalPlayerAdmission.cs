using System;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private SceneLocalPlayerAdmissionCompositeLifecycleParticipant
            sceneLocalPlayerCompositeLifecycleParticipant;

        internal bool TryComposeSceneLocalPlayerAdmissionLifecycle(
            SceneLocalPlayerAdmissionRuntimeHostModule sceneModule,
            out string issue)
        {
            issue = string.Empty;
            if (!IsReady)
            {
                issue = diagnostic;
                return false;
            }

            if (sceneModule == null || !sceneModule.IsReady)
            {
                issue = "Scene Local Player admission lifecycle composition requires a ready Scene admission module.";
                return false;
            }

            if (sceneLocalPlayerCompositeLifecycleParticipant == null)
            {
                sceneLocalPlayerCompositeLifecycleParticipant =
                    new SceneLocalPlayerAdmissionCompositeLifecycleParticipant(
                        activityLifecycleParticipant,
                        sceneModule);
            }

            runtimeHost.SetActivityContentExecutionParticipantSource(
                sceneLocalPlayerCompositeLifecycleParticipant);
            return true;
        }
    }

    internal static class LocalPlayerProvisioningSceneAdmissionLifecycleExtensions
    {
        internal static void RegisterSceneLocalPlayerAdmissionLifecycleSourceIfAvailable(
            this LocalPlayerProvisioningRuntimeHostModule provisioning)
        {
            if (provisioning == null)
            {
                throw new ArgumentNullException(nameof(provisioning));
            }

            PlayerActorPreparationRuntimeHostModule preparation =
                provisioning.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            SceneLocalPlayerAdmissionRuntimeHostModule sceneAdmission =
                provisioning.GetComponent<SceneLocalPlayerAdmissionRuntimeHostModule>();
            if (sceneAdmission == null || !sceneAdmission.IsReady)
            {
                return;
            }

            string issue = string.Empty;
            if (preparation == null ||
                !preparation.TryComposeSceneLocalPlayerAdmissionLifecycle(
                    sceneAdmission,
                    out issue))
            {
                if (string.IsNullOrWhiteSpace(issue))
                {
                    issue = "Player Actor preparation runtime module is unavailable.";
                }

                throw new InvalidOperationException(
                    "Local Player provisioning could not restore the composed Scene Local Player Activity lifecycle source. " +
                    issue);
            }
        }
    }
}
