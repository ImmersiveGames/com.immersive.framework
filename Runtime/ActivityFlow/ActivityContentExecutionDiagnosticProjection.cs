using System;
using Immersive.Framework.ApiStatus;
using Immersive.Logging.Records;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Internal structured diagnostic projection for Activity Content Execution.
    /// It reads an already-computed lifecycle result and does not resolve, execute or mutate Activity content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Structured Activity Content Execution diagnostic fields for framework-owned log surfaces.")]
    internal static class ActivityContentExecutionDiagnosticProjection
    {
        internal static LogField[] AppendTo(
            LogField[] fields,
            ActivityContentExecutionLifecycleResult execution)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            LogField[] executionFields = CreateFields(execution);
            var combined = new LogField[fields.Length + executionFields.Length];
            Array.Copy(fields, combined, fields.Length);
            Array.Copy(executionFields, 0, combined, fields.Length, executionFields.Length);
            return combined;
        }

        private static LogField[] CreateFields(
            ActivityContentExecutionLifecycleResult execution)
        {
            return LogFields.Of(
                LogFields.Field("activityContentExecution", execution.DiagnosticStatus),
                LogFields.Field("activityContentExecutionParticipantSource", execution.ParticipantSourceStatus),
                LogFields.Field("activityContentExecutionParticipantSourceIssues", execution.ParticipantSourceIssueCount),
                LogFields.Field("activityContentExecutionParticipants", execution.ParticipantCount),
                LogFields.Field("activityContentExecutionEnter", execution.EnterResult.Status),
                LogFields.Field("activityContentEnterDiagnostic", execution.EnterResult.ToDiagnosticString()),
                LogFields.Field("activityContentExecutionEnterRequests", execution.EnterRequestCount),
                LogFields.Field("activityContentExecutionExit", execution.ExitResult.Status),
                LogFields.Field("activityContentExecutionExitRequests", execution.ExitRequestCount),
                LogFields.Field("activityContentExecutionBlockingIssues", execution.BlockingIssueCount),
                LogFields.Field("activityContentExecutionBlocksReadiness", execution.BlocksReadiness),
                LogFields.Field("activityContentParticipantExecution", execution.DiagnosticStatus),
                LogFields.Field("activityContentParticipantSource", execution.ParticipantSourceStatus),
                LogFields.Field("activityContentParticipantSourceIssues", execution.ParticipantSourceIssueCount),
                LogFields.Field("activityContentParticipantCount", execution.ParticipantCount),
                LogFields.Field("activityContentParticipantEnter", execution.EnterResult.Status),
                LogFields.Field("activityContentParticipantEnterRequests", execution.EnterRequestCount),
                LogFields.Field("activityContentParticipantExit", execution.ExitResult.Status),
                LogFields.Field("activityContentParticipantExitRequests", execution.ExitRequestCount),
                LogFields.Field("activityContentParticipantBlockingIssues", execution.BlockingIssueCount),
                LogFields.Field("activityContentParticipantBlocksReadiness", execution.BlocksReadiness));
        }
    }
}
