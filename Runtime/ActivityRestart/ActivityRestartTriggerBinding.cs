using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.ActivityRestart
{
    internal static class ActivityRestartTriggerBinding
    {
        internal static ActivityRestartTriggerBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IActivityRestartRuntimePort activityRestartRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (activityRestartRuntime == null)
            {
                return ActivityRestartTriggerBindingResult.Rejected(
                    "RejectedMissingActivityRestartRuntime",
                    $"Activity Restart trigger binding requires an Activity Restart runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<ActivityRestartTrigger> triggers = CollectTriggers(roots);
            if (triggers.Count == 0)
            {
                return ActivityRestartTriggerBindingResult.OptionalAbsent(rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            for (int index = 0; index < triggers.Count; index++)
            {
                ActivityRestartTrigger trigger = triggers[index];
                bool wasBound = trigger.HasActivityRestartRuntimeBinding;
                if (trigger.TryBindActivityRestartRuntime(activityRestartRuntime, out string issue))
                {
                    if (wasBound)
                    {
                        idempotent++;
                    }
                    else
                    {
                        bound++;
                    }

                    continue;
                }

                rejected++;
                string scene = trigger.gameObject.scene.name.NormalizeTextOrFallback("<unknown>");
                issues.Add($"trigger='{trigger.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            return rejected > 0
                ? ActivityRestartTriggerBindingResult.Rejected(
                    "RejectedTriggerBinding",
                    $"Activity Restart trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    triggers.Count,
                    bound,
                    idempotent,
                    rejected)
                : ActivityRestartTriggerBindingResult.Completed(rootCount, triggers.Count, bound, idempotent);
        }

        private static List<ActivityRestartTrigger> CollectTriggers(IReadOnlyList<GameObject> roots)
        {
            var result = new List<ActivityRestartTrigger>();
            var seenRoots = new HashSet<GameObject>();
            var seenTriggers = new HashSet<ActivityRestartTrigger>();
            if (roots == null)
            {
                return result;
            }

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root))
                {
                    continue;
                }

                ActivityRestartTrigger[] candidates = root.GetComponentsInChildren<ActivityRestartTrigger>(true);
                for (int triggerIndex = 0; triggerIndex < candidates.Length; triggerIndex++)
                {
                    if (candidates[triggerIndex] != null && seenTriggers.Add(candidates[triggerIndex]))
                    {
                        result.Add(candidates[triggerIndex]);
                    }
                }
            }

            return result;
        }

        private static int CountUniqueRoots(IReadOnlyList<GameObject> roots)
        {
            var result = new HashSet<GameObject>();
            if (roots == null)
            {
                return 0;
            }

            for (int index = 0; index < roots.Count; index++)
            {
                if (roots[index] != null)
                {
                    result.Add(roots[index]);
                }
            }

            return result.Count;
        }
    }
}
