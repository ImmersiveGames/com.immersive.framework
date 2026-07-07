using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Unity-facing declaration for a stable player participation seat.
    /// This component declares slot identity only. It does not join players, own input behavior, spawn actors or change occupancy.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Slots/Player Slot Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 PlayerSlot identity declaration.")]
    public sealed class PlayerSlotDeclaration : MonoBehaviour
    {
        [Tooltip("Stable framework player slot id. This is not PlayerInput.playerIndex or an ActorId.")]
        [SerializeField] private string slotId = "player.1";
        [Tooltip("Human-readable diagnostic label only.")]
        [SerializeField] private string displayName = "Player 1";
        [Tooltip("Optional local PlayerInput evidence. This is not the slot identity.")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("Diagnostic reason/source for this declaration.")]
        [SerializeField] private string reason = "player-slot.declaration";

        public PlayerSlotId PlayerSlotId => new PlayerSlotId(slotId.NormalizeText());

        public string DisplayName => displayName.NormalizeTextOrFallback(name);

        public PlayerInput PlayerInputEvidence => playerInput;

        public bool HasPlayerInputEvidence => playerInput != null;

        public string Reason => reason.NormalizeText();

        public bool TryCreateDescriptor(
            string source,
            out PlayerSlotDescriptor descriptor,
            out PlayerSlotSetIssue issue)
        {
            descriptor = default;
            issue = default;
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerSlotDeclaration));
            string normalizedSlotId = slotId.NormalizeText();

            if (string.IsNullOrWhiteSpace(normalizedSlotId))
            {
                issue = PlayerSlotSetIssue.BlockingIssue(
                    PlayerSlotSetIssueKind.InvalidPlayerSlotId,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    "PlayerSlot declaration has an empty slot id.");
                return false;
            }

            try
            {
                descriptor = new PlayerSlotDescriptor(
                    new PlayerSlotId(normalizedSlotId),
                    HasPlayerInputEvidence,
                    DisplayName,
                    gameObject.scene.IsValid() ? gameObject.scene.name : string.Empty,
                    gameObject.name,
                    normalizedSource,
                    Reason);
                return true;
            }
            catch (Exception exception)
            {
                issue = PlayerSlotSetIssue.BlockingIssue(
                    PlayerSlotSetIssueKind.InvalidDeclaration,
                    normalizedSlotId,
                    string.Empty,
                    normalizedSource,
                    exception.Message);
                return false;
            }
        }

        internal void ConfigureForDiagnostics(
            string id,
            string label,
            PlayerInput inputEvidence,
            string declarationReason)
        {
            slotId = id.NormalizeText();
            displayName = label.NormalizeTextOrFallback(name);
            playerInput = inputEvidence;
            reason = declarationReason.NormalizeText();
        }

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
    }
}
