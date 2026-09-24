using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Diagnostics-only result for applying scene-authored Activity contributions and visibility rules.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct ActivityContentApplyResult
    {
        private ActivityContentApplyResult(int bindingCount,
            int missingActivityCount,
            int invalidBindingCount,
            int requiredInvalidBindingCount,
            int optionalInvalidBindingCount,
            ActivityContentSet activityContentSet,
            ActivityContentLifecycleResult lifecycleResult,
            string message,
            string detailMessage,
            string warningMessage)
        {
            BindingCount = bindingCount;
            MissingActivityCount = missingActivityCount;
            InvalidBindingCount = invalidBindingCount;
            RequiredInvalidBindingCount = requiredInvalidBindingCount;
            OptionalInvalidBindingCount = optionalInvalidBindingCount;
            ActivityContentSet = activityContentSet;
            LifecycleResult = lifecycleResult;
            Message = message ?? string.Empty;
            DetailMessage = detailMessage ?? string.Empty;
            WarningMessage = warningMessage ?? string.Empty;
        }

        public int BindingCount { get; }

        public int MissingActivityCount { get; }

        public int InvalidBindingCount { get; }

        public int RequiredInvalidBindingCount { get; }

        public int OptionalInvalidBindingCount { get; }

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

        public bool HasRequiredInvalidBindings => RequiredInvalidBindingCount > 0;

        public static ActivityContentApplyResult Empty(ActivityAsset activeActivity = null)
        {
            return new ActivityContentApplyResult(0,
                0,
                0,
                0,
                0,
                ActivityContentSet.Empty(activeActivity),
                ActivityContentLifecycleResult.Skipped(null, activeActivity, "Unknown", "None"),
                string.Empty,
                string.Empty,
                string.Empty);
        }

        internal static ActivityContentApplyResult Inspected(
            ActivityAsset previousActivity,
            ActivityAsset activeActivity,
            int bindingCount,
            int missingActivityCount,
            int invalidBindingCount,
            int requiredInvalidBindingCount,
            int optionalInvalidBindingCount,
            string source,
            string reason,
            string detailMessage,
            string warningMessage)
        {
            if (bindingCount <= 0 &&
                missingActivityCount <= 0 &&
                invalidBindingCount <= 0)
            {
                return Empty(activeActivity);
            }

            string target = activeActivity != null
                ? $"for Activity '{activeActivity.ActivityName}'"
                : "with no active Activity";
            string message =
                $"Activity Content inspected {bindingCount} component(s) {target}. " +
                "No Activity Content lifecycle was executed.";
            if (missingActivityCount > 0)
            {
                message += $" missingActivity='{missingActivityCount}'.";
            }

            if (invalidBindingCount > 0)
            {
                message +=
                    $" invalidConfiguration='{invalidBindingCount}' " +
                    $"requiredInvalidContribution='{requiredInvalidBindingCount}' " +
                    $"optionalInvalidContribution='{optionalInvalidBindingCount}'.";
            }

            return new ActivityContentApplyResult(
                bindingCount,
                missingActivityCount,
                invalidBindingCount,
                requiredInvalidBindingCount,
                optionalInvalidBindingCount,
                ActivityContentSet.Empty(activeActivity),
                ActivityContentLifecycleResult.Skipped(
                    previousActivity,
                    activeActivity,
                    source,
                    reason),
                message,
                detailMessage,
                warningMessage);
        }

        public static ActivityContentApplyResult Applied(
            ActivityAsset activeActivity,
            int bindingCount,
            int activatedCount,
            int deactivatedCount,
            int unchangedCount,
            int missingActivityCount,
            int invalidBindingCount,
            int requiredInvalidBindingCount,
            int optionalInvalidBindingCount,
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

            string message = $"Activity Content applied {bindingCount} component(s) {target}. activated='{activatedCount}' deactivated='{deactivatedCount}' unchanged='{unchangedCount}' activityContentHandles='{activityContentSet.Count}'.";
            if (lifecycleResult.Executed)
            {
                message += $" activityContentLifecycle='{lifecycleResult.DiagnosticStatus}' activityContentEnterBindings='{lifecycleResult.EnterBindingCount}' activityContentEnterReceivers='{lifecycleResult.EnterReceiverCount}' activityContentEnterFailed='{lifecycleResult.EnterFailedReceiverCount}' activityContentExitBindings='{lifecycleResult.ExitBindingCount}' activityContentExitReceivers='{lifecycleResult.ExitReceiverCount}' activityContentExitFailed='{lifecycleResult.ExitFailedReceiverCount}'.";
            }

            if (missingActivityCount > 0)
            {
                message += $" missingActivity='{missingActivityCount}'.";
            }

            if (invalidBindingCount > 0)
            {
                message += $" invalidConfiguration='{invalidBindingCount}' requiredInvalidContribution='{requiredInvalidBindingCount}' optionalInvalidContribution='{optionalInvalidBindingCount}'.";
            }

            return new ActivityContentApplyResult(bindingCount,
                missingActivityCount,
                invalidBindingCount,
                requiredInvalidBindingCount,
                optionalInvalidBindingCount,
                activityContentSet,
                lifecycleResult,
                message,
                detailMessage,
                warningMessage);
        }
    }
}
