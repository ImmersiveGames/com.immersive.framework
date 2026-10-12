using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B frozen/preparatory Activity Player admission stage scope runtime contract; IF-ADR-003.")]
    public interface IActivityPlayerAdmissionStageScopeRuntime
    {
        bool TryCreate(
            ActivityAsset activity,
            int stageSequence,
            string source,
            string reason,
            out ActivityPlayerAdmissionStageScope scope,
            out string issue);

        bool TryRelease(
            ActivityPlayerAdmissionStageScope scope,
            string source,
            string reason,
            out string issue);
    }
}
