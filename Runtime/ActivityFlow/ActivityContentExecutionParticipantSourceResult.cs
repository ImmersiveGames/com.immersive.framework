using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive diagnostic result produced by an Activity Content Execution participant source.
    /// It carries a participant collection and source-level diagnostics only; it does not execute participants.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10K Activity Content Execution participant source result; source diagnostics only.")]
    public readonly struct ActivityContentExecutionParticipantSourceResult : IEquatable<ActivityContentExecutionParticipantSourceResult>
    {
        private ActivityContentExecutionParticipantSourceResult(
            ActivityContentExecutionParticipantSourceRequest request,
            ActivityContentExecutionParticipantCollection collection,
            ActivityContentExecutionParticipantSourceStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionParticipantSourceStatus), status)
                || status == ActivityContentExecutionParticipantSourceStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution participant source status must be explicit.");
            }

            Request = request;
            Collection = collection;
            Status = status;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        private ActivityContentExecutionParticipantSourceRequest Request { get; }

        public ActivityContentExecutionParticipantCollection Collection { get; }

        private ActivityContentExecutionParticipantSourceStatus Status { get; }

        private string Source { get; }

        private string Reason { get; }

        private string Message { get; }

        public bool Executed => Status != ActivityContentExecutionParticipantSourceStatus.None
            && Status != ActivityContentExecutionParticipantSourceStatus.Unknown;

        public bool Failed => Status is ActivityContentExecutionParticipantSourceStatus.Failed or ActivityContentExecutionParticipantSourceStatus.FailedException or ActivityContentExecutionParticipantSourceStatus.RejectedInvalidRequest;

        public bool HasIssues => Collection.HasIssues || Failed;

        private int ParticipantCount => Collection.Count;

        public int IssueCount => Collection.IssueCount + (Failed ? 1 : 0);

        public string DiagnosticStatus => Status.ToString();

        public bool Equals(ActivityContentExecutionParticipantSourceResult other)
        {
            return Request.Equals(other.Request)
                && Collection.Count == other.Collection.Count
                && Collection.IssueCount == other.Collection.IssueCount
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionParticipantSourceResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ Collection.Count;
                hashCode = hashCode * 397 ^ Collection.IssueCount;
                hashCode = hashCode * 397 ^ (int)Status;
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
            return $"status='{Status}' participants='{ParticipantCount}' issues='{IssueCount}' request=({Request.ToDiagnosticString()}) source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ActivityContentExecutionParticipantSourceResult None(string source, string reason, string message)
        {
            return new ActivityContentExecutionParticipantSourceResult(
                default,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.None,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionParticipantSourceResult FromCollection(
            ActivityContentExecutionParticipantSourceRequest request,
            ActivityContentExecutionParticipantCollection collection,
            string source,
            string reason,
            string message)
        {
            var status = ResolveSuccessStatus(collection);
            return new ActivityContentExecutionParticipantSourceResult(request, collection, status, source, reason, message);
        }

        public static ActivityContentExecutionParticipantSourceResult SucceededNoParticipants(
            ActivityContentExecutionParticipantSourceRequest request,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionParticipantSourceResult(
                request,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.SucceededNoParticipants,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionParticipantSourceResult RejectedInvalidRequest(
            ActivityContentExecutionParticipantSourceRequest request,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionParticipantSourceResult(
                request,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.RejectedInvalidRequest,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionParticipantSourceResult FailedResult(
            ActivityContentExecutionParticipantSourceRequest request,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionParticipantSourceResult(
                request,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.Failed,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionParticipantSourceResult FailedException(
            ActivityContentExecutionParticipantSourceRequest request,
            Exception exception,
            string source,
            string reason)
        {
            string message = exception == null
                ? "Activity content execution participant source failed with an unknown exception."
                : $"Activity content execution participant source threw '{exception.GetType().Name}': {exception.Message}";

            return new ActivityContentExecutionParticipantSourceResult(
                request,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.FailedException,
                source,
                reason,
                message);
        }

        private static ActivityContentExecutionParticipantSourceStatus ResolveSuccessStatus(ActivityContentExecutionParticipantCollection collection)
        {
            if (collection is { HasParticipants: true, HasIssues: true })
            {
                return ActivityContentExecutionParticipantSourceStatus.SucceededWithIssues;
            }

            return collection.HasParticipants
                ? ActivityContentExecutionParticipantSourceStatus.Succeeded
                : ActivityContentExecutionParticipantSourceStatus.SucceededNoParticipants;
        }
    }
}
