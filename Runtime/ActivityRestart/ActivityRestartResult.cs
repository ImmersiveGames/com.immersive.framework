using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.ObjectReset;

namespace Immersive.Framework.ActivityRestart
{
    /// <summary>
    /// API status: Experimental. Result for a scene-authored Activity Restart request.
    /// Activity Restart is a composed operation: Object Reset Group, Activity Clear, then Activity Request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F40A Activity Restart composed result.")]
    public sealed class ActivityRestartResult
    {
        public ActivityRestartResult(
            ActivityRestartResultStatus status,
            ActivityAsset activity,
            string activityName,
            string source,
            string reason,
            ObjectResetGroupResult resetGroupResult,
            string clearStatus,
            string clearMessage,
            string reenterStatus,
            string reenterMessage,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityRestartResultStatus), status) || status == ActivityRestartResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity Restart status must be explicit.");
            }

            Status = status;
            Activity = activity;
            ActivityName = activityName.NormalizeText();
            Source = source.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
            ResetGroupResult = resetGroupResult;
            ClearStatus = clearStatus.NormalizeText();
            ClearMessage = clearMessage.NormalizeText();
            ReenterStatus = reenterStatus.NormalizeText();
            ReenterMessage = reenterMessage.NormalizeText();
            Message = message.NormalizeText();
        }

        public ActivityRestartResultStatus Status { get; }

        public ActivityAsset Activity { get; }

        public string ActivityName { get; }

        public string Source { get; }

        public string Reason { get; }

        public ObjectResetGroupResult ResetGroupResult { get; }

        public string ClearStatus { get; }

        public string ClearMessage { get; }

        public string ReenterStatus { get; }

        public string ReenterMessage { get; }

        public string Message { get; }

        public bool Succeeded => Status == ActivityRestartResultStatus.Succeeded;

        public bool CompletedWithWarnings => Status == ActivityRestartResultStatus.CompletedWithWarnings;

        public bool Failed => !Succeeded && !CompletedWithWarnings;

        public bool HasResetGroupResult => ResetGroupResult != null;

        public int ResetTargetCount => ResetGroupResult?.TargetCount ?? 0;

        public int ResetTargetSucceededCount => ResetGroupResult?.TargetSucceededCount ?? 0;

        public int ResetTargetWarningCount => ResetGroupResult?.TargetWarningCount ?? 0;

        public int ResetTargetFailedCount => ResetGroupResult?.TargetFailedCount ?? 0;

        public int ResetBlockingIssueCount => ResetGroupResult?.BlockingIssueCount ?? 0;

        public int ResetNonBlockingIssueCount => ResetGroupResult?.NonBlockingIssueCount ?? 0;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' activity='{ActivityName.ToDiagnosticText()}' source='{Source.ToDiagnosticText()}' reason='{Reason.ToDiagnosticText()}' resetStatus='{(ResetGroupResult != null ? ResetGroupResult.Status.ToString() : "None")}' resetTargets='{ResetTargetCount}' resetTargetSucceeded='{ResetTargetSucceededCount}' resetTargetWarnings='{ResetTargetWarningCount}' resetTargetFailed='{ResetTargetFailedCount}' resetBlockingIssues='{ResetBlockingIssueCount}' resetNonBlockingIssues='{ResetNonBlockingIssueCount}' clearStatus='{ClearStatus.ToDiagnosticText()}' reenterStatus='{ReenterStatus.ToDiagnosticText()}' message='{Message.ToDiagnosticText()}'";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static ActivityRestartResult Rejected(
            ActivityRestartResultStatus status,
            ActivityAsset activity,
            string activityName,
            string source,
            string reason,
            string message)
        {
            return new ActivityRestartResult(
                status,
                activity,
                activityName,
                source,
                reason,
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                message);
        }
    }
}
