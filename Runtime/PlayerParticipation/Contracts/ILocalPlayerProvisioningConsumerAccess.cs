using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Read/write consumer port bound by Framework Core to one live Route or
    /// Activity scope. It forwards only the existing public provisioning
    /// operations; it does not own Player, Slot or Activity state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-03 scoped consumer port for existing Local Player provisioning authority.")]
    public interface ILocalPlayerProvisioningConsumerAccess
    {
        LocalPlayerProvisioningConsumerAccessSnapshot Snapshot { get; }

        bool TryGetObservation(
            out LocalPlayerProvisioningConsumerObservationSnapshot observation);

        PlayerParticipationOperationResult OpenJoining(
            string source,
            string reason);

        PlayerParticipationOperationResult CloseJoining(
            string source,
            string reason);

        LocalPlayerJoinResult RequestJoin(LocalPlayerJoinRequest request);

        SessionPlayerLeaveResult RequestLeave(SessionPlayerLeaveRequest request);
    }
}
