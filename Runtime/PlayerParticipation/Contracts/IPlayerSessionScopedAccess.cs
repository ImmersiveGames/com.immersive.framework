using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Provider-neutral read/write access to one live Route or Activity Player
    /// Session scope. It never creates a Local Player Host.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ACCESS-2 provider-neutral scoped Player Session access.")]
    public interface IPlayerSessionScopedAccess
    {
        PlayerSessionScopedAccessSnapshot Snapshot { get; }

        event Action<PlayerSessionChange> Changed;

        bool TryGetObservation(
            out PlayerSessionScopedObservationSnapshot observation);

        PlayerParticipationOperationResult OpenJoining(string source, string reason);

        PlayerParticipationOperationResult CloseJoining(string source, string reason);

        SessionPlayerLeaveResult RequestLeave(SessionPlayerLeaveRequest request);

        PlayerPreparedActorReplacementResult RequestReplacePreparedActor(
            PlayerPreparedActorReplacementRequest request);

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

    /// <summary>
    /// Manager-Provisioned capability that creates a technical Local Player
    /// Host through the existing PlayerInputManager bridge.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ACCESS-2 explicit Manager-Provisioned local Player join capability.")]
    public interface ILocalPlayerJoinAccess
    {
        LocalPlayerJoinResult RequestJoin(LocalPlayerJoinRequest request);
    }
}
