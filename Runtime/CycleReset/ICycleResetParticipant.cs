using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Core participant contract for Cycle Reset.
    /// Implementations own their local reset behavior; the framework core only orchestrates request, ordering, result aggregation and diagnostics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset participant contract; no object/player/gameplay reset contract.")]
    public interface ICycleResetParticipant
    {
        /// <summary>
        /// Returns passive identity, scope, requiredness and ordering data.
        /// This method must not reset, release, reload, instantiate, destroy or restore gameplay state.
        /// </summary>
        CycleResetParticipantDescriptor GetCycleResetDescriptor();

        /// <summary>
        /// Executes one Cycle Reset dispatch for this participant and returns diagnostics.
        /// The participant decides how to reset; the core validates and aggregates the result.
        /// </summary>
        CycleResetParticipantResult ResetCycle(CycleResetContext context);
    }
}
