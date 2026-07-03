using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Role of a scene inside a Route scene composition plan.
    /// This is planning vocabulary only; execution starts in later F6 cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6B inert route scene composition planning vocabulary.")]
    internal enum RouteSceneRole
    {
        Unknown = 0,
        Primary = 10,
        Additive = 20
    }
}
