using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Persistent-output dependency injector for camera request consumers in loaded scenes.
    /// The output is mandatory and independent from the optional Session publisher.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class CameraOutputInjectionRuntime : IDisposable
    {
        private readonly CameraOutputAuthoring _outputSession;

        internal CameraOutputInjectionRuntime(
            CameraOutputAuthoring outputSession)
        {
            this._outputSession = outputSession ??
                throw new ArgumentNullException(nameof(outputSession));
            SceneManager.sceneLoaded += OnSceneLoaded;

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                AttachScene(SceneManager.GetSceneAt(index));
            }
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachScene(scene);
        }

        private void AttachScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                MonoBehaviour[] behaviours = roots[rootIndex]
                    .GetComponentsInChildren<MonoBehaviour>(true);

                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is ICameraOutputSessionConsumer consumer)
                    {
                        consumer.AttachOutputSession(_outputSession);
                    }
                }
            }
        }
    }
}
