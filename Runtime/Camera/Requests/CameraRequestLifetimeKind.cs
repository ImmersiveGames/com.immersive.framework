namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declares the scope that keeps a camera request eligible.
    /// Runtime enforcement belongs to CameraOutputContext in a later cut.
    /// </summary>
    public enum CameraRequestLifetimeKind
    {
        Undefined = 0,
        Route = 1,
        Activity = 2,
        LocalPlayerEligibility = 3,
        ExplicitOperation = 4,
        SpectatorSession = 5
    }
}
