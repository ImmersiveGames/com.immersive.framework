using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for runtime materialization contracts.
    /// It records request/result outcome only; it does not execute materialization, destroy objects or bind Content Anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8H runtime materialization result status; adds scoped transition/cancellation rejection statuses, no materializer implementation.")]
    public enum RuntimeMaterializationStatus
    {
        /// <summary>
        /// Invalid default value. Materialization results must always use an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Runtime content was materialized and a handle was produced.
        /// </summary>
        Succeeded = 10,

        /// <summary>
        /// The request was invalid before a materializer could execute.
        /// </summary>
        FailedInvalidRequest = 100,

        /// <summary>
        /// The owner root required by the request was missing.
        /// </summary>
        FailedMissingRoot = 110,

        /// <summary>
        /// The materializer produced no valid handle.
        /// </summary>
        FailedInvalidHandle = 120,

        /// <summary>
        /// The produced handle did not match the request owner or identity.
        /// </summary>
        RejectedMismatchedIdentity = 130,

        /// <summary>
        /// The produced handle could not be registered in the runtime root registry.
        /// </summary>
        FailedRegistration = 140,

        /// <summary>
        /// The concrete materializer failed before producing a valid handle.
        /// </summary>
        FailedMaterializer = 150,

        /// <summary>
        /// The owner scope was not active when the request was created or consumed.
        /// </summary>
        RejectedScopeTransition = 160,

        /// <summary>
        /// The owner scope has requested cancellation, usually because Route or Activity exit started.
        /// </summary>
        RejectedScopeCancellation = 170,

        /// <summary>
        /// The request captured an older scope token and is stale for the current runtime scope version.
        /// </summary>
        RejectedStaleScope = 180
    }
}
