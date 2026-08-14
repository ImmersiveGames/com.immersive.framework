using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-021 explicit Scene-Provided Player initial placement policy.")]
    public enum SceneProvidedPlayerInitialPlacementPolicy
    {
        PreserveAuthoredPose = 0,
        ApplyActivityPlacement = 1
    }
}
