using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Foundation event emitted by scene-authored Object Reset triggers.
    /// This is an authored request boundary only; Object Reset remains a logical ObjectEntry-targeted reset foundation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14F Object Reset authored trigger event surface.")]
    public sealed class ObjectResetTriggerEvent : IEvent
    {
        public ObjectResetTriggerEvent(
            MonoBehaviour trigger,
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string source,
            string reason,
            string message,
            ObjectResetResult result,
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

        public ObjectResetResult Result { get; }

        public bool HasResult { get; }

        public bool IsSubmitted => Phase == FlowRequestEventPhase.Submitted;

        public bool IsCompleted => Phase == FlowRequestEventPhase.Completed;

        public bool Succeeded => Outcome == FlowRequestOutcome.Succeeded;

        public bool Ignored => Outcome == FlowRequestOutcome.Ignored;

        public bool Failed => Outcome == FlowRequestOutcome.Failed;

        public ObjectResetResultStatus ResultStatus => HasResult ? Result.Status : ObjectResetResultStatus.Unknown;

        public bool SucceededNoParticipants => HasResult && Result.Status == ObjectResetResultStatus.SucceededNoParticipants;

        public bool CompletedWithWarnings => HasResult && Result.CompletedWithWarnings;

        public bool SucceededWithParticipants => HasResult && Result.Status == ObjectResetResultStatus.Succeeded;

        public int ParticipantCount => HasResult ? Result.ParticipantCount : 0;

        public int SucceededParticipantCount => HasResult ? Result.ParticipantSucceededCount : 0;

        public int SkippedParticipantCount => HasResult ? Result.ParticipantSkippedCount : 0;

        public int FailedParticipantCount => HasResult ? Result.ParticipantFailedCount : 0;

        public int BlockingIssueCount => HasResult ? Result.BlockingIssueCount : 0;

        public int NonBlockingIssueCount => HasResult ? Result.NonBlockingIssueCount : 0;

        public string ResultSummary => HasResult ? Result.ToDiagnosticString() : Message;
    }
}
