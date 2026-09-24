using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-B Camera Presentation prefab materialization status.")]
    internal enum CameraPresentationMaterializationStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededAlreadyReleased = 20,
        RejectedInvalidContext = 100,
        RejectedInvalidDefinition = 110,
        RejectedDuplicateOccurrence = 120,
        RejectedScopeTransition = 130,
        FailedInstantiate = 200,
        FailedInvalidRigInstance = 210,
        FailedRuntimeContentRegistration = 220,
        FailedPresentationTeardown = 300,
        FailedRuntimeContentRelease = 310
    }
}
