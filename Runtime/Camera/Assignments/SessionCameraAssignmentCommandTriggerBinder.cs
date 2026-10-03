using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Camera-specific scene composition binder for explicit Session Camera command
    /// consumers. Discovery is constrained to roots supplied by SceneLifecycle.
    /// </summary>
    internal static class SessionCameraAssignmentCommandConsumerBinder
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
                    $"Session Camera command binding requires a Session command port. roots='{rootCount}' consumers='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount, 0, 0, 0, 0);
            }

            List<ISessionCameraAssignmentCommandConsumer> consumers = Collect(roots);
            if (consumers.Count == 0)
            {
                return SessionCameraAssignmentCommandTriggerBindingResult.OptionalAbsent(rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            var newlyBound = new List<ISessionCameraAssignmentCommandConsumer>();
            for (int index = 0; index < consumers.Count; index++)
            {
                ISessionCameraAssignmentCommandConsumer consumer = consumers[index];
                bool wasBound = consumer.IsBoundToSessionCameraAssignmentCommands(runtime);
                if (consumer.TryBindSessionCameraAssignmentCommands(runtime, out string issue))
                {
                    if (wasBound) idempotent++; else { bound++; newlyBound.Add(consumer); }
                    continue;
                }

                rejected++;
                issues.Add($"consumer='{consumer.GetType().Name}' issue='{issue}'.");
            }

            if (rejected == 0)
            {
                return SessionCameraAssignmentCommandTriggerBindingResult.Completed(
                    rootCount, consumers.Count, bound, idempotent);
            }

            var rollbackIssues = new List<string>();
            for (int index = newlyBound.Count - 1; index >= 0; index--)
            {
                ISessionCameraAssignmentCommandConsumer consumer = newlyBound[index];
                if (!consumer.TryReleaseSessionCameraAssignmentCommands(runtime, out string rollbackIssue))
                    rollbackIssues.Add($"consumer='{consumer.GetType().Name}' rollback='{rollbackIssue}'.");
            }

            return SessionCameraAssignmentCommandTriggerBindingResult.Rejected(
                "RejectedConsumerBinding",
                $"Session Camera command binding failed. roots='{rootCount}' consumers='{consumers.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}' rollback='{(rollbackIssues.Count == 0 ? "Succeeded" : "Failed")}'. {string.Join(" ", issues)} {string.Join(" ", rollbackIssues)}",
                rootCount, consumers.Count, bound, idempotent, rejected);
        }

        internal static SessionCameraAssignmentCommandTriggerBindingResult TryRelease(
            IReadOnlyList<GameObject> roots,
            ISessionCameraAssignmentCommandPort runtime)
        {
            int rootCount = CountRoots(roots);
            if (runtime == null)
            {
                return SessionCameraAssignmentCommandTriggerBindingResult.Rejected(
                    "RejectedMissingRuntime",
                    "Session Camera command release requires the exact Session command port.",
                    rootCount, 0, 0, 0, 0);
            }

            List<ISessionCameraAssignmentCommandConsumer> consumers = Collect(roots);
            int released = 0;
            var issues = new List<string>();
            for (int index = consumers.Count - 1; index >= 0; index--)
            {
                ISessionCameraAssignmentCommandConsumer consumer = consumers[index];
                if (consumer.TryReleaseSessionCameraAssignmentCommands(runtime, out string issue)) released++;
                else issues.Add($"consumer='{consumer.GetType().Name}' issue='{issue}'.");
            }

            return issues.Count == 0
                ? SessionCameraAssignmentCommandTriggerBindingResult.Released(rootCount, consumers.Count, released)
                : SessionCameraAssignmentCommandTriggerBindingResult.Rejected(
                    "RejectedConsumerRelease",
                    $"Session Camera command release failed. roots='{rootCount}' consumers='{consumers.Count}' released='{released}' rejected='{issues.Count}'. {string.Join(" ", issues)}",
                    rootCount, consumers.Count, 0, 0, issues.Count);
        }

        private static List<ISessionCameraAssignmentCommandConsumer> Collect(IReadOnlyList<GameObject> roots)
        {
            var result = new List<ISessionCameraAssignmentCommandConsumer>();
            var seen = new HashSet<ISessionCameraAssignmentCommandConsumer>();
            if (roots == null) return result;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null) continue;
                MonoBehaviour[] candidates = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (candidates[index] is ISessionCameraAssignmentCommandConsumer consumer && seen.Add(consumer))
                        result.Add(consumer);
                }
            }
            return result;
        }

        private static int CountRoots(IReadOnlyList<GameObject> roots)
        {
            if (roots == null) return 0;
            int count = 0;
            for (int index = 0; index < roots.Count; index++) if (roots[index] != null) count++;
            return count;
        }
    }
}
