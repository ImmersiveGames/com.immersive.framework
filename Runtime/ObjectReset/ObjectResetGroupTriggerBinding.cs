using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.Reset;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    internal static class ObjectResetGroupTriggerBinding
    {
        internal static ObjectResetGroupTriggerBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IResetSelectionExecutionRuntimePort resetSelectionExecutionRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (resetSelectionExecutionRuntime == null)
            {
                return ObjectResetGroupTriggerBindingResult.Rejected(
                    "RejectedMissingResetSelectionExecutionRuntime",
                    $"Object Reset Group trigger binding requires a Reset selection execution runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<ObjectResetGroupTrigger> triggers = CollectTriggers(roots);
            if (triggers.Count == 0)
            {
                return ObjectResetGroupTriggerBindingResult.OptionalAbsent(rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            for (int index = 0; index < triggers.Count; index++)
            {
                ObjectResetGroupTrigger trigger = triggers[index];
                bool wasBound = trigger.HasResetSelectionExecutionRuntimeBinding;
                if (trigger.TryBindResetSelectionExecutionRuntime(resetSelectionExecutionRuntime, out string issue))
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
                ? ObjectResetGroupTriggerBindingResult.Rejected(
                    "RejectedTriggerBinding",
                    $"Object Reset Group trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    triggers.Count,
                    bound,
                    idempotent,
                    rejected)
                : ObjectResetGroupTriggerBindingResult.Completed(
                    rootCount,
                    triggers.Count,
                    bound,
                    idempotent);
        }

        private static List<ObjectResetGroupTrigger> CollectTriggers(IReadOnlyList<GameObject> roots)
        {
            var result = new List<ObjectResetGroupTrigger>();
            var seenRoots = new HashSet<GameObject>();
            var seenTriggers = new HashSet<ObjectResetGroupTrigger>();
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

                ObjectResetGroupTrigger[] candidates = root.GetComponentsInChildren<ObjectResetGroupTrigger>(true);
                for (int triggerIndex = 0; triggerIndex < candidates.Length; triggerIndex++)
                {
                    ObjectResetGroupTrigger candidate = candidates[triggerIndex];
                    if (candidate != null && seenTriggers.Add(candidate))
                    {
                        result.Add(candidate);
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
