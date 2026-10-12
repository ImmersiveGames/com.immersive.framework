using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Issue vocabulary for Editor Manager-Provisioned Player authoring validation.")]
    public enum LocalPlayerProvisioningIssueKind
    {
        None = 0,
        MissingRequiredSurface = 10,
        DuplicateSurface = 20,
        MissingPlayerInputManager = 30,
        DivergentPlayerInputManager = 40,
        MissingPlayerPrefab = 50,
        InvalidPlayerHost = 60,
        ManualJoinRequired = 80,
        CSharpEventsRequired = 90,
        MissingLocalPlayerHostPrefab = 100,
        DivergentManagerPrefab = 110
    }
}
