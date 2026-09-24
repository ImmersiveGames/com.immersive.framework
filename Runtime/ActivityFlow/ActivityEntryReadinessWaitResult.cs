using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable terminal evidence for one Activity readiness occurrence wait.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IF-ADR-007 occurrence-scoped Activity entry-readiness waiting result.")]
    internal readonly struct ActivityEntryReadinessWaitResult
    {
        private ActivityEntryReadinessWaitResult(
            ActivityEntryReadinessWaitStatus status,
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readinessState,
            string reason,
            int revision)
        {
            Status = status;
            Occurrence = occurrence;
            ReadinessState = readinessState;
            Reason = reason ?? string.Empty;
            Revision = revision < 0 ? 0 : revision;
        }

        internal ActivityEntryReadinessWaitStatus Status { get; }
        internal ActivityReadinessOccurrence Occurrence { get; }
        internal ActivityReadinessState ReadinessState { get; }
        internal string Reason { get; }
        internal int Revision { get; }
        internal ActivityAsset Activity => Occurrence.Activity;
        internal bool IsTerminal => Status != ActivityEntryReadinessWaitStatus.Unknown;
        internal bool IsReady => Status == ActivityEntryReadinessWaitStatus.Ready;
        internal bool Failed => Status == ActivityEntryReadinessWaitStatus.Failed;
        internal bool Invalidated => Status == ActivityEntryReadinessWaitStatus.Invalidated;
        internal bool Cancelled => Status == ActivityEntryReadinessWaitStatus.Cancelled;
        internal bool Superseded => Status == ActivityEntryReadinessWaitStatus.Superseded;

        internal static ActivityEntryReadinessWaitResult Ready(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readinessState,
            string reason,
            int revision)
        {
            return new ActivityEntryReadinessWaitResult(
                ActivityEntryReadinessWaitStatus.Ready,
                occurrence,
                readinessState,
                reason,
                revision);
        }

        internal static ActivityEntryReadinessWaitResult Failure(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readinessState,
            string reason,
            int revision)
        {
            return new ActivityEntryReadinessWaitResult(
                ActivityEntryReadinessWaitStatus.Failed,
                occurrence,
                readinessState,
                reason,
                revision);
        }

        internal static ActivityEntryReadinessWaitResult Invalidation(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readinessState,
            string reason,
            int revision)
        {
            return new ActivityEntryReadinessWaitResult(
                ActivityEntryReadinessWaitStatus.Invalidated,
                occurrence,
                readinessState,
                reason,
                revision);
        }

        internal static ActivityEntryReadinessWaitResult Cancellation(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readinessState,
            string reason,
            int revision)
        {
            return new ActivityEntryReadinessWaitResult(
                ActivityEntryReadinessWaitStatus.Cancelled,
                occurrence,
                readinessState,
                reason,
                revision);
        }

        internal static ActivityEntryReadinessWaitResult Supersession(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readinessState,
            string reason,
            int revision)
        {
            return new ActivityEntryReadinessWaitResult(
                ActivityEntryReadinessWaitStatus.Superseded,
                occurrence,
                readinessState,
                reason,
                revision);
        }

        internal string ToDiagnosticString()
        {
            return
                $"status='{Status}' activity='{ReadinessState.ActivityName}' " +
                $"occurrence='{Occurrence.TransitionSequence}' revision='{Revision}' " +
                $"readiness='{ReadinessState.DiagnosticStatus}' reason='{Reason}'.";
        }
    }
}
