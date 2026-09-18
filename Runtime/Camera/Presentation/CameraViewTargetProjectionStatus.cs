using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-C/D View presentation target-projection status.")]
    public enum CameraViewTargetProjectionStatus
    {
        None = 0,
        SucceededNoTargets = 10,
        SucceededSingleSubject = 20,
        SucceededGroup = 30,
        BlockedRequiredSubjectMissing = 100,
        BlockedMultipleSubjectsUnsupported = 110,
        RejectedInvalidInput = 120,
        RejectedInvalidPresentation = 130
    }
}
