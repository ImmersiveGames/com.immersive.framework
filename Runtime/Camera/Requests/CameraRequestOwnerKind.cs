namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declares the domain owner publishing camera intent.
    /// The owner does not directly control Cinemachine or an output.
    /// </summary>
    public enum CameraRequestOwnerKind
    {
        Undefined = 0,
        Route = 1,
        Activity = 2,
        LocalPlayer = 3,
        Cutscene = 4,
        ModalPresentation = 5,
        Spectator = 6,
        Debug = 7
    }
}
