using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal,
        "P3K.7E explicit host-scoped immutable evidence adapter.")]
    internal sealed class ExplicitActivityPlayerHandoffEvidenceSource :
        IActivityPlayerHandoffEvidenceSource
    {
        private readonly PlayerParticipationRuntimeContext participationContext;
        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly PlayerGameplayAdmissionRuntimeContext admissionContext;

        internal ExplicitActivityPlayerHandoffEvidenceSource(
            PlayerParticipationRuntimeContext participationContext,
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerGameplayAdmissionRuntimeContext admissionContext)
        {
            this.participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
            this.preparationModule = preparationModule ??
                throw new ArgumentNullException(nameof(preparationModule));
            this.admissionContext = admissionContext ??
                throw new ArgumentNullException(nameof(admissionContext));
        }

        public bool TryCapture(
            out PlayerParticipationSnapshot participation,
            out PlayerActorPreparationSnapshot preparation,
            out PlayerGameplayAdmissionSnapshot admission,
            out string issue)
        {
            participation = participationContext.CreateSnapshot();
            preparation = null;
            admission = admissionContext.CreateSnapshot();
            issue = string.Empty;
            if (!preparationModule.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot host) ||
                host == null || !host.IsInitialized)
            {
                issue = "Current P3J preparation snapshot is unavailable.";
                return false;
            }
            preparation = host.Preparation;
            if (participation == null || !participation.IsInitialized ||
                preparation == null || !preparation.IsInitialized ||
                admission == null || !admission.IsInitialized ||
                !string.Equals(participation.ContextId,
                    preparation.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(participation.ContextId,
                    admission.SessionContextId, StringComparison.Ordinal))
            {
                issue = "Activity Player handoff evidence belongs to different or uninitialized Session identities.";
                return false;
            }
            return true;
        }
    }
}
