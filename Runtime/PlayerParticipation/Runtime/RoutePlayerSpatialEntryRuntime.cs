using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.PlayerParticipation
{
    internal static class RoutePlayerSpatialEntryRuntime
    {
        internal static bool TryApply(
            RoutePlayerSpatialEntryContext context,
            PlayerSlotId playerSlotId,
            Transform target,
            out string issue)
        {
            issue = string.Empty;
            if (!context.IsValid || !playerSlotId.IsValid || target == null)
            {
                issue = "Route Player spatial entry requires a valid Route occurrence, Slot and target Transform.";
                return false;
            }

            if (context.Policy == RoutePlayerSpatialEntryPolicy.PreserveCurrentPose)
            {
                return true;
            }

            if (context.Policy != RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement)
            {
                issue = $"Route Player spatial entry has invalid policy '{context.Policy}'.";
                return false;
            }

            if (!TryResolveExactAnchor(context, playerSlotId, out Transform anchor, out issue))
            {
                return false;
            }

            Transform originalParent = target.parent;
            Vector3 originalScale = target.localScale;
            target.SetPositionAndRotation(anchor.position, anchor.rotation);
            if (!ReferenceEquals(target.parent, originalParent) || target.localScale != originalScale)
            {
                issue =
                    $"Route Player spatial entry changed hierarchy or scale for Slot '{playerSlotId.StableText}', which is outside Route authority.";
                return false;
            }

            return true;
        }

        private static bool TryResolveExactAnchor(
            RoutePlayerSpatialEntryContext context,
            PlayerSlotId playerSlotId,
            out Transform anchor,
            out string issue)
        {
            anchor = null;
            issue = string.Empty;
            int bindingCount = 0;
            IReadOnlyList<RouteContentDiscoveryScene> scenes =
                context.DiscoveryScope.RouteOwnedScenes;

            for (int sceneIndex = 0; sceneIndex < scenes.Count; sceneIndex++)
            {
                RouteContentDiscoveryScene discoveryScene = scenes[sceneIndex];
                if (!TryResolveLoadedScene(discoveryScene, out Scene scene))
                {
                    issue =
                        $"Route-owned scene '{discoveryScene.ScenePath}' is not loaded while resolving Player spatial entry.";
                    return false;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    RoutePlayerSpatialEntryAuthoring[] authorings = roots[rootIndex]
                        .GetComponentsInChildren<RoutePlayerSpatialEntryAuthoring>(true);
                    for (int authoringIndex = 0; authoringIndex < authorings.Length; authoringIndex++)
                    {
                        IReadOnlyList<RoutePlayerSpatialEntryAuthoring.Binding> bindings =
                            authorings[authoringIndex].Bindings;
                        for (int bindingIndex = 0; bindingIndex < bindings.Count; bindingIndex++)
                        {
                            RoutePlayerSpatialEntryAuthoring.Binding binding = bindings[bindingIndex];
                            if (binding == null ||
                                !binding.TryGetPlayerSlotId(out PlayerSlotId bindingSlot, out _) ||
                                bindingSlot != playerSlotId)
                            {
                                continue;
                            }

                            bindingCount++;
                            if (bindingCount > 1)
                            {
                                issue =
                                    $"Route Player spatial entry has duplicate bindings for Slot '{playerSlotId.StableText}'.";
                                return false;
                            }

                            if (binding.PlacementAnchor == null)
                            {
                                issue =
                                    $"Route Player spatial entry binding for Slot '{playerSlotId.StableText}' has no Transform anchor.";
                                return false;
                            }

                            if (binding.PlacementAnchor.gameObject.scene.handle != scene.handle)
                            {
                                issue =
                                    $"Route Player spatial entry anchor for Slot '{playerSlotId.StableText}' is outside its Route-owned scene.";
                                return false;
                            }

                            anchor = binding.PlacementAnchor;
                        }
                    }
                }
            }

            if (bindingCount != 1 || anchor == null)
            {
                issue =
                    $"Route Player spatial entry requires exactly one binding for Slot '{playerSlotId.StableText}'. Found '{bindingCount}'.";
                return false;
            }

            return true;
        }

        private static bool TryResolveLoadedScene(
            RouteContentDiscoveryScene discoveryScene,
            out Scene scene)
        {
            scene = !string.IsNullOrEmpty(discoveryScene.ScenePath)
                ? SceneManager.GetSceneByPath(discoveryScene.ScenePath)
                : SceneManager.GetSceneByName(discoveryScene.SceneName);
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
