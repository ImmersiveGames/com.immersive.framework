using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Passive lifecycle state for runtime scope transition guards.
    /// It is used to reject materialization against scopes that are exiting, cancelled or already removed.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8H runtime scope transition state for scoped materialization guardrails; no async execution or release behavior.")]
    public enum RuntimeScopeTransitionState
    {
        /// <summary>
        /// Invalid default value. Guarded scopes must always report an explicit transition state.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The scope is active and may accept new runtime materialization requests.
        /// </summary>
        Active = 10,

        /// <summary>
        /// The scope has started exiting. New materialization must be rejected.
        /// </summary>
        CancellationRequested = 20,

        /// <summary>
        /// The scope was removed from the logical runtime root registry. Requests from older tokens are stale.
        /// </summary>
        Removed = 30
    }
}
