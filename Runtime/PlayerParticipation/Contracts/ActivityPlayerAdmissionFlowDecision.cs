using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical decision for one pre-activation Activity admission attempt.
    /// It retains the exact P3K.6 evaluation and does not retain Unity object references.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7A immutable Activity Player pre-activation flow decision.")]
    public sealed class ActivityPlayerAdmissionFlowDecision
    {
        internal ActivityPlayerAdmissionFlowDecision(
            int attemptSequence,
            ActivityPlayerAdmissionFlowDisposition disposition,
            ActivityPlayerAdmissionEvaluationResult evaluation,
            string source,
            string reason,
            string message)
        {
            AttemptSequence = attemptSequence;
            Disposition = disposition;
            Evaluation = evaluation;
            Source = source.NormalizeTextOrFallback(
                nameof(ActivityPlayerAdmissionFlowDecision));
            Reason = reason.NormalizeTextOrFallback(
                "activity-player-admission-pre-activation");
            Message = message.NormalizeText();
        }

        public int AttemptSequence { get; }

        public ActivityPlayerAdmissionFlowDisposition Disposition { get; }

        public ActivityPlayerAdmissionEvaluationResult Evaluation { get; }

        public string ActivityName => Evaluation?.ActivityName ?? string.Empty;

        public string SessionContextId => Evaluation?.SessionContextId ?? string.Empty;

        public ActivityPlayerAdmissionEvaluationStatus EvaluationStatus =>
            Evaluation?.Status ?? ActivityPlayerAdmissionEvaluationStatus.Failed;

        public ActivityPlayerAdmissionEvaluationCode EvaluationCode =>
            Evaluation?.Code ?? ActivityPlayerAdmissionEvaluationCode.MissingActivity;

        public PlayerParticipationRequirementLevel RequirementLevel =>
            Evaluation?.RequirementLevel ?? PlayerParticipationRequirementLevel.None;

        public int ProjectedSlotCount => Evaluation?.ProjectedSlotCount ?? 0;

        public int PendingSlotCount => Evaluation?.PendingSlotCount ?? 0;

        public int BlockedSlotCount => Evaluation?.BlockedSlotCount ?? 0;

        public int FailedSlotCount => Evaluation?.FailedSlotCount ?? 0;

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool CanProceed =>
            Disposition == ActivityPlayerAdmissionFlowDisposition.Proceed;

        public bool RequiresResolution =>
            Disposition == ActivityPlayerAdmissionFlowDisposition.AwaitResolution;

        public bool IsRejected =>
            Disposition == ActivityPlayerAdmissionFlowDisposition.RejectBlocked ||
            Disposition == ActivityPlayerAdmissionFlowDisposition.RejectFailed;

        /// <summary>
        /// Retry is explicit and meaningful only for resolvable pending state.
        /// Rejected authoring/runtime evidence must be corrected before another attempt.
        /// </summary>
        public bool CanRetry => RequiresResolution;

        public string ToDiagnosticString()
        {
            return
                $"attempt='{AttemptSequence}' disposition='{Disposition}' " +
                $"activity='{ActivityName}' session='{SessionContextId}' " +
                $"requirement='{RequirementLevel}' evaluationStatus='{EvaluationStatus}' " +
                $"evaluationCode='{EvaluationCode}' projected='{ProjectedSlotCount}' " +
                $"pending='{PendingSlotCount}' blocked='{BlockedSlotCount}' failed='{FailedSlotCount}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }
    }
}
