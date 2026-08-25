using Immersive.Framework.Authoring;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Local context passed to scene-authored Activity content receivers.
    /// This describes one Activity content root reacting to the canonical Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public readonly struct ActivityContentLifecycleContext
    {
        internal ActivityContentLifecycleContext(
            ActivityContentLifecyclePhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            ActivityContentContribution contribution,
            GameObject contentRoot,
            string source,
            string reason)
        {
            Phase = phase;
            Activity = activity;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Contribution = contribution;
            ContentRoot = contentRoot;
            Source = source.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
        }

        public ActivityContentLifecyclePhase Phase { get; }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset NextActivity { get; }

        public ActivityContentContribution Contribution { get; }

        public GameObject ContentRoot { get; }

        public string Source { get; }

        public string Reason { get; }

        public string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        public string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        public string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public bool IsEntered => Phase == ActivityContentLifecyclePhase.Entered;

        public bool IsExited => Phase == ActivityContentLifecyclePhase.Exited;

        internal static ActivityContentLifecycleContext Entered(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityContentContribution contribution,
            string source,
            string reason)
        {
            return new ActivityContentLifecycleContext(
                ActivityContentLifecyclePhase.Entered,
                activity,
                previousActivity,
                activity,
                contribution,
                contribution != null ? contribution.gameObject : null,
                source,
                reason);
        }

        internal static ActivityContentLifecycleContext Exited(
            ActivityAsset activity,
            ActivityAsset nextActivity,
            ActivityContentContribution contribution,
            string source,
            string reason)
        {
            return new ActivityContentLifecycleContext(
                ActivityContentLifecyclePhase.Exited,
                activity,
                activity,
                nextActivity,
                contribution,
                contribution != null ? contribution.gameObject : null,
                source,
                reason);
        }
    }
}
