using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Logical phase in a transition operation narrative.
    /// Phases are observations around existing lifecycle systems; they are not a replacement lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B Transition phase primitive; orchestration diagnostics only.")]
    public enum TransitionPhase
    {
        /// <summary>Invalid default value. Do not use for canonical transition steps.</summary>
        Unknown = 0,

        Planned = 10,
        RequestAdmitted = 20,
        OperationOpened = 30,
        GateBlockApplied = 40,
        PreviousScopeExitObserved = 50,
        ContentReleaseObserved = 60,
        SceneOrContentOperationObserved = 70,
        NextScopeEnterObserved = 80,
        ReadinessObserved = 90,
        GateBlockReleased = 100,
        OperationClosed = 110
    }
}
