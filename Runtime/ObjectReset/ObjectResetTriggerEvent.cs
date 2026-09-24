using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Reset;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Event emitted by scene-authored Object Reset triggers.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12G Object Reset authored trigger event surface.")]
    public sealed class ObjectResetTriggerEvent : IEvent
    {
        public ObjectResetTriggerEvent(
            MonoBehaviour trigger,
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string source,
            string reason,
            string message,
            ResetExecutionResult result,
            bool hasResult)
        {
            Trigger = trigger;
            Phase = phase;
            Outcome = outcome;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
            Result = result;
            HasResult = hasResult;
        }

        public MonoBehaviour Trigger { get; }

        public FlowRequestEventPhase Phase { get; }

        public FlowRequestOutcome Outcome { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public ResetExecutionResult Result { get; }

        public bool HasResult { get; }

        public bool IsSubmitted => Phase == FlowRequestEventPhase.Submitted;

        public bool IsCompleted => Phase == FlowRequestEventPhase.Completed;

        public bool Succeeded => Outcome == FlowRequestOutcome.Succeeded;

        public bool Ignored => Outcome == FlowRequestOutcome.Ignored;

        public bool Failed => Outcome == FlowRequestOutcome.Failed;

        public ResetExecutionStatus ResultStatus => HasResult ? Result.Status : ResetExecutionStatus.Unknown;

        public bool SucceededNoParticipants => HasResult
            && Result.Subjects.Count == 1
            && Result.Subjects[0].Status == ResetSubjectResultStatus.SkippedNoParticipants;

        public bool CompletedWithWarnings => HasResult && Result.Succeeded && Result.NonBlockingIssueCount > 0;

        public bool SucceededWithParticipants => HasResult && Result.Succeeded && Result.ParticipantSucceeded > 0;

        public int ParticipantCount => HasResult ? Result.ParticipantCount : 0;

        public int SucceededParticipantCount => HasResult ? Result.ParticipantSucceeded : 0;

        public int SkippedParticipantCount => HasResult ? Result.ParticipantSkipped : 0;

        public int FailedParticipantCount => HasResult ? Result.ParticipantFailed : 0;

        public int BlockingIssueCount => HasResult ? Result.BlockingIssueCount : 0;

        public int NonBlockingIssueCount => HasResult ? Result.NonBlockingIssueCount : 0;

        public string ResultSummary => HasResult ? Result.ToString() : Message;
    }
}
