using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CameraAuthoring
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E physical Rig presentation apply status.")]
    public enum CameraRigPresentationApplyStatus
    {
        None = 0,
        SucceededFixedNoTargets = 10,
        SucceededSingleSubject = 20,
        SucceededGroup = 30,
        SucceededCleared = 40,
        BlockedRequiredSubjectMissing = 100,
        BlockedUnsupportedPresentation = 110,
        RejectedInvalidInput = 120,
        RejectedStaleInput = 130,
        RejectedInvalidSettings = 140,
        RejectedMissingCinemachineCamera = 150,
        RejectedOwnershipConflict = 160
    }
}
