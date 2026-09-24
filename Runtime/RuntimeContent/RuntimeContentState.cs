using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Minimal lifecycle state vocabulary for runtime-created content.
    /// F8B only defines the vocabulary; handles and release execution are introduced by later F8 cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8B runtime content state primitive; passive vocabulary only.")]
    public enum RuntimeContentState
    {
        /// <summary>
        /// Invalid default value. A valid runtime content record must expose a concrete state.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Identity and ownership are known, but no concrete instance has been produced by a materializer yet.
        /// </summary>
        Declared = 10,

        /// <summary>
        /// A concrete runtime instance exists and is expected to remain owned by its declared scope.
        /// </summary>
        Materialized = 20,

        /// <summary>
        /// Release was requested and is in progress or awaiting execution by the owning policy.
        /// </summary>
        ReleaseRequested = 30,

        /// <summary>
        /// The runtime instance has been released. Later releases must be treated as double-release diagnostics.
        /// </summary>
        Released = 40,

        /// <summary>
        /// Release was attempted but failed. Later cuts decide how this is surfaced in result diagnostics.
        /// </summary>
        ReleaseFailed = 50
    }
}
