using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        internal bool TryGetCurrentPreparation(
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary preparation,
            out string issue)
        {
            preparation = default;
            if (preparationContext == null ||
                !preparationContext.TryGetPreparationSummary(
                    playerSlotId,
                    out preparation) ||
                !preparation.IsValid)
            {
                issue = "Current P3J preparation evidence is unavailable.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        internal bool TryBeginCandidateHandoff(
            PlayerActorCandidatePromotionHandle candidate,
            PlayerActorPreparationToken expectedCurrent,
            string source,
            string reason,
            out PlayerActorPreparationHandoff handoff,
            out string issue)
        {
            if (preparationContext == null)
            {
                handoff = null;
                issue = diagnostic;
                return false;
            }

            bool succeeded = preparationContext.TryBeginCandidateHandoff(
                candidate,
                expectedCurrent,
                source,
                reason,
                out handoff,
                out issue);
            diagnostic = succeeded
                ? $"Player Actor preparation handoff started. slot='{candidate.Token.PlayerSlotId.StableText}'."
                : issue;
            return succeeded;
        }

        internal bool TryRollbackCandidateHandoff(
            PlayerActorPreparationHandoff handoff,
            string source,
            string reason,
            out string issue)
        {
            if (preparationContext == null)
            {
                issue = diagnostic;
                return false;
            }

            bool succeeded = preparationContext.TryRollbackCandidateHandoff(
                handoff,
                source,
                reason,
                out issue);
            diagnostic = succeeded
                ? "Player Actor preparation handoff rolled back."
                : issue;
            return succeeded;
        }

        internal bool TryCommitCandidateHandoff(
            PlayerActorPreparationHandoff handoff,
            string source,
            string reason,
            out string issue)
        {
            if (preparationContext == null)
            {
                issue = diagnostic;
                return false;
            }

            bool succeeded = preparationContext.TryCommitCandidateHandoff(
                handoff,
                source,
                reason,
                out issue);
            diagnostic = succeeded
                ? "Player Actor preparation handoff committed and previous Actor released."
                : issue;
            return succeeded;
        }

        internal bool TryGetPreparedPhysicalEvidence(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            out LocalPlayerHostAuthoring host,
            out PlayerInput playerInput,
            out PlayerActorDeclaration declaration,
            out GameObject logicalActorHost,
            out string issue)
        {
            if (preparationContext == null)
            {
                host = null;
                playerInput = null;
                declaration = null;
                logicalActorHost = null;
                issue = diagnostic;
                return false;
            }

            return preparationContext.TryGetPreparedPhysicalEvidence(
                playerSlotId,
                expectedPreparation,
                out host,
                out playerInput,
                out declaration,
                out logicalActorHost,
                out issue);
        }
    }
}
