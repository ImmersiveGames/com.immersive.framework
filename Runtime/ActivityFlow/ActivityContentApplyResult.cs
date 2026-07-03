using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Diagnostics-only result for applying scene-authored Activity content bindings.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct ActivityContentApplyResult
    {
        private ActivityContentApplyResult(int bindingCount,
            int missingActivityCount,
            ActivityContentSet activityContentSet,
            ActivityContentLifecycleResult lifecycleResult,
            string message,
            string detailMessage,
            string warningMessage)
        {
            BindingCount = bindingCount;
            MissingActivityCount = missingActivityCount;
            ActivityContentSet = activityContentSet;
            LifecycleResult = lifecycleResult;
            Message = message ?? string.Empty;
            DetailMessage = detailMessage ?? string.Empty;
            WarningMessage = warningMessage ?? string.Empty;
        }

        public int BindingCount { get; }

        public int MissingActivityCount { get; }

        public ActivityContentSet ActivityContentSet { get; }

        public ActivityContentLifecycleResult LifecycleResult { get; }

        public int ActivityContentCount => ActivityContentSet.Count;

        public string Message { get; }

        public string DetailMessage { get; }

        public string WarningMessage { get; }

        public bool HasBindings => BindingCount > 0;

        public bool HasLifecycleFailures => LifecycleResult.HasFailures;

        public bool HasDetailMessage => !string.IsNullOrWhiteSpace(DetailMessage);

        public bool HasWarningMessage => !string.IsNullOrWhiteSpace(WarningMessage);

        public static ActivityContentApplyResult Empty(ActivityAsset activeActivity = null)
        {
            return new ActivityContentApplyResult(0,
                0,
                ActivityContentSet.Empty(activeActivity),
                ActivityContentLifecycleResult.Skipped(null, activeActivity, "Unknown", "None"),
                string.Empty,
                string.Empty,
                string.Empty);
        }

        public static ActivityContentApplyResult Applied(
            ActivityAsset activeActivity,
            int bindingCount,
            int activatedCount,
            int deactivatedCount,
            int unchangedCount,
            int missingActivityCount,
            ActivityContentSet activityContentSet,
            ActivityContentLifecycleResult lifecycleResult,
            string detailMessage,
            string warningMessage)
        {
            if (bindingCount <= 0)
            {
                return Empty(activeActivity);
            }

            string target = activeActivity != null
                ? $"for Activity '{activeActivity.ActivityName}'"
                : "with no active Activity";

            string message = $"Activity Content applied {bindingCount} binding(s) {target}. activated='{activatedCount}' deactivated='{deactivatedCount}' unchanged='{unchangedCount}' activityContentHandles='{activityContentSet.Count}'.";
            if (lifecycleResult.Executed)
            {
                message += $" activityContentLifecycle='{lifecycleResult.DiagnosticStatus}' activityContentEnterBindings='{lifecycleResult.EnterBindingCount}' activityContentEnterReceivers='{lifecycleResult.EnterReceiverCount}' activityContentEnterFailed='{lifecycleResult.EnterFailedReceiverCount}' activityContentExitBindings='{lifecycleResult.ExitBindingCount}' activityContentExitReceivers='{lifecycleResult.ExitReceiverCount}' activityContentExitFailed='{lifecycleResult.ExitFailedReceiverCount}'.";
            }

            if (missingActivityCount > 0)
            {
                message += $" missingActivity='{missingActivityCount}'.";
            }

            return new ActivityContentApplyResult(bindingCount,
                missingActivityCount,
                activityContentSet,
                lifecycleResult,
                message,
                detailMessage,
                warningMessage);
        }
    }
}
