using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Internal composition bridge from SceneLifecycleRuntime to authored
    /// SceneLifecycleEvents components. It has no independent lifecycle source.
    /// </summary>
    internal sealed class SceneLifecycleEventsParticipant : ISceneLifecycleParticipant
    {
        public SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            if (scope.Kind != SceneCompositionScopeKind.Scene)
            {
                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Available,
                    "Scene lifecycle event components apply only to Scene scopes.");
            }

            return Dispatch(roots, scope.Scene, "Available", true, scope);
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            if (scope.Kind != SceneCompositionScopeKind.Scene)
            {
                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Releasing,
                    "Scene lifecycle event components apply only to Scene scopes.");
            }

            return Dispatch(roots, scope.Scene, "Releasing", false, scope);
        }

        private static SceneCompositionResult Dispatch(
            IReadOnlyList<GameObject> roots,
            Scene scene,
            string phase,
            bool available,
            SceneCompositionScope scope)
        {
            List<SceneLifecycleEvents> events = Collect(roots);
            for (int index = 0; index < events.Count; index++)
            {
                try
                {
                    if (available)
                    {
                        events[index].NotifySceneAvailable();
                    }
                    else
                    {
                        events[index].NotifySceneReleasing();
                    }
                }
                catch (Exception exception)
                {
                    string diagnostic =
                        $"Scene Lifecycle Events callback failed. phase='{phase}' scene='{SceneLabel(scene)}' object='{events[index].name.NormalizeTextOrFallback("<unnamed>")}' exception='{exception.GetType().Name}' message='{exception.Message.NormalizeTextOrFallback("<empty>")}'.";
                    return SceneCompositionResult.Rejected(
                        scope,
                        available ? SceneCompositionOperation.Available : SceneCompositionOperation.Releasing,
                        diagnostic);
                }
            }

            string completedDiagnostic =
                $"Scene Lifecycle Events callback completed. phase='{phase}' scene='{SceneLabel(scene)}' receiverCount='{events.Count}'.";
            return SceneCompositionResult.Completed(
                scope,
                available ? SceneCompositionOperation.Available : SceneCompositionOperation.Releasing,
                completedDiagnostic);
        }

        private static List<SceneLifecycleEvents> Collect(IReadOnlyList<GameObject> roots)
        {
            var result = new List<SceneLifecycleEvents>();
            var seen = new HashSet<SceneLifecycleEvents>();
            if (roots == null)
            {
                return result;
            }

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                SceneLifecycleEvents[] candidates =
                    root.GetComponentsInChildren<SceneLifecycleEvents>(true);
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

        private static string SceneLabel(Scene scene) => scene.IsValid()
            ? scene.name.NormalizeTextOrFallback("<unnamed>")
            : "<invalid>";
    }
}
