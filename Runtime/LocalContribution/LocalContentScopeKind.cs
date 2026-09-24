using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Experimental. Classifies the local contribution scope inside a known content owner.
    /// This is not a target id, GameObject name, scene name or hierarchy path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Local contribution scope kind introduced by F5B.")]
    public enum LocalContentScopeKind
    {
        /// <summary>
        /// Invalid default value. Local content identity must always declare an explicit local scope kind.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Local contribution authored directly in a scene and discovered through a known content scope.
        /// </summary>
        SceneAuthored = 10
    }
}
