using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Status vocabulary for the authored, explicit ContentAnchor materialization bridge set.
    /// This set batches bridge submissions and rolls back already materialized entries when a later bridge fails, without adding Route/Activity lifecycle automation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-F authored ContentAnchor bridge set proof status; explicit batch submit/release only, no automatic lifecycle wiring.")]
    public enum UnityContentAnchorMaterializationBridgeSetStatus
    {
        Unknown = 0,
        SucceededMaterializedAll = 10,
        SucceededReleasedAll = 20,
        SucceededReleaseNoContent = 30,
        FailedNoBridges = 100,
        FailedNullBridge = 110,
        FailedDuplicateBridge = 120,
        FailedAuthoringValidation = 122,
        FailedBridgeMaterializationPreflight = 125,
        FailedDuplicateMaterializationKey = 126,
        FailedBridgeMaterialization = 130,
        FailedBridgeMaterializationRolledBack = 135,
        FailedBridgeMaterializationRollbackFailed = 136,
        FailedBridgeRelease = 140
    }
}
