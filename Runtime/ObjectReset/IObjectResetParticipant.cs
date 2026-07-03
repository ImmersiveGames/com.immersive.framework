using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Core participant contract for Object Reset.
    /// Implementations own their local reset behavior; the framework core only resolves targets, orders execution, aggregates results and emits diagnostics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C Object Reset participant contract; no Unity baseline adapter yet.")]
    public interface IObjectResetParticipant
    {
        /// <summary>
        /// Returns passive identity, target, requiredness and ordering data.
        /// This method must not reset, release, reload, instantiate, destroy, find objects globally or restore gameplay state.
        /// </summary>
        ObjectResetParticipantDescriptor GetObjectResetDescriptor();

        /// <summary>
        /// Executes one Object Reset dispatch for this participant and returns diagnostics.
        /// The participant decides how to reset its local state; the core validates and aggregates the result.
        /// </summary>
        ObjectResetParticipantResult ResetObject(ObjectResetContext context);
    }
}
