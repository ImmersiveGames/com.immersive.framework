using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Core contract for a logical Activity Content Execution participant.
    /// Implementations may describe and execute Enter/Exit requests, but discovery, ordering, aggregation and lifecycle integration are defined by future F10 cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10D Activity Content Execution participant contract; no discovery runtime or Unity side effects.")]
    public interface IActivityContentExecutionParticipant
    {
        /// <summary>
        /// Returns passive identity, requiredness, phase support and diagnostic ordering for this participant.
        /// This method must not materialize, destroy, reset or mutate gameplay state.
        /// </summary>
        ActivityContentExecutionParticipantDescriptor GetActivityContentExecutionDescriptor();

        /// <summary>
        /// Executes one logical Activity Content Execution request and returns a passive result.
        /// Implementations own their local behavior; the framework core only consumes the returned diagnostics in future cuts.
        /// </summary>
        ActivityContentExecutionResult ExecuteActivityContent(ActivityContentExecutionRequest request);
    }
}
