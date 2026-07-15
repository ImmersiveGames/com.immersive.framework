using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Advanced typed two-phase surface for one reversible Player gameplay chain promotion.
    /// Begin remains rollbackable; Commit crosses the candidate ownership boundary.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7E two-phase per-Slot gameplay chain promotion contract.")]
    public interface IPlayerGameplayChainPromotionRuntime
    {
        PlayerGameplayChainHandoffResult TryBeginPromotion(
            PlayerActorCandidateStageToken expectedCandidate,
            PlayerGameplayAdmissionToken expectedCurrentAdmission,
            string source,
            string reason);

        bool TryValidateCommitPromotion(
            PlayerGameplayChainHandoffToken expectedHandoff,
            out string issue);

        PlayerGameplayChainHandoffResult TryCommitPromotion(
            PlayerGameplayChainHandoffToken expectedHandoff,
            string source,
            string reason);

        PlayerGameplayChainHandoffResult TryRollbackPromotion(
            PlayerGameplayChainHandoffToken expectedHandoff,
            string source,
            string reason);

        PlayerGameplayChainHandoffResult TryRetryCommitCleanup(
            PlayerGameplayChainHandoffToken expectedHandoff,
            string source,
            string reason);
    }
}
