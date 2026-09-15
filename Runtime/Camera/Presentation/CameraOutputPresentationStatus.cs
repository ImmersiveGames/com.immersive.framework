using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "CAMERA-028-C Output presentation mutation evidence.")]
    internal enum CameraOutputPresentationStatus
    {
        None = 0,
        Applied = 10,
        Replaced = 20,
        Cleared = 30,
        NoChange = 40,
        RejectedInvalidSnapshot = 100,
        RejectedInvalidRect = 110,
        RejectedDuplicateOutput = 120,
        RejectedOutputUnavailable = 130,
        RejectedMissingCamera = 140,
        RejectedDisposed = 150
    }
}
