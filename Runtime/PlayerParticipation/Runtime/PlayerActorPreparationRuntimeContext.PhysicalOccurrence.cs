using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeContext
    {
        /// <summary>
        /// Confirma a propriedade física pela Session sem exigir uma atribuição atual de Activity.
        /// Esta evidência é separada daquela consumida pelos sistemas de Camera e Activity.
        /// </summary>
        internal bool TryGetCurrentPhysicalActorOccurrence(
            PlayerSlotId playerSlotId,
            RuntimeContentOwner sessionOwner,
            out PlayerActorPhysicalOccurrence occurrence)
        {
            occurrence = default;
            if (!playerSlotId.IsValid || !sessionOwner.IsValid ||
                sessionOwner.Scope != RuntimeContentScope.Session ||
                !_participationContext.TryGetActorSelection(playerSlotId, out PlayerSlotRuntimeSnapshot slot) ||
                !slot.IsJoined || !slot.HasSelectedActor ||
                !_records.TryGetValue(playerSlotId, out PreparationRecord record) ||
                !record.Summary.IsPrepared || record.Handle == null ||
                record.Handle.State != PlayerActorMaterializationState.Active ||
                record.Handle.Request.Owner != sessionOwner ||
                record.Handle.PlayerActorRuntimeHost == null ||
                record.Handle.PlayerActorDeclaration == null ||
                !ReferenceEquals(record.Handle.LocalPlayerHost, record.Host) ||
                record.Summary.SelectedActorProfileId != slot.SelectedActorProfileId ||
                record.Summary.SelectionRevision != slot.SelectionRevision)
            {
                return false;
            }

            ActorId declaredActorId;
            try { declaredActorId = record.Handle.PlayerActorDeclaration.ActorId; }
            catch (Exception) { return false; }
            if (record.Summary.Token.ActorId != declaredActorId) return false;

            GameObject root = record.Handle.ActorRoot != null
                ? record.Handle.ActorRoot.gameObject
                : null;
            if (root == null || !ReferenceEquals(
                    root.GetComponent<PlayerActorDeclaration>(),
                    record.Handle.PlayerActorDeclaration) ||
                !root.activeInHierarchy)
                return false;

            occurrence = new PlayerActorPhysicalOccurrence(
                record.Handle.OccurrenceId,
                playerSlotId,
                record.Summary.Token,
                record.Handle.PlayerActorDeclaration,
                root,
                sessionOwner);
            return occurrence.IsValid;
        }
    }
}
