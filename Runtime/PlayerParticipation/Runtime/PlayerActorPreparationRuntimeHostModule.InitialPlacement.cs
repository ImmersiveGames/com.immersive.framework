using Immersive.Framework.ActivityFlow;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private ActivityTransitionPreparationContext
            currentActivityInitialPlacementContext;
        private ActivityPlayerInitialPlacementEvidence
            lastActivityInitialPlacementEvidence;

        internal bool TryConfigureActivityInitialPlacementContext(
            ActivityTransitionPreparationContext context,
            out string issue)
        {
            issue = string.Empty;
            if (!IsReady || !context.IsValid)
            {
                issue =
                    "Activity initial placement requires a ready Player Actor preparation module and valid target occurrence context.";
                return false;
            }

            currentActivityInitialPlacementContext = context;
            PlayerParticipationSnapshot snapshot =
                participationContext.CreateSnapshot();
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (!slot.IsJoined ||
                    !TryGetRegisteredHost(
                        slot.PlayerSlotId,
                        out LocalPlayerHostAuthoring host,
                        out _ ) ||
                    host == null)
                {
                    continue;
                }

                ActivityPlayerInitialPlacementRuntimeBinding binding =
                    host.GetComponent<ActivityPlayerInitialPlacementRuntimeBinding>();
                if (binding == null)
                {
                    binding = host.gameObject
                        .AddComponent<ActivityPlayerInitialPlacementRuntimeBinding>();
                }

                binding.Configure(context);
            }

            return true;
        }

        internal bool TryApplyStagedCandidateInitialPlacement(
            PlayerActorCandidateRuntimeHostModule candidateModule,
            PlayerActorCandidateStageToken candidateToken,
            out string issue)
        {
            issue = string.Empty;
            if (!currentActivityInitialPlacementContext.IsValid ||
                candidateModule == null ||
                !candidateToken.IsValid ||
                candidateToken.Owner !=
                    currentActivityInitialPlacementContext.Owner)
            {
                issue =
                    "Candidate initial placement requires the exact current target Activity occurrence and staged candidate token.";
                return false;
            }

            if (!candidateModule.TryGetCandidatePhysicalEvidence(
                    candidateToken,
                    out LocalPlayerHostAuthoring host,
                    out _,
                    out _,
                    out var logicalActorHost,
                    out issue) ||
                host == null || logicalActorHost == null)
            {
                return false;
            }

            ActivityPlayerInitialPlacementRuntimeBinding binding =
                host.GetComponent<ActivityPlayerInitialPlacementRuntimeBinding>();
            if (binding == null)
            {
                binding = host.gameObject
                    .AddComponent<ActivityPlayerInitialPlacementRuntimeBinding>();
                binding.Configure(currentActivityInitialPlacementContext);
            }

            bool applied = binding.TryApplyCandidateBeforePromotion(
                candidateToken,
                logicalActorHost.transform,
                out issue);
            if (applied)
            {
                lastActivityInitialPlacementEvidence =
                    binding.LastEvidence;
            }
            return applied;
        }

        internal bool TryApplySceneProvidedInitialPlacement(
            SceneLocalPlayerAdmissionAuthoring authoring,
            out string issue)
        {
            issue = string.Empty;
            if (!currentActivityInitialPlacementContext.IsValid ||
                authoring == null ||
                authoring.SceneLogicalPlayerActor == null ||
                !authoring.TryGetPlayerSlotId(
                    out PlayerSlotId playerSlotId,
                    out issue))
            {
                if (string.IsNullOrEmpty(issue))
                {
                    issue =
                        "Scene-Provided initial placement requires current Activity occurrence context and complete authoring.";
                }
                return false;
            }

            bool applied = ActivityPlayerInitialPlacementRuntime
                .TryApplyScenePolicy(
                    currentActivityInitialPlacementContext,
                    playerSlotId,
                    authoring.SceneLogicalPlayerActor.ActorId,
                    $"scene-provided:{playerSlotId.StableText}:{authoring.SceneLogicalPlayerActor.ActorId.StableText}",
                    authoring.InitialPlacementPolicy,
                    authoring.SceneLogicalPlayerActor.transform,
                    out lastActivityInitialPlacementEvidence,
                    out issue);
            return applied;
        }

        internal ActivityPlayerInitialPlacementEvidence
            LastActivityInitialPlacementEvidence =>
                lastActivityInitialPlacementEvidence;

        internal bool ShouldRetainPhysicalActorPresentationForIncomingActivity(
            RuntimeContentOwner exitingOwner,
            PlayerSlotId playerSlotId)
        {
            if (!currentActivityInitialPlacementContext.IsValid ||
                !exitingOwner.IsValid ||
                currentActivityInitialPlacementContext.Owner == exitingOwner ||
                !playerSlotId.IsValid ||
                participationContext == null)
            {
                return false;
            }

            return ActivityPlayerParticipationProjectionResolver.TryResolve(
                       currentActivityInitialPlacementContext.Activity,
                       participationContext,
                       out _,
                       out var projectedSlots,
                       out _) &&
                   projectedSlots.Exists(slot => slot.PlayerSlotId == playerSlotId);
        }
    }
}
