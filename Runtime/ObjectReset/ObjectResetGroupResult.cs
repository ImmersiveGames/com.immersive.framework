using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Aggregate result for an Object Reset Group request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F39A Object Reset Group aggregate result.")]
    public sealed class ObjectResetGroupResult
    {
        private readonly ObjectResetGroupTargetResult[] _targetResults;

        public ObjectResetGroupResult(
            string groupId,
            string source,
            string reason,
            ObjectResetGroupResultStatus status,
            IReadOnlyList<ObjectResetGroupTargetResult> targetResults,
            bool stoppedOnFailure,
            string message)
        {
            if (!Enum.IsDefined(typeof(ObjectResetGroupResultStatus), status) || status == ObjectResetGroupResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Object Reset Group status must be explicit.");
            }

            GroupId = groupId.NormalizeText();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Status = status;
            _targetResults = targetResults == null ? Array.Empty<ObjectResetGroupTargetResult>() : targetResults.ToArray();
            StoppedOnFailure = stoppedOnFailure;
            Message = message.NormalizeText();
        }

        public string GroupId { get; }

        public string Source { get; }

        public string Reason { get; }

        public ObjectResetGroupResultStatus Status { get; }

        public IReadOnlyList<ObjectResetGroupTargetResult> TargetResults => _targetResults ?? Array.Empty<ObjectResetGroupTargetResult>();

        public bool StoppedOnFailure { get; }

        public string Message { get; }

        public bool Succeeded => Status is ObjectResetGroupResultStatus.Succeeded or ObjectResetGroupResultStatus.SucceededNoTargets;

        public bool CompletedWithWarnings => Status == ObjectResetGroupResultStatus.CompletedWithWarnings;

        public bool Failed => Status is ObjectResetGroupResultStatus.Failed
            or ObjectResetGroupResultStatus.RejectedInvalidRequest
            or ObjectResetGroupResultStatus.RejectedRuntimeUnavailable
            or ObjectResetGroupResultStatus.RejectedRuntimeContextUnavailable
            or ObjectResetGroupResultStatus.RejectedAlreadyInFlight;

        public int TargetCount => TargetResults.Count;

        public int TargetSucceededCount => TargetResults.Count(result => result.Succeeded);

        public int TargetWarningCount => TargetResults.Count(result => result.CompletedWithWarnings);

        public int TargetSkippedCount => TargetResults.Count(result => result.Skipped);

        public int TargetFailedCount => TargetResults.Count(result => result.Failed);

        public int ResetRequestCount => TargetResults.Count(result => result.HasResetResult);

        public int ParticipantCount => TargetResults.Sum(result => result.ParticipantCount);

        public int ParticipantSucceededCount => TargetResults.Sum(result => result.ParticipantSucceededCount);

        public int ParticipantSkippedCount => TargetResults.Sum(result => result.ParticipantSkippedCount);

        public int ParticipantFailedCount => TargetResults.Sum(result => result.ParticipantFailedCount);

        public int BlockingIssueCount => TargetResults.Sum(result => result.BlockingIssueCount);

        public int NonBlockingIssueCount => TargetResults.Sum(result => result.NonBlockingIssueCount);

        public ObjectResetGroupTargetResult[] SnapshotTargetResults()
        {
            if (TargetCount == 0)
            {
                return Array.Empty<ObjectResetGroupTargetResult>();
            }

            var snapshot = new ObjectResetGroupTargetResult[TargetCount];
            for (int i = 0; i < TargetCount; i++)
            {
                snapshot[i] = TargetResults[i];
            }

            return snapshot;
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"status='{Status}' groupId='{GroupId.ToDiagnosticText()}' source='{Source.ToDiagnosticText()}' reason='{Reason.ToDiagnosticText()}' targets='{TargetCount}' targetSucceeded='{TargetSucceededCount}' targetWarnings='{TargetWarningCount}' targetSkipped='{TargetSkippedCount}' targetFailed='{TargetFailedCount}' resetRequests='{ResetRequestCount}' participants='{ParticipantCount}' participantSucceeded='{ParticipantSucceededCount}' participantSkipped='{ParticipantSkippedCount}' participantFailed='{ParticipantFailedCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' stoppedOnFailure='{StoppedOnFailure}' message='{Message.ToDiagnosticText()}'";
        }

        public static ObjectResetGroupResult Rejected(
            string groupId,
            string source,
            string reason,
            ObjectResetGroupResultStatus status,
            string message)
        {
            return new ObjectResetGroupResult(
                groupId,
                source,
                reason,
                status,
                Array.Empty<ObjectResetGroupTargetResult>(),
                stoppedOnFailure: false,
                message);
        }

        public static ObjectResetGroupResult FromTargets(
            string groupId,
            string source,
            string reason,
            IReadOnlyList<ObjectResetGroupTargetResult> targetResults,
            bool stoppedOnFailure)
        {
            var results = targetResults ?? Array.Empty<ObjectResetGroupTargetResult>();
            ObjectResetGroupResultStatus status;
            string message;

            if (results.Count == 0 || results.All(result => result.Skipped))
            {
                status = ObjectResetGroupResultStatus.SucceededNoTargets;
                message = "Object Reset Group completed with no enabled targets.";
            }
            else if (results.Any(result => result.Failed))
            {
                status = ObjectResetGroupResultStatus.Failed;
                message = "Object Reset Group failed because one or more targets failed.";
            }
            else if (results.Any(result => result.CompletedWithWarnings))
            {
                status = ObjectResetGroupResultStatus.CompletedWithWarnings;
                message = "Object Reset Group completed with target warnings.";
            }
            else
            {
                status = ObjectResetGroupResultStatus.Succeeded;
                message = "Object Reset Group completed successfully.";
            }

            return new ObjectResetGroupResult(
                groupId,
                source,
                reason,
                status,
                results,
                stoppedOnFailure,
                message);
        }
    }
}
