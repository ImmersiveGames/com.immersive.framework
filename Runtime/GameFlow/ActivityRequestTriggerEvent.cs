using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Foundation event emitted by a scene-authored ActivityRequestTrigger.
    /// It is the typed integration surface for code; UnityEvent bridges must live in explicit bridge components.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityRequestTriggerEvent : IEvent
    {
        public ActivityRequestTriggerEvent(
            ActivityRequestTrigger trigger,
            ActivityAsset targetActivity,
            bool clearsActivity,
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string source,
            string reason,
            string message)
        {
            Trigger = trigger;
            TargetActivity = targetActivity;
            ClearsActivity = clearsActivity;
            Phase = phase;
            Outcome = outcome;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public ActivityRequestTrigger Trigger { get; }

        public ActivityAsset TargetActivity { get; }

        public bool ClearsActivity { get; }

        public FlowRequestEventPhase Phase { get; }

        public FlowRequestOutcome Outcome { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool IsSubmitted => Phase == FlowRequestEventPhase.Submitted;

        public bool IsCompleted => Phase == FlowRequestEventPhase.Completed;

        public bool Succeeded => Outcome == FlowRequestOutcome.Succeeded;

        public bool Ignored => Outcome == FlowRequestOutcome.Ignored;

        public bool Failed => Outcome == FlowRequestOutcome.Failed;
    }
}
