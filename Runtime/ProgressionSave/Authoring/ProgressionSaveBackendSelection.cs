using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// Authored Progression Save backend selection.
    /// Built-in JSON and Custom Provider are explicit choices; neither is a fallback
    /// for the other.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-C authored Progression Save backend selection.")]
    public enum ProgressionSaveBackendSelection
    {
        Unknown = 0,
        BuiltInJson = 10,
        CustomProvider = 20
    }
}
