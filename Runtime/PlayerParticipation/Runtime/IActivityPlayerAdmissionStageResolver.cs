using Immersive.Framework.Authoring;

namespace Immersive.Framework.PlayerParticipation
{
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
