using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicitly declares whether a dynamic Activity projection may resolve to zero Slots.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-012 Activity zero-participant projection behavior; serialized ActivityAsset data remains Stable.")]
    public enum ActivityParticipationZeroParticipantPolicy
    {
        Allowed = 0,
        Rejected = 10
    }
}
