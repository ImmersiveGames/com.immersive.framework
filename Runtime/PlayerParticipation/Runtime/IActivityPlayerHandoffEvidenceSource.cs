using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "P3K.7E current immutable evidence source for Activity Player handoff groups.")]
    public interface IActivityPlayerHandoffEvidenceSource
    {
        bool TryCapture(
            out PlayerParticipationSnapshot participation,
            out PlayerActorPreparationSnapshot preparation,
            out PlayerGameplayAdmissionSnapshot admission,
            out string issue);
    }
}
