using Immersive.Framework.ActivityFlow;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Local Player Host-scoped transient bridge between ActivityFlow target occurrence authority
    /// and staged Manager-Provisioned Actor activation. It never discovers an Activity globally.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class ActivityPlayerInitialPlacementRuntimeBinding : MonoBehaviour
    {
        private ActivityTransitionPreparationContext context;
        private ActivityPlayerInitialPlacementEvidence lastEvidence;

        internal void Configure(ActivityTransitionPreparationContext value)
        {
            context = value;
            lastEvidence = default;
        }

        internal bool MatchesOwner(RuntimeContentOwner owner) =>
            context.IsValid && context.Owner == owner;

        internal bool TryApplyCandidateBeforePromotion(
            PlayerActorCandidateStageToken candidateToken,
            Transform target,
            out string issue)
        {
            issue = string.Empty;
            if (!candidateToken.IsValid ||
                target == null ||
                !context.IsValid ||
                candidateToken.Owner != context.Owner)
            {
                issue =
                    "Candidate initial placement requires an exact staged token, target Transform and matching Activity occurrence owner.";
                return false;
            }

            return ActivityPlayerInitialPlacementRuntime
                .TryApplyRequiredPlacement(
                    context,
                    candidateToken.PlayerSlotId,
                    candidateToken.ActorId,
                    candidateToken.RuntimeContentIdentity.StableText,
                    target,
                    out lastEvidence,
                    out issue);
        }

        internal bool TryApplyBeforeActivation(
            PlayerActorMaterializationHandle handle,
            out string issue)
        {
            issue = string.Empty;
            if (handle == null ||
                handle.LogicalActorHost == null ||
                handle.PlayerActorDeclaration == null)
            {
                issue = "Player Actor initial placement activation gate requires a complete materialization handle.";
                return false;
            }

            // Scene-Provided adoption uses a release proxy as the typed handle's LogicalActorHost.
            // Only a framework-owned Actor whose declaration belongs to this physical hierarchy is
            // subject to the mandatory Manager-Provisioned placement gate here.
            Transform declarationTransform =
                handle.PlayerActorDeclaration.transform;
            Transform logicalRoot = handle.LogicalActorHost.transform;
            bool frameworkOwnedPhysicalActor =
                ReferenceEquals(declarationTransform, logicalRoot) ||
                declarationTransform.IsChildOf(logicalRoot);
            if (!frameworkOwnedPhysicalActor ||
                handle.Request.Owner.Scope != RuntimeContentScope.Activity)
            {
                return true;
            }

            if (!context.IsValid ||
                context.Owner != handle.Request.Owner)
            {
                issue =
                    "Activity-scoped Manager-Provisioned Actor cannot activate without current Activity initial-placement occurrence evidence.";
                return false;
            }

            string representationIdentity =
                handle.Request.RuntimeContentIdentity.StableText;
            if (lastEvidence.IsSuccessful &&
                lastEvidence.Owner == context.Owner &&
                lastEvidence.Occurrence.Matches(
                    context.Activity,
                    context.Occurrence.TransitionSequence) &&
                lastEvidence.PlayerSlotId ==
                    handle.Request.Slot.PlayerSlotId &&
                lastEvidence.ActorId == handle.Request.ActorId &&
                string.Equals(
                    lastEvidence.RepresentationIdentity,
                    representationIdentity,
                    System.StringComparison.Ordinal))
            {
                return true;
            }

            return ActivityPlayerInitialPlacementRuntime
                .TryApplyRequiredPlacement(
                    context,
                    handle.Request.Slot.PlayerSlotId,
                    handle.Request.ActorId,
                    representationIdentity,
                    logicalRoot,
                    out lastEvidence,
                    out issue);
        }

        internal ActivityPlayerInitialPlacementEvidence LastEvidence =>
            lastEvidence;
    }
}
