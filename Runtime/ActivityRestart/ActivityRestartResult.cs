using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.Reset;

namespace Immersive.Framework.ActivityRestart
{
    /// <summary>
    /// API status: Experimental. Result for a scene-authored Activity Restart request.
    /// Activity Restart is a composed operation: ResetExecutor, Activity Clear, then Activity Request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12G Activity Restart composed result over ResetExecutor.")]
    public sealed class ActivityRestartResult
    {
        public ActivityRestartResult(
            ActivityRestartResultStatus status,
            ActivityAsset activity,
            string activityName,
            string source,
            string reason,
            ResetExecutionResult resetExecutionResult,
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
            ResetExecutionResult = resetExecutionResult;
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

        public ResetExecutionResult ResetExecutionResult { get; }

        public string ClearStatus { get; }

        public string ClearMessage { get; }

        public string ReenterStatus { get; }

        public string ReenterMessage { get; }

        public string Message { get; }

        public bool Succeeded => Status == ActivityRestartResultStatus.Succeeded;

        public bool CompletedWithWarnings => Status == ActivityRestartResultStatus.CompletedWithWarnings;

        public bool Failed => !Succeeded && !CompletedWithWarnings;

        public bool HasResetExecutionResult => ResetExecutionResult.Status != ResetExecutionStatus.Unknown;

        public string ResetStatus => HasResetExecutionResult ? ResetExecutionResult.Status.ToString() : "None";

        public int ResetSubjectCount => HasResetExecutionResult ? ResetExecutionResult.SubjectCount : 0;

        public int ResetSubjectSucceededCount => HasResetExecutionResult ? ResetExecutionResult.SubjectSucceeded : 0;

        public int ResetSubjectWarningCount => HasResetExecutionResult ? CountResetSubjectWarnings(ResetExecutionResult) : 0;

        public int ResetSubjectFailedCount => HasResetExecutionResult ? ResetExecutionResult.SubjectFailed : 0;

        public int ResetParticipantCount => HasResetExecutionResult ? ResetExecutionResult.ParticipantCount : 0;

        public int ResetParticipantSucceededCount => HasResetExecutionResult ? ResetExecutionResult.ParticipantSucceeded : 0;

        public int ResetParticipantSkippedCount => HasResetExecutionResult ? ResetExecutionResult.ParticipantSkipped : 0;

        public int ResetParticipantFailedCount => HasResetExecutionResult ? ResetExecutionResult.ParticipantFailed : 0;

        public int ResetBlockingIssueCount => HasResetExecutionResult ? ResetExecutionResult.BlockingIssueCount : 0;

        public int ResetNonBlockingIssueCount => HasResetExecutionResult ? ResetExecutionResult.NonBlockingIssueCount : 0;

        public int ResetTargetCount => ResetSubjectCount;

        public int ResetTargetSucceededCount => ResetSubjectSucceededCount;

        public int ResetTargetWarningCount => ResetSubjectWarningCount;

        public int ResetTargetFailedCount => ResetSubjectFailedCount;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' activity='{ActivityName.ToDiagnosticText()}' source='{Source.ToDiagnosticText()}' reason='{Reason.ToDiagnosticText()}' resetStatus='{ResetStatus}' resetSubjects='{ResetSubjectCount}' resetSubjectSucceeded='{ResetSubjectSucceededCount}' resetSubjectWarnings='{ResetSubjectWarningCount}' resetSubjectFailed='{ResetSubjectFailedCount}' resetParticipants='{ResetParticipantCount}' resetParticipantSucceeded='{ResetParticipantSucceededCount}' resetParticipantSkipped='{ResetParticipantSkippedCount}' resetParticipantFailed='{ResetParticipantFailedCount}' resetBlockingIssues='{ResetBlockingIssueCount}' resetNonBlockingIssues='{ResetNonBlockingIssueCount}' clearStatus='{ClearStatus.ToDiagnosticText()}' reenterStatus='{ReenterStatus.ToDiagnosticText()}' message='{Message.ToDiagnosticText()}'";
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
                default,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                message);
        }

        private static int CountResetSubjectWarnings(ResetExecutionResult resetExecutionResult)
        {
            if (resetExecutionResult.Subjects.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < resetExecutionResult.Subjects.Count; i++)
            {
                ResetSubjectResult subject = resetExecutionResult.Subjects[i];
                if (subject.Succeeded && subject.NonBlockingIssueCount > 0)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
