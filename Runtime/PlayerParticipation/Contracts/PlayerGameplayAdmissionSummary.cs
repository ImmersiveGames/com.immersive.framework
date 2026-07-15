using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical gameplay admission evidence for one configured Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.5 immutable per-Slot gameplay admission summary.")]
    public readonly struct PlayerGameplayAdmissionSummary
    {
        internal PlayerGameplayAdmissionSummary(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionState state,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            RuntimeContentIdentity runtimeContentIdentity,
            PlayerActorPreparationToken preparationToken,
            PlayerGameplayOccupancyToken occupancyToken,
            PlayerGameplayInputBindingToken inputBindingToken,
            PlayerGameplayCameraEligibilityToken cameraEligibilityToken,
            PlayerGameplayAdmissionToken token,
            PlayerGameplayCameraEligibilityState cameraEligibilityState,
            PlayerGameplayCameraRequiredness cameraRequiredness,
            bool cameraRequestPublished,
            string cameraRequestId,
            string cameraOutputId,
            bool cameraRequestReleased,
            bool cameraEligibilityReleased,
            bool inputBindingReleased,
            bool occupancyReleased,
            int admissionRevision,
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
            OccupancyToken = occupancyToken;
            InputBindingToken = inputBindingToken;
            CameraEligibilityToken = cameraEligibilityToken;
            Token = token;
            CameraEligibilityState = cameraEligibilityState;
            CameraRequiredness = cameraRequiredness;
            CameraRequestPublished = cameraRequestPublished;
            CameraRequestId = cameraRequestId.NormalizeText();
            CameraOutputId = cameraOutputId.NormalizeText();
            CameraRequestReleased = cameraRequestReleased;
            CameraEligibilityReleased = cameraEligibilityReleased;
            InputBindingReleased = inputBindingReleased;
            OccupancyReleased = occupancyReleased;
            AdmissionRevision = admissionRevision;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayAdmissionState State { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentOwner Owner { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public PlayerGameplayOccupancyToken OccupancyToken { get; }
        public PlayerGameplayInputBindingToken InputBindingToken { get; }
        public PlayerGameplayCameraEligibilityToken CameraEligibilityToken { get; }
        public PlayerGameplayAdmissionToken Token { get; }
        public PlayerGameplayCameraEligibilityState CameraEligibilityState { get; }
        public PlayerGameplayCameraRequiredness CameraRequiredness { get; }
        public bool CameraRequestPublished { get; }
        public string CameraRequestId { get; }
        public string CameraOutputId { get; }
        public bool CameraRequestReleased { get; }
        public bool CameraEligibilityReleased { get; }
        public bool InputBindingReleased { get; }
        public bool OccupancyReleased { get; }
        public int AdmissionRevision { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsNotAdmitted =>
            State == PlayerGameplayAdmissionState.NotAdmitted;

        public bool IsReady =>
            State == PlayerGameplayAdmissionState.Ready;

        public bool IsBlockedByInputGate =>
            State == PlayerGameplayAdmissionState.BlockedByInputGate;

        public bool IsReleaseFailed =>
            State == PlayerGameplayAdmissionState.ReleaseFailed;

        public bool IsAdmitted =>
            IsReady || IsBlockedByInputGate || IsReleaseFailed;

        public bool GameplayReady => IsReady;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            State != PlayerGameplayAdmissionState.None &&
            AdmissionRevision >= 0 &&
            (IsNotAdmitted
                ? !ActorProfileId.IsValid &&
                  !ActorId.IsValid &&
                  !Owner.IsValid &&
                  !RuntimeContentIdentity.IsValid &&
                  !PreparationToken.IsValid &&
                  !OccupancyToken.IsValid &&
                  !InputBindingToken.IsValid &&
                  !CameraEligibilityToken.IsValid &&
                  !Token.IsValid &&
                  CameraEligibilityState ==
                      PlayerGameplayCameraEligibilityState.None &&
                  CameraRequiredness ==
                      PlayerGameplayCameraRequiredness.None &&
                  !CameraRequestPublished &&
                  string.IsNullOrEmpty(CameraRequestId) &&
                  string.IsNullOrEmpty(CameraOutputId) &&
                  !CameraRequestReleased &&
                  !CameraEligibilityReleased &&
                  !InputBindingReleased &&
                  !OccupancyReleased
                : HasCoherentAdmissionIdentity() &&
                  HasCoherentCameraEvidence() &&
                  HasCoherentReleaseProgress());

        private bool HasCoherentAdmissionIdentity()
        {
            return ActorProfileId.IsValid &&
                ActorId.IsValid &&
                Owner.IsValid &&
                RuntimeContentIdentity.IsValid &&
                RuntimeContentIdentity.Owner == Owner &&
                PreparationToken.IsValid &&
                OccupancyToken.IsValid &&
                InputBindingToken.IsValid &&
                CameraEligibilityToken.IsValid &&
                Token.IsValid &&
                Token.SessionContextId == SessionContextId &&
                Token.PlayerSlotId == PlayerSlotId &&
                Token.ActorProfileId == ActorProfileId &&
                Token.ActorId == ActorId &&
                Token.Owner == Owner &&
                Token.RuntimeContentIdentity == RuntimeContentIdentity &&
                Token.MaterializationRevision ==
                    OccupancyToken.MaterializationRevision &&
                Token.OccupancyRevision ==
                    OccupancyToken.OccupancyRevision &&
                Token.InputBindingRevision ==
                    InputBindingToken.BindingRevision &&
                Token.CameraEligibilityRevision ==
                    CameraEligibilityToken.EligibilityRevision &&
                Token.AdmissionRevision == AdmissionRevision;
        }

        private bool HasCoherentCameraEvidence()
        {
            if (CameraEligibilityState ==
                PlayerGameplayCameraEligibilityState.SkippedOptional)
            {
                return CameraRequiredness ==
                        PlayerGameplayCameraRequiredness.Optional &&
                    !CameraRequestPublished &&
                    CameraRequestReleased &&
                    string.IsNullOrEmpty(CameraRequestId) &&
                    string.IsNullOrEmpty(CameraOutputId);
            }

            if (CameraEligibilityState !=
                    PlayerGameplayCameraEligibilityState.Eligible ||
                CameraRequiredness ==
                    PlayerGameplayCameraRequiredness.None)
            {
                return false;
            }

            bool hasRequestIdentity =
                !string.IsNullOrEmpty(CameraRequestId) &&
                !string.IsNullOrEmpty(CameraOutputId);
            bool hasNoRequestIdentity =
                string.IsNullOrEmpty(CameraRequestId) &&
                string.IsNullOrEmpty(CameraOutputId);

            if (IsReady || IsBlockedByInputGate)
            {
                return CameraRequestPublished &&
                    !CameraRequestReleased &&
                    hasRequestIdentity;
            }

            return IsReleaseFailed &&
                CameraRequestPublished == !CameraRequestReleased &&
                (hasRequestIdentity || hasNoRequestIdentity);
        }

        private bool HasCoherentReleaseProgress()
        {
            if (CameraEligibilityReleased && !CameraRequestReleased)
            {
                return false;
            }

            if (InputBindingReleased && !CameraEligibilityReleased)
            {
                return false;
            }

            if (OccupancyReleased && !InputBindingReleased)
            {
                return false;
            }

            if (IsReady || IsBlockedByInputGate)
            {
                return !CameraEligibilityReleased &&
                    !InputBindingReleased &&
                    !OccupancyReleased;
            }

            return IsReleaseFailed;
        }


        public string ToDiagnosticString()
        {
            return
                $"session='{SessionContextId}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"state='{State}' gameplayReady='{GameplayReady}' " +
                $"actorProfile='{(ActorProfileId.IsValid ? ActorProfileId.StableText : string.Empty)}' " +
                $"actor='{(ActorId.IsValid ? ActorId.StableText : string.Empty)}' " +
                $"owner='{(Owner.IsValid ? Owner.StableText : string.Empty)}' " +
                $"runtimeContent='{(RuntimeContentIdentity.IsValid ? RuntimeContentIdentity.StableText : string.Empty)}' " +
                $"admissionToken='{Token.StableText}' " +
                $"cameraState='{CameraEligibilityState}' cameraRequiredness='{CameraRequiredness}' " +
                $"cameraPublished='{CameraRequestPublished}' request='{CameraRequestId}' output='{CameraOutputId}' " +
                $"releaseProgress='camera:{CameraRequestReleased},eligibility:{CameraEligibilityReleased},input:{InputBindingReleased},occupancy:{OccupancyReleased}' " +
                $"admissionRevision='{AdmissionRevision}' source='{Source}' reason='{Reason}' message='{Message}'";
        }

        internal static PlayerGameplayAdmissionSummary NotAdmitted(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            int admissionRevision,
            string source,
            string reason,
            string message)
        {
            return new PlayerGameplayAdmissionSummary(
                sessionContextId,
                playerSlotId,
                PlayerGameplayAdmissionState.NotAdmitted,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                PlayerGameplayCameraEligibilityState.None,
                PlayerGameplayCameraRequiredness.None,
                false,
                string.Empty,
                string.Empty,
                false,
                false,
                false,
                false,
                admissionRevision,
                source,
                reason,
                message);
        }
    }
}
