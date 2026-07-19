using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    internal static class ActivityRequestTriggerBinding
    {
        internal static ActivityRequestTriggerBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IActivityRuntimePort activityRuntime)
        {
            int rootCount = CountRoots(roots);
            if (activityRuntime == null)
            {
                return ActivityRequestTriggerBindingResult.Rejected(
                    "RejectedMissingActivityRuntime",
                    $"Activity request trigger binding requires an Activity runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<ActivityRequestTrigger> triggers = CollectTriggers(roots);
            if (triggers.Count == 0)
            {
                return ActivityRequestTriggerBindingResult.OptionalAbsent(rootCount);
            }

            int boundCount = 0;
            int idempotentCount = 0;
            int rejectedCount = 0;
            var issues = new List<string>();
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
                return ActivityRequestTriggerBindingResult.Rejected(
                    "RejectedTriggerBinding",
                    $"Activity request trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{boundCount}' idempotent='{idempotentCount}' rejected='{rejectedCount}'. {string.Join(" ", issues)}",
                    rootCount,
                    triggers.Count,
                    boundCount,
                    idempotentCount,
                    rejectedCount);
            }

            return ActivityRequestTriggerBindingResult.Completed(
                rootCount,
                triggers.Count,
                boundCount,
                idempotentCount);
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
