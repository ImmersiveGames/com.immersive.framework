using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>Internal identity for one committed Activity entry; it is never derived from the Activity asset alone.</summary>
    internal readonly struct ActivityReadinessOccurrence
    {
        internal ActivityReadinessOccurrence(ActivityAsset activity, int transitionSequence)
        {
            Activity = activity;
            TransitionSequence = transitionSequence;
        }

        internal ActivityAsset Activity { get; }
        internal int TransitionSequence { get; }
        internal bool IsValid => Activity != null && TransitionSequence > 0;

        internal bool Matches(ActivityAsset activity, int transitionSequence)
        {
            return IsValid && ReferenceEquals(Activity, activity) &&
                   TransitionSequence == transitionSequence;
        }
    }
}
