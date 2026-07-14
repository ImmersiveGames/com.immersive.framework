using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical Session summary for one Player Slot logical Actor preparation state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.4 immutable per-Slot Logical Player Actor preparation summary.")]
    public readonly struct PlayerActorPreparationSummary
    {
        internal PlayerActorPreparationSummary(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            PlayerActorPreparationState state,
            ActorProfileId selectedActorProfileId,
            int selectionRevision,
            PlayerActorMaterializationSnapshot materialization,
            string source,
            string reason,
            string message)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            State = state;
            SelectedActorProfileId = selectedActorProfileId;
            SelectionRevision = selectionRevision;
            Materialization = materialization;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerActorPreparationState State { get; }
        public ActorProfileId SelectedActorProfileId { get; }
        public int SelectionRevision { get; }
        public PlayerActorMaterializationSnapshot Materialization { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public ActorProfileId PreparedActorProfileId =>
            Materialization.IsValid ? Materialization.ActorProfileId : default;

        public PlayerActorPreparationToken Token =>
            HasMaterialization
                ? new PlayerActorPreparationToken(
                    SessionContextId,
                    PlayerSlotId,
                    Materialization.ActorId,
                    Materialization.RuntimeContentIdentity,
                    Materialization.MaterializationRevision)
                : default;

        public bool HasSelection => SelectedActorProfileId.IsValid;
        public bool HasMaterialization => Materialization.IsValid;
        public bool IsPrepared => State == PlayerActorPreparationState.Prepared;
        public bool IsReleaseFailed => State == PlayerActorPreparationState.ReleaseFailed;
        public bool IsUnprepared => State == PlayerActorPreparationState.Unprepared;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            State != PlayerActorPreparationState.None &&
            SelectionRevision >= 0 &&
            ((State == PlayerActorPreparationState.Unprepared && !HasMaterialization) ||
             (State is PlayerActorPreparationState.Prepared or PlayerActorPreparationState.ReleaseFailed &&
              HasSelection &&
              HasMaterialization &&
              SelectedActorProfileId == PreparedActorProfileId &&
              Token.IsValid));

        public string ToDiagnosticString()
        {
            return $"session='{SessionContextId}' slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"state='{State}' selectedActorProfile='{(SelectedActorProfileId.IsValid ? SelectedActorProfileId.StableText : string.Empty)}' " +
                $"selectionRevision='{SelectionRevision}' preparationToken='{Token.StableText}' " +
                $"materialization='{(Materialization.IsValid ? Materialization.ToDiagnosticString() : string.Empty)}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }
    }
}
