using System.Collections.Generic;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Pause
{
    internal sealed class PauseProductBindingSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IPauseProductBindingPort _port;

        internal PauseProductBindingSceneLifecycleParticipant(IPauseProductBindingPort port) => _port = port;

        public bool OnSceneAvailable(Scene scene, IReadOnlyList<GameObject> roots, out string diagnostic)
        {
            diagnostic = string.Empty;
            foreach (PausePlayerInputBinding binding in Collect(roots))
            {
                if (!binding.TryInjectBindingPort(_port, out diagnostic))
                {
                    return false;
                }
            }
            return true;
        }

        public bool OnSceneReleasing(Scene scene, IReadOnlyList<GameObject> roots, string reason, out string diagnostic)
        {
            diagnostic = string.Empty;
            foreach (PausePlayerInputBinding binding in Collect(roots))
            {
                if (!binding.ReleaseForSceneLifecycle(reason, out diagnostic))
                {
                    return false;
                }
            }
            return true;
        }

        private static List<PausePlayerInputBinding> Collect(IReadOnlyList<GameObject> roots)
        {
            var result = new List<PausePlayerInputBinding>();
            if (roots == null) return result;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == null) continue;
                result.AddRange(roots[i].GetComponentsInChildren<PausePlayerInputBinding>(true));
            }
            return result;
        }
    }
}
