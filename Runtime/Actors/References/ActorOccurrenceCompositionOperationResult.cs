using Immersive.Composition;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Actors
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-050 D07 NonPlayer composition operation outcome.")]
    public enum ActorOccurrenceCompositionOperationStatus
    {
        Committed = 0,
        NoChange = 1,
        Rejected = 2,
        Stale = 3,
        Cancelled = 4,
        Faulted = 5,
        Degraded = 6
    }

    /// <summary>Operation evidence carries the exact occurrence and RuntimeContent owner that authorized it.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-050 D07 NonPlayer composition operation outcome.")]
    public sealed class ActorOccurrenceCompositionOperationResult
    {
        internal ActorOccurrenceCompositionOperationResult(
            ActorOccurrenceCompositionOperationStatus status,
            ActorId actorId,
            ActorOccurrenceId actorOccurrenceId,
            RuntimeContentOwner owner,
            CompositionResult compositionResult,
            long revisionBefore,
            string source,
            string reason,
            string issue)
        {
            Status = status;
            ActorId = actorId;
            ActorOccurrenceId = actorOccurrenceId;
            Owner = owner;
            CompositionResult = compositionResult;
            RevisionBefore = revisionBefore;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Issue = issue ?? string.Empty;
        }

        public ActorOccurrenceCompositionOperationStatus Status { get; }
        public ActorId ActorId { get; }
        public ActorOccurrenceId ActorOccurrenceId { get; }
        public RuntimeContentOwner Owner { get; }
        public CompositionResult CompositionResult { get; }
        public long RevisionBefore { get; }
        public long CompositionRevision => CompositionResult?.Revision ?? RevisionBefore;
        public string Source { get; }
        public string Reason { get; }
        public string Issue { get; }
    }
}
