using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free declaration of what an Activity operation would do to a scene.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation scene action; no scene side effects are executed by this value.")]
    internal enum ActivityOperationSceneAction
    {
        None = 0,
        Load = 10,
        Release = 20
    }
}
