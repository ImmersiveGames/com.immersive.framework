using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.FlowTriggers
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F42 neutral FlowTrigger state helper; owns only local submission bookkeeping.")]
    internal sealed class FrameworkFlowTriggerState
    {
        public const string PhaseNone = "None";
        public const string PhaseSubmitted = "Submitted";
        public const string PhaseCompleted = "Completed";

        public const string OutcomeNone = "None";
        public const string OutcomeSubmitted = "Submitted";
        public const string OutcomeSucceeded = "Succeeded";
        public const string OutcomeIgnored = "Ignored";
        public const string OutcomeFailed = "Failed";

        private FrameworkFlowTriggerSubmission _lastSubmission = new FrameworkFlowTriggerSubmission(
            false,
            false,
            true,
            false,
            false,
            false,
            PhaseCompleted,
            OutcomeNone,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            0);

        public bool IsSubmissionInFlight => _lastSubmission.InFlight;

        public FrameworkFlowTriggerSubmission LastSubmission => _lastSubmission;

        public string LastPhase => _lastSubmission.Phase;

        public string LastOutcome => _lastSubmission.Outcome;

        public string LastSource => _lastSubmission.Source;

        public string LastReason => _lastSubmission.Reason;

        public string LastMessage => _lastSubmission.Message;

        public int LastIssueCount => _lastSubmission.IssueCount;

        public int LastBlockingIssueCount => _lastSubmission.BlockingIssueCount;

        public bool LastSucceeded => _lastSubmission.Succeeded;

        public bool LastIgnored => _lastSubmission.Ignored;

        public bool LastFailed => _lastSubmission.Failed;

        public bool TryBegin(
            string source,
            string reason,
            string message,
            out FrameworkFlowTriggerSubmission submission)
        {
            if (IsSubmissionInFlight)
            {
                submission = _lastSubmission;
                return false;
            }

            submission = Begin(source, reason, message);
            return true;
        }

        public FrameworkFlowTriggerSubmission Begin(string source, string reason, string message)
        {
            return Set(
                true,
                true,
                false,
                false,
                false,
                false,
                PhaseSubmitted,
                OutcomeSubmitted,
                source,
                reason,
                message,
                0,
                0);
        }

        public FrameworkFlowTriggerSubmission CompleteSucceeded(
            string source,
            string reason,
            string message,
            int issueCount,
            int blockingIssueCount)
        {
            return Complete(OutcomeSucceeded, true, false, false, source, reason, message, issueCount, blockingIssueCount);
        }

        public FrameworkFlowTriggerSubmission CompleteIgnored(
            string source,
            string reason,
            string message,
            int issueCount,
            int blockingIssueCount)
        {
            return Complete(OutcomeIgnored, false, true, false, source, reason, message, issueCount, blockingIssueCount);
        }

        public FrameworkFlowTriggerSubmission CompleteFailed(
            string source,
            string reason,
            string message,
            int issueCount,
            int blockingIssueCount)
        {
            return Complete(OutcomeFailed, false, false, true, source, reason, message, issueCount, blockingIssueCount);
        }

        public FrameworkFlowTriggerSubmission Complete(
            string outcome,
            bool succeeded,
            bool ignored,
            bool failed,
            string source,
            string reason,
            string message,
            int issueCount,
            int blockingIssueCount)
        {
            return Set(
                false,
                false,
                true,
                succeeded,
                ignored,
                failed,
                PhaseCompleted,
                outcome,
                source,
                reason,
                message,
                issueCount,
                blockingIssueCount);
        }

        public FrameworkFlowTriggerSubmission Clear()
        {
            return Set(
                false,
                false,
                true,
                false,
                false,
                false,
                PhaseCompleted,
                OutcomeNone,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0);
        }

        private FrameworkFlowTriggerSubmission Set(
            bool inFlight,
            bool submitted,
            bool completed,
            bool succeeded,
            bool ignored,
            bool failed,
            string phase,
            string outcome,
            string source,
            string reason,
            string message,
            int issueCount,
            int blockingIssueCount)
        {
            _lastSubmission = new FrameworkFlowTriggerSubmission(
                inFlight,
                submitted,
                completed,
                succeeded,
                ignored,
                failed,
                phase.NormalizeText(),
                outcome.NormalizeText(),
                source.NormalizeText(),
                reason.NormalizeText(),
                message.NormalizeText(),
                issueCount,
                blockingIssueCount);
            return _lastSubmission;
        }
    }
}
