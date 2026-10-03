using System;
using System.Collections.Generic;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// Audio-specific composition participant for the Session-owned BGM Director.
    /// It discovers only consumers beneath the roots supplied by Scene Lifecycle.
    /// </summary>
    internal sealed class FrameworkBgmDirectorInjectionRuntime
    {
        private readonly FrameworkBgmDirector _director;
        private readonly Dictionary<SceneCompositionScope, List<IFrameworkBgmDirectorConsumer>>
            _boundConsumersByScope = new();

        internal FrameworkBgmDirectorInjectionRuntime(FrameworkBgmDirector director)
        {
            _director = director != null
                ? director
                : throw new ArgumentNullException(nameof(director));
        }

        public SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            List<IFrameworkBgmDirectorConsumer> consumers = CollectConsumers(roots);
            var newlyAttached = new List<IFrameworkBgmDirectorConsumer>();
            var issues = new List<string>();

            for (int index = 0; index < consumers.Count; index++)
            {
                IFrameworkBgmDirectorConsumer consumer = consumers[index];
                if (!consumer.TryAttachBgmDirector(
                        _director,
                        out bool wasAlreadyAttached,
                        out string issue))
                {
                    issues.Add($"consumer='{GetConsumerName(consumer)}' issue='{Normalize(issue)}'.");
                }
                else if (!wasAlreadyAttached)
                {
                    newlyAttached.Add(consumer);
                }
            }

            if (issues.Count > 0)
            {
                var rollbackIssues = new List<string>();
                for (int index = newlyAttached.Count - 1; index >= 0; index--)
                {
                    if (!newlyAttached[index].TryDetachBgmDirector(_director, out string issue))
                    {
                        rollbackIssues.Add(
                            $"consumer='{GetConsumerName(newlyAttached[index])}' rollback='{Normalize(issue)}'.");
                    }
                }

                return SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Available,
                    $"BGM Director composition rejected. consumers='{consumers.Count}' rejected='{issues.Count}' rollback='{(rollbackIssues.Count == 0 ? "Succeeded" : "Failed")}'. {string.Join(" ", issues)} {string.Join(" ", rollbackIssues)}");
            }

            if (_boundConsumersByScope.TryGetValue(
                    scope,
                    out List<IFrameworkBgmDirectorConsumer> scopeConsumers))
            {
                for (int index = 0; index < consumers.Count; index++)
                {
                    AddUnique(scopeConsumers, consumers[index]);
                }
            }
            else
            {
                _boundConsumersByScope.Add(scope, consumers);
            }

            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Available,
                $"BGM Director composition completed. consumers='{consumers.Count}' newlyAttached='{newlyAttached.Count}' idempotent='{consumers.Count - newlyAttached.Count}'.");
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            List<IFrameworkBgmDirectorConsumer> consumers = scope.Kind == SceneCompositionScopeKind.Session
                ? CollectAllBoundConsumers(roots)
                : _boundConsumersByScope.TryGetValue(
                        scope,
                        out List<IFrameworkBgmDirectorConsumer> boundConsumers)
                    ? boundConsumers
                    : CollectConsumers(roots);
            var issues = new List<string>();
            for (int index = consumers.Count - 1; index >= 0; index--)
            {
                if (!IsAlive(consumers[index]))
                {
                    continue;
                }

                if (!consumers[index].TryDetachBgmDirector(_director, out string issue))
                {
                    issues.Add($"consumer='{GetConsumerName(consumers[index])}' issue='{Normalize(issue)}'.");
                }
            }

            string diagnostic = issues.Count == 0
                ? $"BGM Director composition released. consumers='{consumers.Count}' reason='{Normalize(reason)}'."
                : $"BGM Director composition release rejected. consumers='{consumers.Count}' rejected='{issues.Count}'. {string.Join(" ", issues)}";
            if (issues.Count == 0)
            {
                if (scope.Kind == SceneCompositionScopeKind.Session)
                {
                    _boundConsumersByScope.Clear();
                }
                else
                {
                    _boundConsumersByScope.Remove(scope);
                }
            }

            return issues.Count == 0
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, diagnostic);
        }

        private static List<IFrameworkBgmDirectorConsumer> CollectConsumers(
            IReadOnlyList<GameObject> roots)
        {
            var consumers = new List<IFrameworkBgmDirectorConsumer>();
            var seenRoots = new HashSet<GameObject>();
            var seenConsumers = new HashSet<IFrameworkBgmDirectorConsumer>();
            if (roots == null)
            {
                return consumers;
            }

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root))
                {
                    continue;
                }

                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] != null &&
                        behaviours[index] is IFrameworkBgmDirectorConsumer consumer &&
                        seenConsumers.Add(consumer))
                    {
                        consumers.Add(consumer);
                    }
                }
            }

            return consumers;
        }

        private List<IFrameworkBgmDirectorConsumer> CollectAllBoundConsumers(
            IReadOnlyList<GameObject> sessionRoots)
        {
            List<IFrameworkBgmDirectorConsumer> consumers = CollectConsumers(sessionRoots);
            foreach (KeyValuePair<SceneCompositionScope, List<IFrameworkBgmDirectorConsumer>> pair in
                     _boundConsumersByScope)
            {
                for (int index = 0; index < pair.Value.Count; index++)
                {
                    AddUnique(consumers, pair.Value[index]);
                }
            }

            return consumers;
        }

        private static void AddUnique(
            List<IFrameworkBgmDirectorConsumer> consumers,
            IFrameworkBgmDirectorConsumer candidate)
        {
            if (!IsAlive(candidate))
            {
                return;
            }

            for (int index = 0; index < consumers.Count; index++)
            {
                if (ReferenceEquals(consumers[index], candidate))
                {
                    return;
                }
            }

            consumers.Add(candidate);
        }

        private static bool IsAlive(IFrameworkBgmDirectorConsumer consumer)
        {
            if (consumer is Component component)
            {
                return component != null;
            }

            return consumer != null;
        }

        private static string GetConsumerName(IFrameworkBgmDirectorConsumer consumer) =>
            consumer is Component component && component != null
                ? component.name
                : consumer?.GetType().Name ?? "<null>";

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
