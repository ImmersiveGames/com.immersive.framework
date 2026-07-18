using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    internal static class RouteRequestTriggerBinding
    {
        internal static RouteRequestTriggerBindingResult TryBind(IReadOnlyList<GameObject> roots, IRouteRuntimePort routeRuntime)
        {
            int rootCount = CountRoots(roots);
            if (routeRuntime == null)
            {
                return RouteRequestTriggerBindingResult.Rejected("RejectedMissingRouteRuntime", $"Route request trigger binding requires a Route runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.", rootCount, 0, 0, 0, 0);
            }

            List<RouteRequestTrigger> triggers = CollectTriggers(roots);
            if (triggers.Count == 0)
            {
                return RouteRequestTriggerBindingResult.OptionalAbsent(rootCount);
            }

            int boundCount = 0;
            int idempotentCount = 0;
            int rejectedCount = 0;
            var issues = new List<string>();
            for (int index = 0; index < triggers.Count; index++)
            {
                RouteRequestTrigger trigger = triggers[index];
                bool wasBound = trigger.HasRouteRuntimeBinding;
                if (trigger.TryBindRouteRuntime(routeRuntime, out string issue))
                {
                    if (wasBound) idempotentCount++; else boundCount++;
                    continue;
                }

                rejectedCount++;
                string sceneName = trigger.gameObject.scene.name.NormalizeTextOrFallback("<unknown>");
                issues.Add($"trigger='{trigger.name}' scene='{sceneName}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            if (rejectedCount > 0)
            {
                return RouteRequestTriggerBindingResult.Rejected("RejectedTriggerBinding", $"Route request trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{boundCount}' idempotent='{idempotentCount}' rejected='{rejectedCount}'. {string.Join(" ", issues)}", rootCount, triggers.Count, boundCount, idempotentCount, rejectedCount);
            }

            return RouteRequestTriggerBindingResult.Completed(rootCount, triggers.Count, boundCount, idempotentCount);
        }

        private static List<RouteRequestTrigger> CollectTriggers(IReadOnlyList<GameObject> roots)
        {
            var triggers = new List<RouteRequestTrigger>();
            var seen = new HashSet<RouteRequestTrigger>();
            if (roots == null) return triggers;

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null) continue;

                RouteRequestTrigger[] candidates = root.GetComponentsInChildren<RouteRequestTrigger>(true);
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    RouteRequestTrigger candidate = candidates[candidateIndex];
                    if (candidate != null && seen.Add(candidate)) triggers.Add(candidate);
                }
            }

            return triggers;
        }

        private static int CountRoots(IReadOnlyList<GameObject> roots)
        {
            if (roots == null) return 0;
            int count = 0;
            for (int index = 0; index < roots.Count; index++) if (roots[index] != null) count++;
            return count;
        }
    }
}
