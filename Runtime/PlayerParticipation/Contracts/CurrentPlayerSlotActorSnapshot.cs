using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable read model composed from assignment, Host and Actor authorities.
    /// It owns no mutable state and contains no physical Unity reference.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-3 aggregate current Player Slot, Host and Actor read model.")]
    public readonly struct CurrentPlayerSlotActorSnapshot
    {
        internal CurrentPlayerSlotActorSnapshot(
            PlayerSlotAssignmentSnapshot assignment,
            PlayerHostEvidenceSummary hostEvidence,
            PlayerActorPreparationSummary preparation,
            PlayerActorCorrelationEvidence actorEvidence,
            PlayerCurrentActorEvidenceStatus actorStatus,
            string message)
        {
            Assignment = assignment;
            HostEvidence = hostEvidence;
            Preparation = preparation;
            ActorEvidence = actorEvidence;
            ActorStatus = actorStatus;
            Message = message.NormalizeText();
        }

        public PlayerSlotAssignmentSnapshot Assignment { get; }
        public PlayerHostEvidenceSummary HostEvidence { get; }
        public PlayerActorPreparationSummary Preparation { get; }
        public PlayerActorCorrelationEvidence ActorEvidence { get; }
        public PlayerCurrentActorEvidenceStatus ActorStatus { get; }
        public string Message { get; }
        public bool IsAssigned => Assignment.IsAssigned;
        public bool HasConfirmedHost => HostEvidence.IsConfirmed;
        public bool HasPreparedActor => Preparation.IsPrepared;
        public bool HasCurrentActor =>
            ActorStatus == PlayerCurrentActorEvidenceStatus.SucceededCurrent &&
            ActorEvidence.IsValid;
        public bool IsReadable => IsAssigned && HasConfirmedHost;
    }
}
