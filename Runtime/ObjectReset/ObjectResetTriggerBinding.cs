using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.Reset;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    internal static class ObjectResetTriggerBinding
    {
        internal static ObjectResetTriggerBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IResetExecutionRuntimePort resetExecutionRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (resetExecutionRuntime == null)
            {
                return ObjectResetTriggerBindingResult.Rejected(
                    "RejectedMissingResetExecutionRuntime",
                    $"Object Reset trigger binding requires a Reset execution runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<ObjectResetTrigger> triggers = CollectTriggers(roots);
            if (triggers.Count == 0)
            {
                return ObjectResetTriggerBindingResult.OptionalAbsent(rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            for (int index = 0; index < triggers.Count; index++)
            {
                ObjectResetTrigger trigger = triggers[index];
                bool wasBound = trigger.HasResetExecutionRuntimeBinding;
                if (trigger.TryBindResetExecutionRuntime(resetExecutionRuntime, out string issue))
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
                ? ObjectResetTriggerBindingResult.Rejected(
                    "RejectedTriggerBinding",
                    $"Object Reset trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    triggers.Count,
                    bound,
                    idempotent,
                    rejected)
                : ObjectResetTriggerBindingResult.Completed(
                    rootCount,
                    triggers.Count,
                    bound,
                    idempotent);
        }

        private static List<ObjectResetTrigger> CollectTriggers(IReadOnlyList<GameObject> roots)
        {
            var result = new List<ObjectResetTrigger>();
            var seenRoots = new HashSet<GameObject>();
            var seenTriggers = new HashSet<ObjectResetTrigger>();
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

                ObjectResetTrigger[] candidates = root.GetComponentsInChildren<ObjectResetTrigger>(true);
                for (int triggerIndex = 0; triggerIndex < candidates.Length; triggerIndex++)
                {
                    ObjectResetTrigger candidate = candidates[triggerIndex];
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
