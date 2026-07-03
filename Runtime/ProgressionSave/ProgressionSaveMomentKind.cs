using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Logical moment categories for Progression Save requests.
    /// These values describe intent only; they do not schedule autosave, bind lifecycle events or call a backend by themselves.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save moment contract; no automatic autosave execution.")]
    public enum ProgressionSaveMomentKind
    {
        Unknown = 0,
        Manual = 10,
        Autosave = 20,
        Checkpoint = 30,
        RouteBoundary = 40,
        ActivityBoundary = 50
    }
}
