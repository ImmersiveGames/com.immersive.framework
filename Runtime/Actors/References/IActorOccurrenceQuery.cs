using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Read-only, point-in-time Actor occurrence query obtained by explicit hand-off from a composed
    /// <see cref="ActorOccurrenceQueryBinding"/>. A successful result states physical availability only;
    /// it grants no participation, readiness, input, targeting or gameplay authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-049 Amendment 001 read-only point-in-time Actor occurrence query.")]
    public interface IActorOccurrenceQuery
    {
        /// <summary>
        /// Resolves the selector against committed state at this moment. Never throws; returns
        /// <see cref="ActorOccurrenceResolutionStatus.Unavailable"/> when the query scope is not bound.
        /// For exact-occurrence selectors, <see cref="ActorOccurrenceResolutionStatus.Stale"/> means the
        /// expected occurrence is not physically available now; it is not proof of end of life.
        /// </summary>
        ActorOccurrenceResolution Resolve(ActorOccurrenceReference reference);
    }
}
