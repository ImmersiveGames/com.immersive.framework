using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Stable. Declares how Activity-owned content scenes are loaded by Activity scene composition.
    /// Current runtime supports Additive Activity content scenes only.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.")]
    public enum ActivityContentSceneLoadMode
    {
        Additive = 0
    }
}
