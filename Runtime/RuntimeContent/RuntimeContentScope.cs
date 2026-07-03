using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Declares the lifecycle scope that owns runtime-created content.
    /// This is not a hierarchy lookup key and does not imply Content Anchor placement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8B runtime ownership primitive; no root registry or materialization behavior yet.")]
    public enum RuntimeContentScope
    {
        /// <summary>
        /// Invalid default value. Runtime-created content must always declare an explicit owner scope.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Owned by the current framework session/runtime lifetime.
        /// </summary>
        Session = 10,

        /// <summary>
        /// Owned by the currently active Route lifetime.
        /// </summary>
        Route = 20,

        /// <summary>
        /// Owned by the currently active Activity lifetime.
        /// </summary>
        Activity = 30,

        /// <summary>
        /// Owned by an explicit runtime request and released explicitly by its handle/policy.
        /// </summary>
        Transient = 40
    }
}
