using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// Same-assembly, explicit scene injection owned by one FrameworkBgmDirector.
    /// It does not expose a global/static authority and does not make the director persistent.
    /// Persistence remains the responsibility of the Framework Persistent Content composition.
    /// </summary>
    internal sealed class FrameworkBgmDirectorInjectionRuntime : IDisposable
    {
        private readonly FrameworkBgmDirector director;
        private bool disposed;

        internal FrameworkBgmDirectorInjectionRuntime(FrameworkBgmDirector director)
        {
            this.director = director != null
                ? director
                : throw new ArgumentNullException(nameof(director));

            SceneManager.sceneLoaded += HandleSceneLoaded;
            InjectAllLoadedScenes();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            DetachAllLoadedScenes();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InjectScene(scene);
        }

        private void InjectAllLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                InjectScene(SceneManager.GetSceneAt(i));
            }
        }

        private void DetachAllLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                DetachScene(SceneManager.GetSceneAt(i));
            }
        }

        private void InjectScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IFrameworkBgmDirectorConsumer consumer)
                    {
                        consumer.AttachBgmDirector(director);
                    }
                }
            }
        }

        private void DetachScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IFrameworkBgmDirectorConsumer consumer)
                    {
                        consumer.DetachBgmDirector(director);
                    }
                }
            }
        }
    }
}
