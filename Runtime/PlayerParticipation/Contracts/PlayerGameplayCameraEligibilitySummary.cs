using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3K.4 immutable per-Slot Player camera capability summary.")]
    public readonly struct PlayerGameplayCameraEligibilitySummary
    {
        internal PlayerGameplayCameraEligibilitySummary(string sessionContextId, PlayerSlotId playerSlotId,
            PlayerGameplayCameraEligibilityState state, PlayerGameplayCameraRequiredness requiredness,
            ActorProfileId actorProfileId, ActorId actorId, RuntimeContentOwner owner,
            RuntimeContentIdentity runtimeContentIdentity, PlayerActorPreparationToken preparationToken,
            PlayerGameplayCameraEligibilityToken token, string cameraRigName, string followTargetName,
            string lookAtTargetName, int precedence, string requestId, string lifetimeScopeId,
            string tieBreakerId, bool requestPublished, string publisherSource, bool requestReleased,
            int cameraRevision, string source, string reason, string message)
        {
            SessionContextId=sessionContextId.NormalizeText(); PlayerSlotId=playerSlotId; State=state; Requiredness=requiredness;
            ActorProfileId=actorProfileId; ActorId=actorId; Owner=owner; RuntimeContentIdentity=runtimeContentIdentity;
            PreparationToken=preparationToken; Token=token; CameraRigName=cameraRigName.NormalizeText();
            FollowTargetName=followTargetName.NormalizeText(); LookAtTargetName=lookAtTargetName.NormalizeText();
            Precedence=precedence; RequestId=requestId.NormalizeText(); LifetimeScopeId=lifetimeScopeId.NormalizeText();
            TieBreakerId=tieBreakerId.NormalizeText(); CameraRequestPublished=requestPublished;
            CameraPublisherSource=publisherSource.NormalizeText(); CameraRequestReleased=requestReleased;
            CameraRevision=cameraRevision; Source=source.NormalizeText(); Reason=reason.NormalizeText(); Message=message.NormalizeText();
        }
        public string SessionContextId { get; } public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayCameraEligibilityState State { get; } public PlayerGameplayCameraRequiredness Requiredness { get; }
        public ActorProfileId ActorProfileId { get; } public ActorId ActorId { get; } public RuntimeContentOwner Owner { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; } public PlayerActorPreparationToken PreparationToken { get; }
        public PlayerGameplayCameraEligibilityToken Token { get; } public string CameraRigName { get; }
        public string FollowTargetName { get; } public string LookAtTargetName { get; } public int Precedence { get; }
        public string RequestId { get; } public string LifetimeScopeId { get; } public string TieBreakerId { get; }
        public bool CameraRequestPublished { get; } public string CameraPublisherSource { get; }
        public bool CameraRequestReleased { get; } public int CameraRevision { get; }
        public int EligibilityRevision => CameraRevision; public string Source { get; } public string Reason { get; } public string Message { get; }
        public bool IsNotEvaluated => State == PlayerGameplayCameraEligibilityState.NotEvaluated;
        public bool IsSkippedOptional => State == PlayerGameplayCameraEligibilityState.SkippedOptional;
        public bool IsEligible => State == PlayerGameplayCameraEligibilityState.Eligible;
        public bool HasCurrentDecision => IsSkippedOptional || IsEligible;
        public bool IsRequired => Requiredness == PlayerGameplayCameraRequiredness.Required;
        public bool IsValid => !string.IsNullOrEmpty(SessionContextId) && PlayerSlotId.IsValid && State != PlayerGameplayCameraEligibilityState.None &&
            (IsNotEvaluated ? Requiredness == PlayerGameplayCameraRequiredness.None && !Token.IsValid :
             Token.IsValid && Token.SessionContextId == SessionContextId && Token.PlayerSlotId == PlayerSlotId && Token.PreparationToken == PreparationToken &&
             (IsSkippedOptional ? Requiredness == PlayerGameplayCameraRequiredness.Optional && !CameraRequestPublished && CameraRequestReleased :
              (Requiredness == PlayerGameplayCameraRequiredness.Optional || Requiredness == PlayerGameplayCameraRequiredness.Required) &&
              !string.IsNullOrEmpty(RequestId) && !string.IsNullOrEmpty(Token.CameraOutputId) && CameraRequestPublished && !CameraRequestReleased));
        public string ToDiagnosticString() => $"session='{SessionContextId}' slot='{PlayerSlotId.StableText}' state='{State}' token='{Token.StableText}' output='{Token.CameraOutputId}' request='{RequestId}' published='{CameraRequestPublished}' released='{CameraRequestReleased}'";
        internal static PlayerGameplayCameraEligibilitySummary NotEvaluated(string sessionContextId, PlayerSlotId playerSlotId, int revision, string source, string reason, string message) =>
            new PlayerGameplayCameraEligibilitySummary(sessionContextId, playerSlotId, PlayerGameplayCameraEligibilityState.NotEvaluated, PlayerGameplayCameraRequiredness.None, default, default, default, default, default, default, string.Empty, string.Empty, string.Empty, 0, string.Empty, string.Empty, string.Empty, false, string.Empty, false, revision, source, reason, message);
    }
}
