using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Identifies the exact current Player and Actor occurrence a contribution may update.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-050 D05 exact-occurrence contribution request.")]
    public sealed class PlayerActorCompositionContributionRequest
    {
        public PlayerActorCompositionContributionRequest(
            PlayerSlotId playerSlotId,
            PlayerOccurrenceId playerOccurrenceId,
            ActorId actorId,
            ActorOccurrenceId actorOccurrenceId,
            int priority,
            string source,
            string reason)
        {
            PlayerSlotId = playerSlotId;
            PlayerOccurrenceId = playerOccurrenceId;
            ActorId = actorId;
            ActorOccurrenceId = actorOccurrenceId;
            Priority = priority;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public PlayerSlotId PlayerSlotId { get; }
        public PlayerOccurrenceId PlayerOccurrenceId { get; }
        public ActorId ActorId { get; }
        public ActorOccurrenceId ActorOccurrenceId { get; }
        public int Priority { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PlayerSlotId.IsValid && PlayerOccurrenceId.IsValid &&
            ActorId.IsValid && ActorOccurrenceId.IsValid &&
            !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(Reason);
    }
}
