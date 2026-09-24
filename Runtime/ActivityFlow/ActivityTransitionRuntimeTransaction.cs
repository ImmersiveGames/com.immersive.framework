using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Mutable transaction record owned exclusively by ActivityFlowRuntime.
    /// It is not a service locator, queue, global manager or gameplay-facing API.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ARCH-A2 Activity transition transaction implementation detail.")]
    internal sealed class ActivityTransitionRuntimeTransaction
    {
        private ActivityTransitionPhase _phase;
        private ActivityTransitionTerminalStatus _terminalStatus;
        private bool _commitReached;
        private bool _previousContentExited;
        private bool _previousParticipantsExited;
        private bool _targetParticipantsEntered;
        private bool _targetContentEntered;
        private PreviousActivityFinalizationStatus _previousFinalizationStatus;
        private bool _previousScenesReleased;
        private ActivityReadinessState _readinessState;
        private string _message;

        internal ActivityTransitionRuntimeTransaction(
            int sequence,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            if (sequence <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequence),
                    sequence,
                    "Activity transition sequence must be positive.");
            }

            Sequence = sequence;
            PreviousActivity = previousActivity;
            TargetActivity = targetActivity;
            Source = source.NormalizeTextOrFallback(
                nameof(ActivityTransitionRuntimeTransaction));
            Reason = reason.NormalizeTextOrFallback("activity-transition");
            _phase = ActivityTransitionPhase.PreparingTarget;
            _terminalStatus = ActivityTransitionTerminalStatus.None;
            _previousFinalizationStatus = previousActivity == null
                ? PreviousActivityFinalizationStatus.NotRequired
                : PreviousActivityFinalizationStatus.Pending;
            _previousScenesReleased = previousActivity == null;
            _message = "Activity transition target preparation started.";
        }

        internal int Sequence { get; }

        internal ActivityAsset PreviousActivity { get; }

        internal ActivityAsset TargetActivity { get; }

        internal string Source { get; }

        internal string Reason { get; }

        internal bool CommitReached => _commitReached;

        internal bool IsTerminal => _terminalStatus != ActivityTransitionTerminalStatus.None;

        internal ActivityTransitionSnapshot Snapshot => new ActivityTransitionSnapshot(
            Sequence,
            _phase,
            _terminalStatus,
            PreviousActivity,
            TargetActivity,
            _commitReached,
            _previousContentExited,
            _previousParticipantsExited,
            _targetParticipantsEntered,
            _targetContentEntered,
            _previousFinalizationStatus,
            _previousScenesReleased,
            _readinessState,
            Source,
            Reason,
            _message);

        internal void MarkReadyToCommit(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.PreparingTarget);
            _phase = ActivityTransitionPhase.ReadyToCommit;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Activity transition reached ReadyToCommit.");
        }

        internal void Commit(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.ReadyToCommit);
            _commitReached = true;
            _phase = ActivityTransitionPhase.CommittedTransitioning;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Target Activity authority committed.");
        }

        internal void BeginPreviousExit(string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.CommittedTransitioning);
            _phase = ActivityTransitionPhase.PreviousExiting;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Previous Activity exit started.");
        }

        internal void MarkPreviousContentExited(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.PreviousExiting);
            _previousContentExited = true;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Previous Activity scene content exit completed.");
        }

        internal void MarkPreviousParticipantsExited(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.PreviousExiting);
            _previousParticipantsExited = true;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Previous Activity participant exit completed.");
        }

        internal void BeginTargetEnter(string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.PreviousExiting);
            if (!_previousContentExited || !_previousParticipantsExited)
            {
                throw new InvalidOperationException(
                    "Target Activity enter cannot begin before previous content and participants finish exit.");
            }

            _phase = ActivityTransitionPhase.TargetEntering;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Target Activity enter started.");
        }

        internal void MarkTargetParticipantsEntered(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.TargetEntering);
            _targetParticipantsEntered = true;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Target Activity participants entered.");
        }

        internal void MarkTargetContentEntered(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.TargetEntering);
            if (!_targetParticipantsEntered)
            {
                throw new InvalidOperationException(
                    "Target Activity scene content cannot enter before target participants.");
            }

            _targetContentEntered = true;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Target Activity scene content entered.");
        }

        internal void BeginPreviousFinalization(string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.TargetEntering);
            if (!_targetParticipantsEntered || !_targetContentEntered)
            {
                throw new InvalidOperationException(
                    "Previous Activity finalization cannot begin before target enter completes.");
            }

            _phase = ActivityTransitionPhase.PreviousFinalizing;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Previous Activity finalization started.");
        }

        internal void MarkPreviousFinalized(bool succeeded, string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.PreviousFinalizing);
            _previousFinalizationStatus = PreviousActivity == null
                ? PreviousActivityFinalizationStatus.NotRequired
                : succeeded
                    ? PreviousActivityFinalizationStatus.Succeeded
                    : PreviousActivityFinalizationStatus.Failed;
            _message = NormalizeDiagnostic(
                diagnostic,
                succeeded
                    ? "Previous Activity finalization completed."
                    : "Previous Activity finalization failed.");
        }

        internal void MarkPreviousScenesReleased(bool succeeded, string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.PreviousFinalizing);
            _previousScenesReleased = PreviousActivity == null || succeeded;
            if (!succeeded && PreviousActivity != null)
            {
                _previousFinalizationStatus = PreviousActivityFinalizationStatus.Failed;
            }

            _message = NormalizeDiagnostic(
                diagnostic,
                succeeded
                    ? "Previous Activity scene release completed."
                    : "Previous Activity scene release failed.");
        }

        internal ActivityTransitionSnapshot FailBeforeCommit(string diagnostic)
        {
            if (_commitReached)
            {
                throw new InvalidOperationException(
                    "A committed Activity transition cannot finish as FailedBeforeCommit.");
            }

            _terminalStatus = ActivityTransitionTerminalStatus.FailedBeforeCommit;
            _phase = ActivityTransitionPhase.FailedBeforeCommit;
            _message = NormalizeDiagnostic(
                diagnostic,
                "Activity transition failed before authority commit.");
            return Snapshot;
        }

        internal ActivityTransitionSnapshot Complete(
            ActivityReadinessState finalReadinessState,
            bool previousFinalizationSucceeded,
            bool previousSceneReleaseSucceeded,
            string diagnostic)
        {
            RequireCommittedNonTerminal();
            _readinessState = finalReadinessState;

            if (PreviousActivity != null &&
                (!previousFinalizationSucceeded || !previousSceneReleaseSucceeded))
            {
                _previousFinalizationStatus = PreviousActivityFinalizationStatus.Failed;
                _terminalStatus =
                    ActivityTransitionTerminalStatus.CommittedFinalizationFailed;
                _phase = ActivityTransitionPhase.CommittedFinalizationFailed;
            }
            else if (TargetActivity != null && !finalReadinessState.IsReady)
            {
                _terminalStatus = ActivityTransitionTerminalStatus.CommittedNotReady;
                _phase = ActivityTransitionPhase.CommittedNotReady;
            }
            else
            {
                _terminalStatus = ActivityTransitionTerminalStatus.CommittedReady;
                _phase = ActivityTransitionPhase.Completed;
            }

            _message = NormalizeDiagnostic(
                diagnostic,
                $"Activity transition completed as '{_terminalStatus}'.");
            return Snapshot;
        }

        internal ActivityTransitionSnapshot FailCommittedException(
            ActivityReadinessState finalReadinessState,
            string diagnostic)
        {
            RequireCommittedNonTerminal();
            _readinessState = finalReadinessState;
            bool targetReady = TargetActivity == null || finalReadinessState.IsReady;
            _previousFinalizationStatus = PreviousActivity == null
                ? PreviousActivityFinalizationStatus.NotRequired
                : PreviousActivityFinalizationStatus.Failed;
            _previousScenesReleased = PreviousActivity == null;
            _terminalStatus = targetReady
                ? ActivityTransitionTerminalStatus.CommittedFinalizationFailed
                : ActivityTransitionTerminalStatus.CommittedNotReady;
            _phase = targetReady
                ? ActivityTransitionPhase.CommittedFinalizationFailed
                : ActivityTransitionPhase.CommittedNotReady;
            _message = NormalizeDiagnostic(
                diagnostic,
                targetReady
                    ? "Committed Activity transition failed during previous finalization."
                    : "Committed Activity transition did not complete target readiness.");
            return Snapshot;
        }

        private void RequireCommittedNonTerminal()
        {
            if (!_commitReached)
            {
                throw new InvalidOperationException(
                    "Activity transition operation requires committed target authority.");
            }

            if (IsTerminal)
            {
                throw new InvalidOperationException(
                    "Activity transition is already terminal.");
            }
        }

        private void RequirePhase(ActivityTransitionPhase expected)
        {
            if (_phase != expected)
            {
                throw new InvalidOperationException(
                    $"Activity transition phase mismatch. expected='{expected}' actual='{_phase}'.");
            }
        }

        private static string NormalizeDiagnostic(
            string diagnostic,
            string fallback)
        {
            return diagnostic.NormalizeTextOrFallback(fallback);
        }
    }
}
