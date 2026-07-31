using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Editor-only startup behavior for entering Play Mode while developing scenes.
    /// Player/runtime builds always use FrameworkStartup.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.")]
    public enum FrameworkEditorPlayModeStartup
    {
        FrameworkStartup = 0,
        CurrentSceneOnly = 1
    }
}
