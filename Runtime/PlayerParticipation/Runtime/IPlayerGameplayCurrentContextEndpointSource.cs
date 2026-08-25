using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.PlayerParticipation
{
    internal interface IPlayerGameplayCurrentContextEndpointSource
    {
        bool TryResolveGameplayEndpoints(
            PlayerActorPreparationSummary preparation,
            out LocalPlayerHostAuthoring host,
            out PlayerActorDeclaration actorDeclaration,
            out UnityPlayerInputGateAdapter gateAdapter,
            out PlayerGameplayCameraAuthoring cameraAuthoring,
            out PlayerGameplayCameraRequiredness cameraRequiredness,
            out CameraOutputAuthoring outputSession,
            out string issue);
    }
}
