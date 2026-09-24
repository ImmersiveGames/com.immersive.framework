using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "ADR-024 public prepared Actor replacement request.")]
    public readonly struct PlayerPreparedActorReplacementRequest
    {
        public const int NoExpectedRevision = -1;

        public PlayerPreparedActorReplacementRequest(PlayerSlotId playerSlotId, ActorProfile replacementActorProfile, string source, string reason, int expectedSelectionRevision = NoExpectedRevision, int expectedSessionRevision = NoExpectedRevision)
        {
            PlayerSlotId = playerSlotId;
            ReplacementActorProfile = replacementActorProfile;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            ExpectedSelectionRevision = expectedSelectionRevision;
            ExpectedSessionRevision = expectedSessionRevision;
        }

        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfile ReplacementActorProfile { get; }
        public string Source { get; }
        public string Reason { get; }
        public int ExpectedSelectionRevision { get; }
        public int ExpectedSessionRevision { get; }
        public bool HasExpectedSelectionRevision => ExpectedSelectionRevision >= 0;
        public bool HasExpectedSessionRevision => ExpectedSessionRevision >= 0;
        public bool IsValid => PlayerSlotId.IsValid && ReplacementActorProfile != null && !string.IsNullOrEmpty(Source) && !string.IsNullOrEmpty(Reason) && ExpectedSelectionRevision >= NoExpectedRevision && ExpectedSessionRevision >= NoExpectedRevision;
    }
}
