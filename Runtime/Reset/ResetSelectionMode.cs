using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Built-in subject selection modes for ResetExecutor requests.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12D Reset subject selection modes independent from ObjectEntry.")]
    public enum ResetSelectionMode
    {
        Unknown = 0,
        ExplicitSubjects = 10,
        CurrentActivitySubjects = 20,
        CurrentRouteSubjects = 30,
        CurrentRouteAndActivitySubjects = 40,
        AllCurrentSubjects = 50,
        RuntimeOnlySubjects = 60,
        SceneOnlySubjects = 70
    }
}
