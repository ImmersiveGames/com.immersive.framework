using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable per-Slot evidence produced by one Activity Player Actor lifecycle phase.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.6 per-Slot Activity-owned Logical Player Actor lifecycle evidence.")]
    public readonly struct ActivityPlayerActorSlotLifecycleSnapshot
    {
        internal ActivityPlayerActorSlotLifecycleSnapshot(
            PlayerSlotId playerSlotId,
            bool joined,
            ActorProfileId selectedActorProfileId,
            bool selectionApplied,
            PlayerActorPreparationToken preparationToken,
            bool preparationApplied,
            bool released,
            PlayerActorPreparationStatus preparationStatus,
            string message)
        {
            PlayerSlotId = playerSlotId;
            Joined = joined;
            SelectedActorProfileId = selectedActorProfileId;
            SelectionApplied = selectionApplied;
            PreparationToken = preparationToken;
            PreparationApplied = preparationApplied;
            Released = released;
            PreparationStatus = preparationStatus;
            Message = message ?? string.Empty;
        }

        public PlayerSlotId PlayerSlotId { get; }
        public bool Joined { get; }
        public ActorProfileId SelectedActorProfileId { get; }
        public bool SelectionApplied { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public bool PreparationApplied { get; }
        public bool Released { get; }
        public PlayerActorPreparationStatus PreparationStatus { get; }
        public string Message { get; }
        public bool HasSelection => SelectedActorProfileId.IsValid;
        public bool HasPreparation => PreparationToken.IsValid;

        public string ToDiagnosticString()
        {
            return $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"joined='{Joined}' selectedActor='{(SelectedActorProfileId.IsValid ? SelectedActorProfileId.StableText : string.Empty)}' " +
                $"selectionApplied='{SelectionApplied}' preparation='{PreparationToken.StableText}' " +
                $"preparationApplied='{PreparationApplied}' released='{Released}' " +
                $"preparationStatus='{PreparationStatus}' message='{Message}'";
        }
    }
}
