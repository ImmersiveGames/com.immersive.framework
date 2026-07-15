using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical diagnostics for the official FrameworkRuntimeHost-scoped
    /// P3K.2-P3K.7E composition.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7F official Session Player gameplay authority composition snapshot.")]
    public sealed class PlayerGameplayRuntimeHostSnapshot
    {
        internal PlayerGameplayRuntimeHostSnapshot(
            bool initialized,
            string sessionContextId,
            PlayerGameplayOccupancySnapshot occupancy,
            PlayerGameplayInputBindingSnapshot inputBinding,
            PlayerGameplayCameraEligibilitySnapshot cameraEligibility,
            PlayerGameplayAdmissionSnapshot admission,
            PlayerActorCandidateRuntimeHostSnapshot candidates,
            ActivityPlayerHandoffGroupSnapshot handoffGroup,
            int activePerSlotHandoffCount,
            PlayerGameplayRuntimeOperationStatus lastOperationStatus,
            string diagnostic)
        {
            IsInitialized = initialized;
            SessionContextId = sessionContextId ?? string.Empty;
            Occupancy = occupancy;
            InputBinding = inputBinding;
            CameraEligibility = cameraEligibility;
            Admission = admission;
            Candidates = candidates;
            HandoffGroup = handoffGroup;
            ActivePerSlotHandoffCount = activePerSlotHandoffCount;
            LastOperationStatus = lastOperationStatus;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool IsInitialized { get; }
        public string SessionContextId { get; }
        public PlayerGameplayOccupancySnapshot Occupancy { get; }
        public PlayerGameplayInputBindingSnapshot InputBinding { get; }
        public PlayerGameplayCameraEligibilitySnapshot CameraEligibility { get; }
        public PlayerGameplayAdmissionSnapshot Admission { get; }
        public PlayerActorCandidateRuntimeHostSnapshot Candidates { get; }
        public ActivityPlayerHandoffGroupSnapshot HandoffGroup { get; }
        public int ActivePerSlotHandoffCount { get; }
        public PlayerGameplayRuntimeOperationStatus LastOperationStatus { get; }
        public string Diagnostic { get; }

        public int ConfiguredSlotCount => Occupancy?.ConfiguredSlotCount ?? 0;
        public int OccupiedCount => Occupancy?.OccupiedCount ?? 0;
        public int BoundInputCount => InputBinding?.BoundCount ?? 0;
        public int CameraDecisionCount => (CameraEligibility?.EligibleCount ?? 0) + (CameraEligibility?.SkippedOptionalCount ?? 0);
        public int GameplayReadyCount => Admission?.ReadyCount ?? 0;
        public int CandidateCount => Candidates?.CandidateCount ?? 0;
        public bool HasActiveHandoffGroup =>
            HandoffGroup != null &&
            HandoffGroup.Token.IsValid &&
            !HandoffGroup.IsCommitted;

        internal static PlayerGameplayRuntimeHostSnapshot Unavailable(
            string diagnostic)
        {
            return new PlayerGameplayRuntimeHostSnapshot(
                false,
                string.Empty,
                null,
                null,
                null,
                null,
                PlayerActorCandidateRuntimeHostSnapshot.Unavailable(diagnostic),
                null,
                0,
                PlayerGameplayRuntimeOperationStatus.RejectedRuntimeUnavailable,
                diagnostic);
        }

        public string ToDiagnosticString()
        {
            return
                $"initialized='{IsInitialized}' session='{SessionContextId}' " +
                $"configured='{ConfiguredSlotCount}' occupied='{OccupiedCount}' " +
                $"inputBound='{BoundInputCount}' cameraDecisions='{CameraDecisionCount}' " +
                $"gameplayReady='{GameplayReadyCount}' candidates='{CandidateCount}' " +
                $"activePerSlotHandoffs='{ActivePerSlotHandoffCount}' " +
                $"group='{(HandoffGroup != null ? HandoffGroup.State.ToString() : string.Empty)}' " +
                $"lastStatus='{LastOperationStatus}' diagnostic='{Diagnostic}'";
        }
    }
}
