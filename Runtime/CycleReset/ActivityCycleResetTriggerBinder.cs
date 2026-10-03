using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.CycleReset
{
    internal static class ActivityCycleResetTriggerBinder
    {
        internal static ActivityCycleResetTriggerBinderResult TryBind(
            IReadOnlyList<GameObject> roots,
            IActivityCycleResetRuntimePort runtime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (runtime == null)
                return ActivityCycleResetTriggerBinderResult.Rejected(
                    "RejectedMissingActivityCycleResetRuntime",
                    "Activity Cycle Reset trigger binding requires a non-null runtime port.",
                    rootCount, 0, 0, 0, 0);

            List<ActivityCycleResetTrigger> triggers = CollectTriggers(roots);
            int bound = 0;
            int idempotent = 0;
            var newlyBound = new List<ActivityCycleResetTrigger>();
            var issues = new List<string>();
            for (int index = 0; index < triggers.Count; index++)
            {
                ActivityCycleResetTrigger trigger = triggers[index];
                bool wasBound = trigger.HasActivityCycleResetRuntimeBinding;
                if (trigger.TryBindActivityCycleResetRuntime(runtime, out string issue))
                {
                    if (wasBound) idempotent++;
                    else { bound++; newlyBound.Add(trigger); }
                    continue;
                }
                issues.Add($"trigger='{trigger.name}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            if (issues.Count == 0)
                return ActivityCycleResetTriggerBinderResult.Completed(rootCount, triggers.Count, bound, idempotent);

            var rollbackIssues = new List<string>();
            for (int index = newlyBound.Count - 1; index >= 0; index--)
                if (!newlyBound[index].TryReleaseActivityCycleResetRuntime(runtime, out string issue))
                    rollbackIssues.Add($"trigger='{newlyBound[index].name}' rollback='{issue}'.");
            return ActivityCycleResetTriggerBinderResult.Rejected(
                "RejectedTriggerBinding",
                $"Activity Cycle Reset trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{issues.Count}' rollback='{(rollbackIssues.Count == 0 ? "Succeeded" : "Failed")}'. {string.Join(" ", issues)} {string.Join(" ", rollbackIssues)}",
                rootCount, triggers.Count, bound, idempotent, issues.Count);
        }

        internal static bool TryRelease(
            IReadOnlyList<GameObject> roots,
            IActivityCycleResetRuntimePort runtime,
            out int triggerCount,
            out string diagnostic)
        {
            List<ActivityCycleResetTrigger> triggers = CollectTriggers(roots);
            triggerCount = triggers.Count;
            if (runtime == null)
            {
                diagnostic = "Activity Cycle Reset trigger release requires the exact runtime port.";
                return false;
            }
            var issues = new List<string>();
            for (int index = triggers.Count - 1; index >= 0; index--)
                if (!triggers[index].TryReleaseActivityCycleResetRuntime(runtime, out string issue))
                    issues.Add($"trigger='{triggers[index].name}' issue='{issue}'.");
            diagnostic = issues.Count == 0
                ? $"Activity Cycle Reset trigger release completed. triggers='{triggerCount}'."
                : $"Activity Cycle Reset trigger release failed. rejected='{issues.Count}'. {string.Join(" ", issues)}";
            return issues.Count == 0;
        }

        private static List<ActivityCycleResetTrigger> CollectTriggers(IReadOnlyList<GameObject> roots)
        {
            var result = new List<ActivityCycleResetTrigger>();
            var seenRoots = new HashSet<GameObject>();
            var seenTriggers = new HashSet<ActivityCycleResetTrigger>();
            if (roots == null) return result;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root)) continue;
                ActivityCycleResetTrigger[] candidates = root.GetComponentsInChildren<ActivityCycleResetTrigger>(true);
                for (int index = 0; index < candidates.Length; index++)
                    if (candidates[index] != null && seenTriggers.Add(candidates[index])) result.Add(candidates[index]);
            }
            return result;
        }

        private static int CountUniqueRoots(IReadOnlyList<GameObject> roots)
        {
            var result = new HashSet<GameObject>();
            if (roots != null)
                for (int index = 0; index < roots.Count; index++)
                    if (roots[index] != null) result.Add(roots[index]);
            return result.Count;
        }
    }
}
