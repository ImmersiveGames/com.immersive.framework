using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>Read-only facts for one committed Activity transition in a Route.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route-scoped observation of committed Activity transitions.")]
    public readonly struct RouteActivityTransitionContext
    {
        internal RouteActivityTransitionContext(
            ActivityAsset previousActivity,
            ActivityAsset currentActivity,
            string source,
            string reason)
        {
            PreviousActivity = previousActivity;
            CurrentActivity = currentActivity;
            Source = source;
            Reason = reason;
        }

        public ActivityAsset PreviousActivity { get; }
        public ActivityAsset CurrentActivity { get; }
        public string Source { get; }
        public string Reason { get; }
    }
}
