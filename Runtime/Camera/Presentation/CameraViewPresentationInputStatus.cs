using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-C View presentation-input projection status.")]
    public enum CameraViewPresentationInputStatus
    {
        None = 0,
        Succeeded = 10,
        RejectedInvalidRequest = 100,
        RejectedViewNotFound = 110,
        RejectedDivergentSnapshot = 120
    }
}
