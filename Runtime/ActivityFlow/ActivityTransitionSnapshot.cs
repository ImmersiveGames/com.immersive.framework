using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable diagnostic snapshot for one Activity transition transaction.
    /// It separates current authority, transaction phase, readiness and previous finalization.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ARCH-A2 Activity transition transaction snapshot; diagnostics and QA evidence only.")]
    internal readonly struct ActivityTransitionSnapshot
    {
        internal ActivityTransitionSnapshot(
            int sequence,
            ActivityTransitionPhase phase,
            ActivityTransitionTerminalStatus terminalStatus,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            bool commitReached,
            bool previousContentExited,
            bool previousParticipantsExited,
            bool targetParticipantsEntered,
            bool targetContentEntered,
            PreviousActivityFinalizationStatus previousFinalizationStatus,
            bool previousScenesReleased,
            ActivityReadinessState readinessState,
            string source,
            string reason,
            string message)
        {
            Sequence = sequence;
            Phase = phase;
            TerminalStatus = terminalStatus;
            PreviousActivity = previousActivity;
            TargetActivity = targetActivity;
            CommitReached = commitReached;
            PreviousContentExited = previousContentExited;
            PreviousParticipantsExited = previousParticipantsExited;
            TargetParticipantsEntered = targetParticipantsEntered;
            TargetContentEntered = targetContentEntered;
            PreviousFinalizationStatus = previousFinalizationStatus;
            PreviousScenesReleased = previousScenesReleased;
            ReadinessState = readinessState;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public int Sequence { get; }

        public ActivityTransitionPhase Phase { get; }

        public ActivityTransitionTerminalStatus TerminalStatus { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset TargetActivity { get; }

        public bool CommitReached { get; }

        public bool PreviousContentExited { get; }

        public bool PreviousParticipantsExited { get; }

        public bool TargetParticipantsEntered { get; }

        public bool TargetContentEntered { get; }

        public PreviousActivityFinalizationStatus PreviousFinalizationStatus { get; }

        public bool PreviousScenesReleased { get; }

        public ActivityReadinessState ReadinessState { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool IsValid => Sequence > 0 && Phase != ActivityTransitionPhase.Unknown;

        public bool IsTerminal => TerminalStatus is
            ActivityTransitionTerminalStatus.FailedBeforeCommit or
            ActivityTransitionTerminalStatus.CommittedReady or
            ActivityTransitionTerminalStatus.CommittedNotReady or
            ActivityTransitionTerminalStatus.CommittedFinalizationFailed;

        public bool IsInProgress => IsValid && !IsTerminal;

        public bool FailedBeforeCommit =>
            TerminalStatus == ActivityTransitionTerminalStatus.FailedBeforeCommit;

        public bool CommittedReady =>
            TerminalStatus == ActivityTransitionTerminalStatus.CommittedReady;

        public bool CommittedNotReady =>
            TerminalStatus == ActivityTransitionTerminalStatus.CommittedNotReady;

        public bool CommittedFinalizationFailed =>
            TerminalStatus == ActivityTransitionTerminalStatus.CommittedFinalizationFailed;

        public bool PreviousFinalizationSucceeded =>
            PreviousFinalizationStatus is
                PreviousActivityFinalizationStatus.NotRequired or
                PreviousActivityFinalizationStatus.Succeeded;

        public string ToDiagnosticString()
        {
            string previousId = ActivityIdText(PreviousActivity);
            string targetId = ActivityIdText(TargetActivity);
            string readiness = ReadinessState.DiagnosticStatus.ToDiagnosticText();
            string source = Source.ToDiagnosticText();
            string reason = Reason.ToDiagnosticText();
            string message = Message.ToDiagnosticText();

            return
                $"sequence='{Sequence}' phase='{Phase}' terminal='{TerminalStatus}' " +
                $"previous='{previousId}' target='{targetId}' commitReached='{CommitReached}' " +
                $"previousContentExited='{PreviousContentExited}' " +
                $"previousParticipantsExited='{PreviousParticipantsExited}' " +
                $"targetParticipantsEntered='{TargetParticipantsEntered}' " +
                $"targetContentEntered='{TargetContentEntered}' " +
                $"previousFinalization='{PreviousFinalizationStatus}' " +
                $"previousScenesReleased='{PreviousScenesReleased}' readiness='{readiness}' " +
                $"source='{source}' reason='{reason}' message='{message}'";
        }

        private static string ActivityIdText(ActivityAsset activity)
        {
            if (activity == null)
            {
                return "<none>";
            }

            return activity.HasValidActivityId
                ? activity.ActivityId.StableText
                : activity.ActivityName.ToDiagnosticText();
        }
    }
}
