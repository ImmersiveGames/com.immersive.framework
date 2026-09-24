using System;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// One-shot event-driven waiter bound to one Activity readiness occurrence state.
    /// It never discovers runtime state and never changes Activity readiness.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IF-ADR-007 one-shot occurrence-scoped Activity entry-readiness waiter.")]
    internal sealed class ActivityEntryReadinessWaiter
    {
        private readonly object _syncRoot = new object();
        private readonly ActivityReadinessOccurrenceState _occurrenceState;
        private readonly ActivityReadinessOccurrence _occurrence;
        private readonly TaskCompletionSource<ActivityEntryReadinessWaitResult>
            _completion =
                new TaskCompletionSource<ActivityEntryReadinessWaitResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

        private ActivityReadinessState _lastReadinessState;
        private int _lastRevision;
        private bool _completed;
        private bool _subscribed;
        private CancellationTokenRegistration _cancellationRegistration;
        private bool _hasCancellationRegistration;

        internal ActivityEntryReadinessWaiter(
            ActivityReadinessOccurrenceState occurrenceState,
            CancellationToken cancellationToken)
        {
            _occurrenceState = occurrenceState;
            _occurrence = occurrenceState != null
                ? occurrenceState.Occurrence
                : default;

            if (cancellationToken.IsCancellationRequested)
            {
                Complete(
                    ActivityEntryReadinessWaitResult.Cancellation(
                        _occurrence,
                        default,
                        "CancelledBeforeWait",
                        1));
                return;
            }

            if (occurrenceState == null ||
                !_occurrence.IsValid)
            {
                Complete(
                    ActivityEntryReadinessWaitResult.Invalidation(
                        _occurrence,
                        default,
                        "OccurrenceUnavailable",
                        1));
                return;
            }

            occurrenceState.Changed += HandleOccurrenceStateChanged;
            _subscribed = true;

            if (cancellationToken.CanBeCanceled)
            {
                CancellationTokenRegistration registration =
                    cancellationToken.Register(Cancel);
                AttachCancellationRegistration(registration);
            }

            Observe(occurrenceState);
        }

        internal Task<ActivityEntryReadinessWaitResult> Completion =>
            _completion.Task;

        private void HandleOccurrenceStateChanged(
            ActivityReadinessOccurrenceState occurrenceState)
        {
            if (!ReferenceEquals(
                    occurrenceState,
                    _occurrenceState))
            {
                return;
            }

            Observe(occurrenceState);
        }

        private void Observe(
            ActivityReadinessOccurrenceState occurrenceState)
        {
            if (occurrenceState == null ||
                !occurrenceState.Occurrence.Matches(
                    _occurrence.Activity,
                    _occurrence.TransitionSequence))
            {
                return;
            }

            ActivityReadinessState readinessState =
                occurrenceState.AggregateReadiness;
            int revision = occurrenceState.Revision;

            lock (_syncRoot)
            {
                if (_completed || revision < _lastRevision)
                {
                    return;
                }

                _lastReadinessState = readinessState;
                _lastRevision = revision;
            }

            if (occurrenceState.IsInvalidated)
            {
                Complete(
                    ActivityEntryReadinessWaitResult.Invalidation(
                        _occurrence,
                        readinessState,
                        "OccurrenceInvalidated",
                        revision));
                return;
            }

            if (readinessState.IsReady)
            {
                Complete(
                    ActivityEntryReadinessWaitResult.Ready(
                        _occurrence,
                        readinessState,
                        ResolveReason(
                            readinessState.DiagnosticReason,
                            readinessState.Reason,
                            "Ready"),
                        revision));
                return;
            }

            if (readinessState.HasTerminalFailure)
            {
                Complete(
                    ActivityEntryReadinessWaitResult.Failure(
                        _occurrence,
                        readinessState,
                        ResolveReason(
                            readinessState.DiagnosticReason,
                            readinessState.Reason,
                            "ActivityReadinessFailed"),
                        revision));
            }
        }

        private void Cancel()
        {
            Complete(
                ActivityEntryReadinessWaitResult.Cancellation(
                    _occurrence,
                    SnapshotReadinessState(),
                    "Cancelled",
                    NextRevision()));
        }

        private void AttachCancellationRegistration(
            CancellationTokenRegistration registration)
        {
            bool disposeImmediately;
            lock (_syncRoot)
            {
                disposeImmediately = _completed;
                if (!disposeImmediately)
                {
                    _cancellationRegistration = registration;
                    _hasCancellationRegistration = true;
                }
            }

            if (disposeImmediately)
            {
                registration.Dispose();
            }
        }

        private void Complete(
            ActivityEntryReadinessWaitResult result)
        {
            CancellationTokenRegistration cancellationRegistration;
            bool disposeCancellation;
            bool unsubscribe;

            lock (_syncRoot)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
                unsubscribe = _subscribed;
                _subscribed = false;
                cancellationRegistration = _cancellationRegistration;
                disposeCancellation = _hasCancellationRegistration;
                _hasCancellationRegistration = false;
            }

            if (unsubscribe &&
                _occurrenceState != null)
            {
                _occurrenceState.Changed -=
                    HandleOccurrenceStateChanged;
            }

            if (disposeCancellation)
            {
                cancellationRegistration.Dispose();
            }

            _completion.TrySetResult(result);
        }

        private ActivityReadinessState SnapshotReadinessState()
        {
            lock (_syncRoot)
            {
                return _lastReadinessState;
            }
        }

        private int NextRevision()
        {
            lock (_syncRoot)
            {
                _lastRevision++;
                return _lastRevision;
            }
        }

        private static string ResolveReason(
            string primary,
            string secondary,
            string fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return primary;
            }

            return !string.IsNullOrWhiteSpace(secondary)
                ? secondary
                : fallback;
        }
    }
}
