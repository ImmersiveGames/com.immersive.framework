using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Default Actor Selection")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-07 explicit Player Session default Actor selection command.")]
    public sealed class PlayerSessionDefaultActorSelectionCommandTrigger : PlayerSessionCommandTriggerBase
    {
        private const string Source = nameof(PlayerSessionDefaultActorSelectionCommandTrigger);

        [SerializeField]
        [Tooltip("Existing public selection authoring surface. It owns the Actor-selection command boundary.")]
        private LocalPlayerActorSelectionRequestAuthoring actorSelectionRequests;

        [SerializeField]
        [Tooltip("Slot whose configured default Actor will be selected. This does not select an arbitrary Actor.")]
        private PlayerSlotProfile playerSlot;

        [SerializeField]
        [Tooltip("Expected selection revision, or -1 when no optimistic revision check is required.")]
        private int expectedSelectionRevision = PlayerActorSelectionRequest.NoExpectedRevision;

        public LocalPlayerActorSelectionRequestAuthoring ActorSelectionRequests => actorSelectionRequests;
        public PlayerSlotProfile PlayerSlot => playerSlot;
        public int ExpectedSelectionRevision => expectedSelectionRevision;
        public PlayerActorSelectionResult LastActorSelectionResult { get; private set; }

        [ContextMenu("Invoke Default Actor Selection")]
        public override void Invoke()
        {
            LastActorSelectionResult = null;
            string reason = BeginInvocation("DefaultActorSelection");
            PlayerSlotId playerSlotId = default;
            if (playerSlot != null)
            {
                playerSlot.TryGetPlayerSlotId(out playerSlotId, out _);
            }

            var request = new PlayerActorSelectionRequest(
                playerSlotId,
                null,
                Source,
                reason,
                expectedSelectionRevision);
            if (!TryGetAccess(out ILocalPlayerProvisioningConsumerAccess _, out string scopeIssue))
            {
                CompleteResult(PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectDefaultActor", request, scopeIssue));
                return;
            }

            if (actorSelectionRequests == null)
            {
                CompleteResult(PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectDefaultActor",
                    request,
                    "Default Actor Selection requires an explicit Local Player Actor Selection Requests component."));
                return;
            }

            CompleteResult(actorSelectionRequests.RequestDefaultActorSelection(
                playerSlotId,
                expectedSelectionRevision,
                Source,
                reason));
        }

        protected override bool TryValidateCommandConfiguration(out string issue)
        {
            if (actorSelectionRequests == null)
            {
                issue = "Default Actor Selection requires an explicit Local Player Actor Selection Requests component.";
                return false;
            }

            if (!actorSelectionRequests.TryValidateConfiguration(out issue))
            {
                return false;
            }

            if (playerSlot == null)
            {
                issue = "Default Actor Selection requires a Player Slot Profile. It never accepts a raw Slot identity string.";
                return false;
            }

            if (expectedSelectionRevision < PlayerActorSelectionRequest.NoExpectedRevision)
            {
                issue = "Expected Selection Revision must be -1 or a non-negative revision.";
                return false;
            }

            return playerSlot.TryGetPlayerSlotId(out _, out issue);
        }

        private void CompleteResult(PlayerActorSelectionResult result)
        {
            LastActorSelectionResult = result;
            Complete("DefaultActorSelection", Outcome(result), Describe(result));
        }
    }
}
