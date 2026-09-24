using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Logical kind of flow transition being narrated by the framework.
    /// This does not select a visual effect, loading screen, curtain or scene loader.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B Transition kind primitive; orchestration category only.")]
    public enum TransitionKind
    {
        /// <summary>Invalid default value. Do not use for canonical transition plans or results.</summary>
        Unknown = 0,

        SessionStartup = 10,
        RouteStartup = 20,
        RouteSwitch = 30,
        ActivityStartup = 40,
        ActivitySwitch = 50,
        ActivityClear = 60,
        SceneComposition = 70,
        ContentOperation = 80,
        RuntimeScopeChange = 90
    }
}
