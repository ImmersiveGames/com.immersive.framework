using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7D exact internal lease for one candidate promotion attempt.")]
    internal sealed class PlayerActorCandidatePromotionHandle
    {
        private readonly PlayerActorCandidateStageRuntimeContext owner;
        private bool completed;

        internal PlayerActorCandidatePromotionHandle(
            PlayerActorCandidateStageRuntimeContext owner,
            PlayerActorCandidateStageToken token,
            PlayerActorMaterializationHandle materializationHandle,
            LocalPlayerHostAuthoring host,
            PlayerActorCandidateStageSnapshot snapshot)
        {
            this.owner = owner;
            Token = token;
            MaterializationHandle = materializationHandle;
            Host = host;
            Snapshot = snapshot;
        }

        internal PlayerActorCandidateStageToken Token { get; }
        internal PlayerActorMaterializationHandle MaterializationHandle { get; }
        internal LocalPlayerHostAuthoring Host { get; }
        internal PlayerActorCandidateStageSnapshot Snapshot { get; private set; }
        internal bool IsCompleted => completed;
        internal bool IsValid =>
            owner != null &&
            Token.IsValid &&
            MaterializationHandle != null &&
            Host != null &&
            Snapshot != null &&
            Snapshot.Token == Token;

        internal bool TryActivate(string source, string reason, out string issue)
        {
            if (MaterializationHandle == null)
            {
                issue = "Candidate materialization handle is missing.";
                return false;
            }

            return MaterializationHandle.TryActivate(source, reason, out issue);
        }

        internal bool TryDeactivate(string source, string reason, out string issue)
        {
            if (MaterializationHandle == null)
            {
                issue = "Candidate materialization handle is missing.";
                return false;
            }

            return MaterializationHandle.TryDeactivate(source, reason, out issue);
        }

        internal PlayerActorMaterializationSnapshot CreateMaterializationSnapshot() =>
            MaterializationHandle != null
                ? MaterializationHandle.CreateSnapshot()
                : default;

        internal bool TryCancel(string source, string reason, out string issue)
        {
            if (owner == null)
            {
                issue = "Candidate promotion owner is missing.";
                return false;
            }

            return owner.TryCancelPromotion(this, source, reason, out issue);
        }

        internal bool TryComplete(string source, string reason, out string issue)
        {
            if (owner == null)
            {
                issue = "Candidate promotion owner is missing.";
                return false;
            }

            return owner.TryCompletePromotion(this, source, reason, out issue);
        }

        internal void UpdateSnapshot(PlayerActorCandidateStageSnapshot snapshot) =>
            Snapshot = snapshot;

        internal void MarkCompleted() => completed = true;
    }
}
