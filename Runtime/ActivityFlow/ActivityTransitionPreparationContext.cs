using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable target-Activity preparation context exposed only while ActivityFlow owns a
    /// non-terminal pre-commit transition. It carries the canonical occurrence and the exact
    /// Activity-owned discovery scope; it is not a global lookup surface.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IF-ADR-021 transient target Activity occurrence/discovery context for Player initial placement.")]
    internal readonly struct ActivityTransitionPreparationContext
    {
        internal ActivityTransitionPreparationContext(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            ActivityReadinessOccurrence occurrence,
            ActivityContentDiscoveryScope discoveryScope)
        {
            Activity = activity;
            Owner = owner;
            Occurrence = occurrence;
            DiscoveryScope = discoveryScope;
        }

        internal ActivityAsset Activity { get; }
        internal RuntimeContentOwner Owner { get; }
        internal ActivityReadinessOccurrence Occurrence { get; }
        internal ActivityContentDiscoveryScope DiscoveryScope { get; }

        internal bool IsValid =>
            Activity != null &&
            Owner.IsValid &&
            Owner.Scope == RuntimeContentScope.Activity &&
            Occurrence.IsValid &&
            Occurrence.Matches(Activity, Occurrence.TransitionSequence);
    }
}
