using Immersive.Framework.ApiStatus;
using Immersive.Framework.Actors;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable diagnostic result for one Scene Logical Player Actor adoption or release operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B2B Scene Logical Player Actor adoption result.")]
    public sealed class ScenePlayerActorAdoptionResult
    {
        internal ScenePlayerActorAdoptionResult(
            ScenePlayerActorAdoptionStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            ActorProfile actorProfile,
            PlayerActorDeclaration sceneActor,
            ScenePlayerActorAdoptionToken token,
            bool stateChanged,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PlayerSlotId = playerSlotId;
            ActorProfile = actorProfile;
            SceneActor = sceneActor;
            Token = token;
            StateChanged = stateChanged;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ScenePlayerActorAdoptionStatus Status { get; }
        public string Operation { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfile ActorProfile { get; }
        public ActorProfileId ActorProfileId =>
            ActorProfile != null &&
            ActorProfile.TryGetActorProfileId(out ActorProfileId actorProfileId, out _)
                ? actorProfileId
                : default;
        public PlayerActorDeclaration SceneActor { get; }
        public ScenePlayerActorAdoptionToken Token { get; }
        public PlayerActorPhysicalOwnership PhysicalOwnership =>
            PlayerActorPhysicalOwnership.ExternalSceneOwned;
        public bool StateChanged { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            ScenePlayerActorAdoptionStatus.SucceededAdopted or
            ScenePlayerActorAdoptionStatus.SucceededAlreadyAdopted or
            ScenePlayerActorAdoptionStatus.SucceededReleased;

        public bool Failed => Status is
            ScenePlayerActorAdoptionStatus.FailedRuntimeContentRegistration or
            ScenePlayerActorAdoptionStatus.FailedActivation or
            ScenePlayerActorAdoptionStatus.FailedRelease or
            ScenePlayerActorAdoptionStatus.FailedRollback;

        public bool Rejected => !Succeeded && !Failed && Status != ScenePlayerActorAdoptionStatus.None;

        public string ToDiagnosticString()
        {
            string actorName = SceneActor != null ? SceneActor.name : string.Empty;
            return $"operation='{Operation}' status='{Status}' slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"actorProfile='{(ActorProfileId.IsValid ? ActorProfileId.StableText : string.Empty)}' sceneActor='{actorName}' " +
                $"ownership='{PhysicalOwnership}' token='{Token.StableText}' stateChanged='{StateChanged}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        internal static ScenePlayerActorAdoptionResult RuntimeUnavailable(
            string operation,
            SceneLocalPlayerAdmissionAuthoring authoring,
            string source,
            string reason,
            string message)
        {
            PlayerSlotId playerSlotId = default;
            if (authoring != null)
            {
                authoring.TryGetPlayerSlotId(out playerSlotId, out _);
            }
            return new ScenePlayerActorAdoptionResult(
                ScenePlayerActorAdoptionStatus.RejectedRuntimeUnavailable,
                operation,
                playerSlotId,
                authoring != null ? authoring.ActorProfile : null,
                authoring != null ? authoring.SceneLogicalPlayerActor : null,
                default,
                false,
                source,
                reason,
                message.NormalizeTextOrFallback("Scene Player Actor adoption runtime is unavailable."));
        }
    }
}
