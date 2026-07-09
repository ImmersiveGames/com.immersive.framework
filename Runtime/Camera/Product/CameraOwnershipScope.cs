namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declares the ownership boundary that is allowed to drive or coordinate a camera.
    /// This does not create runtime authority by itself.
    /// </summary>
    public enum CameraOwnershipScope
    {
        Undefined = 0,
        Explicit = 5,
        Route = 10,
        Activity = 20,
        SinglePlayer = 30,
        LocalPlayer = 40,
        PlayerGroup = 50,
        SpectatorOrDebug = 60
    }
}
