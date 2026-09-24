using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Declares the lifecycle cycle targeted by one Cycle Reset request.
    /// This is core lifecycle scope only; it is not object, component, player, actor, pool or save scope.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset scope primitive; Route/Activity cycle reset only.")]
    public enum CycleResetScope
    {
        /// <summary>
        /// Invalid default value. Cycle Reset requests must always declare Route or Activity explicitly.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Reset the currently active Route cycle. Policy may include the currently active Activity.
        /// </summary>
        Route = 10,

        /// <summary>
        /// Reset the currently active Activity cycle only.
        /// </summary>
        Activity = 20
    }
}
