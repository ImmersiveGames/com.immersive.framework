using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.CycleReset
{
    internal static class RouteCycleResetTriggerBinding
    {
        internal static RouteCycleResetTriggerBindingResult TryBind(IReadOnlyList<GameObject> roots, IRouteCycleResetRuntimePort routeCycleResetRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (routeCycleResetRuntime == null)
            {
                return RouteCycleResetTriggerBindingResult.Rejected("RejectedMissingRouteCycleResetRuntime", $"Route Cycle Reset trigger binding requires a Route Cycle Reset runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.", rootCount, 0, 0, 0, 0);
            }

            List<RouteCycleResetTrigger> triggers = CollectTriggers(roots);
            if (triggers.Count == 0)
            {
                return RouteCycleResetTriggerBindingResult.OptionalAbsent(rootCount);
            }

            int boundCount = 0;
            int idempotentCount = 0;
            int rejectedCount = 0;
            var issues = new List<string>();
            for (int index = 0; index < triggers.Count; index++)
            {
                RouteCycleResetTrigger trigger = triggers[index];
                bool wasBound = trigger.HasRouteCycleResetRuntimeBinding;
                if (trigger.TryBindRouteCycleResetRuntime(routeCycleResetRuntime, out string issue))
                {
                    if (wasBound) idempotentCount++; else boundCount++;
                    continue;
                }

                rejectedCount++;
                string sceneName = trigger.gameObject.scene.name.NormalizeTextOrFallback("<unknown>");
                issues.Add($"trigger='{trigger.name}' scene='{sceneName}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            return rejectedCount > 0
                ? RouteCycleResetTriggerBindingResult.Rejected("RejectedTriggerBinding", $"Route Cycle Reset trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{boundCount}' idempotent='{idempotentCount}' rejected='{rejectedCount}'. {string.Join(" ", issues)}", rootCount, triggers.Count, boundCount, idempotentCount, rejectedCount)
                : RouteCycleResetTriggerBindingResult.Completed(rootCount, triggers.Count, boundCount, idempotentCount);
        }

        private static List<RouteCycleResetTrigger> CollectTriggers(IReadOnlyList<GameObject> roots)
        {
            var triggers = new List<RouteCycleResetTrigger>();
            var seenRoots = new HashSet<GameObject>();
            var seenTriggers = new HashSet<RouteCycleResetTrigger>();
            if (roots == null) return triggers;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root)) continue;
                RouteCycleResetTrigger[] candidates = root.GetComponentsInChildren<RouteCycleResetTrigger>(true);
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    RouteCycleResetTrigger candidate = candidates[candidateIndex];
                    if (candidate != null && seenTriggers.Add(candidate)) triggers.Add(candidate);
                }
            }
            return triggers;
        }

        private static int CountUniqueRoots(IReadOnlyList<GameObject> roots)
        {
            var uniqueRoots = new HashSet<GameObject>();
            if (roots == null) return 0;
            for (int index = 0; index < roots.Count; index++)
            {
                if (roots[index] != null) uniqueRoots.Add(roots[index]);
            }
            return uniqueRoots.Count;
        }
    }
}
