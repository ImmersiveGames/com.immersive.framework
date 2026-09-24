using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal sealed class ActivityEnteredEvent : IEvent
    {
        public ActivityEnteredEvent(ActivityAsset activity, ActivityAsset previousActivity, string source, string reason)
        {
            Activity = activity;
            PreviousActivity = previousActivity;
            Source = source.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
        }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public string Source { get; }

        public string Reason { get; }
    }
}
