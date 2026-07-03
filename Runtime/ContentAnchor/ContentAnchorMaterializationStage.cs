using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Stage vocabulary for the reusable RuntimeContent + ContentAnchor materialization service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "MAT-4 failed-stage vocabulary for ContentAnchor materialization service diagnostics.")]
    internal enum ContentAnchorMaterializationStage
    {
        None = 0,
        RuntimeHost = 10,
        RuntimeContentRuntime = 20,
        AnchorTransform = 30,
        MaterializationRequest = 40,
        PhysicalMaterialization = 50,
        RuntimeContentApply = 60,
        MaterializedEvidence = 70,
        LogicalBinding = 80,
        PhysicalPlacement = 90
    }
}
