using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Replace Actor Selection")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-07 explicit Player Session Actor-selection replacement command.")]
    public sealed class PlayerSessionReplaceActorSelectionCommandTrigger : PlayerSessionCommandTriggerBase
    {
        private const string Source = nameof(PlayerSessionReplaceActorSelectionCommandTrigger);

        [SerializeField]
        [Tooltip("Slot whose current Actor selection will be replaced.")]
        private PlayerSlotProfile playerSlot;

        [SerializeField]
        [Tooltip("Actor Profile that replaces the current selection.")]
        private ActorProfile actorProfile;

        [SerializeField]
        [Tooltip("Expected selection revision, or -1 when no optimistic revision check is required.")]
        private int expectedSelectionRevision = PlayerActorSelectionRequest.NoExpectedRevision;

        public PlayerSlotProfile PlayerSlot => playerSlot;
        public ActorProfile ActorProfile => actorProfile;
        public int ExpectedSelectionRevision => expectedSelectionRevision;
        public PlayerActorSelectionResult LastActorSelectionResult { get; private set; }

        [ContextMenu("Invoke Replace Actor Selection")]
        public override void Invoke()
        {
            LastActorSelectionResult = null;
            string reason = BeginInvocation("ReplaceActorSelection");
            PlayerSlotId playerSlotId = default;
            if (playerSlot != null)
            {
                playerSlot.TryGetPlayerSlotId(out playerSlotId, out _);
            }

            var request = new PlayerActorSelectionRequest(
                playerSlotId,
                actorProfile,
                Source,
                reason,
                expectedSelectionRevision);
            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out string scopeIssue))
            {
                CompleteResult(PlayerActorSelectionResult.RuntimeUnavailable(
                    "ReplaceActorSelection", request, scopeIssue));
                return;
            }

            CompleteResult(access.RequestReplaceActorSelection(request));
        }

        protected override bool TryValidateCommandConfiguration(out string issue)
        {
            if (playerSlot == null)
            {
                issue = "Replace Actor Selection requires a Player Slot Profile. It never accepts a raw Slot identity string.";
                return false;
            }

            if (actorProfile == null)
            {
                issue = "Replace Actor Selection requires an Actor Profile.";
                return false;
            }

            if (expectedSelectionRevision < PlayerActorSelectionRequest.NoExpectedRevision)
            {
                issue = "Expected Selection Revision must be -1 or a non-negative revision.";
                return false;
            }

            if (!playerSlot.TryGetPlayerSlotId(out _, out issue))
            {
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private void CompleteResult(PlayerActorSelectionResult result)
        {
            LastActorSelectionResult = result;
            Complete("ReplaceActorSelection", Outcome(result), Describe(result));
        }
    }
}
