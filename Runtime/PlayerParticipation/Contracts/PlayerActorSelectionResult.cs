using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable evidence for one Session Actor selection operation.
    /// Current authority remains the Player participation context and Slot snapshot.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3H Player Actor selection result.")]
    public sealed class PlayerActorSelectionResult
    {
        internal PlayerActorSelectionResult(
            PlayerActorSelectionStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerSlotProfile playerSlotProfile,
            ActorProfile previousActorProfile,
            ActorProfile selectedActorProfile,
            int previousSelectionRevision,
            int selectionRevision,
            PlayerActorSelectionDuplicatePolicy duplicatePolicy,
            PlayerSlotId conflictingPlayerSlotId,
            string source,
            string reason,
            string message,
            PlayerSlotRuntimeSnapshot slot,
            PlayerParticipationSnapshot snapshot)
        {
            Status = status;
            Operation = operation ?? string.Empty;
            PlayerSlotId = playerSlotId;
            PlayerSlotProfile = playerSlotProfile;
            PreviousActorProfile = previousActorProfile;
            SelectedActorProfile = selectedActorProfile;
            PreviousSelectionRevision = previousSelectionRevision;
            SelectionRevision = selectionRevision;
            DuplicatePolicy = duplicatePolicy;
            ConflictingPlayerSlotId = conflictingPlayerSlotId;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
            Slot = slot;
            Snapshot = snapshot;
        }

        public PlayerActorSelectionStatus Status { get; }
        public string Operation { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerSlotProfile PlayerSlotProfile { get; }
        public ActorProfile PreviousActorProfile { get; }
        public ActorProfile SelectedActorProfile { get; }
        public ActorProfileId PreviousActorProfileId => ResolveId(PreviousActorProfile);
        public ActorProfileId SelectedActorProfileId => ResolveId(SelectedActorProfile);
        public int PreviousSelectionRevision { get; }
        public int SelectionRevision { get; }
        public PlayerActorSelectionDuplicatePolicy DuplicatePolicy { get; }
        public PlayerSlotId ConflictingPlayerSlotId { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public PlayerSlotRuntimeSnapshot Slot { get; }
        public PlayerParticipationSnapshot Snapshot { get; }

        public bool Succeeded => Status is
            PlayerActorSelectionStatus.SucceededSelected or
            PlayerActorSelectionStatus.SucceededReplaced or
            PlayerActorSelectionStatus.SucceededCleared;

        public bool Rejected => !Succeeded && Status != PlayerActorSelectionStatus.None;

        public bool StateChanged => SelectionRevision != PreviousSelectionRevision;

        internal static PlayerActorSelectionResult RuntimeUnavailable(
            string operation,
            PlayerActorSelectionRequest request,
            string message)
        {
            return new PlayerActorSelectionResult(
                PlayerActorSelectionStatus.RejectedRuntimeUnavailable,
                operation,
                request.PlayerSlotId,
                null,
                null,
                null,
                0,
                0,
                PlayerActorSelectionDuplicatePolicy.Unspecified,
                default,
                request.Source,
                request.Reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Player Actor selection runtime is unavailable."
                    : message.Trim(),
                default,
                PlayerParticipationSnapshot.Empty(
                    PlayerParticipationOperationStatus.None,
                    string.IsNullOrWhiteSpace(message)
                        ? "Player Actor selection runtime is unavailable."
                        : message.Trim()));
        }

        public string ToDiagnosticString()
        {
            return $"operation='{Operation}' status='{Status}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"previousActor='{IdentityText(PreviousActorProfileId)}' selectedActor='{IdentityText(SelectedActorProfileId)}' " +
                $"previousSelectionRevision='{PreviousSelectionRevision}' selectionRevision='{SelectionRevision}' " +
                $"policy='{(DuplicatePolicy.IsDefinedPolicy() ? DuplicatePolicy.ToString() : string.Empty)}' " +
                $"conflictingSlot='{(ConflictingPlayerSlotId.IsValid ? ConflictingPlayerSlotId.StableText : string.Empty)}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        private static string IdentityText(ActorProfileId actorProfileId)
        {
            return actorProfileId.IsValid ? actorProfileId.StableText : string.Empty;
        }

        private static ActorProfileId ResolveId(ActorProfile actorProfile)
        {
            return actorProfile != null &&
                actorProfile.TryGetActorProfileId(out ActorProfileId actorProfileId, out _)
                    ? actorProfileId
                    : default;
        }
    }
}
