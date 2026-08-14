using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.PlayerParticipation
{
    internal static class ActivityPlayerInitialPlacementRuntime
    {
        internal static bool TryApplyRequiredPlacement(
            ActivityTransitionPreparationContext context,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            string representationIdentity,
            Transform target,
            out ActivityPlayerInitialPlacementEvidence evidence,
            out string issue)
        {
            return TryApply(
                context,
                playerSlotId,
                actorId,
                representationIdentity,
                SceneProvidedPlayerInitialPlacementPolicy.ApplyActivityPlacement,
                target,
                out evidence,
                out issue);
        }

        internal static bool TryApplyScenePolicy(
            ActivityTransitionPreparationContext context,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            string representationIdentity,
            SceneProvidedPlayerInitialPlacementPolicy policy,
            Transform target,
            out ActivityPlayerInitialPlacementEvidence evidence,
            out string issue)
        {
            return TryApply(
                context,
                playerSlotId,
                actorId,
                representationIdentity,
                policy,
                target,
                out evidence,
                out issue);
        }

        private static bool TryApply(
            ActivityTransitionPreparationContext context,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            string representationIdentity,
            SceneProvidedPlayerInitialPlacementPolicy policy,
            Transform target,
            out ActivityPlayerInitialPlacementEvidence evidence,
            out string issue)
        {
            evidence = default;
            issue = string.Empty;
            if (!context.IsValid ||
                !playerSlotId.IsValid ||
                target == null ||
                !Enum.IsDefined(
                    typeof(SceneProvidedPlayerInitialPlacementPolicy),
                    policy))
            {
                issue = "Activity Player initial placement requires a valid occurrence, Slot, policy and target Transform.";
                evidence = Failure(
                    context,
                    playerSlotId,
                    actorId,
                    representationIdentity,
                    policy,
                    target,
                    null,
                    issue);
                return false;
            }

            if (policy ==
                SceneProvidedPlayerInitialPlacementPolicy.PreserveAuthoredPose)
            {
                evidence = new ActivityPlayerInitialPlacementEvidence(
                    context.Owner,
                    context.Occurrence,
                    playerSlotId,
                    actorId,
                    representationIdentity,
                    policy,
                    target,
                    null,
                    target.position,
                    target.rotation,
                    ActivityPlayerInitialPlacementStatus.Preserved,
                    "Scene-Provided Player preserved its authored world pose for the current Activity occurrence.");
                return true;
            }

            if (!TryResolveExactAnchor(
                    context,
                    playerSlotId,
                    out Transform anchor,
                    out issue))
            {
                evidence = Failure(
                    context,
                    playerSlotId,
                    actorId,
                    representationIdentity,
                    policy,
                    target,
                    null,
                    issue);
                return false;
            }

            Transform originalParent = target.parent;
            Vector3 originalLocalScale = target.localScale;
            target.SetPositionAndRotation(
                anchor.position,
                anchor.rotation);
            if (!ReferenceEquals(target.parent, originalParent) ||
                target.localScale != originalLocalScale)
            {
                issue =
                    "Initial placement changed hierarchy or scale, which is outside IF-ADR-021 authority.";
                evidence = Failure(
                    context,
                    playerSlotId,
                    actorId,
                    representationIdentity,
                    policy,
                    target,
                    anchor,
                    issue);
                return false;
            }

            evidence = new ActivityPlayerInitialPlacementEvidence(
                context.Owner,
                context.Occurrence,
                playerSlotId,
                actorId,
                representationIdentity,
                policy,
                target,
                anchor,
                target.position,
                target.rotation,
                ActivityPlayerInitialPlacementStatus.Applied,
                "Activity initial placement applied exact world position and rotation.");
            return true;
        }

        private static bool TryResolveExactAnchor(
            ActivityTransitionPreparationContext context,
            PlayerSlotId playerSlotId,
            out Transform anchor,
            out string issue)
        {
            anchor = null;
            issue = string.Empty;
            int exactBindingCount = 0;
            IReadOnlyList<ActivityContentDiscoveryScene> ownedScenes =
                context.DiscoveryScope.ActivityOwnedScenes;

            for (int sceneIndex = 0;
                 sceneIndex < ownedScenes.Count;
                 sceneIndex++)
            {
                ActivityContentDiscoveryScene discoveryScene =
                    ownedScenes[sceneIndex];
                if (!discoveryScene.MatchesActivity(context.Activity))
                {
                    continue;
                }

                if (!TryResolveLoadedScene(
                        discoveryScene,
                        out Scene scene))
                {
                    issue =
                        $"Activity-owned scene '{discoveryScene.ScenePath}' is not loaded while resolving Player initial placement.";
                    return false;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0;
                     rootIndex < roots.Length;
                     rootIndex++)
                {
                    ActivityPlayerInitialPlacementAuthoring[] authorings =
                        roots[rootIndex]
                            .GetComponentsInChildren<ActivityPlayerInitialPlacementAuthoring>(true);
                    for (int authoringIndex = 0;
                         authoringIndex < authorings.Length;
                         authoringIndex++)
                    {
                        IReadOnlyList<ActivityPlayerInitialPlacementAuthoring.Binding> bindings =
                            authorings[authoringIndex].Bindings;
                        for (int bindingIndex = 0;
                             bindingIndex < bindings.Count;
                             bindingIndex++)
                        {
                            ActivityPlayerInitialPlacementAuthoring.Binding binding =
                                bindings[bindingIndex];
                            if (binding == null ||
                                !binding.TryGetPlayerSlotId(
                                    out PlayerSlotId bindingSlot,
                                    out _ ) ||
                                bindingSlot != playerSlotId)
                            {
                                continue;
                            }

                            exactBindingCount++;
                            if (exactBindingCount > 1)
                            {
                                issue =
                                    $"Activity initial placement has duplicate bindings for Slot '{playerSlotId.StableText}'.";
                                return false;
                            }

                            if (binding.PlacementAnchor == null)
                            {
                                issue =
                                    $"Activity initial placement binding for Slot '{playerSlotId.StableText}' has no Transform anchor.";
                                return false;
                            }

                            if (binding.PlacementAnchor.gameObject.scene.handle !=
                                scene.handle)
                            {
                                issue =
                                    $"Activity initial placement anchor for Slot '{playerSlotId.StableText}' is outside its canonical Activity-owned scene.";
                                return false;
                            }

                            anchor = binding.PlacementAnchor;
                        }
                    }
                }
            }

            if (exactBindingCount != 1 || anchor == null)
            {
                issue =
                    $"Activity initial placement requires exactly one binding for Slot '{playerSlotId.StableText}'. Found '{exactBindingCount}'.";
                return false;
            }

            return true;
        }

        private static bool TryResolveLoadedScene(
            ActivityContentDiscoveryScene discoveryScene,
            out Scene scene)
        {
            scene = !string.IsNullOrEmpty(discoveryScene.ScenePath)
                ? SceneManager.GetSceneByPath(discoveryScene.ScenePath)
                : SceneManager.GetSceneByName(discoveryScene.SceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        private static ActivityPlayerInitialPlacementEvidence Failure(
            ActivityTransitionPreparationContext context,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            string representationIdentity,
            SceneProvidedPlayerInitialPlacementPolicy policy,
            Transform target,
            Transform anchor,
            string issue)
        {
            return new ActivityPlayerInitialPlacementEvidence(
                context.Owner,
                context.Occurrence,
                playerSlotId,
                actorId,
                representationIdentity,
                policy,
                target,
                anchor,
                target != null ? target.position : default,
                target != null ? target.rotation : default,
                ActivityPlayerInitialPlacementStatus.Failed,
                issue);
        }
    }
}
