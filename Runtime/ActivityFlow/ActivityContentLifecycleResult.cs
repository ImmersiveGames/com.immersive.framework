using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Diagnostics-only aggregate result for Activity Content local lifecycle callback dispatch.
    /// This records enter/exit callback execution; it does not own content, release content, load profiles, load scenes, or materialize runtime objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline diagnostics surface introduced by F4C for Activity Content callback execution.")]
    internal readonly struct ActivityContentLifecycleResult
    {
        public ActivityContentLifecycleResult(
            ActivityAsset previousActivity,
            ActivityAsset activeActivity,
            bool executed,
            int enterBindingCount,
            int enterReceiverCount,
            int enterFailedReceiverCount,
            int exitBindingCount,
            int exitReceiverCount,
            int exitFailedReceiverCount,
            string source,
            string reason)
        {
            PreviousActivity = previousActivity;
            ActiveActivity = activeActivity;
            Executed = executed;
            EnterBindingCount = enterBindingCount;
            EnterReceiverCount = enterReceiverCount;
            EnterFailedReceiverCount = enterFailedReceiverCount;
            ExitBindingCount = exitBindingCount;
            ExitReceiverCount = exitReceiverCount;
            ExitFailedReceiverCount = exitFailedReceiverCount;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset ActiveActivity { get; }

        public bool Executed { get; }

        public int EnterBindingCount { get; }

        public int EnterReceiverCount { get; }

        public int EnterFailedReceiverCount { get; }

        public int ExitBindingCount { get; }

        public int ExitReceiverCount { get; }

        public int ExitFailedReceiverCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool HasEnter => EnterBindingCount > 0;

        public bool HasExit => ExitBindingCount > 0;

        public bool HasReceivers => EnterReceiverCount > 0 || ExitReceiverCount > 0;

        public bool HasFailures => EnterFailedReceiverCount > 0 || ExitFailedReceiverCount > 0;

        public string DiagnosticStatus => Executed ? "Executed" : "Skipped";

        public string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        public string ActiveActivityName => ActiveActivity != null ? ActiveActivity.ActivityName : string.Empty;

        public static ActivityContentLifecycleResult Skipped(
            ActivityAsset previousActivity,
            ActivityAsset activeActivity,
            string source,
            string reason)
        {
            return new ActivityContentLifecycleResult(
                previousActivity,
                activeActivity,
                false,
                0,
                0,
                0,
                0,
                0,
                0,
                NormalizeSource(source),
                NormalizeReason(reason));
        }

        public static ActivityContentLifecycleResult ExecutedWith(
            ActivityAsset previousActivity,
            ActivityAsset activeActivity,
            int enterBindingCount,
            int enterReceiverCount,
            int enterFailedReceiverCount,
            int exitBindingCount,
            int exitReceiverCount,
            int exitFailedReceiverCount,
            string source,
            string reason)
        {
            bool executed = enterBindingCount > 0 || exitBindingCount > 0;
            return new ActivityContentLifecycleResult(
                previousActivity,
                activeActivity,
                executed,
                enterBindingCount,
                enterReceiverCount,
                enterFailedReceiverCount,
                exitBindingCount,
                exitReceiverCount,
                exitFailedReceiverCount,
                NormalizeSource(source),
                NormalizeReason(reason));
        }

        private static string NormalizeSource(string source)
        {
            return source.NormalizeTextOrFallback("Unknown");
        }

        private static string NormalizeReason(string reason)
        {
            return reason.NormalizeTextOrFallback("None");
        }
    }
}
