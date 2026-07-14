using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical gameplay input binding evidence for one configured Player Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.3 immutable per-Slot gameplay input binding summary.")]
    public readonly struct PlayerGameplayInputBindingSummary
    {
        internal PlayerGameplayInputBindingSummary(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingState state,
            PlayerGameplayInputAvailability availability,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            RuntimeContentIdentity runtimeContentIdentity,
            PlayerActorPreparationToken preparationToken,
            PlayerGameplayOccupancyToken occupancyToken,
            PlayerGameplayInputBindingToken token,
            string actionMapName,
            string previousActionMapName,
            string playerInputName,
            int bindingRevision,
            string source,
            string reason,
            string message)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            State = state;
            Availability = availability;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            Owner = owner;
            RuntimeContentIdentity = runtimeContentIdentity;
            PreparationToken = preparationToken;
            OccupancyToken = occupancyToken;
            Token = token;
            ActionMapName = actionMapName.NormalizeText();
            PreviousActionMapName = previousActionMapName.NormalizeText();
            PlayerInputName = playerInputName.NormalizeText();
            BindingRevision = bindingRevision;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayInputBindingState State { get; }
        public PlayerGameplayInputAvailability Availability { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentOwner Owner { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public PlayerGameplayOccupancyToken OccupancyToken { get; }
        public PlayerGameplayInputBindingToken Token { get; }
        public string ActionMapName { get; }
        public string PreviousActionMapName { get; }
        public string PlayerInputName { get; }
        public int BindingRevision { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsUnbound => State == PlayerGameplayInputBindingState.Unbound;
        public bool IsBound => State == PlayerGameplayInputBindingState.Bound;
        public bool IsReleaseFailed => State == PlayerGameplayInputBindingState.ReleaseFailed;
        public bool IsAllowed => IsBound && Availability == PlayerGameplayInputAvailability.Allowed;
        public bool IsBlockedByGate => IsBound && Availability == PlayerGameplayInputAvailability.BlockedByGate;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            State != PlayerGameplayInputBindingState.None &&
            BindingRevision >= 0 &&
            (IsUnbound
                ? Availability == PlayerGameplayInputAvailability.Unknown &&
                  !ActorProfileId.IsValid && !ActorId.IsValid && !Owner.IsValid &&
                  !RuntimeContentIdentity.IsValid && !PreparationToken.IsValid &&
                  !OccupancyToken.IsValid && !Token.IsValid &&
                  string.IsNullOrEmpty(ActionMapName) && string.IsNullOrEmpty(PlayerInputName)
                : ActorProfileId.IsValid && ActorId.IsValid && Owner.IsValid &&
                  RuntimeContentIdentity.IsValid && RuntimeContentIdentity.Owner == Owner &&
                  PreparationToken.IsValid && OccupancyToken.IsValid && Token.IsValid &&
                  !string.IsNullOrEmpty(ActionMapName) && !string.IsNullOrEmpty(PlayerInputName) &&
                  Token.SessionContextId == SessionContextId &&
                  Token.PlayerSlotId == PlayerSlotId &&
                  Token.ActorProfileId == ActorProfileId &&
                  Token.ActorId == ActorId &&
                  Token.Owner == Owner &&
                  Token.RuntimeContentIdentity == RuntimeContentIdentity &&
                  Token.PreparationToken == PreparationToken &&
                  Token.OccupancyToken == OccupancyToken &&
                  Token.BindingRevision == BindingRevision &&
                  Availability != PlayerGameplayInputAvailability.Unknown);

        public string ToDiagnosticString()
        {
            return $"session='{SessionContextId}' slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"state='{State}' availability='{Availability}' " +
                $"actorProfile='{(ActorProfileId.IsValid ? ActorProfileId.StableText : string.Empty)}' " +
                $"actor='{(ActorId.IsValid ? ActorId.StableText : string.Empty)}' " +
                $"owner='{(Owner.IsValid ? Owner.StableText : string.Empty)}' " +
                $"runtimeContent='{(RuntimeContentIdentity.IsValid ? RuntimeContentIdentity.StableText : string.Empty)}' " +
                $"preparationToken='{PreparationToken.StableText}' occupancyToken='{OccupancyToken.StableText}' " +
                $"bindingToken='{Token.StableText}' actionMap='{ActionMapName}' previousMap='{PreviousActionMapName}' " +
                $"playerInput='{PlayerInputName}' bindingRevision='{BindingRevision}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        internal static PlayerGameplayInputBindingSummary Unbound(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            int bindingRevision,
            string source,
            string reason,
            string message)
        {
            return new PlayerGameplayInputBindingSummary(
                sessionContextId,
                playerSlotId,
                PlayerGameplayInputBindingState.Unbound,
                PlayerGameplayInputAvailability.Unknown,
                default, default, default, default, default, default, default,
                string.Empty, string.Empty, string.Empty,
                bindingRevision,
                source,
                reason,
                message);
        }
    }
}
