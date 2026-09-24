using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.FlowTriggers
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F42 neutral FlowTrigger submission snapshot; no domain request or runtime ownership.")]
    internal readonly struct FrameworkFlowTriggerSubmission
    {
        internal FrameworkFlowTriggerSubmission(
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
            InFlight = inFlight;
            Submitted = submitted;
            Completed = completed;
            Succeeded = succeeded;
            Ignored = ignored;
            Failed = failed;
            Phase = phase.NormalizeText();
            Outcome = outcome.NormalizeText();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
            IssueCount = issueCount < 0 ? 0 : issueCount;
            BlockingIssueCount = blockingIssueCount < 0 ? 0 : blockingIssueCount;
        }

        public bool InFlight { get; }

        public bool Submitted { get; }

        public bool Completed { get; }

        public bool Succeeded { get; }

        public bool Ignored { get; }

        public bool Failed { get; }

        public string Phase { get; }

        public string Outcome { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public int IssueCount { get; }

        public int BlockingIssueCount { get; }
    }
}
