namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Shared status for camera authoring operations and target resolution.
    /// </summary>
    public enum CameraOperationStatus
    {
        NotRun = 0,
        Succeeded = 10,
        SucceededWithWarnings = 20,
        Blocked = 30
    }
}
