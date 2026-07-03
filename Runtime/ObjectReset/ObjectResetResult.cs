using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Aggregate result for Object Reset foundation operations.
    /// F14D includes target resolution, deterministic plan execution and participant aggregation; no Unity reset side effects are implemented here.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14D Object Reset aggregate result; target resolution and participant aggregation only.")]
    public readonly struct ObjectResetResult : IEquatable<ObjectResetResult>
    {
        private readonly ObjectResetIssue[] _issues;
        private readonly ObjectResetParticipantResult[] _participantResults;

        public ObjectResetResult(
            ObjectResetRequest request,
            ObjectResetResultStatus status,
            ObjectEntryDescriptor resolvedTarget,
            bool hasResolvedTarget,
            IReadOnlyList<ObjectResetIssue> issues,
            string message)
            : this(
                request,
                status,
                resolvedTarget,
                hasResolvedTarget,
                Array.Empty<ObjectResetParticipantResult>(),
                issues,
                message)
        {
        }

        public ObjectResetResult(
            ObjectResetRequest request,
            ObjectResetResultStatus status,
            ObjectEntryDescriptor resolvedTarget,
            bool hasResolvedTarget,
            IReadOnlyList<ObjectResetParticipantResult> participantResults,
            IReadOnlyList<ObjectResetIssue> issues,
            string message)
        {
            if (!Enum.IsDefined(typeof(ObjectResetResultStatus), status) || status == ObjectResetResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Object Reset result status must be explicit.");
            }

            Request = request;
            Status = status;
            ResolvedTarget = resolvedTarget;
            HasResolvedTarget = hasResolvedTarget;
            _participantResults = participantResults == null
                ? Array.Empty<ObjectResetParticipantResult>()
                : participantResults.ToArray();
            _issues = issues == null ? Array.Empty<ObjectResetIssue>() : issues.ToArray();
            Message = message.NormalizeText();
        }

        public ObjectResetRequest Request { get; }

        public ObjectResetResultStatus Status { get; }

        public ObjectEntryDescriptor ResolvedTarget { get; }

        public bool HasResolvedTarget { get; }

        public IReadOnlyList<ObjectResetParticipantResult> ParticipantResults => _participantResults ?? Array.Empty<ObjectResetParticipantResult>();

        public IReadOnlyList<ObjectResetIssue> Issues => _issues ?? Array.Empty<ObjectResetIssue>();

        public string Message { get; }

        public bool Succeeded => Status is ObjectResetResultStatus.Succeeded or ObjectResetResultStatus.SucceededNoParticipants;

        public bool CompletedWithWarnings => Status == ObjectResetResultStatus.CompletedWithWarnings;

        public bool Failed => Status is ObjectResetResultStatus.Failed or ObjectResetResultStatus.RejectedInvalidRequest or ObjectResetResultStatus.RejectedRuntimeContextUnavailable or ObjectResetResultStatus.RejectedTargetNotFound or ObjectResetResultStatus.RejectedForeignTarget;

        public int ParticipantCount => ParticipantResults.Count;

        public int ParticipantSucceededCount => ParticipantResults.Count(result => result.Succeeded);

        public int ParticipantSkippedCount => ParticipantResults.Count(result => result.WasSkipped);

        public int ParticipantFailedCount => ParticipantResults.Count(result => result.Failed);

        public int ParticipantBlockingFailureCount => ParticipantResults.Count(result => result.BlocksReset);

        public int RequiredParticipantCount => ParticipantResults.Count(result => result.IsRequired);

        public int OptionalParticipantCount => ParticipantResults.Count(result => result.IsOptional);

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public ObjectResetIssue[] SnapshotIssues()
        {
            if (IssueCount == 0)
            {
                return Array.Empty<ObjectResetIssue>();
            }

            var snapshot = new ObjectResetIssue[IssueCount];
            for (int i = 0; i < IssueCount; i++)
            {
                snapshot[i] = Issues[i];
            }

            return snapshot;
        }

        public ObjectResetParticipantResult[] SnapshotParticipantResults()
        {
            if (ParticipantCount == 0)
            {
                return Array.Empty<ObjectResetParticipantResult>();
            }

            var snapshot = new ObjectResetParticipantResult[ParticipantCount];
            for (int i = 0; i < ParticipantCount; i++)
            {
                snapshot[i] = ParticipantResults[i];
            }

            return snapshot;
        }

        public bool Equals(ObjectResetResult other)
        {
            if (!Request.Equals(other.Request)
                || Status != other.Status
                || HasResolvedTarget != other.HasResolvedTarget
                || !ResolvedTarget.Equals(other.ResolvedTarget)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || IssueCount != other.IssueCount
                || ParticipantCount != other.ParticipantCount)
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

            for (int i = 0; i < ParticipantCount; i++)
            {
                if (!ParticipantResults[i].Equals(other.ParticipantResults[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ (HasResolvedTarget ? 1 : 0);
                hashCode = hashCode * 397 ^ ResolvedTarget.GetHashCode();
                hashCode = hashCode * 397 ^ IssueCount;
                hashCode = hashCode * 397 ^ ParticipantCount;
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
            return $"status='{Status}' succeeded='{Succeeded}' completedWithWarnings='{CompletedWithWarnings}' failed='{Failed}' hasResolvedTarget='{HasResolvedTarget}' participants='{ParticipantCount}' participantSucceeded='{ParticipantSucceededCount}' participantSkipped='{ParticipantSkippedCount}' participantFailed='{ParticipantFailedCount}' participantBlockingFailures='{ParticipantBlockingFailureCount}' {Request.ToDiagnosticString()} issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' message='{Message}'";
        }

        public static ObjectResetResult TargetResolved(ObjectResetRequest request, ObjectEntryDescriptor resolvedTarget)
        {
            return new ObjectResetResult(
                request,
                ObjectResetResultStatus.Succeeded,
                resolvedTarget,
                hasResolvedTarget: true,
                Array.Empty<ObjectResetIssue>(),
                "Object Reset target resolved from current Object Entry snapshot.");
        }

        public static ObjectResetResult Rejected(
            ObjectResetRequest request,
            ObjectResetResultStatus status,
            ObjectResetIssue issue,
            string message)
        {
            return new ObjectResetResult(
                request,
                status,
                default,
                hasResolvedTarget: false,
                new[] { issue },
                message);
        }

        public static ObjectResetResult FromExecution(
            ObjectResetRequest request,
            ObjectResetResultStatus status,
            ObjectEntryDescriptor resolvedTarget,
            bool hasResolvedTarget,
            ObjectResetPlan plan,
            IReadOnlyList<ObjectResetParticipantResult> participantResults,
            IReadOnlyList<ObjectResetIssue> issues,
            string message)
        {
            return new ObjectResetResult(
                request,
                status,
                resolvedTarget,
                hasResolvedTarget,
                participantResults,
                issues,
                string.IsNullOrWhiteSpace(message) && plan != null ? plan.Summary : message);
        }
    }
}
