using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Aggregate result for one ResetExecutor run.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12C ResetExecutor aggregate result.")]
    public readonly struct ResetExecutionResult : IEquatable<ResetExecutionResult>
    {
        private readonly IReadOnlyList<ResetSubjectResult> _subjects;
        private readonly IReadOnlyList<ResetIssue> _issues;

        public ResetExecutionResult(
            ResetExecutionStatus status,
            IReadOnlyList<ResetSubjectResult> subjects,
            IReadOnlyList<ResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ResetExecutionStatus), status) || status == ResetExecutionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Reset execution result status must be explicit.");
            }

            Status = status;
            _subjects = subjects == null ? Array.Empty<ResetSubjectResult>() : subjects.ToArray();
            _issues = issues == null ? Array.Empty<ResetIssue>() : issues.ToArray();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ResetExecutionStatus Status { get; }

        public IReadOnlyList<ResetSubjectResult> Subjects => _subjects ?? Array.Empty<ResetSubjectResult>();

        public IReadOnlyList<ResetIssue> Issues => _issues ?? Array.Empty<ResetIssue>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == ResetExecutionStatus.Succeeded || Status == ResetExecutionStatus.SucceededNoSubjects;

        public bool Failed => !Succeeded;

        public int SubjectCount => Subjects.Count;

        public int SubjectSucceeded => Subjects.Count(subject => subject.Succeeded);

        public int SubjectFailed => Subjects.Count(subject => subject.Failed);

        public int ParticipantCount => Subjects.Sum(subject => subject.ParticipantCount);

        public int ParticipantSucceeded => Subjects.Sum(subject => subject.ParticipantSucceeded);

        public int ParticipantSkipped => Subjects.Sum(subject => subject.ParticipantSkipped);

        public int ParticipantFailed => Subjects.Sum(subject => subject.ParticipantFailed);

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking) + Subjects.Sum(subject => subject.BlockingIssueCount);

        public int NonBlockingIssueCount => Issues.Count(issue => !issue.IsBlocking) + Subjects.Sum(subject => subject.NonBlockingIssueCount);

        public bool Equals(ResetExecutionResult other)
        {
            return Status == other.Status
                && Subjects.SequenceEqual(other.Subjects)
                && Issues.SequenceEqual(other.Issues)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetExecutionResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                for (int i = 0; i < Subjects.Count; i++)
                {
                    hashCode = hashCode * 397 ^ Subjects[i].GetHashCode();
                }

                for (int i = 0; i < Issues.Count; i++)
                {
                    hashCode = hashCode * 397 ^ Issues[i].GetHashCode();
                }

                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"status='{Status}' subjects='{SubjectCount}' subjectSucceeded='{SubjectSucceeded}' subjectFailed='{SubjectFailed}' participants='{ParticipantCount}' participantSucceeded='{ParticipantSucceeded}' participantSkipped='{ParticipantSkipped}' participantFailed='{ParticipantFailed}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ResetExecutionResult RejectedInvalidRequest(ResetIssue issue, string source, string reason)
        {
            return new ResetExecutionResult(
                ResetExecutionStatus.RejectedInvalidRequest,
                Array.Empty<ResetSubjectResult>(),
                new[] { issue },
                source,
                reason,
                "Reset execution rejected because the request is invalid.");
        }

        public static ResetExecutionResult FailedNoSubjects(ResetIssue issue, string source, string reason)
        {
            return new ResetExecutionResult(
                ResetExecutionStatus.FailedNoSubjects,
                Array.Empty<ResetSubjectResult>(),
                new[] { issue },
                source,
                reason,
                "Reset execution failed because no subjects were selected.");
        }

        public static ResetExecutionResult SucceededNoSubjects(ResetIssue issue, string source, string reason)
        {
            return new ResetExecutionResult(
                ResetExecutionStatus.SucceededNoSubjects,
                Array.Empty<ResetSubjectResult>(),
                new[] { issue },
                source,
                reason,
                "Reset execution succeeded with no subjects because the request allowed an empty selection.");
        }

        public static ResetExecutionResult FromSubjectResults(
            IReadOnlyList<ResetSubjectResult> subjects,
            IReadOnlyList<ResetIssue> issues,
            string source,
            string reason,
            bool failed)
        {
            ResetExecutionStatus status = failed ? ResetExecutionStatus.Failed : ResetExecutionStatus.Succeeded;
            return new ResetExecutionResult(
                status,
                subjects,
                issues,
                source,
                reason,
                failed ? "Reset execution completed with blocking issues." : "Reset execution completed successfully.");
        }
    }
}
