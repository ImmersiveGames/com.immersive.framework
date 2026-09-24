using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A Camera Subject availability operation status.")]
    public enum CameraSubjectAvailabilityStatus
    {
        None = 0,
        SucceededAvailable = 10,
        SucceededAlreadyAvailable = 20,
        SucceededUnavailable = 30,
        SucceededAlreadyUnavailable = 40,
        RejectedInvalidRequest = 100,
        RejectedSubjectConflict = 110,
        RejectedForeignOrStaleToken = 120
    }
}
