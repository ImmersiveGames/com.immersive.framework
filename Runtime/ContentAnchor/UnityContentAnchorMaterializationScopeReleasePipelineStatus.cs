using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for explicit Unity ContentAnchor materialization scope release proof.
    /// This reports adapter-side physical release plus logical RuntimeContent/ContentAnchor cleanup; it is not automatic lifecycle wiring.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-D ContentAnchor materialization scope release pipeline status; explicit proof, no automatic Route/Activity lifecycle wiring.")]
    internal enum UnityContentAnchorMaterializationScopeReleasePipelineStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededNoContent = 20,
        FailedMissingRuntimeHost = 100,
        FailedMissingRuntimeContentRuntime = 110,
        FailedInvalidContext = 120,
        FailedPhysicalRelease = 130,
        FailedLogicalRelease = 140,
        FailedBindingCleanup = 150
    }
}
