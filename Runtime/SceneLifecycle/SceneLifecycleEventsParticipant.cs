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
        public bool OnSceneAvailable(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            out string diagnostic)
        {
            return Dispatch(roots, scene, "Available", true, out diagnostic);
        }

        public bool OnSceneReleasing(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            string reason,
            out string diagnostic)
        {
            return Dispatch(roots, scene, "Releasing", false, out diagnostic);
        }

        private static bool Dispatch(
            IReadOnlyList<GameObject> roots,
            Scene scene,
            string phase,
            bool available,
            out string diagnostic)
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
                    diagnostic =
                        $"Scene Lifecycle Events callback failed. phase='{phase}' scene='{SceneLabel(scene)}' object='{events[index].name.NormalizeTextOrFallback("<unnamed>")}' exception='{exception.GetType().Name}' message='{exception.Message.NormalizeTextOrFallback("<empty>")}'.";
                    return false;
                }
            }

            diagnostic =
                $"Scene Lifecycle Events callback completed. phase='{phase}' scene='{SceneLabel(scene)}' receiverCount='{events.Count}'.";
            return true;
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
