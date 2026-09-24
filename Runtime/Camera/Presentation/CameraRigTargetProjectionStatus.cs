using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E Rig presentation target-projection status.")]
    public enum CameraRigTargetProjectionStatus
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
