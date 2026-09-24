using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit physical lifetime authority for one prepared Logical Player Actor.
    /// Runtime preparation and gameplay state are separate from physical ownership.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B2B explicit physical ownership for prepared Logical Player Actors.")]
    public enum PlayerActorPhysicalOwnership
    {
        Unspecified = 0,
        FrameworkOwned = 10,
        ExternalSceneOwned = 20
    }
}
