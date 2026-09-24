using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Foundation event emitted by a scene-authored RouteRequestTrigger.
    /// It is the typed integration surface for code; UnityEvent bridges must live in explicit bridge components.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable authored Game Flow request trigger for Route/Activity navigation. Breaking changes require ADR/migration.")]
    public sealed class RouteRequestTriggerEvent : IEvent
    {
        public RouteRequestTriggerEvent(
            RouteRequestTrigger trigger,
            RouteAsset targetRoute,
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string source,
            string reason,
            string message)
        {
            Trigger = trigger;
            TargetRoute = targetRoute;
            Phase = phase;
            Outcome = outcome;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public RouteRequestTrigger Trigger { get; }

        public RouteAsset TargetRoute { get; }

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
