using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for explicit lifecycle-owned release plan execution.
    /// Statuses report delegated release execution and lifecycle registry state changes only; they do not imply Route/Activity auto-release, Unity object destruction or ContentAnchor binding cleanup.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-Q explicit lifecycle materialization release execution status; no lifecycle auto-wiring.")]
    public enum LifecycleMaterializationReleaseExecutionStatus
    {
        /// <summary>
        /// Invalid default value. Execution results must return an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The release plan had no requests and no mutation was required.
        /// </summary>
        SucceededNoRequests = 10,

        /// <summary>
        /// Every planned request was released successfully and every lifecycle registry entry was marked released.
        /// </summary>
        SucceededReleasedAll = 20,

        /// <summary>
        /// Execution ran, but one or more requests failed or could not be reflected back into the lifecycle registry.
        /// </summary>
        FailedPartialRelease = 100,

        /// <summary>
        /// Execution was rejected because the supplied plan was not a valid lifecycle materialization release plan.
        /// </summary>
        FailedInvalidPlan = 110
    }
}
