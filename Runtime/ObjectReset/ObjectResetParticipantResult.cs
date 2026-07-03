using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Passive result returned by one Object Reset participant.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C Object Reset participant result; diagnostics only.")]
    public readonly struct ObjectResetParticipantResult : IEquatable<ObjectResetParticipantResult>
    {
        public ObjectResetParticipantResult(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget,
            ObjectResetParticipantDescriptor participant,
            ObjectResetParticipantResultStatus status,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Object Reset participant result requires a valid request.", nameof(request));
            }

            if (!request.Target.Matches(resolvedTarget))
            {
                throw new ArgumentException("Object Reset participant result requires a resolved target matching the request target.", nameof(resolvedTarget));
            }

            if (!participant.IsValid)
            {
                throw new ArgumentException("Object Reset participant result requires a valid participant descriptor.", nameof(participant));
            }

            if (!participant.SupportsResolvedTarget(request, resolvedTarget))
            {
                throw new ArgumentException("Object Reset participant result descriptor does not support the resolved request target.", nameof(participant));
            }

            if (!Enum.IsDefined(typeof(ObjectResetParticipantResultStatus), status)
                || status == ObjectResetParticipantResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Object Reset participant result status must be explicit.");
            }

            if (issueCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(issueCount), issueCount, "Object Reset participant issue count cannot be negative.");
            }

            Request = request;
            ResolvedTarget = resolvedTarget;
            Participant = participant;
            Status = status;
            IssueCount = issueCount;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ObjectResetRequest Request { get; }

        public ObjectEntryDescriptor ResolvedTarget { get; }

        public ObjectResetParticipantDescriptor Participant { get; }

        public ObjectResetParticipantId ParticipantId => Participant.ParticipantId;

        public ObjectResetTarget Target => Participant.Target;

        public ObjectResetParticipantRequiredness Requiredness => Participant.Requiredness;

        public ObjectResetParticipantResultStatus Status { get; }

        public int IssueCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == ObjectResetParticipantResultStatus.Succeeded;

        public bool WasSkipped => Status is ObjectResetParticipantResultStatus.SkippedNotApplicable or ObjectResetParticipantResultStatus.SkippedOptional;

        public bool Failed => Status is ObjectResetParticipantResultStatus.FailedBlocking or ObjectResetParticipantResultStatus.FailedNonBlocking or ObjectResetParticipantResultStatus.RejectedInvalidRequest;

        public bool BlocksReset => Status is ObjectResetParticipantResultStatus.FailedBlocking or ObjectResetParticipantResultStatus.RejectedInvalidRequest;

        public bool IsRequired => Participant.IsRequired;

        public bool IsOptional => Participant.IsOptional;

        public bool IsValid => Request.IsValid
            && Request.Target.Matches(ResolvedTarget)
            && Participant.IsValid
            && Participant.SupportsResolvedTarget(Request, ResolvedTarget)
            && Status != ObjectResetParticipantResultStatus.Unknown
            && IssueCount >= 0;

        public bool Equals(ObjectResetParticipantResult other)
        {
            return Request.Equals(other.Request)
                && ResolvedTarget.Equals(other.ResolvedTarget)
                && Participant.Equals(other.Participant)
                && Status == other.Status
                && IssueCount == other.IssueCount
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetParticipantResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ ResolvedTarget.GetHashCode();
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
            return $"participantId='{ParticipantId.StableText}' requiredness='{Requiredness}' status='{Status}' succeeded='{Succeeded}' skipped='{WasSkipped}' failed='{Failed}' blocksReset='{BlocksReset}' issueCount='{IssueCount}' source='{sourceText}' reason='{reasonText}' message='{messageText}' {Target.ToDiagnosticString()}";
        }

        public static ObjectResetParticipantResult Success(
            ObjectResetContext context,
            string source,
            string reason,
            string message)
        {
            return new ObjectResetParticipantResult(
                context.Request,
                context.ResolvedTarget,
                context.Participant,
                ObjectResetParticipantResultStatus.Succeeded,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Object Reset participant succeeded." : message);
        }

        public static ObjectResetParticipantResult Skipped(
            ObjectResetContext context,
            ObjectResetParticipantResultStatus status,
            string source,
            string reason,
            string message)
        {
            if (status != ObjectResetParticipantResultStatus.SkippedNotApplicable
                && status != ObjectResetParticipantResultStatus.SkippedOptional)
            {
                throw new ArgumentException("Object Reset skipped participant result requires a skipped status.", nameof(status));
            }

            return new ObjectResetParticipantResult(
                context.Request,
                context.ResolvedTarget,
                context.Participant,
                status,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Object Reset participant skipped." : message);
        }

        public static ObjectResetParticipantResult Failure(
            ObjectResetContext context,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            return context.IsRequired
                ? BlockingFailure(context, issueCount, source, reason, message)
                : NonBlockingFailure(context, issueCount, source, reason, message);
        }

        public static ObjectResetParticipantResult BlockingFailure(
            ObjectResetContext context,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            return new ObjectResetParticipantResult(
                context.Request,
                context.ResolvedTarget,
                context.Participant,
                ObjectResetParticipantResultStatus.FailedBlocking,
                Math.Max(1, issueCount),
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Required Object Reset participant failed." : message);
        }

        public static ObjectResetParticipantResult NonBlockingFailure(
            ObjectResetContext context,
            int issueCount,
            string source,
            string reason,
            string message)
        {
            return new ObjectResetParticipantResult(
                context.Request,
                context.ResolvedTarget,
                context.Participant,
                ObjectResetParticipantResultStatus.FailedNonBlocking,
                Math.Max(1, issueCount),
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Optional Object Reset participant failed." : message);
        }

        public static bool operator ==(ObjectResetParticipantResult left, ObjectResetParticipantResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectResetParticipantResult left, ObjectResetParticipantResult right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
