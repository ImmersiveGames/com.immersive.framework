using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for runtime release contracts and logical release execution.
    /// It reports release outcome only; it does not execute physical cleanup.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8J runtime release status vocabulary; logical handle/registry release only.")]
    public enum RuntimeReleaseStatus
    {
        /// <summary>
        /// Invalid default value. Release results must always use an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Release succeeded and the handle reached Released state.
        /// </summary>
        Succeeded = 10,

        /// <summary>
        /// The handle was already Released. The request is treated as an idempotent success.
        /// </summary>
        SucceededAlreadyReleased = 20,

        /// <summary>
        /// The request was invalid before release could execute.
        /// </summary>
        FailedInvalidRequest = 100,

        /// <summary>
        /// The owner root required by the request was missing.
        /// </summary>
        FailedMissingRoot = 110,

        /// <summary>
        /// The handle required by the request was not registered in its owner root.
        /// </summary>
        FailedMissingHandle = 120,

        /// <summary>
        /// The requested identity does not belong to the supplied scope context.
        /// </summary>
        RejectedMismatchedIdentity = 130,

        /// <summary>
        /// The handle state transition was not allowed.
        /// </summary>
        RejectedInvalidTransition = 140,

        /// <summary>
        /// A physical adapter reported release failure before the core applied logical release state.
        /// </summary>
        FailedReleaseAdapter = 150,

        /// <summary>
        /// The handle reached Released state but could not be unregistered from the logical scope root.
        /// </summary>
        FailedUnregister = 160
    }
}
