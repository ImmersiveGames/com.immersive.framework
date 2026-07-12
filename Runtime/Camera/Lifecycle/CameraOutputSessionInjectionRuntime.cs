using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Session-owned explicit dependency injector for camera request sources in loaded Route scenes.
    /// It is constructed by FrameworkRuntimeHost and has no static access path.
    /// </summary>
    internal sealed class CameraOutputSessionInjectionRuntime : IDisposable
    {
        private readonly CameraOutputSessionBinding outputSession;
        private readonly SessionCameraOverrideBinding sessionOverride;

        internal CameraOutputSessionInjectionRuntime(
            CameraOutputSessionBinding outputSession,
            SessionCameraOverrideBinding sessionOverride)
        {
            this.outputSession = outputSession ?? throw new ArgumentNullException(nameof(outputSession));
            this.sessionOverride = sessionOverride ?? throw new ArgumentNullException(nameof(sessionOverride));
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
                    if (behaviours[index] is ICameraOutputSessionConsumer consumer)
                    {
                        consumer.AttachOutputSession(outputSession);
                    }

                    if (behaviours[index] is ISessionCameraOverrideConsumer sessionConsumer)
                    {
                        sessionConsumer.AttachSessionCameraOverride(sessionOverride);
                    }
                }
            }
        }
    }
}
