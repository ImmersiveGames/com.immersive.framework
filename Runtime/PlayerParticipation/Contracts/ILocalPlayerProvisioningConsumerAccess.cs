using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Read/write consumer port bound by Framework Core to one live Route or
    /// Activity scope. It forwards canonical provisioning and Actor-selection
    /// requests; it does not own Player, Slot or Activity state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-03 scoped consumer port for existing Local Player provisioning authority.")]
    public interface ILocalPlayerProvisioningConsumerAccess
    {
        LocalPlayerProvisioningConsumerAccessSnapshot Snapshot { get; }

        event Action<PlayerSessionChange> Changed;

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

        PlayerActorSelectionResult RequestSelectActorProfile(
            PlayerActorSelectionRequest request);

        PlayerActorSelectionResult RequestSelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason);

        PlayerActorSelectionResult RequestReplaceActorSelection(
            PlayerActorSelectionRequest request);

        PlayerActorSelectionResult RequestClearActorSelection(
            PlayerActorSelectionRequest request);
    }
}
