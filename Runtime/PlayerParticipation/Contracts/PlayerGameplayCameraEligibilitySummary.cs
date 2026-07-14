using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical prepared-Player camera eligibility evidence for one Slot.
    /// Unity object references remain inside the internal runtime authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 immutable per-Slot prepared Player camera eligibility summary.")]
    public readonly struct PlayerGameplayCameraEligibilitySummary
    {
        internal PlayerGameplayCameraEligibilitySummary(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            PlayerGameplayCameraEligibilityState state,
            PlayerGameplayCameraRequiredness requiredness,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            RuntimeContentIdentity runtimeContentIdentity,
            PlayerActorPreparationToken preparationToken,
            PlayerGameplayOccupancyToken occupancyToken,
            PlayerGameplayInputBindingToken inputBindingToken,
            PlayerGameplayCameraEligibilityToken token,
            string cameraRigName,
            string followTargetName,
            string lookAtTargetName,
            int precedence,
            string requestId,
            string lifetimeScopeId,
            string tieBreakerId,
            int eligibilityRevision,
            string source,
            string reason,
            string message)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            State = state;
            Requiredness = requiredness;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            Owner = owner;
            RuntimeContentIdentity = runtimeContentIdentity;
            PreparationToken = preparationToken;
            OccupancyToken = occupancyToken;
            InputBindingToken = inputBindingToken;
            Token = token;
            CameraRigName = cameraRigName.NormalizeText();
            FollowTargetName = followTargetName.NormalizeText();
            LookAtTargetName = lookAtTargetName.NormalizeText();
            Precedence = precedence;
            RequestId = requestId.NormalizeText();
            LifetimeScopeId = lifetimeScopeId.NormalizeText();
            TieBreakerId = tieBreakerId.NormalizeText();
            EligibilityRevision = eligibilityRevision;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayCameraEligibilityState State { get; }
        public PlayerGameplayCameraRequiredness Requiredness { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentOwner Owner { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public PlayerGameplayOccupancyToken OccupancyToken { get; }
        public PlayerGameplayInputBindingToken InputBindingToken { get; }
        public PlayerGameplayCameraEligibilityToken Token { get; }
        public string CameraRigName { get; }
        public string FollowTargetName { get; }
        public string LookAtTargetName { get; }
        public int Precedence { get; }
        public string RequestId { get; }
        public string LifetimeScopeId { get; }
        public string TieBreakerId { get; }
        public int EligibilityRevision { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsNotEvaluated =>
            State == PlayerGameplayCameraEligibilityState.NotEvaluated;

        public bool IsSkippedOptional =>
            State == PlayerGameplayCameraEligibilityState.SkippedOptional;

        public bool IsEligible =>
            State == PlayerGameplayCameraEligibilityState.Eligible;

        public bool HasCurrentDecision =>
            IsSkippedOptional || IsEligible;

        public bool IsRequired =>
            Requiredness == PlayerGameplayCameraRequiredness.Required;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            State != PlayerGameplayCameraEligibilityState.None &&
            EligibilityRevision >= 0 &&
            (IsNotEvaluated
                ? Requiredness == PlayerGameplayCameraRequiredness.None &&
                  !ActorProfileId.IsValid &&
                  !ActorId.IsValid &&
                  !Owner.IsValid &&
                  !RuntimeContentIdentity.IsValid &&
                  !PreparationToken.IsValid &&
                  !OccupancyToken.IsValid &&
                  !InputBindingToken.IsValid &&
                  !Token.IsValid &&
                  string.IsNullOrEmpty(CameraRigName) &&
                  string.IsNullOrEmpty(FollowTargetName) &&
                  string.IsNullOrEmpty(LookAtTargetName) &&
                  string.IsNullOrEmpty(RequestId) &&
                  string.IsNullOrEmpty(LifetimeScopeId) &&
                  string.IsNullOrEmpty(TieBreakerId)
                : ActorProfileId.IsValid &&
                  ActorId.IsValid &&
                  Owner.IsValid &&
                  RuntimeContentIdentity.IsValid &&
                  RuntimeContentIdentity.Owner == Owner &&
                  PreparationToken.IsValid &&
                  OccupancyToken.IsValid &&
                  InputBindingToken.IsValid &&
                  Token.IsValid &&
                  Token.SessionContextId == SessionContextId &&
                  Token.PlayerSlotId == PlayerSlotId &&
                  Token.ActorProfileId == ActorProfileId &&
                  Token.ActorId == ActorId &&
                  Token.Owner == Owner &&
                  Token.RuntimeContentIdentity == RuntimeContentIdentity &&
                  Token.PreparationToken == PreparationToken &&
                  Token.OccupancyToken == OccupancyToken &&
                  Token.InputBindingToken == InputBindingToken &&
                  Token.EligibilityRevision == EligibilityRevision &&
                  (IsSkippedOptional
                      ? Requiredness == PlayerGameplayCameraRequiredness.Optional &&
                        string.IsNullOrEmpty(CameraRigName) &&
                        string.IsNullOrEmpty(FollowTargetName) &&
                        string.IsNullOrEmpty(LookAtTargetName) &&
                        string.IsNullOrEmpty(RequestId) &&
                        string.IsNullOrEmpty(LifetimeScopeId) &&
                        string.IsNullOrEmpty(TieBreakerId)
                      : (Requiredness == PlayerGameplayCameraRequiredness.Optional ||
                         Requiredness == PlayerGameplayCameraRequiredness.Required) &&
                        !string.IsNullOrEmpty(CameraRigName) &&
                        !string.IsNullOrEmpty(FollowTargetName) &&
                        !string.IsNullOrEmpty(RequestId) &&
                        !string.IsNullOrEmpty(LifetimeScopeId) &&
                        !string.IsNullOrEmpty(TieBreakerId)));

        public string ToDiagnosticString()
        {
            return
                $"session='{SessionContextId}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"state='{State}' requiredness='{Requiredness}' " +
                $"actorProfile='{(ActorProfileId.IsValid ? ActorProfileId.StableText : string.Empty)}' " +
                $"actor='{(ActorId.IsValid ? ActorId.StableText : string.Empty)}' " +
                $"owner='{(Owner.IsValid ? Owner.StableText : string.Empty)}' " +
                $"runtimeContent='{(RuntimeContentIdentity.IsValid ? RuntimeContentIdentity.StableText : string.Empty)}' " +
                $"preparationToken='{PreparationToken.StableText}' " +
                $"occupancyToken='{OccupancyToken.StableText}' " +
                $"inputBindingToken='{InputBindingToken.StableText}' " +
                $"eligibilityToken='{Token.StableText}' " +
                $"rig='{CameraRigName}' follow='{FollowTargetName}' lookAt='{LookAtTargetName}' " +
                $"precedence='{Precedence}' request='{RequestId}' lifetime='{LifetimeScopeId}' " +
                $"tieBreaker='{TieBreakerId}' eligibilityRevision='{EligibilityRevision}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        internal static PlayerGameplayCameraEligibilitySummary NotEvaluated(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            int eligibilityRevision,
            string source,
            string reason,
            string message)
        {
            return new PlayerGameplayCameraEligibilitySummary(
                sessionContextId,
                playerSlotId,
                PlayerGameplayCameraEligibilityState.NotEvaluated,
                PlayerGameplayCameraRequiredness.None,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                eligibilityRevision,
                source,
                reason,
                message);
        }
    }
}
