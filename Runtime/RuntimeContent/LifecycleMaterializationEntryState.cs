using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Lifecycle-owned registry state for one materialized runtime content entry.
    /// This state is registry evidence only; it does not instantiate, destroy, pool, unload scenes or bind Content Anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-N lifecycle-owned materialization registry state; evidence only, no physical side effects.")]
    public enum LifecycleMaterializationEntryState
    {
        /// <summary>
        /// Invalid default value. Lifecycle materialization entries must always expose an explicit state.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A materialized runtime content identity is known to be owned by the lifecycle registry.
        /// </summary>
        Active = 10,

        /// <summary>
        /// A lifecycle release was requested for this entry, but release completion has not been recorded yet.
        /// </summary>
        ReleaseRequested = 20,

        /// <summary>
        /// The lifecycle-owned registry recorded that release completed for this entry.
        /// </summary>
        Released = 30,

        /// <summary>
        /// The lifecycle-owned registry recorded that release failed for this entry.
        /// </summary>
        ReleaseFailed = 40
    }
}
