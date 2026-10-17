using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-050 M02 explicit logical Actor identity policy for a prepared physical replacement.")]
    public enum PlayerActorReplacementIdentityPolicy
    {
        PreserveLogicalActor = 0,
        StartNewLogicalActor = 1
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "ADR-024 public prepared Actor replacement request.")]
    public readonly struct PlayerPreparedActorReplacementRequest
    {
        public const int NoExpectedRevision = -1;

        public PlayerPreparedActorReplacementRequest(PlayerSlotId playerSlotId, ActorProfile replacementActorProfile, string source, string reason, int expectedSelectionRevision = NoExpectedRevision, int expectedSessionRevision = NoExpectedRevision)
            : this(
                playerSlotId,
                replacementActorProfile,
                source,
                reason,
                expectedSelectionRevision,
                expectedSessionRevision,
                PlayerActorReplacementIdentityPolicy.PreserveLogicalActor)
        {
        }

        public PlayerPreparedActorReplacementRequest(
            PlayerSlotId playerSlotId,
            ActorProfile replacementActorProfile,
            string source,
            string reason,
            PlayerActorReplacementIdentityPolicy identityPolicy,
            int expectedSelectionRevision = NoExpectedRevision,
            int expectedSessionRevision = NoExpectedRevision)
            : this(
                playerSlotId,
                replacementActorProfile,
                source,
                reason,
                expectedSelectionRevision,
                expectedSessionRevision,
                identityPolicy)
        {
        }

        public PlayerPreparedActorReplacementRequest(
            PlayerSlotId playerSlotId,
            ActorProfile replacementActorProfile,
            string source,
            string reason,
            int expectedSelectionRevision,
            int expectedSessionRevision,
            PlayerActorReplacementIdentityPolicy identityPolicy)
        {
            PlayerSlotId = playerSlotId;
            ReplacementActorProfile = replacementActorProfile;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            ExpectedSelectionRevision = expectedSelectionRevision;
            ExpectedSessionRevision = expectedSessionRevision;
            IdentityPolicy = identityPolicy;
        }

        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfile ReplacementActorProfile { get; }
        public string Source { get; }
        public string Reason { get; }
        public int ExpectedSelectionRevision { get; }
        public int ExpectedSessionRevision { get; }
        public PlayerActorReplacementIdentityPolicy IdentityPolicy { get; }
        public bool HasExpectedSelectionRevision => ExpectedSelectionRevision >= 0;
        public bool HasExpectedSessionRevision => ExpectedSessionRevision >= 0;
        public bool IsValid =>
            PlayerSlotId.IsValid &&
            ReplacementActorProfile != null &&
            !string.IsNullOrEmpty(Source) &&
            !string.IsNullOrEmpty(Reason) &&
            ExpectedSelectionRevision >= NoExpectedRevision &&
            ExpectedSessionRevision >= NoExpectedRevision &&
            (IdentityPolicy == PlayerActorReplacementIdentityPolicy.PreserveLogicalActor ||
             IdentityPolicy == PlayerActorReplacementIdentityPolicy.StartNewLogicalActor);
    }
}
