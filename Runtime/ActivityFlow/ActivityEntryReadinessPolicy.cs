using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Activity-owned policy describing how initial occurrence readiness controls
    /// visual reveal and capability release during Activity entry.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-007 Activity entry-readiness authoring policy.")]
    public enum ActivityEntryReadinessPolicy
    {
        /// <summary>
        /// Preserves post-transition observation. Readiness may complete or fail after
        /// the normal transition and operation capability gate have been released.
        /// </summary>
        ObserveOnly = 0,

        /// <summary>
        /// Keeps the target visually covered and keeps input, interaction and gameplay
        /// blocked until the initial readiness occurrence reaches Ready.
        /// </summary>
        WaitCovered = 10,

        /// <summary>
        /// Reveals the target after materialization but keeps input, interaction and
        /// gameplay blocked until the initial readiness occurrence reaches Ready.
        /// </summary>
        WaitVisible = 20
    }
}
