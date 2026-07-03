using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Status vocabulary for the authored, opt-in Unity ContentAnchor materialization bridge.
    /// This is an explicit scene-facing bridge result and not lifecycle automation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-E authored ContentAnchor materialization bridge proof status; no automatic lifecycle wiring or consumer ownership.")]
    public enum UnityContentAnchorMaterializationBridgeStatus
    {
        Unknown = 0,
        SucceededMaterialized = 10,
        SucceededReleased = 20,
        SucceededReleaseNoContent = 30,
        FailedRuntimeUnavailable = 100,
        FailedRuntimeContentRuntimeUnavailable = 110,
        FailedConfiguration = 120,
        FailedScopeRoot = 130,
        FailedScopeContext = 140,
        FailedAnchorSet = 150,
        FailedMaterializationPipeline = 160,
        FailedScopeRelease = 170
    }
}
