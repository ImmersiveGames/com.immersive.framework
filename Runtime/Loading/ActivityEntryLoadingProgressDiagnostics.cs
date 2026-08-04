using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// Immutable operation evidence for participant-aware WaitCovered Loading.
    /// It projects the envelope state without becoming a Loading or readiness authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IF-READY-PROGRESS-03 participant-aware Activity entry Loading diagnostics.")]
    internal readonly struct ActivityEntryLoadingProgressDiagnostics
    {
        internal ActivityEntryLoadingProgressDiagnostics(
            ActivityEntryLoadingProgressPlan plan,
            ActivityReadinessProgressSnapshot readiness,
            bool hasReadinessSnapshot,
            FrameworkLoadingProgress lastProgress,
            bool hasReportedProgress,
            bool terminalCompletionIssued,
            bool terminalFailureObserved,
            bool loadingHidden,
            bool revealCompleted,
            int rejectedReadinessSnapshotCount)
        {
            TechnicalRangeStart01 = plan.TechnicalRange.Start01;
            TechnicalRangeEnd01 = plan.TechnicalRange.End01;
            ReadinessRangeStart01 = plan.ReadinessRange.Start01;
            ReadinessRangeEnd01 = plan.ReadinessRange.End01;
            Occurrence = hasReadinessSnapshot
                ? readiness.Occurrence
                : default;
            RequiredCount = hasReadinessSnapshot
                ? readiness.RequiredCount
                : 0;
            RequiredCompletedCount = hasReadinessSnapshot
                ? readiness.RequiredCompletedCount
                : 0;
            RequiredPendingCount = hasReadinessSnapshot
                ? readiness.RequiredPendingCount
                : 0;
            RequiredFailedCount = hasReadinessSnapshot
                ? readiness.RequiredFailedCount
                : 0;
            RequiredReleasedCount = hasReadinessSnapshot
                ? readiness.RequiredReleasedCount
                : 0;
            OptionalCount = hasReadinessSnapshot
                ? readiness.OptionalCount
                : 0;
            OptionalCompletedCount = hasReadinessSnapshot
                ? readiness.OptionalCompletedCount
                : 0;
            OptionalPendingCount = hasReadinessSnapshot
                ? readiness.OptionalPendingCount
                : 0;
            OptionalFailedCount = hasReadinessSnapshot
                ? readiness.OptionalFailedCount
                : 0;
            OptionalReleasedCount = hasReadinessSnapshot
                ? readiness.OptionalReleasedCount
                : 0;
            ReadinessRatio = hasReadinessSnapshot
                ? readiness.ReadinessRatio
                : 0f;
            LastProgress = lastProgress;
            HasReportedProgress = hasReportedProgress;
            TerminalCompletionIssued = terminalCompletionIssued;
            TerminalFailureObserved = terminalFailureObserved;
            LoadingHidden = loadingHidden;
            RevealCompleted = revealCompleted;
            RejectedReadinessSnapshotCount =
                rejectedReadinessSnapshotCount < 0
                    ? 0
                    : rejectedReadinessSnapshotCount;
        }

        internal float TechnicalRangeStart01 { get; }
        internal float TechnicalRangeEnd01 { get; }
        internal float ReadinessRangeStart01 { get; }
        internal float ReadinessRangeEnd01 { get; }
        internal ActivityReadinessOccurrence Occurrence { get; }
        internal int RequiredCount { get; }
        internal int RequiredCompletedCount { get; }
        internal int RequiredPendingCount { get; }
        internal int RequiredFailedCount { get; }
        internal int RequiredReleasedCount { get; }
        internal int OptionalCount { get; }
        internal int OptionalCompletedCount { get; }
        internal int OptionalPendingCount { get; }
        internal int OptionalFailedCount { get; }
        internal int OptionalReleasedCount { get; }
        internal float ReadinessRatio { get; }
        internal FrameworkLoadingProgress LastProgress { get; }
        internal bool HasReportedProgress { get; }
        internal bool TerminalCompletionIssued { get; }
        internal bool TerminalFailureObserved { get; }
        internal bool LoadingHidden { get; }
        internal bool RevealCompleted { get; }
        internal int RejectedReadinessSnapshotCount { get; }
        internal bool HasOccurrence => Occurrence.IsValid;
        internal bool IsValid =>
            ReadinessRangeEnd01 > ReadinessRangeStart01;
    }
}
