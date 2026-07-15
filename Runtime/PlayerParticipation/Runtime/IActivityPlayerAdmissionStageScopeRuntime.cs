using Immersive.Framework.Authoring;

namespace Immersive.Framework.PlayerParticipation
{
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
