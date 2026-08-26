using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Clear Actor Selection")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-07 explicit Player Session Actor-selection clear command.")]
    public sealed class PlayerSessionClearActorSelectionCommandTrigger : PlayerSessionCommandTriggerBase
    {
        private const string Source = nameof(PlayerSessionClearActorSelectionCommandTrigger);

        [SerializeField]
        [Tooltip("Slot whose Actor selection will be cleared.")]
        private PlayerSlotProfile playerSlot;

        [SerializeField]
        [Tooltip("Expected selection revision, or -1 when no optimistic revision check is required.")]
        private int expectedSelectionRevision = PlayerActorSelectionRequest.NoExpectedRevision;

        public PlayerSlotProfile PlayerSlot => playerSlot;
        public int ExpectedSelectionRevision => expectedSelectionRevision;
        public PlayerActorSelectionResult LastActorSelectionResult { get; private set; }

        [ContextMenu("Invoke Clear Actor Selection")]
        public override void Invoke()
        {
            LastActorSelectionResult = null;
            string reason = BeginInvocation("ClearActorSelection");
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
            if (!TryGetAccess(out ILocalPlayerProvisioningConsumerAccess access, out string scopeIssue))
            {
                CompleteResult(PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection", request, scopeIssue));
                return;
            }

            CompleteResult(access.RequestClearActorSelection(request));
        }

        protected override bool TryValidateCommandConfiguration(out string issue)
        {
            if (playerSlot == null)
            {
                issue = "Clear Actor Selection requires a Player Slot Profile. It never accepts a raw Slot identity string.";
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
            Complete("ClearActorSelection", Outcome(result), Describe(result));
        }
    }
}
