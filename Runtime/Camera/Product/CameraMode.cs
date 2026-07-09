namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Product-level camera mode. This is authoring intent, not a direct Unity Camera or Cinemachine implementation detail.
    /// </summary>
    public enum CameraMode
    {
        Undefined = 0,
        RouteCamera = 10,
        ActivityCamera = 20,
        SinglePlayerFollowCamera = 30,
        LocalPlayerCamera = 40,
        SharedPlayerGroupCamera = 50,
        SpectatorOrDebugCamera = 60
    }
}
