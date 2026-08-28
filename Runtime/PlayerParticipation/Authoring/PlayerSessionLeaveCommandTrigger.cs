using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Leave")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-07 explicit Player Session Leave command.")]
    public sealed class PlayerSessionLeaveCommandTrigger : PlayerSessionCommandTriggerBase
    {
        private const string Source = nameof(PlayerSessionLeaveCommandTrigger);

        [SerializeField]
        [Tooltip("Exact Player Slot whose current joined Session occurrence will Leave. A target is always required, including single-player products.")]
        private PlayerSlotProfile playerSlot;

        [SerializeField]
        [Tooltip("Advanced/debug override for the exact joined occurrence revision. Use -1 to resolve the current occurrence from the scoped observation.")]
        private int expectedLeaveOccurrenceRevision = -1;

        public PlayerSlotProfile PlayerSlot => playerSlot;
        public int ExpectedLeaveOccurrenceRevision => expectedLeaveOccurrenceRevision;
        public SessionPlayerLeaveRequest LastLeaveRequest { get; private set; }
        public SessionPlayerLeaveResult LastLeaveResult { get; private set; }

        [ContextMenu("Invoke Leave")]
        public override void Invoke()
        {
            LastLeaveResult = null;
            string reason = BeginInvocation("Leave");
            PlayerSlotId playerSlotId = default;
            if (playerSlot != null)
            {
                playerSlot.TryGetPlayerSlotId(out playerSlotId, out _);
            }

            if (LastLeaveRequest.IsValid && LastLeaveRequest.PlayerSlotId != playerSlotId)
            {
                LastLeaveRequest = default;
            }

            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out string scopeIssue))
            {
                CompleteResult(SessionPlayerLeaveResult.RuntimeUnavailable(default, scopeIssue));
                return;
            }

            if (!access.TryGetObservation(
                    out PlayerSessionScopedObservationSnapshot observation) ||
                observation == null || !observation.IsAvailable)
            {
                CompleteResult(SessionPlayerLeaveResult.RuntimeUnavailable(
                    default,
                    "Leave could not read the current scoped Player observation required to correlate the target occurrence."));
                return;
            }

            PlayerSessionScopedSlotObservation target = default;
            bool found = false;
            for (int index = 0; index < observation.Slots.Count; index++)
            {
                PlayerSessionScopedSlotObservation candidate = observation.Slots[index];
                if (candidate.Slot.PlayerSlotId != playerSlotId)
                {
                    continue;
                }

                target = candidate;
                found = true;
                break;
            }

            int occurrenceRevision = expectedLeaveOccurrenceRevision >= 0
                ? expectedLeaveOccurrenceRevision
                : found ? target.Slot.Revision : 0;
            if (expectedLeaveOccurrenceRevision < 0 && found &&
                target.Slot.AllocationState == PlayerSlotAllocationState.Leaving &&
                LastLeaveRequest.IsValid && LastLeaveRequest.PlayerSlotId == playerSlotId)
            {
                occurrenceRevision = LastLeaveRequest.ExpectedOccurrenceRevision;
            }

            CompleteResult(access.RequestLeave(new SessionPlayerLeaveRequest(
                playerSlotId,
                occurrenceRevision,
                Source,
                reason)));
        }

        protected override bool TryValidateCommandConfiguration(out string issue)
        {
            if (playerSlot == null)
            {
                issue = "Leave requires an explicit Player Slot Profile target, including single-player products.";
                return false;
            }

            if (expectedLeaveOccurrenceRevision < -1)
            {
                issue = "Expected Leave Occurrence Revision must be -1 or a non-negative revision.";
                return false;
            }

            return playerSlot.TryGetPlayerSlotId(out _, out issue);
        }

        private void CompleteResult(SessionPlayerLeaveResult result)
        {
            LastLeaveResult = result;
            if (result != null && result.Request.IsValid)
            {
                LastLeaveRequest = result.Request;
            }

            Complete("Leave", Outcome(result), Describe(result));
        }
    }
}
