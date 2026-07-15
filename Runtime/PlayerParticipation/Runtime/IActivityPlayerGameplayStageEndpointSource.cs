using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit physical and Session-authority endpoint source used by the canonical
    /// staged P3J/P3K resolver. It is supplied by host composition; no global lookup is used.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B explicit host endpoint source for staged Player gameplay resolution.")]
    public interface IActivityPlayerGameplayStageEndpointSource
    {
        PlayerParticipationSnapshot CreateParticipationSnapshot();

        PlayerActorPreparationSnapshot CreatePreparationSnapshot();

        bool TryEnsureSelectedActor(
            ActivityAsset activity,
            PlayerSlotId playerSlotId,
            string source,
            string reason,
            out PlayerSlotRuntimeSnapshot selection,
            out bool createdByStage,
            out string issue);

        bool TryReleaseSelectedActor(
            PlayerSlotRuntimeSnapshot selection,
            string source,
            string reason,
            out string issue);

        bool TryEnsurePrepared(
            ActivityAsset activity,
            ActivityPlayerAdmissionStageScope stagedScope,
            PlayerSlotId playerSlotId,
            string source,
            string reason,
            out PlayerActorPreparationSummary preparation,
            out bool createdByStage,
            out string issue);

        bool TryResolveGameplayEndpoints(
            PlayerActorPreparationSummary preparation,
            out LocalPlayerHostAuthoring host,
            out PlayerActorDeclaration actorDeclaration,
            out UnityPlayerInputGateAdapter gateAdapter,
            out PlayerGameplayCameraAuthoring cameraAuthoring,
            out PlayerGameplayCameraRequiredness cameraRequiredness,
            out CameraOutputSessionBinding outputSession,
            out string issue);

        bool TryReleasePreparation(
            PlayerActorPreparationSummary preparation,
            string source,
            string reason,
            out string issue);
    }
}
