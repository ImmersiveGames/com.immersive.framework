using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    internal static class SessionCameraAssignmentCommandTriggerBinder
    {
        internal static SessionCameraAssignmentCommandTriggerBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            ISessionCameraAssignmentCommandPort runtime)
        {
            int rootCount = CountRoots(roots);
            if (runtime == null)
            {
                return SessionCameraAssignmentCommandTriggerBindingResult.Rejected(
                    "RejectedMissingRuntime",
                    $"Session Camera Assignment command binding requires a runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount, 0, 0, 0, 0);
            }

            List<SessionCameraAssignmentCommandTrigger> triggers = Collect(roots);
            if (triggers.Count == 0)
            {
                return SessionCameraAssignmentCommandTriggerBindingResult.OptionalAbsent(rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            for (int index = 0; index < triggers.Count; index++)
            {
                SessionCameraAssignmentCommandTrigger trigger = triggers[index];
                bool wasBound = trigger.HasRuntimeBinding;
                if (trigger.TryBind(runtime, out string issue))
                {
                    if (wasBound) idempotent++; else bound++;
                    continue;
                }

                rejected++;
                issues.Add($"trigger='{trigger.name}' issue='{issue}'.");
            }

            return rejected == 0
                ? SessionCameraAssignmentCommandTriggerBindingResult.Completed(
                    rootCount, triggers.Count, bound, idempotent)
                : SessionCameraAssignmentCommandTriggerBindingResult.Rejected(
                    "RejectedTriggerBinding",
                    $"Session Camera Assignment command binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount, triggers.Count, bound, idempotent, rejected);
        }

        private static List<SessionCameraAssignmentCommandTrigger> Collect(IReadOnlyList<GameObject> roots)
        {
            var result = new List<SessionCameraAssignmentCommandTrigger>();
            var seen = new HashSet<SessionCameraAssignmentCommandTrigger>();
            if (roots == null) return result;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null) continue;
                SessionCameraAssignmentCommandTrigger[] candidates =
                    root.GetComponentsInChildren<SessionCameraAssignmentCommandTrigger>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (candidates[index] != null && seen.Add(candidates[index]))
                    {
                        result.Add(candidates[index]);
                    }
                }
            }
            return result;
        }

        private static int CountRoots(IReadOnlyList<GameObject> roots)
        {
            if (roots == null) return 0;
            int count = 0;
            for (int index = 0; index < roots.Count; index++)
            {
                if (roots[index] != null) count++;
            }
            return count;
        }
    }
}
