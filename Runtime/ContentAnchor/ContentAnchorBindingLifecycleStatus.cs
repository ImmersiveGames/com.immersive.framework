using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for logical Content Anchor binding lifecycle cleanup.
    /// This status reports binding registry cleanup only; it does not perform placement, materialization or physical release.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9D Content Anchor binding lifecycle cleanup status; logical binding registry only.")]
    public enum ContentAnchorBindingLifecycleStatus
    {
        /// <summary>
        /// Invalid default value. Lifecycle cleanup results must always use an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Cleanup executed and removed one or more bindings.
        /// </summary>
        Succeeded = 10,

        /// <summary>
        /// Cleanup executed successfully but no bindings matched the supplied lifecycle key.
        /// </summary>
        SucceededNoBindings = 20,

        /// <summary>
        /// Cleanup was rejected because the supplied runtime owner/scope key was invalid.
        /// </summary>
        RejectedInvalidRuntimeOwner = 100,

        /// <summary>
        /// Cleanup was rejected because the supplied runtime content identity was invalid.
        /// </summary>
        RejectedInvalidRuntimeContent = 110,

        /// <summary>
        /// Cleanup was rejected because the supplied Content Anchor key was invalid.
        /// </summary>
        RejectedInvalidAnchor = 120,

        /// <summary>
        /// Cleanup was rejected because the supplied Content Anchor owner/scope key was invalid.
        /// </summary>
        RejectedInvalidAnchorOwner = 130
    }
}
