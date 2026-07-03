using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for Content Anchor binding contracts.
    /// This reports logical binding outcomes only; it does not materialize prefabs, move transforms or release physical content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9A Content Anchor binding status vocabulary; no physical placement implementation.")]
    public enum ContentAnchorBindingStatus
    {
        /// <summary>
        /// Invalid default value. Binding results must always use an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Binding succeeded and produced a Content Anchor content handle.
        /// </summary>
        Succeeded = 10,

        /// <summary>
        /// The requested binding already existed and the existing handle is still valid.
        /// </summary>
        SucceededAlreadyBound = 20,

        /// <summary>
        /// The binding request was invalid before resolution could execute.
        /// </summary>
        FailedInvalidRequest = 100,

        /// <summary>
        /// No Content Anchor matching the request identity was available.
        /// </summary>
        FailedMissingAnchor = 110,

        /// <summary>
        /// The resolved Content Anchor did not match the explicit request identity/kind.
        /// </summary>
        RejectedMismatchedAnchor = 120,

        /// <summary>
        /// The runtime content handle supplied to the binding did not match the request identity.
        /// </summary>
        RejectedMismatchedRuntimeContent = 130,

        /// <summary>
        /// The runtime content handle was already released and cannot be bound.
        /// </summary>
        RejectedReleasedRuntimeContent = 140,

        /// <summary>
        /// Runtime materialization failed before a binding handle could be created.
        /// </summary>
        FailedRuntimeMaterialization = 150,

        /// <summary>
        /// Runtime registry/handle state failed before a binding handle could be created.
        /// </summary>
        FailedRuntimeRegistration = 160
    }
}
