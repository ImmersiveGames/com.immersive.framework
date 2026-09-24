using System;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        /// <summary>Read-only persistent diagnostics for the last Scene-Provided Player operation in this Play Session.</summary>
        public SceneLocalPlayerAdmissionDiagnosticsSnapshot SceneLocalPlayerAdmissionDiagnostics
        {
            get
            {
                SceneLocalPlayerAdmissionRuntimeHostModule module =
                    GetComponent<SceneLocalPlayerAdmissionRuntimeHostModule>();
                return module != null
                    ? module.LastDiagnostics
                    : SceneLocalPlayerAdmissionDiagnosticsSnapshot.Empty(
                        "Scene Local Player admission runtime is not attached.");
            }
        }

        /// <summary>
        /// Composes Scene Local Player admission directly from the Session Player participation
        /// authority, then wraps the canonical Player Activity lifecycle with phase-aware Scene
        /// admission ordering. This path remains independent from PlayerInputManager provisioning.
        ///
        /// A Game Application with Player Session disabled has no Player participation
        /// authority by design. In that case Scene Local Player admission is NotConfigured and
        /// this method returns without attaching any Player runtime module or creating fallback state.
        /// </summary>
        private void ApplySceneLocalPlayerAdmissionRuntime()
        {
            if (!this.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext participationContext))
            {
                _logger?.Debug(
                    "Scene Local Player admission runtime is not configured because the Game Application has no enabled Player Session runtime.");
                return;
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
