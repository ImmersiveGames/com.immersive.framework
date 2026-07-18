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
        private ActivityTransitionPhase phase;
        private ActivityTransitionTerminalStatus terminalStatus;
        private bool commitReached;
        private bool previousContentExited;
        private bool previousParticipantsExited;
        private bool targetParticipantsEntered;
        private bool targetContentEntered;
        private PreviousActivityFinalizationStatus previousFinalizationStatus;
        private bool previousScenesReleased;
        private ActivityReadinessState readinessState;
        private string message;

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
            phase = ActivityTransitionPhase.PreparingTarget;
            terminalStatus = ActivityTransitionTerminalStatus.None;
            previousFinalizationStatus = previousActivity == null
                ? PreviousActivityFinalizationStatus.NotRequired
                : PreviousActivityFinalizationStatus.Pending;
            previousScenesReleased = previousActivity == null;
            message = "Activity transition target preparation started.";
        }

        internal int Sequence { get; }

        internal ActivityAsset PreviousActivity { get; }

        internal ActivityAsset TargetActivity { get; }

        internal string Source { get; }

        internal string Reason { get; }

        internal bool CommitReached => commitReached;

        internal bool IsTerminal => terminalStatus != ActivityTransitionTerminalStatus.None;

        internal ActivityTransitionSnapshot Snapshot => new ActivityTransitionSnapshot(
            Sequence,
            phase,
            terminalStatus,
            PreviousActivity,
            TargetActivity,
            commitReached,
            previousContentExited,
            previousParticipantsExited,
            targetParticipantsEntered,
            targetContentEntered,
            previousFinalizationStatus,
            previousScenesReleased,
            readinessState,
            Source,
            Reason,
            message);

        internal void MarkReadyToCommit(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.PreparingTarget);
            phase = ActivityTransitionPhase.ReadyToCommit;
            message = NormalizeDiagnostic(
                diagnostic,
                "Activity transition reached ReadyToCommit.");
        }

        internal void Commit(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.ReadyToCommit);
            commitReached = true;
            phase = ActivityTransitionPhase.CommittedTransitioning;
            message = NormalizeDiagnostic(
                diagnostic,
                "Target Activity authority committed.");
        }

        internal void BeginPreviousExit(string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.CommittedTransitioning);
            phase = ActivityTransitionPhase.PreviousExiting;
            message = NormalizeDiagnostic(
                diagnostic,
                "Previous Activity exit started.");
        }

        internal void MarkPreviousContentExited(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.PreviousExiting);
            previousContentExited = true;
            message = NormalizeDiagnostic(
                diagnostic,
                "Previous Activity scene content exit completed.");
        }

        internal void MarkPreviousParticipantsExited(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.PreviousExiting);
            previousParticipantsExited = true;
            message = NormalizeDiagnostic(
                diagnostic,
                "Previous Activity participant exit completed.");
        }

        internal void BeginTargetEnter(string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.PreviousExiting);
            if (!previousContentExited || !previousParticipantsExited)
            {
                throw new InvalidOperationException(
                    "Target Activity enter cannot begin before previous content and participants finish exit.");
            }

            phase = ActivityTransitionPhase.TargetEntering;
            message = NormalizeDiagnostic(
                diagnostic,
                "Target Activity enter started.");
        }

        internal void MarkTargetParticipantsEntered(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.TargetEntering);
            targetParticipantsEntered = true;
            message = NormalizeDiagnostic(
                diagnostic,
                "Target Activity participants entered.");
        }

        internal void MarkTargetContentEntered(string diagnostic)
        {
            RequirePhase(ActivityTransitionPhase.TargetEntering);
            if (!targetParticipantsEntered)
            {
                throw new InvalidOperationException(
                    "Target Activity scene content cannot enter before target participants.");
            }

            targetContentEntered = true;
            message = NormalizeDiagnostic(
                diagnostic,
                "Target Activity scene content entered.");
        }

        internal void BeginPreviousFinalization(string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.TargetEntering);
            if (!targetParticipantsEntered || !targetContentEntered)
            {
                throw new InvalidOperationException(
                    "Previous Activity finalization cannot begin before target enter completes.");
            }

            phase = ActivityTransitionPhase.PreviousFinalizing;
            message = NormalizeDiagnostic(
                diagnostic,
                "Previous Activity finalization started.");
        }

        internal void MarkPreviousFinalized(bool succeeded, string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.PreviousFinalizing);
            previousFinalizationStatus = PreviousActivity == null
                ? PreviousActivityFinalizationStatus.NotRequired
                : succeeded
                    ? PreviousActivityFinalizationStatus.Succeeded
                    : PreviousActivityFinalizationStatus.Failed;
            message = NormalizeDiagnostic(
                diagnostic,
                succeeded
                    ? "Previous Activity finalization completed."
                    : "Previous Activity finalization failed.");
        }

        internal void MarkPreviousScenesReleased(bool succeeded, string diagnostic)
        {
            RequireCommittedNonTerminal();
            RequirePhase(ActivityTransitionPhase.PreviousFinalizing);
            previousScenesReleased = PreviousActivity == null || succeeded;
            if (!succeeded && PreviousActivity != null)
            {
                previousFinalizationStatus = PreviousActivityFinalizationStatus.Failed;
            }

            message = NormalizeDiagnostic(
                diagnostic,
                succeeded
                    ? "Previous Activity scene release completed."
                    : "Previous Activity scene release failed.");
        }

        internal ActivityTransitionSnapshot FailBeforeCommit(string diagnostic)
        {
            if (commitReached)
            {
                throw new InvalidOperationException(
                    "A committed Activity transition cannot finish as FailedBeforeCommit.");
            }

            terminalStatus = ActivityTransitionTerminalStatus.FailedBeforeCommit;
            phase = ActivityTransitionPhase.FailedBeforeCommit;
            message = NormalizeDiagnostic(
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
            readinessState = finalReadinessState;

            if (PreviousActivity != null &&
                (!previousFinalizationSucceeded || !previousSceneReleaseSucceeded))
            {
                previousFinalizationStatus = PreviousActivityFinalizationStatus.Failed;
                terminalStatus =
                    ActivityTransitionTerminalStatus.CommittedFinalizationFailed;
                phase = ActivityTransitionPhase.CommittedFinalizationFailed;
            }
            else if (TargetActivity != null && !finalReadinessState.IsReady)
            {
                terminalStatus = ActivityTransitionTerminalStatus.CommittedNotReady;
                phase = ActivityTransitionPhase.CommittedNotReady;
            }
            else
            {
                terminalStatus = ActivityTransitionTerminalStatus.CommittedReady;
                phase = ActivityTransitionPhase.Completed;
            }

            message = NormalizeDiagnostic(
                diagnostic,
                $"Activity transition completed as '{terminalStatus}'.");
            return Snapshot;
        }

        internal ActivityTransitionSnapshot FailCommittedException(
            ActivityReadinessState finalReadinessState,
            string diagnostic)
        {
            RequireCommittedNonTerminal();
            readinessState = finalReadinessState;
            previousFinalizationStatus = PreviousActivity == null
                ? PreviousActivityFinalizationStatus.NotRequired
                : PreviousActivityFinalizationStatus.Failed;
            previousScenesReleased = PreviousActivity == null;
            terminalStatus =
                ActivityTransitionTerminalStatus.CommittedFinalizationFailed;
            phase = ActivityTransitionPhase.CommittedFinalizationFailed;
            message = NormalizeDiagnostic(
                diagnostic,
                "Committed Activity transition failed during exit, enter or finalization.");
            return Snapshot;
        }

        private void RequireCommittedNonTerminal()
        {
            if (!commitReached)
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
            if (phase != expected)
            {
                throw new InvalidOperationException(
                    $"Activity transition phase mismatch. expected='{expected}' actual='{phase}'.");
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
