using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Aggregate status for a passive Loading progress observation.
    /// It reports combined LoadingStep state only; it does not execute SceneLifecycle, Transition, UI or readiness mutation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22C Loading progress aggregation status; observation-only.")]
    public enum LoadingProgressAggregationStatus
    {
        Unknown = 0,
        NoSteps = 10,
        Running = 20,
        Completed = 30,
        CompletedWithSkippedSteps = 40,
        Failed = 50,
        Canceled = 60
    }
}
