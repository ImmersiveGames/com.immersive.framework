using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.GameFlow;

namespace Immersive.Framework.ActivityRestart
{
    /// <summary>
    /// API status: Experimental. Event payload emitted by ActivityRestartTrigger.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F40A Activity Restart trigger event payload.")]
    public sealed class ActivityRestartTriggerEvent : IEvent, IEquatable<ActivityRestartTriggerEvent>
    {
        public ActivityRestartTriggerEvent(
            ActivityRestartTrigger trigger,
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string source,
            string reason,
            string message,
            ActivityRestartResult result,
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

        public ActivityRestartTrigger Trigger { get; }

        public FlowRequestEventPhase Phase { get; }

        public FlowRequestOutcome Outcome { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public ActivityRestartResult Result { get; }

        public bool HasResult { get; }

        public bool Equals(ActivityRestartTriggerEvent other)
        {
            if (other == null)
            {
                return false;
            }

            return Equals(Trigger, other.Trigger)
                && Phase == other.Phase
                && Outcome == other.Outcome
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Equals(Result, other.Result)
                && HasResult == other.HasResult;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ActivityRestartTriggerEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Trigger != null ? Trigger.GetHashCode() : 0;
                hashCode = hashCode * 397 ^ (int)Phase;
                hashCode = hashCode * 397 ^ (int)Outcome;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hashCode = hashCode * 397 ^ (Result != null ? Result.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (HasResult ? 1 : 0);
                return hashCode;
            }
        }
    }
}
