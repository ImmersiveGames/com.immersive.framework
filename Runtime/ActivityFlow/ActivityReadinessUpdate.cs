using Immersive.Framework.Authoring;
using Immersive.Foundation.Events;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>Internal post-transition readiness update carried only through explicit runtime owners.</summary>
    internal sealed class ActivityReadinessUpdate : IEvent
    {
        internal ActivityReadinessUpdate(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readinessState,
            string reason,
            int revision)
        {
            Occurrence = occurrence;
            ReadinessState = readinessState;
            Reason = reason ?? string.Empty;
            Revision = revision;
        }

        internal ActivityReadinessOccurrence Occurrence { get; }
        internal ActivityReadinessState ReadinessState { get; }
        internal string Reason { get; }
        internal int Revision { get; }
        internal ActivityAsset Activity => Occurrence.Activity;
        internal bool IsValid => Occurrence.IsValid && ReadinessState.HasActivity;
    }
}
