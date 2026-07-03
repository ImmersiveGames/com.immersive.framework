using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Declares whether a lifecycle materialization release plan targets one owner or a whole scope.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-P lifecycle materialization release plan target kind.")]
    public enum LifecycleMaterializationReleasePlanTargetKind
    {
        /// <summary>
        /// Invalid default value. Release plans must target an explicit owner or scope.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The plan was built for one typed RuntimeContentOwner.
        /// </summary>
        Owner = 10,

        /// <summary>
        /// The plan was built for all owners inside one RuntimeContentScope.
        /// </summary>
        Scope = 20
    }
}
