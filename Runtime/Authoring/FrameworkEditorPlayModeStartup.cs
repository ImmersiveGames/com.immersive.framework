using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Editor-only startup behavior for entering Play Mode while developing scenes.
    /// Player/runtime builds always use FrameworkStartup.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public enum FrameworkEditorPlayModeStartup
    {
        FrameworkStartup = 0,
        CurrentSceneOnly = 1
    }
}
