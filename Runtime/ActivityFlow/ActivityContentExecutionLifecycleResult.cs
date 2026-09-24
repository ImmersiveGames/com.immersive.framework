using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive lifecycle-level diagnostic result for Activity Content Execution.
    /// It groups participant source resolution, enter/exit plans and aggregate results for ActivityFlow integration only; it does not discover scene objects globally, mutate Unity objects, materialize content or execute gameplay systems by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10I Activity Content Execution lifecycle integration result; diagnostics only and empty by default until participant discovery exists.")]
    public readonly struct ActivityContentExecutionLifecycleResult : IEquatable<ActivityContentExecutionLifecycleResult>
    {
        private ActivityContentExecutionLifecycleResult(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            ActivityContentExecutionParticipantSourceResult participantSourceResult,
            ActivityContentExecutionParticipantCollection participants,
            ActivityContentExecutionPhasePlan enterPlan,
            ActivityContentExecutionAggregateResult enterResult,
            ActivityContentExecutionPhasePlan exitPlan,
            ActivityContentExecutionAggregateResult exitResult,
            ActivityContentExecutionLifecycleStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionLifecycleStatus), status)
                || status == ActivityContentExecutionLifecycleStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution lifecycle status must be explicit.");
            }

            Activity = activity;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            ParticipantSourceResult = participantSourceResult;
            Participants = participants;
            EnterPlan = enterPlan;
            EnterResult = enterResult;
            ExitPlan = exitPlan;
            ExitResult = exitResult;
            Status = status;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        private ActivityAsset Activity { get; }

        private ActivityAsset PreviousActivity { get; }

        private ActivityAsset NextActivity { get; }

        private ActivityContentExecutionParticipantSourceResult ParticipantSourceResult { get; }

        private ActivityContentExecutionParticipantCollection Participants { get; }

        private ActivityContentExecutionPhasePlan EnterPlan { get; }

        public ActivityContentExecutionAggregateResult EnterResult { get; }

        private ActivityContentExecutionPhasePlan ExitPlan { get; }

        public ActivityContentExecutionAggregateResult ExitResult { get; }

        public ActivityContentExecutionLifecycleStatus Status { get; }

        private string Source { get; }

        private string Reason { get; }

        private string Message { get; }

        public bool Executed => Status != ActivityContentExecutionLifecycleStatus.None
            && Status != ActivityContentExecutionLifecycleStatus.Unknown;

        public bool Succeeded => Status is ActivityContentExecutionLifecycleStatus.Succeeded or ActivityContentExecutionLifecycleStatus.SucceededNoContent or ActivityContentExecutionLifecycleStatus.SucceededWithNonBlockingIssues;

        private bool HasEnterPlan => EnterPlan.Status != ActivityContentExecutionPhasePlanStatus.Unknown;

        private bool HasExitPlan => ExitPlan.Status != ActivityContentExecutionPhasePlanStatus.Unknown;

        private bool HasEnterResult => EnterResult.Status != ActivityContentExecutionAggregateStatus.Unknown;

        private bool HasExitResult => ExitResult.Status != ActivityContentExecutionAggregateStatus.Unknown;

        public bool BlocksReadiness => EnterBlocksReadiness || ExitBlocksReadiness || Status == ActivityContentExecutionLifecycleStatus.FailedBlocking || Status == ActivityContentExecutionLifecycleStatus.RejectedParticipantSource;

        private bool EnterBlocksReadiness => HasEnterResult && EnterResult.BlocksReadiness;

        private bool ExitBlocksReadiness => HasExitResult && ExitResult.BlocksReadiness;

        public int ParticipantCount => Participants.Count;

        private int ParticipantIssueCount => Participants.IssueCount;

        public int ParticipantSourceIssueCount => ParticipantSourceResult.IssueCount;

        public string ParticipantSourceStatus => ParticipantSourceResult.DiagnosticStatus;

        public int EnterRequestCount => HasEnterPlan ? EnterPlan.RequestCount : 0;

        public int ExitRequestCount => HasExitPlan ? ExitPlan.RequestCount : 0;

        public int EnterResultCount => HasEnterResult ? EnterResult.ResultCount : 0;

        public int ExitResultCount => HasExitResult ? ExitResult.ResultCount : 0;

        public int BlockingIssueCount => (HasEnterResult ? EnterResult.BlockingIssueCount : 0) + (HasExitResult ? ExitResult.BlockingIssueCount : 0);

        private int NonBlockingIssueCount => (HasEnterResult ? EnterResult.NonBlockingIssueCount : 0) + (HasExitResult ? ExitResult.NonBlockingIssueCount : 0);

        public string DiagnosticStatus => Status.ToString();

        private string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        private string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        private string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public bool Equals(ActivityContentExecutionLifecycleResult other)
        {
            return ReferenceEquals(Activity, other.Activity)
                && ReferenceEquals(PreviousActivity, other.PreviousActivity)
                && ReferenceEquals(NextActivity, other.NextActivity)
                && ParticipantSourceResult.Equals(other.ParticipantSourceResult)
                && Participants.Count == other.Participants.Count
                && Participants.IssueCount == other.Participants.IssueCount
                && EnterPlan.Equals(other.EnterPlan)
                && EnterResult.Equals(other.EnterResult)
                && ExitPlan.Equals(other.ExitPlan)
                && ExitResult.Equals(other.ExitResult)
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionLifecycleResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Activity != null ? Activity.GetHashCode() : 0;
                hashCode = hashCode * 397 ^ (PreviousActivity != null ? PreviousActivity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (NextActivity != null ? NextActivity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ ParticipantSourceResult.GetHashCode();
                hashCode = hashCode * 397 ^ Participants.Count;
                hashCode = hashCode * 397 ^ Participants.IssueCount;
                hashCode = hashCode * 397 ^ EnterPlan.GetHashCode();
                hashCode = hashCode * 397 ^ EnterResult.GetHashCode();
                hashCode = hashCode * 397 ^ ExitPlan.GetHashCode();
                hashCode = hashCode * 397 ^ ExitResult.GetHashCode();
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
            string activityText = ActivityName.ToDiagnosticText();
            string previousText = PreviousActivityName.ToDiagnosticText();
            string nextText = NextActivityName.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            var builder = new StringBuilder();
            builder.Append($"Activity Content Participant Execution Lifecycle status='{Status}' activity='{activityText}' previousActivity='{previousText}' nextActivity='{nextText}' participantSource='{ParticipantSourceStatus}' participantSourceIssues='{ParticipantSourceIssueCount}' participants='{ParticipantCount}' participantIssues='{ParticipantIssueCount}' enterPlan='{EnterPlan.Status}' enterRequests='{EnterRequestCount}' enterStatus='{EnterResult.Status}' enterResults='{EnterResultCount}' exitPlan='{ExitPlan.Status}' exitRequests='{ExitRequestCount}' exitStatus='{ExitResult.Status}' exitResults='{ExitResultCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' blocksReadiness='{BlocksReadiness}' source='{sourceText}' reason='{reasonText}' message='{messageText}'");
            return builder.ToString();
        }

        public static ActivityContentExecutionLifecycleResult None(string source, string reason, string message)
        {
            return new ActivityContentExecutionLifecycleResult(
                null,
                null,
                null,
                ActivityContentExecutionParticipantSourceResult.None(source, reason, message),
                ActivityContentExecutionParticipantCollection.Empty(),
                default,
                default,
                default,
                default,
                ActivityContentExecutionLifecycleStatus.None,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionLifecycleResult FromResults(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            ActivityContentExecutionParticipantSourceResult participantSourceResult,
            ActivityContentExecutionParticipantCollection participants,
            ActivityContentExecutionPhasePlan enterPlan,
            ActivityContentExecutionAggregateResult enterResult,
            ActivityContentExecutionPhasePlan exitPlan,
            ActivityContentExecutionAggregateResult exitResult,
            string source,
            string reason,
            string message)
        {
            var status = ResolveStatus(participantSourceResult, participants, enterResult, exitResult);
            return new ActivityContentExecutionLifecycleResult(
                activity,
                previousActivity,
                nextActivity,
                participantSourceResult,
                participants,
                enterPlan,
                enterResult,
                exitPlan,
                exitResult,
                status,
                source,
                reason,
                message);
        }

        private static ActivityContentExecutionLifecycleStatus ResolveStatus(
            ActivityContentExecutionParticipantSourceResult participantSourceResult,
            ActivityContentExecutionParticipantCollection participants,
            ActivityContentExecutionAggregateResult enterResult,
            ActivityContentExecutionAggregateResult exitResult)
        {
            if (participantSourceResult.Failed)
            {
                return ActivityContentExecutionLifecycleStatus.RejectedParticipantSource;
            }

            bool hasEnter = enterResult.Status != ActivityContentExecutionAggregateStatus.Unknown;
            bool hasExit = exitResult.Status != ActivityContentExecutionAggregateStatus.Unknown;
            if (!hasEnter && !hasExit)
            {
                return ActivityContentExecutionLifecycleStatus.None;
            }

            if (hasEnter && enterResult.Status == ActivityContentExecutionAggregateStatus.RejectedInvalidResults
                || hasExit && exitResult.Status == ActivityContentExecutionAggregateStatus.RejectedInvalidResults)
            {
                return ActivityContentExecutionLifecycleStatus.RejectedInvalidContext;
            }

            if (hasEnter && enterResult.BlocksReadiness || hasExit && exitResult.BlocksReadiness)
            {
                return ActivityContentExecutionLifecycleStatus.FailedBlocking;
            }

            if (hasEnter && enterResult.Failed || hasExit && exitResult.Failed)
            {
                return ActivityContentExecutionLifecycleStatus.FailedNonBlocking;
            }

            if (hasEnter && enterResult.HasNonBlockingIssues || hasExit && exitResult.HasNonBlockingIssues || participants.HasIssues || participantSourceResult.HasIssues)
            {
                return ActivityContentExecutionLifecycleStatus.SucceededWithNonBlockingIssues;
            }

            bool noEnterContent = !hasEnter || enterResult.Skipped;
            bool noExitContent = !hasExit || exitResult.Skipped;
            if (!participants.HasParticipants && noEnterContent && noExitContent)
            {
                return ActivityContentExecutionLifecycleStatus.SucceededNoContent;
            }

            return ActivityContentExecutionLifecycleStatus.Succeeded;
        }
        
    }
}
