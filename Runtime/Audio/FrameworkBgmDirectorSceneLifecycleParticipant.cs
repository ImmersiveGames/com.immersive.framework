using System.Collections.Generic;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FrameworkBgmDirector))]
    internal sealed class FrameworkBgmDirectorSceneLifecycleParticipant :
        MonoBehaviour,
        ISceneLifecycleParticipant
    {
        private FrameworkBgmDirectorInjectionRuntime _runtime;

        public SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            return TryGetRuntime(out FrameworkBgmDirectorInjectionRuntime runtime, out string issue)
                ? runtime.OnSceneAvailable(scope, roots)
                : SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Available,
                    issue);
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            return TryGetRuntime(out FrameworkBgmDirectorInjectionRuntime runtime, out string issue)
                ? runtime.OnSceneReleasing(scope, roots, reason)
                : SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Releasing,
                    issue);
        }

        private bool TryGetRuntime(
            out FrameworkBgmDirectorInjectionRuntime runtime,
            out string issue)
        {
            runtime = _runtime;
            if (runtime != null)
            {
                issue = string.Empty;
                return true;
            }

            FrameworkBgmDirector director = GetComponent<FrameworkBgmDirector>();
            if (director == null)
            {
                issue = "BGM Director Scene Lifecycle participant requires its FrameworkBgmDirector authority.";
                return false;
            }

            _runtime = runtime = new FrameworkBgmDirectorInjectionRuntime(director);
            issue = string.Empty;
            return true;
        }
    }
}
