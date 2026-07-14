using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped request to select, replace or clear the ActorProfile bound to one PlayerSlotId.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3H Player Actor selection request.")]
    public readonly struct PlayerActorSelectionRequest
    {
        public const int NoExpectedRevision = -1;

        public PlayerActorSelectionRequest(
            PlayerSlotId playerSlotId,
            ActorProfile actorProfile,
            string source,
            string reason,
            int expectedSelectionRevision = NoExpectedRevision)
        {
            PlayerSlotId = playerSlotId;
            ActorProfile = actorProfile;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            ExpectedSelectionRevision = expectedSelectionRevision;
        }

        public PlayerSlotId PlayerSlotId { get; }

        public ActorProfile ActorProfile { get; }

        public string Source { get; }

        public string Reason { get; }

        public int ExpectedSelectionRevision { get; }

        public bool HasExpectedSelectionRevision =>
            ExpectedSelectionRevision >= 0;

        public bool IsValid =>
            PlayerSlotId.IsValid &&
            !string.IsNullOrEmpty(Source) &&
            !string.IsNullOrEmpty(Reason) &&
            ExpectedSelectionRevision >= NoExpectedRevision;

        public string ToDiagnosticString()
        {
            return $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"actorProfile='{(ActorProfile != null ? ActorProfile.ActorProfileIdText : string.Empty)}' " +
                $"expectedSelectionRevision='{ExpectedSelectionRevision}' source='{Source}' reason='{Reason}'";
        }
    }
}
