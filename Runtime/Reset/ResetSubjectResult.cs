using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Aggregate reset execution result for one ResetSubject.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12C Reset subject execution result.")]
    public readonly struct ResetSubjectResult : IEquatable<ResetSubjectResult>
    {
        private readonly IReadOnlyList<ResetParticipantResult> _participants;
        private readonly IReadOnlyList<ResetIssue> _issues;

        public ResetSubjectResult(
            ResetSubjectId subjectId,
            ResetSubject subject,
            ResetSubjectResultStatus status,
            IReadOnlyList<ResetParticipantResult> participants,
            IReadOnlyList<ResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            if (!subjectId.IsValid)
            {
                throw new ArgumentException("Reset subject result requires a valid subject id.", nameof(subjectId));
            }

            if (!Enum.IsDefined(typeof(ResetSubjectResultStatus), status) || status == ResetSubjectResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Reset subject result status must be explicit.");
            }

            SubjectId = subjectId;
            Subject = subject;
            Status = status;
            _participants = participants == null ? Array.Empty<ResetParticipantResult>() : participants.ToArray();
            _issues = issues == null ? Array.Empty<ResetIssue>() : issues.ToArray();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ResetSubjectId SubjectId { get; }

        public ResetSubject Subject { get; }

        public ResetSubjectResultStatus Status { get; }

        public IReadOnlyList<ResetParticipantResult> Participants => _participants ?? Array.Empty<ResetParticipantResult>();

        public IReadOnlyList<ResetIssue> Issues => _issues ?? Array.Empty<ResetIssue>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool HasSubject => Subject.IsValid;

        public bool Succeeded => Status == ResetSubjectResultStatus.Succeeded || Status == ResetSubjectResultStatus.SkippedNoParticipants;

        public bool Failed => Status == ResetSubjectResultStatus.Failed
            || Status == ResetSubjectResultStatus.FailedNoParticipants
            || Status == ResetSubjectResultStatus.FailedSubjectNotFound;

        public bool HasBlockingIssues => Issues.Any(issue => issue.IsBlocking)
            || Participants.Any(participant => participant.BlocksReset);

        public int ParticipantCount => Participants.Count;

        public int ParticipantSucceeded => Participants.Count(participant => participant.Succeeded);

        public int ParticipantSkipped => Participants.Count(participant => participant.WasSkipped);

        public int ParticipantFailed => Participants.Count(participant => participant.Failed);

        public int ParticipantBlockingFailed => Participants.Count(participant => participant.BlocksReset);

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking) + ParticipantBlockingFailed;

        public int NonBlockingIssueCount => Issues.Count(issue => !issue.IsBlocking)
            + Participants.Where(participant => participant.Failed && !participant.BlocksReset).Sum(participant => Math.Max(1, participant.IssueCount))
            + Participants.Where(participant => participant.WasSkipped).Sum(participant => participant.IssueCount);

        public bool Equals(ResetSubjectResult other)
        {
            return SubjectId.Equals(other.SubjectId)
                && Subject.Equals(other.Subject)
                && Status == other.Status
                && Participants.SequenceEqual(other.Participants)
                && Issues.SequenceEqual(other.Issues)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetSubjectResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SubjectId.GetHashCode();
                hashCode = hashCode * 397 ^ Subject.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                for (int i = 0; i < Participants.Count; i++)
                {
                    hashCode = hashCode * 397 ^ Participants[i].GetHashCode();
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
            return $"subjectId='{SubjectId.StableText}' status='{Status}' participants='{ParticipantCount}' participantSucceeded='{ParticipantSucceeded}' participantSkipped='{ParticipantSkipped}' participantFailed='{ParticipantFailed}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ResetSubjectResult SucceededResult(
            ResetSubject subject,
            IReadOnlyList<ResetParticipantResult> participants,
            IReadOnlyList<ResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            return new ResetSubjectResult(subject.SubjectId, subject, ResetSubjectResultStatus.Succeeded, participants, issues, source, reason, message);
        }

        public static ResetSubjectResult SkippedNoParticipants(
            ResetSubject subject,
            ResetIssue issue,
            string source,
            string reason)
        {
            return new ResetSubjectResult(
                subject.SubjectId,
                subject,
                ResetSubjectResultStatus.SkippedNoParticipants,
                Array.Empty<ResetParticipantResult>(),
                new[] { issue },
                source,
                reason,
                "Reset subject skipped because no participants were registered and the request allowed empty participants.");
        }

        public static ResetSubjectResult FailedNoParticipants(
            ResetSubject subject,
            ResetIssue issue,
            string source,
            string reason)
        {
            return new ResetSubjectResult(
                subject.SubjectId,
                subject,
                ResetSubjectResultStatus.FailedNoParticipants,
                Array.Empty<ResetParticipantResult>(),
                new[] { issue },
                source,
                reason,
                "Reset subject failed because no participants were registered.");
        }

        public static ResetSubjectResult FailedSubjectNotFound(
            ResetSubjectId subjectId,
            ResetIssue issue,
            string source,
            string reason)
        {
            return new ResetSubjectResult(
                subjectId,
                default,
                ResetSubjectResultStatus.FailedSubjectNotFound,
                Array.Empty<ResetParticipantResult>(),
                new[] { issue },
                source,
                reason,
                "Reset subject failed because the requested subject was not registered.");
        }

        public static ResetSubjectResult FailedResult(
            ResetSubject subject,
            IReadOnlyList<ResetParticipantResult> participants,
            IReadOnlyList<ResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            return new ResetSubjectResult(subject.SubjectId, subject, ResetSubjectResultStatus.Failed, participants, issues, source, reason, message);
        }
    }
}
