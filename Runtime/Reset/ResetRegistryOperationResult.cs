using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Structured result for one ResetRegistry mutation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset registry mutation result primitive.")]
    public readonly struct ResetRegistryOperationResult : IEquatable<ResetRegistryOperationResult>
    {
        private readonly ResetIssue[] _issues;

        public ResetRegistryOperationResult(
            ResetRegistryOperationStatus status,
            ResetRegistrationHandle handle,
            ResetSubject subject,
            ResetParticipantDescriptor participant,
            IReadOnlyList<ResetIssue> issues,
            string message)
        {
            if (!Enum.IsDefined(typeof(ResetRegistryOperationStatus), status) || status == ResetRegistryOperationStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Reset registry operation status must be explicit.");
            }

            Status = status;
            Handle = handle;
            Subject = subject;
            Participant = participant;
            _issues = issues == null ? Array.Empty<ResetIssue>() : issues.ToArray();
            Message = message.NormalizeText();
        }

        public ResetRegistryOperationStatus Status { get; }

        public ResetRegistrationHandle Handle { get; }

        public ResetSubject Subject { get; }

        public ResetParticipantDescriptor Participant { get; }

        public IReadOnlyList<ResetIssue> Issues => _issues ?? Array.Empty<ResetIssue>();

        public string Message { get; }

        public bool Succeeded => Status is ResetRegistryOperationStatus.Registered or ResetRegistryOperationStatus.Unregistered or ResetRegistryOperationStatus.AlreadyUnregistered;

        public bool Failed => !Succeeded;

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public ResetIssue[] SnapshotIssues()
        {
            if (IssueCount == 0)
            {
                return Array.Empty<ResetIssue>();
            }

            var snapshot = new ResetIssue[IssueCount];
            for (int i = 0; i < IssueCount; i++)
            {
                snapshot[i] = Issues[i];
            }

            return snapshot;
        }

        public bool Equals(ResetRegistryOperationResult other)
        {
            if (Status != other.Status
                || !Handle.Equals(other.Handle)
                || !Subject.Equals(other.Subject)
                || !Participant.Equals(other.Participant)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || IssueCount != other.IssueCount)
            {
                return false;
            }

            for (int i = 0; i < IssueCount; i++)
            {
                if (!Issues[i].Equals(other.Issues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ResetRegistryOperationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ Handle.GetHashCode();
                hashCode = hashCode * 397 ^ Subject.GetHashCode();
                hashCode = hashCode * 397 ^ Participant.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                for (int i = 0; i < IssueCount; i++)
                {
                    hashCode = hashCode * 397 ^ Issues[i].GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            string subjectId = Subject.IsValid ? Subject.SubjectId.StableText : "<none>";
            string participantId = Participant.IsValid ? Participant.ParticipantId.StableText : "<none>";
            string messageText = Message.ToDiagnosticText();
            return $"status='{Status}' handle='{Handle}' subjectId='{subjectId}' participantId='{participantId}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' message='{messageText}'";
        }

        public static ResetRegistryOperationResult RegisteredSubject(
            ResetRegistrationHandle handle,
            ResetSubject subject,
            string message)
        {
            return new ResetRegistryOperationResult(ResetRegistryOperationStatus.Registered, handle, subject, default, Array.Empty<ResetIssue>(), message);
        }

        public static ResetRegistryOperationResult RegisteredParticipant(
            ResetRegistrationHandle handle,
            ResetSubject subject,
            ResetParticipantDescriptor participant,
            string message)
        {
            return new ResetRegistryOperationResult(ResetRegistryOperationStatus.Registered, handle, subject, participant, Array.Empty<ResetIssue>(), message);
        }

        public static ResetRegistryOperationResult Unregistered(
            ResetRegistrationHandle handle,
            ResetSubject subject,
            ResetParticipantDescriptor participant,
            string message)
        {
            return new ResetRegistryOperationResult(ResetRegistryOperationStatus.Unregistered, handle, subject, participant, Array.Empty<ResetIssue>(), message);
        }

        public static ResetRegistryOperationResult AlreadyUnregistered(
            ResetRegistrationHandle handle,
            ResetIssue issue,
            string message)
        {
            return new ResetRegistryOperationResult(ResetRegistryOperationStatus.AlreadyUnregistered, handle, default, default, new[] { issue }, message);
        }

        public static ResetRegistryOperationResult Rejected(
            ResetRegistryOperationStatus status,
            ResetIssue issue,
            string message)
        {
            return new ResetRegistryOperationResult(status, default, default, default, new[] { issue }, message);
        }
    }
}
