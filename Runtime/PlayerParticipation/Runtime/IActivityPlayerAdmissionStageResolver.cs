using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B frozen/preparatory Activity Player admission stage resolution contract; IF-ADR-003.")]
    public interface IActivityPlayerAdmissionStageResolver
    {
        ActivityPlayerAdmissionStageResolution Resolve(
            ActivityAsset activity,
            ActivityPlayerAdmissionStageScope stagedScope,
            string source,
            string reason);

        bool TryRollback(
            ActivityPlayerAdmissionStageResolution resolution,
            string source,
            string reason,
            out string issue);
    }
}
