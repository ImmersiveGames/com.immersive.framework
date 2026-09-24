using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Immutable Route occurrence context published only after Route scene composition
    /// has produced a valid discovery scope. It has no Activity dependency.
    /// </summary>
    internal readonly struct RoutePlayerSpatialEntryContext
    {
        internal RoutePlayerSpatialEntryContext(
            RouteAsset route,
            int occurrenceSequence,
            RouteContentDiscoveryScope discoveryScope)
        {
            Route = route;
            OccurrenceSequence = occurrenceSequence;
            DiscoveryScope = discoveryScope;
        }

        internal RouteAsset Route { get; }
        internal int OccurrenceSequence { get; }
        internal RouteContentDiscoveryScope DiscoveryScope { get; }
        internal RoutePlayerSpatialEntryPolicy Policy =>
            Route != null ? Route.PlayerSpatialEntryPolicy : default;
        internal bool IsValid => Route != null &&
            Route.HasValidRouteId &&
            OccurrenceSequence > 0 &&
            ReferenceEquals(DiscoveryScope.Route, Route) &&
            Route.HasDefinedPlayerSpatialEntryPolicy;

        internal bool Matches(RoutePlayerSpatialEntryContext other) =>
            IsValid && other.IsValid &&
            ReferenceEquals(Route, other.Route) &&
            OccurrenceSequence == other.OccurrenceSequence;
    }

    internal interface IRoutePlayerSpatialEntryLifecycleParticipant
    {
        bool TryEnterRouteSpatialEntry(
            RoutePlayerSpatialEntryContext context,
            out string issue);

        void ExitRouteSpatialEntry(RoutePlayerSpatialEntryContext context);
    }
}
