using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Passive result returned by one Cycle Reset participant.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset participant result; diagnostics only.")]
    public readonly struct CycleResetParticipantResult : IEquatable<CycleResetParticipantResult>
    {
        public CycleResetParticipantResult(
            CycleResetRequest request,
            CycleResetParticipantDescriptor participant,
            CycleResetParticipantResultStatus status,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Cycle Reset participant result requires a valid request.", nameof(request));
            }

            if (!participant.IsValid)
            {
                throw new ArgumentException("Cycle Reset participant result requires a valid participant descriptor.", nameof(participant));
            }

            if (!participant.SupportsRequest(request))
            {
                throw new ArgumentException("Cycle Reset participant result descriptor does not support the request scope.", nameof(participant));
            }

            if (!Enum.IsDefined(typeof(CycleResetParticipantResultStatus), status)
                || status == CycleResetParticipantResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Cycle Reset participant result status must be explicit.");
            }

            if (issueCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(issueCount), issueCount, "Cycle Reset participant issue count cannot be negative.");
            }

            Request = request;
            Participant = participant;
            Status = status;
            IssueCount = issueCount;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public CycleResetRequest Request { get; }

        public CycleResetParticipantDescriptor Participant { get; }

        public CycleResetParticipantId ParticipantId => Participant.ParticipantId;

        public CycleResetScope ParticipantScope => Participant.ParticipantScope;

        public CycleResetParticipantRequiredness Requiredness => Participant.Requiredness;

        public CycleResetParticipantResultStatus Status { get; }

        public int IssueCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == CycleResetParticipantResultStatus.Succeeded;

        public bool WasSkipped => Status is CycleResetParticipantResultStatus.SkippedNotApplicable or CycleResetParticipantResultStatus.SkippedOptional;

        public bool Failed => Status is CycleResetParticipantResultStatus.FailedBlocking or CycleResetParticipantResultStatus.FailedNonBlocking or CycleResetParticipantResultStatus.RejectedInvalidRequest;

        public bool BlocksReset => Status is CycleResetParticipantResultStatus.FailedBlocking or CycleResetParticipantResultStatus.RejectedInvalidRequest;

        public bool IsRequired => Participant.IsRequired;

        public bool IsOptional => Participant.IsOptional;

        public bool IsValid => Request.IsValid
            && Participant.IsValid
            && Participant.SupportsRequest(Request)
            && Status != CycleResetParticipantResultStatus.Unknown
            && IssueCount >= 0;

        public bool Equals(CycleResetParticipantResult other)
        {
            return Request.Equals(other.Request)
                && Participant.Equals(other.Participant)
                && Status == other.Status
                && IssueCount == other.IssueCount
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetParticipantResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ Participant.GetHashCode();
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
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"participantId='{ParticipantId.StableText}' participantScope='{ParticipantScope}' requiredness='{Requiredness}' status='{Status}' succeeded='{Succeeded}' skipped='{WasSkipped}' failed='{Failed}' blocksReset='{BlocksReset}' issueCount='{IssueCount}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static CycleResetParticipantResult Success(
            CycleResetContext context,
            string source,
            string reason,
            string message)
        {
            return new CycleResetParticipantResult(
                context.Request,
                context.Participant,
                CycleResetParticipantResultStatus.Succeeded,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Cycle Reset participant succeeded." : message);
        }

        public static CycleResetParticipantResult Skipped(
            CycleResetContext context,
            CycleResetParticipantResultStatus status,
            string source,
            string reason,
            string message)
        {
            if (status != CycleResetParticipantResultStatus.SkippedNotApplicable
                && status != CycleResetParticipantResultStatus.SkippedOptional)
            {
                throw new ArgumentException("Cycle Reset skipped participant result requires a skipped status.", nameof(status));
            }

            return new CycleResetParticipantResult(
                context.Request,
                context.Participant,
                status,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Cycle Reset participant skipped." : message);
        }

        public static CycleResetParticipantResult Failure(
            CycleResetContext context,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            return context.IsRequired
                ? BlockingFailure(context, issueCount, source, reason, message)
                : NonBlockingFailure(context, issueCount, source, reason, message);
        }

        public static CycleResetParticipantResult BlockingFailure(
            CycleResetContext context,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            return new CycleResetParticipantResult(
                context.Request,
                context.Participant,
                CycleResetParticipantResultStatus.FailedBlocking,
                Math.Max(1, issueCount),
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Required Cycle Reset participant failed." : message);
        }

        public static CycleResetParticipantResult NonBlockingFailure(
            CycleResetContext context,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            return new CycleResetParticipantResult(
                context.Request,
                context.Participant,
                CycleResetParticipantResultStatus.FailedNonBlocking,
                Math.Max(1, issueCount),
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Optional Cycle Reset participant failed." : message);
        }

        public static bool operator ==(CycleResetParticipantResult left, CycleResetParticipantResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetParticipantResult left, CycleResetParticipantResult right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
