using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        // "Current" é a ocorrência física preparada retida pela Session, nunca uma
        // representação local de Activity.
        internal bool TryGetCurrentPreparation(
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary preparation,
            out string issue)
        {
            preparation = default;
            issue = string.Empty;
            if (_preparationContext == null ||
                !_preparationContext.TryGetPreparationSummary(
                    playerSlotId,
                    out preparation))
            {
                issue = "Current Session physical Player preparation is unavailable.";
                return false;
            }

            if (!preparation.IsPrepared)
            {
                issue = preparation.Message;
                return false;
            }

            return true;
        }

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
            if (_preparationContext == null)
            {
                issue = _diagnostic;
                return false;
            }

            return _preparationContext.TryGetPreparedPhysicalEvidence(
                playerSlotId,
                expectedPreparation,
                out host,
                out playerInput,
                out actorDeclaration,
                out materialization,
                out issue);
        }

        internal bool TryGetCurrentSessionPhysicalHost(
            PlayerSlotId playerSlotId,
            out LocalPlayerHostAuthoring host,
            out string issue)
        {
            host = null;
            issue = string.Empty;
            if (!TryGetRetainedHostEvidence(
                    playerSlotId,
                    out PlayerHostEvidenceSnapshot evidence) ||
                !evidence.HasSessionPhysicalHost ||
                !evidence.HostIsAvailable)
            {
                issue = "Current Session physical Player Host evidence is unavailable.";
                return false;
            }

            host = evidence.Host;
            return true;
        }
    }
}
