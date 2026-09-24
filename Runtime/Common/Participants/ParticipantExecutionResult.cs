using System;
using System.Collections.Generic;

namespace Immersive.Framework.Common
{
    internal sealed class ParticipantExecutionResult
    {
        private readonly ParticipantExecutionIssue[] _issues;
        private readonly string[] _executionOrder;

        internal ParticipantExecutionResult(
            string source,
            string reason,
            IReadOnlyList<string> executionOrder,
            IReadOnlyList<ParticipantExecutionIssue> issues,
            int participantCount,
            int successfulCount,
            int blockingFailureCount,
            int optionalFailureCount,
            int invalidResultCount,
            int exceptionCount)
        {
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            _executionOrder = FrameworkCollectionCopy.ToArrayOrEmpty(executionOrder);
            _issues = FrameworkCollectionCopy.ToArrayOrEmpty(issues);
            ParticipantCount = participantCount;
            SuccessfulCount = successfulCount;
            BlockingFailureCount = blockingFailureCount;
            OptionalFailureCount = optionalFailureCount;
            InvalidResultCount = invalidResultCount;
            ExceptionCount = exceptionCount;
        }

        public string Source { get; }

        public string Reason { get; }

        public int ParticipantCount { get; }

        public int SuccessfulCount { get; }

        public int BlockingFailureCount { get; }

        public int OptionalFailureCount { get; }

        public int InvalidResultCount { get; }

        public int ExceptionCount { get; }

        public int FailedCount => BlockingFailureCount + OptionalFailureCount + InvalidResultCount + ExceptionCount;

        public IReadOnlyList<string> ExecutionOrder => _executionOrder ?? Array.Empty<string>();

        public IReadOnlyList<ParticipantExecutionIssue> Issues => _issues ?? Array.Empty<ParticipantExecutionIssue>();

        public int IssueCount => FrameworkIssueCounting.Sum(Issues, issue => issue.IssueCount);

        public int BlockingIssueCount => FrameworkIssueCounting.Sum(Issues, issue => issue.IsBlocking ? issue.IssueCount : 0);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public bool HasIssues => FrameworkIssueCounting.HasAny(Issues);

        public bool HasBlockingIssues => FrameworkIssueCounting.HasAnyWhere(Issues, issue => issue.IsBlocking);

        public bool Succeeded => FailedCount == 0;

        public bool Failed => FailedCount > 0;

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"source='{sourceText}' reason='{reasonText}' participants='{ParticipantCount}' succeeded='{SuccessfulCount}' failed='{FailedCount}' blockingFailures='{BlockingFailureCount}' optionalFailures='{OptionalFailureCount}' invalidResults='{InvalidResultCount}' exceptions='{ExceptionCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' order=[{string.Join("; ", ExecutionOrder)}] issuesDetail=[{BuildIssuesText()}]";
        }

        private string BuildIssuesText()
        {
            if (Issues.Count == 0)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < Issues.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                builder.Append(Issues[i].ToDiagnosticString());
            }

            return builder.ToString();
        }
    }
}
