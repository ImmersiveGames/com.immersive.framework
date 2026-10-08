using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Scene-authored owner of one transient Runtime Gameplay Availability block.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Gameplay Availability Block")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-ADR-044 UnityEvent authoring surface for one consumer-owned gameplay availability block.")]
    public sealed class PlayerGameplayAvailabilityBlockTrigger : PlayerSessionScopedAccessConsumer
    {
        private const string Source = nameof(PlayerGameplayAvailabilityBlockTrigger);
        private const string DefaultReason = "consumer-gameplay-availability-block";

        [SerializeField]
        [Tooltip("Player Slot whose current joined occurrence owns this block.")]
        private PlayerSlotProfile playerSlot;

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional diagnostic reason for this block request. This does not define gameplay rules.")]
        private string reason;

        private PlayerGameplayAvailabilityBlockToken _ownedToken;

        public PlayerSlotProfile PlayerSlot => playerSlot;
        public string Reason => reason ?? string.Empty;
        public bool OwnsActiveBlock => _ownedToken.IsValid;
        public PlayerGameplayAvailabilityBlockResult LastResult { get; private set; }
        public string LastDiagnostic { get; private set; } = "No gameplay availability block command has been invoked.";

        /// <summary>Acquires this component's single consumer-owned block, if not already held.</summary>
        public void RequestBlock()
        {
            if (_ownedToken.IsValid)
            {
                LastDiagnostic = "This component already owns its gameplay availability block.";
                return;
            }

            if (!TryResolvePlayerSlot(out PlayerSlotId playerSlotId, out string issue))
            {
                LastResult = PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedInvalidRequest,
                    default,
                    issue);
                LastDiagnostic = issue;
                return;
            }

            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out issue))
            {
                LastResult = PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedRuntimeUnavailable,
                    playerSlotId,
                    issue);
                LastDiagnostic = issue;
                return;
            }

            LastResult = access.RequestBlockRuntimeGameplay(
                playerSlotId,
                Source,
                ResolveReason());
            LastDiagnostic = LastResult.Message;
            if (LastResult.Status == PlayerGameplayAvailabilityBlockStatus.SucceededBlocked)
            {
                _ownedToken = LastResult.BlockToken;
            }
        }

        /// <summary>Releases only the block token acquired by this component.</summary>
        public void RequestRelease()
        {
            if (!_ownedToken.IsValid)
            {
                LastDiagnostic = "This component does not own an active gameplay availability block.";
                return;
            }

            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out string issue))
            {
                LastResult = PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedRuntimeUnavailable,
                    default,
                    issue);
                LastDiagnostic = issue;
                return;
            }

            LastResult = access.RequestReleaseRuntimeGameplay(
                _ownedToken,
                Source,
                ResolveReason());
            LastDiagnostic = LastResult.Message;
            if (LastResult.Status == PlayerGameplayAvailabilityBlockStatus.SucceededReleased ||
                LastResult.Status == PlayerGameplayAvailabilityBlockStatus.RejectedForeignOrStaleToken)
            {
                _ownedToken = default;
            }
        }

        public bool TryValidateConfiguration(out string issue)
        {
            if (!TryValidateScope(out issue))
            {
                return false;
            }

            return TryResolvePlayerSlot(out _, out issue);
        }

        protected override void OnScopedAccessReleasing(IPlayerSessionScopedAccess scopedAccess)
        {
            if (_ownedToken.IsValid)
            {
                LastResult = scopedAccess.RequestReleaseRuntimeGameplay(
                    _ownedToken,
                    Source,
                    "consumer-scope-released");
                LastDiagnostic = LastResult.Message;
                _ownedToken = default;
            }
        }

        private bool TryResolvePlayerSlot(out PlayerSlotId playerSlotId, out string issue)
        {
            playerSlotId = default;
            if (playerSlot == null)
            {
                issue = "Gameplay Availability Block requires a Player Slot Profile; raw Slot identity strings are not accepted.";
                return false;
            }

            return playerSlot.TryGetPlayerSlotId(out playerSlotId, out issue);
        }

        private string ResolveReason() =>
            string.IsNullOrWhiteSpace(reason) ? DefaultReason : reason.Trim();
    }
}
