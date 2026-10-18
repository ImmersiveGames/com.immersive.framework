using Immersive.Composition;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-050 D05 Actor composition operation outcome.")]
    public enum PlayerActorCompositionOperationStatus
    {
        Committed = 0,
        NoChange = 1,
        Rejected = 2,
        Stale = 3,
        Cancelled = 4,
        Faulted = 5,
        Degraded = 6
    }

    /// <summary>Operation evidence retains the exact occurrence identities supplied at open.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-050 D05 Actor composition operation outcome.")]
    public sealed class PlayerActorCompositionOperationResult
    {
        internal PlayerActorCompositionOperationResult(
            PlayerActorCompositionOperationStatus status,
            PlayerSlotId playerSlotId,
            PlayerOccurrenceId playerOccurrenceId,
            ActorId actorId,
            ActorOccurrenceId actorOccurrenceId,
            CompositionResult compositionResult,
            string source,
            string reason,
            string issue)
        {
            Status = status;
            PlayerSlotId = playerSlotId;
            PlayerOccurrenceId = playerOccurrenceId;
            ActorId = actorId;
            ActorOccurrenceId = actorOccurrenceId;
            CompositionResult = compositionResult;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Issue = issue ?? string.Empty;
        }

        public PlayerActorCompositionOperationStatus Status { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerOccurrenceId PlayerOccurrenceId { get; }
        public ActorId ActorId { get; }
        public ActorOccurrenceId ActorOccurrenceId { get; }
        public CompositionResult CompositionResult { get; }
        public long CompositionRevision => CompositionResult?.Revision ?? 0;
        public string Source { get; }
        public string Reason { get; }
        public string Issue { get; }

        internal static PlayerActorCompositionOperationResult From(
            PlayerActorCompositionContributionRequest request,
            CompositionResult result,
            string source,
            string reason,
            string localIssue = null,
            PlayerActorCompositionOperationStatus? localStatus = null)
        {
            PlayerActorCompositionOperationStatus status = localStatus ?? (result == null
                ? PlayerActorCompositionOperationStatus.Stale
                : (PlayerActorCompositionOperationStatus)result.Status);
            string issue = localIssue ?? (result != null && result.Issues.Count > 0
                ? string.Join(" | ", System.Linq.Enumerable.Select(
                    result.Issues,
                    item => item.ToString()))
                : string.Empty);
            return new PlayerActorCompositionOperationResult(
                status,
                request.PlayerSlotId,
                request.PlayerOccurrenceId,
                request.ActorId,
                request.ActorOccurrenceId,
                result,
                source,
                reason,
                issue);
        }
    }
}
