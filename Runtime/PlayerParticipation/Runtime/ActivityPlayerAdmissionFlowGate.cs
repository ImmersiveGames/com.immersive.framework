using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Stateless with respect to Player authority. The gate numbers explicit attempts,
    /// delegates all domain evaluation to P3K.6 and maps the truthful result to one
    /// pre-activation flow disposition. It does not mutate or continue ActivityFlow.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7A pre-activation Activity Player admission decision gate.")]
    internal sealed class ActivityPlayerAdmissionFlowGate
    {
        private int attemptSequence;

        internal int AttemptSequence => attemptSequence;

        internal ActivityPlayerAdmissionFlowDecision Evaluate(
            ActivityAsset activity,
            PlayerParticipationSnapshot participationSnapshot,
            PlayerActorPreparationSnapshot preparationSnapshot,
            PlayerGameplayAdmissionSnapshot gameplayAdmissionSnapshot,
            string source,
            string reason)
        {
            ActivityPlayerAdmissionEvaluationResult evaluation;
            try
            {
                evaluation = ActivityPlayerAdmissionEvaluator.Evaluate(
                    activity,
                    participationSnapshot,
                    preparationSnapshot,
                    gameplayAdmissionSnapshot);
            }
            catch (Exception exception)
            {
                evaluation = CreateUnexpectedFailure(activity, exception);
            }

            return CreateDecision(evaluation, source, reason);
        }

        internal ActivityPlayerAdmissionFlowDecision CreateDecision(
            ActivityPlayerAdmissionEvaluationResult evaluation,
            string source,
            string reason)
        {
            if (evaluation == null)
            {
                evaluation = CreateMissingEvaluationFailure();
            }

            attemptSequence++;
            ActivityPlayerAdmissionFlowDisposition disposition =
                MapDisposition(evaluation.Status);
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(ActivityPlayerAdmissionFlowGate));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "activity-player-admission-pre-activation");
            string message = BuildMessage(evaluation, disposition);

            return new ActivityPlayerAdmissionFlowDecision(
                attemptSequence,
                disposition,
                evaluation,
                resolvedSource,
                resolvedReason,
                message);
        }

        internal static ActivityPlayerAdmissionFlowDisposition MapDisposition(
            ActivityPlayerAdmissionEvaluationStatus status)
        {
            return status switch
            {
                ActivityPlayerAdmissionEvaluationStatus.Satisfied =>
                    ActivityPlayerAdmissionFlowDisposition.Proceed,
                ActivityPlayerAdmissionEvaluationStatus.PendingResolution =>
                    ActivityPlayerAdmissionFlowDisposition.AwaitResolution,
                ActivityPlayerAdmissionEvaluationStatus.Blocked =>
                    ActivityPlayerAdmissionFlowDisposition.RejectBlocked,
                _ => ActivityPlayerAdmissionFlowDisposition.RejectFailed
            };
        }

        private static string BuildMessage(
            ActivityPlayerAdmissionEvaluationResult evaluation,
            ActivityPlayerAdmissionFlowDisposition disposition)
        {
            string action = disposition switch
            {
                ActivityPlayerAdmissionFlowDisposition.Proceed =>
                    "Activity pre-activation may proceed.",
                ActivityPlayerAdmissionFlowDisposition.AwaitResolution =>
                    "Activity pre-activation is awaiting explicit Player state resolution and retry.",
                ActivityPlayerAdmissionFlowDisposition.RejectBlocked =>
                    "Activity pre-activation is rejected by current participation policy/state.",
                _ =>
                    "Activity pre-activation is rejected because authoring or runtime evidence failed."
            };

            return $"{action} {evaluation.ToDiagnosticString()}";
        }

        private static ActivityPlayerAdmissionEvaluationResult CreateUnexpectedFailure(
            ActivityAsset activity,
            Exception exception)
        {
            string activityName = activity != null
                ? activity.ActivityName
                : "<missing>";
            return new ActivityPlayerAdmissionEvaluationResult(
                activityName,
                string.Empty,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                PlayerParticipationRequirementLevel.None,
                ActivityPlayerAdmissionEvaluationStatus.Failed,
                ActivityPlayerAdmissionEvaluationCode.InvalidSlotEvidence,
                Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                $"Activity Player admission evaluator threw '{exception.GetType().Name}'. {exception.Message}");
        }

        private static ActivityPlayerAdmissionEvaluationResult CreateMissingEvaluationFailure()
        {
            return new ActivityPlayerAdmissionEvaluationResult(
                "<missing>",
                string.Empty,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                PlayerParticipationRequirementLevel.None,
                ActivityPlayerAdmissionEvaluationStatus.Failed,
                ActivityPlayerAdmissionEvaluationCode.MissingActivity,
                Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                "Activity Player admission flow gate received no evaluation result.");
        }
    }
}
