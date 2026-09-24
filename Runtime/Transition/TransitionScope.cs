using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Framework lifecycle surface covered by a Transition request.
    /// This is orchestration metadata only; it does not own Route, Activity, scene or visual lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24B Transition request scope; no visual or lifecycle ownership.")]
    public enum TransitionScope
    {
        Unknown = 0,

        Startup = 10,
        Route = 20,
        Activity = 30,
        ActivityClear = 40
    }
}
