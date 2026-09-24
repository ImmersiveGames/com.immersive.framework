using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E Composition presentation-input projection status.")]
    public enum CameraCompositionPresentationInputStatus
    {
        None = 0,
        Succeeded = 10,
        RejectedInvalidRequest = 100,
        RejectedDivergentSnapshot = 120
    }
}
