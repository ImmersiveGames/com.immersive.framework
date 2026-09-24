using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Result status for lifecycle-owned materialization registry operations.
    /// Statuses report registry state changes only; they do not imply Unity object destruction or lifecycle auto-wiring.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-N lifecycle-owned materialization registry operation status; registry-only contract.")]
    public enum LifecycleMaterializationRegistryOperationStatus
    {
        /// <summary>
        /// Invalid default value. Registry operations must return an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A materialized entry was registered in the lifecycle-owned registry.
        /// </summary>
        SucceededRegistered = 10,

        /// <summary>
        /// The same materialized entry was already registered and no mutation was required.
        /// </summary>
        SucceededAlreadyRegistered = 20,

        /// <summary>
        /// Release was requested for a registered entry.
        /// </summary>
        SucceededReleaseRequested = 30,

        /// <summary>
        /// Release completion was recorded for a registered entry.
        /// </summary>
        SucceededReleased = 40,

        /// <summary>
        /// Release failure was recorded for a registered entry.
        /// </summary>
        SucceededReleaseFailedRecorded = 50,

        /// <summary>
        /// Registration was rejected because an entry already exists for the runtime content identity.
        /// </summary>
        RejectedDuplicateEntry = 100,

        /// <summary>
        /// Operation was rejected because no entry exists for the requested runtime content identity.
        /// </summary>
        RejectedMissingEntry = 110,

        /// <summary>
        /// Operation was rejected because the supplied handle does not match the requested identity.
        /// </summary>
        RejectedMismatchedHandle = 120,

        /// <summary>
        /// Operation was rejected because the current registry state does not allow the requested transition.
        /// </summary>
        RejectedInvalidTransition = 130
    }
}
