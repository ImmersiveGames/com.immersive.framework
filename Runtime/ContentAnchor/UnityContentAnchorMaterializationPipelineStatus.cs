using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Status vocabulary for the explicit Unity RuntimeContent + ContentAnchor composition proof.
    /// It reports orchestration state only; materialization, logical binding and placement remain owned by their explicit adapters/runtimes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-C Unity RuntimeContent/ContentAnchor materialization pipeline proof status; no consumer ownership.")]
    public enum UnityContentAnchorMaterializationPipelineStatus
    {
        Unknown = 0,
        Succeeded = 10,
        FailedMissingRuntimeHost = 100,
        FailedMissingRuntimeContentRuntime = 110,
        FailedMissingAnchorTransform = 120,
        FailedMaterializationRequest = 130,
        FailedMaterialization = 140,
        FailedMaterializationApply = 150,
        FailedMissingPhysicalEvidence = 160,
        FailedLogicalBinding = 170,
        FailedPhysicalPlacement = 180,
        FailedRollbackPhysicalRelease = 190,
        FailedRollbackLogicalRelease = 200
    }
}
