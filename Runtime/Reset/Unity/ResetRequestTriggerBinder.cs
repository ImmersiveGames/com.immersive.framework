using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    internal static class ResetRequestTriggerBinder
    {
        internal static bool TryBind(
            IReadOnlyList<GameObject> roots,
            IResetSelectionExecutionRuntimePort runtime,
            out int triggerCount,
            out string diagnostic)
        {
            triggerCount = 0;
            if (runtime == null)
            {
                diagnostic = "Reset request trigger binding requires a runtime port.";
                return false;
            }

            var seenRoots = new HashSet<GameObject>();
            var seenTriggers = new HashSet<ResetRequestTrigger>();
            var triggers = new List<ResetRequestTrigger>();
            if (roots != null)
            {
                for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
                {
                    GameObject root = roots[rootIndex];
                    if (root == null || !seenRoots.Add(root)) continue;
                    ResetRequestTrigger[] found = root.GetComponentsInChildren<ResetRequestTrigger>(true);
                    for (int index = 0; index < found.Length; index++)
                    {
                        if (found[index] != null && seenTriggers.Add(found[index])) triggers.Add(found[index]);
                    }
                }
            }

            triggerCount = triggers.Count;
            var issues = new List<string>();
            for (int index = 0; index < triggers.Count; index++)
            {
                if (!triggers[index].TryBind(runtime, out string issue))
                    issues.Add($"trigger='{triggers[index].name}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            diagnostic = issues.Count == 0
                ? $"Reset request trigger binding completed. triggers='{triggerCount}'."
                : $"Reset request trigger binding rejected. triggers='{triggerCount}' rejected='{issues.Count}'. {string.Join(" ", issues)}";
            return issues.Count == 0;
        }
    }
}
