using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive result for one logical Activity Content Execution operation.
    /// It records readiness contribution and diagnostics only; it does not mutate GameObjects, reset objects, release physical resources or perform gameplay behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10B Activity Content Execution result contract; no execution runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionResult : IEquatable<ActivityContentExecutionResult>
    {
        private ActivityContentExecutionResult(
            ActivityContentExecutionRequest request,
            ActivityContentExecutionStatus status,
            int blockingIssueCount,
            int nonBlockingIssueCount,
            string source,
            string reason,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Activity content execution result requires a valid request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(ActivityContentExecutionStatus), status)
                || status == ActivityContentExecutionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution status must be explicit.");
            }

            if (blockingIssueCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockingIssueCount), blockingIssueCount, "Blocking issue count cannot be negative.");
            }

            if (nonBlockingIssueCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nonBlockingIssueCount), nonBlockingIssueCount, "Non-blocking issue count cannot be negative.");
            }

            Request = request;
            Status = status;
            BlockingIssueCount = blockingIssueCount;
            NonBlockingIssueCount = nonBlockingIssueCount;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ActivityContentExecutionRequest Request { get; }

        public ActivityContentExecutionStatus Status { get; }

        public int BlockingIssueCount { get; }

        public int NonBlockingIssueCount { get; }

        private string Source { get; }

        private string Reason { get; }

        private string Message { get; }

        public ActivityContentExecutionPhase Phase => Request.Phase;

        public ActivityContentExecutionRequiredness Requiredness => Request.Requiredness;

        private RuntimeContentIdentity Identity => Request.Identity;

        private RuntimeContentOwner Owner => Request.Owner;

        public bool Succeeded => Status is ActivityContentExecutionStatus.Succeeded or ActivityContentExecutionStatus.SucceededNoOp;

        public bool Skipped => Status is ActivityContentExecutionStatus.SkippedNotApplicable or ActivityContentExecutionStatus.SkippedOptional;

        public bool Failed => Status is ActivityContentExecutionStatus.FailedNonBlocking or ActivityContentExecutionStatus.FailedBlocking or ActivityContentExecutionStatus.RejectedInvalidRequest;

        public bool HasBlockingIssues => BlockingIssueCount > 0 || Status == ActivityContentExecutionStatus.FailedBlocking;

        public bool HasNonBlockingIssues => NonBlockingIssueCount > 0 || Status == ActivityContentExecutionStatus.FailedNonBlocking;

        private bool BlocksReadiness => HasBlockingIssues;

        public bool Equals(ActivityContentExecutionResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && BlockingIssueCount == other.BlockingIssueCount
                && NonBlockingIssueCount == other.NonBlockingIssueCount
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ BlockingIssueCount;
                hashCode = hashCode * 397 ^ NonBlockingIssueCount;
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
            return $"phase='{Phase}' identity='{Identity.StableText}' owner='{Owner.StableText}' status='{Status}' requiredness='{Requiredness}' succeeded='{Succeeded}' skipped='{Skipped}' failed='{Failed}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' blocksReadiness='{BlocksReadiness}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ActivityContentExecutionResult Success(
            ActivityContentExecutionRequest request,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionResult(
                request,
                ActivityContentExecutionStatus.Succeeded,
                0,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution succeeded." : message);
        }

        public static ActivityContentExecutionResult SucceededNoOp(
            ActivityContentExecutionRequest request,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionResult(
                request,
                ActivityContentExecutionStatus.SucceededNoOp,
                0,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution completed with no operation." : message);
        }

        public static ActivityContentExecutionResult SkippedResult(
            ActivityContentExecutionRequest request,
            ActivityContentExecutionStatus status,
            string source,
            string reason,
            string message)
        {
            if (status != ActivityContentExecutionStatus.SkippedNotApplicable
                && status != ActivityContentExecutionStatus.SkippedOptional)
            {
                throw new ArgumentException("Skipped Activity content execution result requires a skipped status.", nameof(status));
            }

            return new ActivityContentExecutionResult(
                request,
                status,
                0,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution was skipped." : message);
        }

        public static ActivityContentExecutionResult NonBlockingFailure(
            ActivityContentExecutionRequest request,
            int nonBlockingIssueCount,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionResult(
                request,
                ActivityContentExecutionStatus.FailedNonBlocking,
                0,
                nonBlockingIssueCount <= 0 ? 1 : nonBlockingIssueCount,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution failed without blocking readiness." : message);
        }

        public static ActivityContentExecutionResult BlockingFailure(
            ActivityContentExecutionRequest request,
            int blockingIssueCount,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionResult(
                request,
                ActivityContentExecutionStatus.FailedBlocking,
                blockingIssueCount <= 0 ? 1 : blockingIssueCount,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution failed and blocks readiness." : message);
        }
    }
}
