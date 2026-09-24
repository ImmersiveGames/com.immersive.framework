using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeContext
    {
        internal bool TryGetPreparedPhysicalEvidence(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            out LocalPlayerHostAuthoring host,
            out PlayerInput playerInput,
            out PlayerActorDeclaration actorDeclaration,
            out PlayerActorMaterializationHandle materialization,
            out string issue)
        {
            host = null;
            playerInput = null;
            actorDeclaration = null;
            materialization = null;
            issue = string.Empty;

            if (!playerSlotId.IsValid || !expectedPreparation.IsValid ||
                expectedPreparation.PlayerSlotId != playerSlotId ||
                expectedPreparation.SessionContextId != _sessionContextId ||
                !_records.TryGetValue(playerSlotId, out PreparationRecord record) ||
                !record.Summary.IsPrepared ||
                record.Summary.Token != expectedPreparation ||
                record.Handle == null || record.Host == null ||
                !ReferenceEquals(record.Handle.LocalPlayerHost, record.Host) ||
                record.Handle.PlayerInput == null ||
                record.Handle.PlayerActorDeclaration == null)
            {
                issue = "Exact prepared Session physical Player evidence is unavailable, stale or divergent.";
                return false;
            }

            host = record.Host;
            playerInput = record.Handle.PlayerInput;
            actorDeclaration = record.Handle.PlayerActorDeclaration;
            materialization = record.Handle;
            return true;
        }
    }
}
