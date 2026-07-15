using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Narrow explicit endpoint source used only while rebuilding one current gameplay
    /// chain during a synchronous P3K.7D cutover.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7D narrow physical endpoint source for reversible gameplay handoff.")]
    public interface IPlayerGameplayChainHandoffEndpointSource
    {
        bool TryResolveGameplayEndpoints(
            PlayerActorPreparationSummary preparation,
            out LocalPlayerHostAuthoring host,
            out PlayerActorDeclaration actorDeclaration,
            out UnityPlayerInputGateAdapter gateAdapter,
            out PlayerGameplayCameraAuthoring cameraAuthoring,
            out PlayerGameplayCameraRequiredness cameraRequiredness,
            out CameraOutputSessionBinding outputSession,
            out string issue);
    }
}
