using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Declares how Activity-owned content scenes are loaded by Activity scene composition.
    /// Current runtime supports Additive Activity content scenes only.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity content scene load modes are consumed by Activity scene composition.")]
    public enum ActivityContentSceneLoadMode
    {
        Additive = 0
    }
}
