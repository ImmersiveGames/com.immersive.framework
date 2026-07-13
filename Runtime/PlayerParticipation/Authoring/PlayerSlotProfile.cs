using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable product identity and presentation metadata for one Player participation seat.
    /// Runtime allocation, join, selection and occupancy state must remain outside this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerSlotProfile",
        menuName = "Immersive Framework/Player/Player Slot Profile",
        order = 10)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3C immutable Player Slot identity Profile.")]
    public sealed class PlayerSlotProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Canonical stable PlayerSlotId authored only by this Profile.")]
        [SerializeField] private string playerSlotId = "player.1";
        [SerializeField] private string displayName = "Player 1";
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("Presentation")]
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField] private Sprite icon;
        [Tooltip("Presentation metadata only. Game/Application array order controls default allocation.")]
        [SerializeField] private int displayOrder;

        /// <summary>
        /// Canonical normalized serialized identity text owned by this Profile.
        /// </summary>
        public string PlayerSlotIdText => playerSlotId.NormalizeText();

        /// <summary>
        /// Typed identity. Invalid authoring fails explicitly instead of producing a fallback Slot.
        /// </summary>
        public PlayerSlotId PlayerSlotId => GetRequiredPlayerSlotId();

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public Color AccentColor => accentColor;

        public Sprite Icon => icon;

        public int DisplayOrder => displayOrder;

        public bool TryGetPlayerSlotId(out PlayerSlotId resolvedPlayerSlotId, out string issue)
        {
            string normalizedId = PlayerSlotIdText;
            if (string.IsNullOrEmpty(normalizedId))
            {
                resolvedPlayerSlotId = default;
                issue = $"PlayerSlotProfile '{name}' requires a non-empty PlayerSlotId.";
                return false;
            }

            try
            {
                resolvedPlayerSlotId = PlayerSlotId.From(normalizedId);
                issue = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                resolvedPlayerSlotId = default;
                issue = $"PlayerSlotProfile '{name}' has invalid PlayerSlotId '{normalizedId}'. {exception.Message}";
                return false;
            }
        }

        public PlayerSlotId GetRequiredPlayerSlotId()
        {
            if (TryGetPlayerSlotId(out PlayerSlotId resolvedPlayerSlotId, out string issue))
            {
                return resolvedPlayerSlotId;
            }

            throw new InvalidOperationException(issue);
        }
    }
}
