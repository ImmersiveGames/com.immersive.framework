using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    internal static class ActivityRequestTriggerBinder
    {
        internal static ActivityRequestTriggerBinderResult TryBind(
            IReadOnlyList<GameObject> roots,
            IActivityRuntimePort activityRuntime)
        {
            int rootCount = CountRoots(roots);
            if (activityRuntime == null)
            {
                return ActivityRequestTriggerBinderResult.Rejected(
                    "RejectedMissingActivityRuntime",
                    $"Activity request trigger binder requires an Activity runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<ActivityRequestTrigger> triggers = CollectTriggers(roots);
            if (triggers.Count == 0)
            {
                return ActivityRequestTriggerBinderResult.OptionalAbsent(rootCount);
            }

            int boundCount = 0;
            int idempotentCount = 0;
            int rejectedCount = 0;
            var issues = new List<string>();
            var newlyBound = new List<ActivityRequestTrigger>();
            for (int index = 0; index < triggers.Count; index++)
            {
                ActivityRequestTrigger trigger = triggers[index];
                bool wasBound = trigger.HasActivityRuntimeBinding;
                if (trigger.TryBindActivityRuntime(activityRuntime, out string issue))
                {
                    if (wasBound)
                    {
                        idempotentCount++;
                    }
                    else
                    {
                        boundCount++;
                        newlyBound.Add(trigger);
                    }

                    continue;
                }

                rejectedCount++;
                string sceneName = trigger.gameObject.scene.name
                    .NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"trigger='{trigger.name}' scene='{sceneName}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            if (rejectedCount > 0)
            {
                var rollbackIssues = new List<string>();
                for (int index = newlyBound.Count - 1; index >= 0; index--)
                    if (!newlyBound[index].TryReleaseActivityRuntime(activityRuntime, out string rollbackIssue))
                        rollbackIssues.Add($"trigger='{newlyBound[index].name}' rollback='{rollbackIssue}'.");
                return ActivityRequestTriggerBinderResult.Rejected(
                    "RejectedTriggerBinding",
                    $"Activity request trigger binder failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{boundCount}' idempotent='{idempotentCount}' rejected='{rejectedCount}' rollback='{(rollbackIssues.Count == 0 ? "Succeeded" : "Failed")}'. {string.Join(" ", issues)} {string.Join(" ", rollbackIssues)}",
                    rootCount,
                    triggers.Count,
                    boundCount,
                    idempotentCount,
                    rejectedCount);
            }

            return ActivityRequestTriggerBinderResult.Completed(
                rootCount,
                triggers.Count,
                boundCount,
                idempotentCount);
        }

        internal static bool TryRelease(
            IReadOnlyList<GameObject> roots,
            IActivityRuntimePort activityRuntime,
            out int triggerCount,
            out string diagnostic)
        {
            List<ActivityRequestTrigger> triggers = CollectTriggers(roots);
            triggerCount = triggers.Count;
            if (activityRuntime == null)
            {
                diagnostic = "Activity request trigger release requires the exact Activity runtime port.";
                return false;
            }

            var issues = new List<string>();
            for (int index = triggers.Count - 1; index >= 0; index--)
                if (!triggers[index].TryReleaseActivityRuntime(activityRuntime, out string issue))
                    issues.Add($"trigger='{triggers[index].name}' issue='{issue}'.");

            diagnostic = issues.Count == 0
                ? $"Activity request trigger release completed. triggers='{triggerCount}'."
                : $"Activity request trigger release failed. triggers='{triggerCount}' rejected='{issues.Count}'. {string.Join(" ", issues)}";
            return issues.Count == 0;
        }

        private static List<ActivityRequestTrigger> CollectTriggers(
            IReadOnlyList<GameObject> roots)
        {
            var triggers = new List<ActivityRequestTrigger>();
            var seen = new HashSet<ActivityRequestTrigger>();
            if (roots == null)
            {
                return triggers;
            }

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                ActivityRequestTrigger[] candidates =
                    root.GetComponentsInChildren<ActivityRequestTrigger>(true);
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    ActivityRequestTrigger candidate = candidates[candidateIndex];
                    if (candidate != null && seen.Add(candidate))
                    {
                        triggers.Add(candidate);
                    }
                }
            }

            return triggers;
        }

        private static int CountRoots(IReadOnlyList<GameObject> roots)
        {
            if (roots == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < roots.Count; index++)
            {
                if (roots[index] != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
