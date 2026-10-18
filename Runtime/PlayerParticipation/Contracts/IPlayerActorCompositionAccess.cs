using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Owner-scoped access to the composition of one current Player Actor occurrence.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-050 D05 owner-scoped in-place Actor composition access.")]
    public interface IPlayerActorCompositionAccess
    {
        bool TryOpenContribution(
            PlayerActorCompositionContributionRequest request,
            out PlayerActorCompositionContribution contribution,
            out string issue);
    }
}
