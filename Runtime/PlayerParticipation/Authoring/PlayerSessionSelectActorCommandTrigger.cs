using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Select Actor")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-07 explicit Player Session Actor selection command.")]
    public sealed class PlayerSessionSelectActorCommandTrigger : PlayerSessionCommandTriggerBase
    {
        private const string Source = nameof(PlayerSessionSelectActorCommandTrigger);

        [SerializeField]
        [Tooltip("Slot that will receive the selected Actor Profile.")]
        private PlayerSlotProfile playerSlot;

        [SerializeField]
        [Tooltip("Actor Profile to select for the Player Slot.")]
        private ActorProfile actorProfile;

        [SerializeField]
        [Tooltip("Expected selection revision, or -1 when no optimistic revision check is required.")]
        private int expectedSelectionRevision = PlayerActorSelectionRequest.NoExpectedRevision;

        public PlayerSlotProfile PlayerSlot => playerSlot;
        public ActorProfile ActorProfile => actorProfile;
        public int ExpectedSelectionRevision => expectedSelectionRevision;
        public PlayerActorSelectionResult LastActorSelectionResult { get; private set; }

        [ContextMenu("Invoke Select Actor")]
        public override void Invoke()
        {
            LastActorSelectionResult = null;
            string reason = BeginInvocation("SelectActor");
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
                    "SelectActorProfile", request, scopeIssue));
                return;
            }

            CompleteResult(access.RequestSelectActorProfile(request));
        }

        protected override bool TryValidateCommandConfiguration(out string issue)
        {
            if (playerSlot == null)
            {
                issue = "Select Actor requires a Player Slot Profile. It never accepts a raw Slot identity string.";
                return false;
            }

            if (actorProfile == null)
            {
                issue = "Select Actor requires an Actor Profile.";
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
            Complete("SelectActor", Outcome(result), Describe(result));
        }
    }
}
