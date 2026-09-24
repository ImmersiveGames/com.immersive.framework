using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical effective occupancy evidence for one configured Player Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.2 immutable per-Slot effective gameplay occupancy summary.")]
    public readonly struct PlayerGameplayOccupancySummary
    {
        internal PlayerGameplayOccupancySummary(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            PlayerGameplayOccupancyState state,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            RuntimeContentIdentity runtimeContentIdentity,
            PlayerActorPreparationToken preparationToken,
            PlayerGameplayOccupancyToken token,
            int occupancyRevision,
            string source,
            string reason,
            string message)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            State = state;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            Owner = owner;
            RuntimeContentIdentity = runtimeContentIdentity;
            PreparationToken = preparationToken;
            Token = token;
            OccupancyRevision = occupancyRevision;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayOccupancyState State { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentOwner Owner { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public PlayerGameplayOccupancyToken Token { get; }
        public int OccupancyRevision { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsVacant => State == PlayerGameplayOccupancyState.Vacant;
        public bool IsOccupied => State == PlayerGameplayOccupancyState.Occupied;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            State != PlayerGameplayOccupancyState.None &&
            OccupancyRevision >= 0 &&
            (IsVacant
                ? !ActorProfileId.IsValid &&
                  !ActorId.IsValid &&
                  !Owner.IsValid &&
                  !RuntimeContentIdentity.IsValid &&
                  !PreparationToken.IsValid &&
                  !Token.IsValid
                : ActorProfileId.IsValid &&
                  ActorId.IsValid &&
                  Owner.IsValid &&
                  RuntimeContentIdentity.IsValid &&
                  RuntimeContentIdentity.Owner == Owner &&
                  PreparationToken.IsValid &&
                  Token.IsValid &&
                  Token.SessionContextId == SessionContextId &&
                  Token.PlayerSlotId == PlayerSlotId &&
                  Token.ActorProfileId == ActorProfileId &&
                  Token.ActorId == ActorId &&
                  Token.Owner == Owner &&
                  Token.RuntimeContentIdentity == RuntimeContentIdentity &&
                  Token.PreparationToken == PreparationToken &&
                  Token.OccupancyRevision == OccupancyRevision);

        public string ToDiagnosticString()
        {
            return $"session='{SessionContextId}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"state='{State}' " +
                $"actorProfile='{(ActorProfileId.IsValid ? ActorProfileId.StableText : string.Empty)}' " +
                $"actor='{(ActorId.IsValid ? ActorId.StableText : string.Empty)}' " +
                $"owner='{(Owner.IsValid ? Owner.StableText : string.Empty)}' " +
                $"runtimeContent='{(RuntimeContentIdentity.IsValid ? RuntimeContentIdentity.StableText : string.Empty)}' " +
                $"preparationToken='{PreparationToken.StableText}' " +
                $"occupancyToken='{Token.StableText}' " +
                $"occupancyRevision='{OccupancyRevision}' source='{Source}' reason='{Reason}' " +
                $"message='{Message}'";
        }

        internal static PlayerGameplayOccupancySummary Vacant(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            int occupancyRevision,
            string source,
            string reason,
            string message)
        {
            return new PlayerGameplayOccupancySummary(
                sessionContextId,
                playerSlotId,
                PlayerGameplayOccupancyState.Vacant,
                default,
                default,
                default,
                default,
                default,
                default,
                occupancyRevision,
                source,
                reason,
                message);
        }
    }
}
