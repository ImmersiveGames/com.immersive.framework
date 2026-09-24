using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Result returned by one synchronous reset participant invocation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset participant result primitive.")]
    public readonly struct ResetParticipantResult : IEquatable<ResetParticipantResult>
    {
        public ResetParticipantResult(
            ResetParticipantDescriptor descriptor,
            ResetParticipantResultStatus status,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            if (!descriptor.IsValid)
            {
                throw new ArgumentException("Reset participant result requires a valid descriptor.", nameof(descriptor));
            }

            if (!Enum.IsDefined(typeof(ResetParticipantResultStatus), status) || status == ResetParticipantResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Reset participant result status must be explicit.");
            }

            Descriptor = descriptor;
            Status = status;
            IssueCount = Math.Max(0, issueCount);
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ResetParticipantDescriptor Descriptor { get; }

        public ResetParticipantResultStatus Status { get; }

        public int IssueCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public ResetParticipantId ParticipantId => Descriptor.ParticipantId;

        public ResetSubjectId SubjectId => Descriptor.SubjectId;

        public ResetParticipantRequiredness Requiredness => Descriptor.Requiredness;

        public bool IsRequired => Descriptor.IsRequired;

        public bool IsOptional => Descriptor.IsOptional;

        public bool Succeeded => Status == ResetParticipantResultStatus.Succeeded;

        public bool WasSkipped => Status == ResetParticipantResultStatus.Skipped;

        public bool Failed => Status == ResetParticipantResultStatus.Failed;

        public bool BlocksReset => Failed && IsRequired;

        public bool Equals(ResetParticipantResult other)
        {
            return Descriptor.Equals(other.Descriptor)
                && Status == other.Status
                && IssueCount == other.IssueCount
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetParticipantResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Descriptor.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ IssueCount;
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
            return $"subjectId='{SubjectId.StableText}' participantId='{ParticipantId.StableText}' status='{Status}' requiredness='{Requiredness}' blocksReset='{BlocksReset}' issueCount='{IssueCount}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ResetParticipantResult CreateSucceeded(
            ResetParticipantDescriptor descriptor,
            string source,
            string reason,
            string message)
        {
            return new ResetParticipantResult(descriptor, ResetParticipantResultStatus.Succeeded, 0, source, reason, message);
        }

        public static ResetParticipantResult CreateSkipped(
            ResetParticipantDescriptor descriptor,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            return new ResetParticipantResult(descriptor, ResetParticipantResultStatus.Skipped, issueCount, source, reason, message);
        }

        public static ResetParticipantResult CreateFailed(
            ResetParticipantDescriptor descriptor,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            return new ResetParticipantResult(descriptor, ResetParticipantResultStatus.Failed, Math.Max(1, issueCount), source, reason, message);
        }
    }
}
