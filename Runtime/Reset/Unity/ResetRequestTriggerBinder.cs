using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    internal static class ResetRequestTriggerBinder
    {
        internal static bool TryBind(
            IReadOnlyList<GameObject> roots,
            IResetTargetExecutionRuntimePort runtime,
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
            var newlyBound = new List<ResetRequestTrigger>();
            for (int index = 0; index < triggers.Count; index++)
            {
                bool wasBound = triggers[index].HasRuntimeBinding;
                if (!triggers[index].TryBind(runtime, out string issue))
                    issues.Add($"trigger='{triggers[index].name}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
                else if (!wasBound)
                    newlyBound.Add(triggers[index]);
            }

            if (issues.Count > 0)
            {
                var rollbackIssues = new List<string>();
                for (int index = newlyBound.Count - 1; index >= 0; index--)
                {
                    if (!newlyBound[index].TryUnbind(runtime, out string issue))
                        rollbackIssues.Add($"trigger='{newlyBound[index].name}' rollback='{issue}'.");
                }
                diagnostic = $"Reset request trigger binding rejected. triggers='{triggerCount}' rejected='{issues.Count}' rollback='{(rollbackIssues.Count == 0 ? "Succeeded" : "Failed")}'. {string.Join(" ", issues)} {string.Join(" ", rollbackIssues)}";
                return false;
            }

            diagnostic = $"Reset request trigger binding completed. triggers='{triggerCount}' newlyBound='{newlyBound.Count}'.";
            return issues.Count == 0;
        }

        internal static bool TryRelease(
            IReadOnlyList<GameObject> roots,
            IResetTargetExecutionRuntimePort runtime,
            out int triggerCount,
            out string diagnostic)
        {
            List<ResetRequestTrigger> triggers = Collect(roots);
            triggerCount = triggers.Count;
            if (runtime == null)
            {
                diagnostic = "Reset request trigger release requires the exact Session runtime port.";
                return false;
            }

            var issues = new List<string>();
            for (int index = triggers.Count - 1; index >= 0; index--)
            {
                if (!triggers[index].TryUnbind(runtime, out string issue))
                {
                    issues.Add($"trigger='{triggers[index].name}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
                }
            }

            diagnostic = issues.Count == 0
                ? $"Reset request trigger release completed. triggers='{triggerCount}'."
                : $"Reset request trigger release rejected. triggers='{triggerCount}' rejected='{issues.Count}'. {string.Join(" ", issues)}";
            return issues.Count == 0;
        }

        private static List<ResetRequestTrigger> Collect(IReadOnlyList<GameObject> roots)
        {
            var seenRoots = new HashSet<GameObject>();
            var seenTriggers = new HashSet<ResetRequestTrigger>();
            var triggers = new List<ResetRequestTrigger>();
            if (roots == null) return triggers;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root)) continue;
                ResetRequestTrigger[] found = root.GetComponentsInChildren<ResetRequestTrigger>(true);
                for (int index = 0; index < found.Length; index++)
                    if (found[index] != null && seenTriggers.Add(found[index])) triggers.Add(found[index]);
            }
            return triggers;
        }
    }
}
