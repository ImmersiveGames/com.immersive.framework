using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Session-owned explicit dependency injector for Session camera consumers in loaded scenes.
    /// It is constructed by FrameworkRuntimeHost and has no static access path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class CameraOutputSessionInjectionRuntime : IDisposable
    {
        private readonly SessionCameraOverride _sessionOverride;

        internal CameraOutputSessionInjectionRuntime(
            SessionCameraOverride sessionOverride)
        {
            this._sessionOverride = sessionOverride ?? throw new ArgumentNullException(nameof(sessionOverride));
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
            if (!scene.IsValid() || !scene.isLoaded) return;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                MonoBehaviour[] behaviours = roots[rootIndex].GetComponentsInChildren<MonoBehaviour>(true);
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is ISessionCameraOverrideConsumer sessionConsumer)
                    {
                        sessionConsumer.AttachSessionCameraOverride(_sessionOverride);
                    }
                }
            }
        }
    }
}
