using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Passive contract that exposes whether an Actor instance
    /// is ready to participate in view or control boundaries.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49B passive Actor readiness contract.")]
    public interface IActorReadiness
    {
        /// <summary>
        /// Gets whether the Actor can be used as view-facing evidence, such as a camera target,
        /// HUD/status data source or presentation target.
        /// </summary>
        bool IsReadyForView { get; }

        /// <summary>
        /// Gets whether the Actor can receive player control according to policy.
        /// This must never be true while <see cref="IsReadyForView"/> is false.
        /// </summary>
        bool IsReadyForControl { get; }

        /// <summary>
        /// Creates an immutable diagnostic snapshot of the current readiness state.
        /// </summary>
        ActorReadinessSnapshot CreateSnapshot();
    }
}
