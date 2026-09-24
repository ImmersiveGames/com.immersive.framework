using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Requiredness classification for Activity Content Execution readiness contribution.
    /// Required content may block readiness when execution fails; optional content should remain diagnostic unless a future policy says otherwise.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10B Activity Content Execution requiredness contract.")]
    public enum ActivityContentExecutionRequiredness
    {
        Unknown = 0,
        Optional = 10,
        Required = 20
    }
}
