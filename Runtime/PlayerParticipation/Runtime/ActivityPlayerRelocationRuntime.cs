using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.PlayerParticipation
{
    internal static class ActivityPlayerRelocationRuntime
    {
        internal static bool TryPreflight(
            ActivityTransitionPreparationContext context,
            PlayerSlotId playerSlotId,
            out string issue)
        {
            issue = string.Empty;
            if (!context.IsValid || !playerSlotId.IsValid ||
                !context.Activity.HasDefinedPlayerRelocationPolicy ||
                context.Activity.PlayerRelocationPolicy !=
                    ActivityPlayerRelocationPolicy.ApplyExplicitRelocation)
            {
                issue =
                    "Activity Player relocation preflight requires a valid explicit-relocation Activity occurrence and Player Slot.";
                return false;
            }

            return TryResolveExactAnchor(
                context,
                playerSlotId,
                out _,
                out issue);
        }

        internal static bool TryApply(
            ActivityTransitionPreparationContext context, PlayerSlotId playerSlotId,
            ActorId actorId, string representationIdentity, Transform target,
            out ActivityPlayerRelocationEvidence evidence, out string issue)
        {
            evidence = default;
            issue = string.Empty;
            if (!context.IsValid || !playerSlotId.IsValid || target == null ||
                !context.Activity.HasDefinedPlayerRelocationPolicy)
            {
                issue = "Activity Player relocation requires a valid target Activity occurrence, Slot, target Transform and policy.";
                evidence = Failure(context, playerSlotId, actorId, representationIdentity, target, null, issue);
                return false;
            }

            if (context.Activity.PlayerRelocationPolicy !=
                ActivityPlayerRelocationPolicy.ApplyExplicitRelocation)
            {
                issue = $"Activity '{context.Activity.ActivityName}' does not request explicit Player relocation.";
                evidence = Failure(context, playerSlotId, actorId, representationIdentity, target, null, issue);
                return false;
            }

            if (!TryResolveExactAnchor(
                    context,
                    playerSlotId,
                    out Transform anchor,
                    out issue))
            {
                evidence = Failure(context, playerSlotId, actorId, representationIdentity, target, null, issue);
                return false;
            }

            Transform parent = target.parent;
            Vector3 scale = target.localScale;
            target.SetPositionAndRotation(anchor.position, anchor.rotation);
            if (!ReferenceEquals(target.parent, parent) || target.localScale != scale)
            {
                issue = "Activity Player relocation changed hierarchy or scale, which is outside IF-ADR-021 authority.";
                evidence = Failure(context, playerSlotId, actorId, representationIdentity, target, anchor, issue);
                return false;
            }

            evidence = new ActivityPlayerRelocationEvidence(
                context.Owner, context.Occurrence, playerSlotId, actorId,
                representationIdentity, target, anchor, target.position, target.rotation,
                ActivityPlayerRelocationStatus.Applied,
                "Activity explicit relocation applied exact world position and rotation.");
            return true;
        }

        private static bool TryResolveExactAnchor(
            ActivityTransitionPreparationContext context, PlayerSlotId playerSlotId,
            out Transform anchor, out string issue)
        {
            anchor = null;
            issue = string.Empty;
            var visitedScenes = new HashSet<ulong>();
            int matches = 0;

            IReadOnlyList<RouteContentDiscoveryScene> routeScenes =
                context.DiscoveryScope.RouteScope.RouteOwnedScenes;
            for (int index = 0; index < routeScenes.Count; index++)
            {
                if (!TryResolveLoadedScene(routeScenes[index].ScenePath, routeScenes[index].SceneName,
                        out Scene scene, out issue) ||
                    !TryInspectScene(scene, context, playerSlotId, ref matches, ref anchor, out issue))
                {
                    issue = BuildInvalidConfigurationDiagnostic(
                        context,
                        playerSlotId,
                        matches,
                        issue);
                    return false;
                }
                visitedScenes.Add(scene.handle.GetRawData());
            }

            IReadOnlyList<ActivityContentDiscoveryScene> activityScenes =
                context.DiscoveryScope.ActivityOwnedScenes;
            for (int index = 0; index < activityScenes.Count; index++)
            {
                ActivityContentDiscoveryScene discoveryScene = activityScenes[index];
                if (!discoveryScene.MatchesActivity(context.Activity))
                {
                    continue;
                }

                if (!TryResolveLoadedScene(discoveryScene.ScenePath, discoveryScene.SceneName,
                        out Scene scene, out issue))
                {
                    issue = BuildInvalidConfigurationDiagnostic(
                        context,
                        playerSlotId,
                        matches,
                        issue);
                    return false;
                }

                if (!visitedScenes.Add(scene.handle.GetRawData()))
                {
                    continue;
                }

                if (!TryInspectScene(scene, context, playerSlotId, ref matches, ref anchor, out issue))
                {
                    issue = BuildInvalidConfigurationDiagnostic(
                        context,
                        playerSlotId,
                        matches,
                        issue);
                    return false;
                }
            }

            if (matches == 0)
            {
                issue = BuildInvalidConfigurationDiagnostic(
                    context,
                    playerSlotId,
                    matches,
                    "Exactly one Activity Player Relocation binding is required for this Activity + Player Slot.\n\n" +
                    "Valid locations:\n" +
                    "- Route Primary Scene\n" +
                    "- Route Content\n" +
                    "- Activity Content");
                return false;
            }

            if (matches > 1)
            {
                issue = BuildInvalidConfigurationDiagnostic(
                    context,
                    playerSlotId,
                    matches,
                    "Exactly one Activity Player Relocation binding is required.\n" +
                    "Remove the duplicate binding(s).");
                return false;
            }

            if (anchor == null)
            {
                issue = BuildInvalidConfigurationDiagnostic(
                    context,
                    playerSlotId,
                    matches,
                    "The matching Activity Player Relocation binding is invalid.");
                return false;
            }

            return true;
        }

        private static bool TryInspectScene(
            Scene scene, ActivityTransitionPreparationContext context, PlayerSlotId playerSlotId,
            ref int matches, ref Transform anchor, out string issue)
        {
            issue = string.Empty;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                ActivityPlayerRelocationAuthoring[] authorings = roots[rootIndex]
                    .GetComponentsInChildren<ActivityPlayerRelocationAuthoring>(true);
                for (int authoringIndex = 0; authoringIndex < authorings.Length; authoringIndex++)
                {
                    ActivityPlayerRelocationAuthoring authoring =
                        authorings[authoringIndex];
                    if (!authoring.TryValidateBindings(out issue))
                    {
                        int matchingBindings = CountExactBindings(
                            authoring,
                            context.Activity.ActivityId,
                            playerSlotId);
                        matches += matchingBindings;
                        if (matchingBindings > 1)
                        {
                            issue =
                                "Exactly one Activity Player Relocation binding is required.\n" +
                                "Remove the duplicate binding(s).";
                        }
                        return false;
                    }

                    IReadOnlyList<ActivityPlayerRelocationAuthoring.Binding> bindings =
                        authoring.Bindings;
                    for (int bindingIndex = 0; bindingIndex < bindings.Count; bindingIndex++)
                    {
                        ActivityPlayerRelocationAuthoring.Binding binding = bindings[bindingIndex];
                        if (binding == null ||
                            !binding.TryGetActivityId(out var bindingActivityId, out issue) ||
                            !binding.TryGetPlayerSlotId(out var bindingSlotId, out issue))
                        {
                            return false;
                        }

                        // A shared Route-primary authoring component may map several Activities.
                        if (bindingActivityId != context.Activity.ActivityId || bindingSlotId != playerSlotId)
                        {
                            continue;
                        }

                        matches++;
                        if (binding.RelocationAnchor == null ||
                            binding.RelocationAnchor.gameObject.scene.handle != scene.handle)
                        {
                            issue = "The matching Activity Player Relocation binding is invalid. " +
                                "Its anchor must belong to the discovered Route or Activity scene.";
                            return false;
                        }

                        anchor = binding.RelocationAnchor;
                    }
                }
            }
            return true;
        }

        private static int CountExactBindings(
            ActivityPlayerRelocationAuthoring authoring,
            ActivityId activityId,
            PlayerSlotId playerSlotId)
        {
            int count = 0;
            IReadOnlyList<ActivityPlayerRelocationAuthoring.Binding> bindings =
                authoring.Bindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                ActivityPlayerRelocationAuthoring.Binding binding = bindings[index];
                if (binding != null &&
                    binding.TryGetActivityId(
                        out ActivityId bindingActivityId,
                        out _) &&
                    binding.TryGetPlayerSlotId(
                        out PlayerSlotId bindingSlotId,
                        out _) &&
                    bindingActivityId == activityId &&
                    bindingSlotId == playerSlotId)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryResolveLoadedScene(string path, string name, out Scene scene, out string issue)
        {
            scene = !string.IsNullOrEmpty(path) ? SceneManager.GetSceneByPath(path) : SceneManager.GetSceneByName(name);
            issue = scene.IsValid() && scene.isLoaded
                ? string.Empty
                : $"Required relocation discovery scene '{path ?? name}' is not loaded.";
            return string.IsNullOrEmpty(issue);
        }

        private static string BuildInvalidConfigurationDiagnostic(
            ActivityTransitionPreparationContext context,
            PlayerSlotId playerSlotId,
            int matchingBindings,
            string detail)
        {
            return
                "[Immersive.Framework][ActivityPlayerRelocation]\n\n" +
                "Activity Player Relocation configuration is invalid.\n\n" +
                $"Activity: '{context.Activity.ActivityName}'\n" +
                $"ActivityId: '{context.Activity.ActivityId.StableText}'\n" +
                $"Slot: '{playerSlotId.StableText}'\n" +
                "Policy: 'ApplyExplicitRelocation'\n" +
                $"Matching bindings: '{matchingBindings}'\n\n" +
                detail +
                "\n\nActivity Player reconciliation cannot continue.";
        }

        private static ActivityPlayerRelocationEvidence Failure(
            ActivityTransitionPreparationContext context, PlayerSlotId slot, ActorId actor,
            string representation, Transform target, Transform anchor, string issue) =>
            new ActivityPlayerRelocationEvidence(
                context.Owner, context.Occurrence, slot, actor, representation, target, anchor,
                target != null ? target.position : default,
                target != null ? target.rotation : default,
                ActivityPlayerRelocationStatus.Failed, issue);
    }
}
