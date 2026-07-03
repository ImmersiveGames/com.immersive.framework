using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive status for a Loading operation observation.
    /// It reports state; it does not execute SceneLifecycle, Transition, UI or gameplay readiness.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B Loading operation status primitive.")]
    public enum LoadingOperationStatus
    {
        Unknown = 0,
        Pending = 10,
        Running = 20,
        Completed = 30,
        Failed = 40,
        Canceled = 50
    }
}
