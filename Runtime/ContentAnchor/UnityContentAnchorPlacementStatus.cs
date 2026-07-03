using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Unity adapter-side status vocabulary for physical Content Anchor placement.
    /// This status is outside logical ContentAnchor binding and does not materialize or release runtime content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-B Unity Content Anchor physical placement adapter proof status; no materialization, release, pooling or Addressables.")]
    public enum UnityContentAnchorPlacementStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededAlreadyPlaced = 20,
        FailedInvalidBinding = 100,
        FailedMissingPhysicalEvidence = 110,
        RejectedMismatchedRuntimeIdentity = 120,
        RejectedReleasedPhysicalEvidence = 130,
        FailedMissingInstance = 140,
        FailedMissingAnchorTransform = 150,
        FailedPlacementAdapter = 160
    }
}
