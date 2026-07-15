using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical evidence for one projected Slot evaluated against an
    /// Activity participation requirement.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.6 immutable per-Slot Activity Player admission result.")]
    public readonly struct ActivityPlayerAdmissionSlotResult
    {
        internal ActivityPlayerAdmissionSlotResult(
            int projectedIndex,
            int configuredIndex,
            PlayerSlotId playerSlotId,
            PlayerParticipationRequirementLevel requirementLevel,
            ActivityPlayerAdmissionSlotStatus status,
            ActivityPlayerAdmissionMissingRequirement missingRequirement,
            ActivityPlayerAdmissionEvaluationCode code,
            ActorProfileId selectedActorProfileId,
            ActorId preparedActorId,
            bool joined,
            bool selectedActor,
            bool logicalActorPrepared,
            bool gameplayReady,
            string message)
        {
            ProjectedIndex = projectedIndex;
            ConfiguredIndex = configuredIndex;
            PlayerSlotId = playerSlotId;
            RequirementLevel = requirementLevel;
            Status = status;
            MissingRequirement = missingRequirement;
            Code = code;
            SelectedActorProfileId = selectedActorProfileId;
            PreparedActorId = preparedActorId;
            Joined = joined;
            SelectedActor = selectedActor;
            LogicalActorPrepared = logicalActorPrepared;
            GameplayReady = gameplayReady;
            Message = message.NormalizeText();
        }

        public int ProjectedIndex { get; }
        public int ConfiguredIndex { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerParticipationRequirementLevel RequirementLevel { get; }
        public ActivityPlayerAdmissionSlotStatus Status { get; }
        public ActivityPlayerAdmissionMissingRequirement MissingRequirement { get; }
        public ActivityPlayerAdmissionEvaluationCode Code { get; }
        public ActorProfileId SelectedActorProfileId { get; }
        public ActorId PreparedActorId { get; }
        public bool Joined { get; }
        public bool SelectedActor { get; }
        public bool LogicalActorPrepared { get; }
        public bool GameplayReady { get; }
        public string Message { get; }

        public bool IsSatisfied => Status == ActivityPlayerAdmissionSlotStatus.Satisfied;
        public bool IsPendingResolution => Status == ActivityPlayerAdmissionSlotStatus.PendingResolution;
        public bool IsBlocked => Status == ActivityPlayerAdmissionSlotStatus.Blocked;
        public bool IsFailed => Status == ActivityPlayerAdmissionSlotStatus.Failed;

        public bool IsValid =>
            ProjectedIndex >= 0 &&
            ConfiguredIndex >= 0 &&
            PlayerSlotId.IsValid &&
            System.Enum.IsDefined(typeof(PlayerParticipationRequirementLevel), RequirementLevel) &&
            Status != ActivityPlayerAdmissionSlotStatus.None &&
            Code != ActivityPlayerAdmissionEvaluationCode.None &&
            !string.IsNullOrEmpty(Message);

        public string ToDiagnosticString()
        {
            return
                $"projectedIndex='{ProjectedIndex}' configuredIndex='{ConfiguredIndex}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"requirement='{RequirementLevel}' status='{Status}' missing='{MissingRequirement}' code='{Code}' " +
                $"selectedActorProfile='{(SelectedActorProfileId.IsValid ? SelectedActorProfileId.StableText : string.Empty)}' " +
                $"preparedActor='{(PreparedActorId.IsValid ? PreparedActorId.StableText : string.Empty)}' " +
                $"joined='{Joined}' selected='{SelectedActor}' prepared='{LogicalActorPrepared}' ready='{GameplayReady}' " +
                $"message='{Message}'";
        }
    }
}
