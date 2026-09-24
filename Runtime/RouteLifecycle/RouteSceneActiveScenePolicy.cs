using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Policy that describes which scene should remain active after Route scene composition.
    /// F6E currently preserves Primary Scene as the active scene after composition.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Route scene composition planning vocabulary consumed by F6E.")]
    internal enum RouteSceneActiveScenePolicy
    {
        Unknown = 0,
        PrimarySceneActive = 10
    }
}
