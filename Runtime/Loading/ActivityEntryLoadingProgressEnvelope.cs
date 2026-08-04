using System;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// Operation-scoped progress authority that reserves a stable final range for the
    /// captured Activity readiness occurrence. It wraps the existing Loading reporter,
    /// enforces monotonic determinate progress and never owns readiness or presentation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IF-READY-PROGRESS-02/03 Activity entry Loading progress envelope.")]
    internal sealed class ActivityEntryLoadingProgressEnvelope
    {
        internal const string ReadinessPhase = "ActivityReadiness";

        private readonly ActivityEntryLoadingProgressPlan _plan;
        private readonly IFrameworkLoadingProgressReporter _rootReporter;
        private readonly EnvelopeReporter _envelopeReporter;
        private readonly IFrameworkLoadingProgressReporter _technicalReporter;
        private FrameworkLoadingProgress _lastAcceptedProgress;
        private ActivityReadinessOccurrence _readinessOccurrence;
        private ActivityReadinessProgressSnapshot _lastReadinessSnapshot;
        private bool _hasReadinessSnapshot;
        private bool _hasReportedProgress;
        private bool _hasDeterminateProgress;
        private bool _reportingReadyTerminal;
        private bool _terminalCompletionIssued;
        private bool _terminalFailureObserved;
        private int _rejectedReadinessSnapshotCount;
        private readonly object _queuedReportSyncRoot = new object();
        private Task _queuedReportTail = Task.CompletedTask;

        internal ActivityEntryLoadingProgressEnvelope(
            IFrameworkLoadingProgressReporter rootReporter,
            ActivityEntryLoadingProgressPlan plan,
            string technicalPhase,
            string technicalMessage)
        {
            _plan = plan;
            _rootReporter = rootReporter ??
                NoOpFrameworkLoadingProgressReporter.Instance;
            _envelopeReporter = new EnvelopeReporter(this);
            _technicalReporter = new TechnicalRangeReporter(
                this,
                technicalPhase,
                technicalMessage);
            _lastAcceptedProgress = FrameworkLoadingProgress.Unsupported(
                "ActivityEntryLoadingEnvelope",
                "No progress has been accepted for this Activity entry operation.");
        }

        internal ActivityEntryLoadingProgressPlan Plan => _plan;
        internal IFrameworkLoadingProgressReporter TechnicalReporter =>
            _technicalReporter;
        internal FrameworkLoadingProgress LastAcceptedProgress =>
            _lastAcceptedProgress;
        internal ActivityReadinessOccurrence ReadinessOccurrence =>
            _readinessOccurrence;
        internal bool IsEnabled => _rootReporter.IsEnabled;
        internal bool HasReportedProgress => _hasReportedProgress;
        internal bool HasDeterminateProgress => _hasDeterminateProgress;
        internal float LastAcceptedProgress01 =>
            _hasDeterminateProgress ? _lastAcceptedProgress.Value01 : 0f;
        internal bool TerminalCompletionIssued => _terminalCompletionIssued;
        internal bool TerminalFailureObserved => _terminalFailureObserved;
        internal int RejectedReadinessSnapshotCount =>
            _rejectedReadinessSnapshotCount;

        internal ActivityEntryLoadingProgressDiagnostics CreateDiagnostics(
            bool loadingHidden,
            bool revealCompleted)
        {
            return new ActivityEntryLoadingProgressDiagnostics(
                _plan,
                _lastReadinessSnapshot,
                _hasReadinessSnapshot,
                _lastAcceptedProgress,
                _hasReportedProgress,
                _terminalCompletionIssued,
                _terminalFailureObserved,
                loadingHidden,
                revealCompleted,
                _rejectedReadinessSnapshotCount);
        }

        internal Task QueueReadinessAsync(
            ActivityReadinessProgressSnapshot snapshot)
        {
            lock (_queuedReportSyncRoot)
            {
                _queuedReportTail = ReportQueuedReadinessAsync(
                    _queuedReportTail,
                    snapshot);
                return _queuedReportTail;
            }
        }

        internal Task FlushQueuedReportsAsync()
        {
            lock (_queuedReportSyncRoot)
            {
                return _queuedReportTail;
            }
        }

        private async Task ReportQueuedReadinessAsync(
            Task previous,
            ActivityReadinessProgressSnapshot snapshot)
        {
            await previous;
            await ReportReadinessAsync(snapshot);
        }

        internal async Awaitable ReportReadinessAsync(
            ActivityReadinessProgressSnapshot snapshot)
        {
            if (!_plan.ReservesReadinessPhase ||
                !IsEnabled ||
                _terminalCompletionIssued ||
                _terminalFailureObserved)
            {
                return;
            }

            if (!snapshot.IsValid)
            {
                throw new ArgumentException(
                    "Activity readiness progress snapshot must be valid.",
                    nameof(snapshot));
            }

            if (!_readinessOccurrence.IsValid)
            {
                _readinessOccurrence = snapshot.Occurrence;
            }
            else if (!_readinessOccurrence.Matches(
                         snapshot.Occurrence.Activity,
                         snapshot.Occurrence.TransitionSequence))
            {
                _rejectedReadinessSnapshotCount++;
                return;
            }

            _lastReadinessSnapshot = snapshot;
            _hasReadinessSnapshot = true;

            if (snapshot.HasTerminalFailure)
            {
                MarkTerminalFailure();
                return;
            }

            string message = BuildReadinessMessage(snapshot);
            if (snapshot.IsReady)
            {
                _reportingReadyTerminal = true;
                try
                {
                    await _envelopeReporter.ReportAsync(
                        FrameworkLoadingProgress.Determinate(
                            1f,
                            ReadinessPhase,
                            message));
                }
                finally
                {
                    _reportingReadyTerminal = false;
                }

                return;
            }

            float mappedProgress =
                _plan.ReadinessRange.Map(snapshot.ReadinessRatio);
            if (mappedProgress >= 1f)
            {
                return;
            }

            await _envelopeReporter.ReportAsync(
                FrameworkLoadingProgress.Determinate(
                    mappedProgress,
                    ReadinessPhase,
                    message));
        }

        internal void MarkTerminalFailure()
        {
            if (_terminalCompletionIssued)
            {
                return;
            }

            _terminalFailureObserved = true;
        }

        private async Awaitable ReportEnvelopeProgressAsync(
            FrameworkLoadingProgress progress)
        {
            if (!IsEnabled ||
                _terminalCompletionIssued ||
                _terminalFailureObserved)
            {
                return;
            }

            if (!progress.Supported || !progress.IsDeterminate)
            {
                if (_hasDeterminateProgress ||
                    (_hasReportedProgress &&
                     _lastAcceptedProgress.Equals(progress)))
                {
                    return;
                }

                _lastAcceptedProgress = progress;
                _hasReportedProgress = true;
                await _rootReporter.ReportAsync(progress);
                return;
            }

            float value01 = progress.Value01;
            if (_plan.ReservesReadinessPhase &&
                value01 >= 1f &&
                !_reportingReadyTerminal)
            {
                return;
            }

            if (_hasDeterminateProgress)
            {
                if (value01 < _lastAcceptedProgress.Value01 ||
                    (value01.Equals(_lastAcceptedProgress.Value01) &&
                     _lastAcceptedProgress.Equals(progress)))
                {
                    return;
                }
            }

            _lastAcceptedProgress = progress;
            _hasReportedProgress = true;
            _hasDeterminateProgress = true;
            if (value01 >= 1f)
            {
                _terminalCompletionIssued = true;
            }

            await _rootReporter.ReportAsync(progress);
        }

        private FrameworkLoadingProgress MapTechnicalProgress(
            FrameworkLoadingProgress progress,
            string phase,
            string messagePrefix)
        {
            string resolvedPhase = string.IsNullOrWhiteSpace(phase)
                ? "TechnicalLoading"
                : phase.Trim();
            string resolvedMessage = BuildTechnicalMessage(
                messagePrefix,
                progress.Message);

            if (!progress.Supported || !progress.IsDeterminate)
            {
                return FrameworkLoadingProgress.Indeterminate(
                    progress.Supported,
                    resolvedPhase,
                    resolvedMessage);
            }

            return FrameworkLoadingProgress.Determinate(
                _plan.TechnicalRange.Map(progress.Value01),
                resolvedPhase,
                resolvedMessage);
        }

        private static string BuildTechnicalMessage(
            string messagePrefix,
            string childMessage)
        {
            string prefix = string.IsNullOrWhiteSpace(messagePrefix)
                ? string.Empty
                : messagePrefix.Trim();
            string child = string.IsNullOrWhiteSpace(childMessage)
                ? string.Empty
                : childMessage.Trim();

            if (prefix.Length == 0)
            {
                return child;
            }

            return child.Length == 0 ? prefix : $"{prefix} {child}";
        }

        private static string BuildReadinessMessage(
            ActivityReadinessProgressSnapshot snapshot)
        {
            return
                $"Required completed='{snapshot.RequiredCompletedCount}' " +
                $"total='{snapshot.RequiredCount}' " +
                $"pending='{snapshot.RequiredPendingCount}' " +
                $"failed='{snapshot.RequiredFailedCount}' " +
                $"released='{snapshot.RequiredReleasedCount}'; " +
                $"Optional completed='{snapshot.OptionalCompletedCount}' " +
                $"total='{snapshot.OptionalCount}' " +
                $"pending='{snapshot.OptionalPendingCount}' " +
                $"failed='{snapshot.OptionalFailedCount}' " +
                $"released='{snapshot.OptionalReleasedCount}'.";
        }

        private sealed class TechnicalRangeReporter :
            IFrameworkLoadingProgressReporter
        {
            private readonly ActivityEntryLoadingProgressEnvelope _owner;
            private readonly string _phase;
            private readonly string _messagePrefix;

            internal TechnicalRangeReporter(
                ActivityEntryLoadingProgressEnvelope owner,
                string phase,
                string messagePrefix)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _phase = phase;
                _messagePrefix = messagePrefix;
            }

            public bool IsEnabled =>
                _owner.IsEnabled && _owner.Plan.HasTechnicalRange;
            public bool HasReportedProgress => _owner.HasReportedProgress;
            public FrameworkLoadingProgress LastProgress =>
                _owner.LastAcceptedProgress;

            public async Awaitable ReportAsync(
                FrameworkLoadingProgress progress)
            {
                if (!IsEnabled)
                {
                    return;
                }

                await _owner.ReportEnvelopeProgressAsync(
                    _owner.MapTechnicalProgress(
                        progress,
                        _phase,
                        _messagePrefix));
            }
        }

        private sealed class EnvelopeReporter :
            IFrameworkLoadingProgressReporter
        {
            private readonly ActivityEntryLoadingProgressEnvelope _owner;

            internal EnvelopeReporter(
                ActivityEntryLoadingProgressEnvelope owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public bool IsEnabled => _owner.IsEnabled;
            public bool HasReportedProgress => _owner.HasReportedProgress;
            public FrameworkLoadingProgress LastProgress =>
                _owner.LastAcceptedProgress;

            public async Awaitable ReportAsync(
                FrameworkLoadingProgress progress)
            {
                await _owner.ReportEnvelopeProgressAsync(progress);
            }
        }
    }
}
