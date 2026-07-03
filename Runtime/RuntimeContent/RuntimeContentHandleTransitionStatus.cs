using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Passive transition result status for runtime content handles.
    /// This reports state changes only; it does not execute materialization or release side effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8C runtime content handle transition status; passive diagnostics only.")]
    public enum RuntimeContentHandleTransitionStatus
    {
        /// <summary>
        /// Invalid default value. A transition result must report an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The requested state transition was applied to the handle.
        /// </summary>
        Applied = 10,

        /// <summary>
        /// The requested state transition was not needed because the handle was already in the target state.
        /// </summary>
        IgnoredAlreadyInState = 20,

        /// <summary>
        /// The requested state transition was rejected because it would violate the handle lifecycle.
        /// </summary>
        RejectedInvalidTransition = 30
    }
}
