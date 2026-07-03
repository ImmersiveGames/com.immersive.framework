using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Result status for lifecycle-owned materialization release plan creation.
    /// Statuses describe plan construction only; they do not execute release, destroy objects, unregister handles or clean ContentAnchor bindings.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-P lifecycle materialization release plan status; planning only, no release execution.")]
    public enum LifecycleMaterializationReleasePlanStatus
    {
        /// <summary>
        /// Invalid default value. Release plan creation must return an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// At least one release request candidate was planned.
        /// </summary>
        SucceededPlanned = 10,

        /// <summary>
        /// The registry query succeeded, but no entries currently require release.
        /// </summary>
        SucceededEmpty = 20
    }
}
