using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.PlayerParticipation
{
    internal static class ActivityPlayerRelocationRuntime
    {
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

            if (!TryResolveExactAnchor(context, playerSlotId, out Transform anchor, out issue))
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
                    return false;
                }

                if (!visitedScenes.Add(scene.handle.GetRawData()))
                {
                    continue;
                }

                if (!TryInspectScene(scene, context, playerSlotId, ref matches, ref anchor, out issue))
                {
                    return false;
                }
            }

            if (matches != 1 || anchor == null)
            {
                issue = $"Activity explicit relocation requires exactly one binding for Activity '{context.Activity.ActivityId.StableText}' + Slot '{playerSlotId.StableText}'. Found '{matches}'.";
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
                    if (!authorings[authoringIndex].TryValidateBindings(out issue))
                    {
                        return false;
                    }

                    IReadOnlyList<ActivityPlayerRelocationAuthoring.Binding> bindings =
                        authorings[authoringIndex].Bindings;
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
                        if (matches > 1)
                        {
                            issue = $"Activity explicit relocation has duplicate bindings for Activity '{bindingActivityId.StableText}' + Slot '{playerSlotId.StableText}'.";
                            return false;
                        }

                        if (binding.RelocationAnchor == null ||
                            binding.RelocationAnchor.gameObject.scene.handle != scene.handle)
                        {
                            issue = $"Activity explicit relocation binding for Activity '{bindingActivityId.StableText}' + Slot '{playerSlotId.StableText}' requires an anchor in its discovered Route/Activity scene.";
                            return false;
                        }

                        anchor = binding.RelocationAnchor;
                    }
                }
            }
            return true;
        }

        private static bool TryResolveLoadedScene(string path, string name, out Scene scene, out string issue)
        {
            scene = !string.IsNullOrEmpty(path) ? SceneManager.GetSceneByPath(path) : SceneManager.GetSceneByName(name);
            issue = scene.IsValid() && scene.isLoaded
                ? string.Empty
                : $"Required relocation discovery scene '{path ?? name}' is not loaded.";
            return string.IsNullOrEmpty(issue);
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
