using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    internal static class RouteRequestTriggerBinder
    {
        internal static RouteRequestTriggerBinderResult TryBind(IReadOnlyList<GameObject> roots, IRouteRuntimePort routeRuntime)
        {
            int rootCount = CountRoots(roots);
            if (routeRuntime == null)
            {
                return RouteRequestTriggerBinderResult.Rejected("RejectedMissingRouteRuntime", $"Route request trigger binding requires a Route runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.", rootCount, 0, 0, 0, 0);
            }

            List<RouteRequestTrigger> triggers = CollectTriggers(roots);
            if (triggers.Count == 0)
            {
                return RouteRequestTriggerBinderResult.OptionalAbsent(rootCount);
            }

            int boundCount = 0;
            int idempotentCount = 0;
            int rejectedCount = 0;
            var issues = new List<string>();
            var newlyBound = new List<RouteRequestTrigger>();
            for (int index = 0; index < triggers.Count; index++)
            {
                RouteRequestTrigger trigger = triggers[index];
                bool wasBound = trigger.HasRouteRuntimeBinding;
                if (trigger.TryBindRouteRuntime(routeRuntime, out string issue))
                {
                    if (wasBound) idempotentCount++; else boundCount++;
                    if (!wasBound) newlyBound.Add(trigger);
                    continue;
                }

                rejectedCount++;
                string sceneName = trigger.gameObject.scene.name.NormalizeTextOrFallback("<unknown>");
                issues.Add($"trigger='{trigger.name}' scene='{sceneName}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            if (rejectedCount > 0)
            {
                var rollbackIssues = new List<string>();
                for (int index = newlyBound.Count - 1; index >= 0; index--)
                    if (!newlyBound[index].TryReleaseRouteRuntime(routeRuntime, out string rollbackIssue))
                        rollbackIssues.Add($"trigger='{newlyBound[index].name}' rollback='{rollbackIssue}'.");
                return RouteRequestTriggerBinderResult.Rejected("RejectedTriggerBinding", $"Route request trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{boundCount}' idempotent='{idempotentCount}' rejected='{rejectedCount}' rollback='{(rollbackIssues.Count == 0 ? "Succeeded" : "Failed")}'. {string.Join(" ", issues)} {string.Join(" ", rollbackIssues)}", rootCount, triggers.Count, boundCount, idempotentCount, rejectedCount);
            }

            return RouteRequestTriggerBinderResult.Completed(rootCount, triggers.Count, boundCount, idempotentCount);
        }

        internal static bool TryRelease(
            IReadOnlyList<GameObject> roots,
            IRouteRuntimePort routeRuntime,
            out int triggerCount,
            out string diagnostic)
        {
            List<RouteRequestTrigger> triggers = CollectTriggers(roots);
            triggerCount = triggers.Count;
            if (routeRuntime == null)
            {
                diagnostic = "Route request trigger release requires the exact Route runtime port.";
                return false;
            }

            var issues = new List<string>();
            for (int index = triggers.Count - 1; index >= 0; index--)
                if (!triggers[index].TryReleaseRouteRuntime(routeRuntime, out string issue))
                    issues.Add($"trigger='{triggers[index].name}' issue='{issue}'.");

            diagnostic = issues.Count == 0
                ? $"Route request trigger release completed. triggers='{triggerCount}'."
                : $"Route request trigger release failed. triggers='{triggerCount}' rejected='{issues.Count}'. {string.Join(" ", issues)}";
            return issues.Count == 0;
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
