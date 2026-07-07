using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Passive authored relation between a PlayerSlot and an Actor.
    /// This component does not set occupants, clear occupants, spawn actors, destroy actors or resolve capabilities.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Slots/Player Slot Occupancy")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 passive PlayerSlot to Actor occupancy declaration.")]
    public sealed class PlayerSlotOccupancy : MonoBehaviour
    {
        [Tooltip("Optional slot declaration evidence. If present, the PlayerSlotId is resolved from this declaration.")]
        [SerializeField] private PlayerSlotDeclaration slotDeclaration;
        [Tooltip("Stable player slot id used when no slot declaration is assigned.")]
        [SerializeField] private string slotId = "player.1";
        [Tooltip("Optional Actor declaration evidence. If present, the ActorId is resolved from this declaration.")]
        [SerializeField] private ActorDeclaration actorDeclaration;
        [Tooltip("Explicit occupied ActorId used when no ActorDeclaration is assigned.")]
        [SerializeField] private string occupiedActorId;
        [Tooltip("Human-readable diagnostic label only.")]
        [SerializeField] private string displayName = "Player Slot Occupancy";
        [Tooltip("Diagnostic reason/source for this declaration.")]
        [SerializeField] private string reason = "player-slot.occupancy";

        public bool HasSlotDeclaration => slotDeclaration != null;

        public bool HasActorDeclaration => actorDeclaration != null;

        public string DisplayName => displayName.NormalizeTextOrFallback(name);

        public string Reason => reason.NormalizeText();

        public bool TryCreateDescriptor(
            string source,
            out PlayerSlotOccupancyDescriptor descriptor,
            out PlayerSlotSetIssue issue)
        {
            descriptor = default;
            issue = default;
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerSlotOccupancy));

            if (!TryResolvePlayerSlotId(normalizedSource, out PlayerSlotId resolvedSlotId, out issue))
            {
                return false;
            }

            if (!TryResolveActorId(normalizedSource, resolvedSlotId.StableText, out ActorId resolvedActorId, out issue))
            {
                return false;
            }

            try
            {
                descriptor = new PlayerSlotOccupancyDescriptor(
                    resolvedSlotId,
                    resolvedActorId,
                    HasActorDeclaration,
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
                    resolvedSlotId.StableText,
                    resolvedActorId.StableText,
                    normalizedSource,
                    exception.Message);
                return false;
            }
        }

        internal void ConfigureForDiagnostics(
            PlayerSlotDeclaration slotReference,
            string id,
            ActorDeclaration actorReference,
            string actorId,
            string label,
            string declarationReason)
        {
            slotDeclaration = slotReference;
            slotId = id.NormalizeText();
            actorDeclaration = actorReference;
            occupiedActorId = actorId.NormalizeText();
            displayName = label.NormalizeTextOrFallback(name);
            reason = declarationReason.NormalizeText();
        }

        private bool TryResolvePlayerSlotId(
            string normalizedSource,
            out PlayerSlotId resolvedSlotId,
            out PlayerSlotSetIssue issue)
        {
            resolvedSlotId = default;
            issue = default;

            if (slotDeclaration != null)
            {
                try
                {
                    resolvedSlotId = slotDeclaration.PlayerSlotId;
                    return true;
                }
                catch (Exception exception)
                {
                    issue = PlayerSlotSetIssue.BlockingIssue(
                        PlayerSlotSetIssueKind.InvalidPlayerSlotId,
                        string.Empty,
                        string.Empty,
                        normalizedSource,
                        exception.Message);
                    return false;
                }
            }

            string normalizedSlotId = slotId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedSlotId))
            {
                issue = PlayerSlotSetIssue.BlockingIssue(
                    PlayerSlotSetIssueKind.InvalidPlayerSlotId,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    "PlayerSlot occupancy has an empty slot id and no valid slot declaration.");
                return false;
            }

            try
            {
                resolvedSlotId = new PlayerSlotId(normalizedSlotId);
                return true;
            }
            catch (Exception exception)
            {
                issue = PlayerSlotSetIssue.BlockingIssue(
                    PlayerSlotSetIssueKind.InvalidPlayerSlotId,
                    normalizedSlotId,
                    string.Empty,
                    normalizedSource,
                    exception.Message);
                return false;
            }
        }

        private bool TryResolveActorId(
            string normalizedSource,
            string slotIdText,
            out ActorId resolvedActorId,
            out PlayerSlotSetIssue issue)
        {
            resolvedActorId = default;
            issue = default;

            if (actorDeclaration != null)
            {
                try
                {
                    resolvedActorId = actorDeclaration.ActorId;
                    return true;
                }
                catch (Exception exception)
                {
                    issue = PlayerSlotSetIssue.BlockingIssue(
                        PlayerSlotSetIssueKind.InvalidOccupiedActorId,
                        slotIdText,
                        string.Empty,
                        normalizedSource,
                        exception.Message);
                    return false;
                }
            }

            string normalizedActorId = occupiedActorId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                issue = PlayerSlotSetIssue.BlockingIssue(
                    PlayerSlotSetIssueKind.MissingOccupiedActor,
                    slotIdText,
                    string.Empty,
                    normalizedSource,
                    "PlayerSlot occupancy requires an ActorDeclaration or explicit occupied ActorId.");
                return false;
            }

            try
            {
                resolvedActorId = new ActorId(normalizedActorId);
                return true;
            }
            catch (Exception exception)
            {
                issue = PlayerSlotSetIssue.BlockingIssue(
                    PlayerSlotSetIssueKind.InvalidOccupiedActorId,
                    slotIdText,
                    normalizedActorId,
                    normalizedSource,
                    exception.Message);
                return false;
            }
        }

        private void Reset()
        {
            if (slotDeclaration == null)
            {
                slotDeclaration = GetComponent<PlayerSlotDeclaration>();
            }

            if (actorDeclaration == null)
            {
                actorDeclaration = GetComponent<ActorDeclaration>();
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
    }
}
