using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive readiness status for Loading observations.
    /// It reports whether a prerequisite is ready; it does not mutate Activity readiness or gameplay state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22G Loading readiness status primitive; observation only.")]
    public enum LoadingReadinessStatus
    {
        Unknown = 0,
        NotObserved = 10,
        Waiting = 20,
        Ready = 30,
        Blocked = 40,
        Failed = 50,
        Skipped = 60
    }
}
