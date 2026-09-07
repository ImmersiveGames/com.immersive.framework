using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Session-owned explicit dependency injector for Camera Subject availability consumers
    /// in loaded scenes. It follows the CameraOutputInjectionRuntime convention: constructed
    /// by FrameworkRuntimeHost, no static access path, no Find-based polling.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class CameraSubjectAvailabilityInjectionRuntime : IDisposable
    {
        private readonly ICameraSubjectAvailabilitySource _availabilitySource;
        private readonly List<ICameraSubjectAvailabilityConsumer>
            _attachedConsumers =
                new List<ICameraSubjectAvailabilityConsumer>();

        internal CameraSubjectAvailabilityInjectionRuntime(
            ICameraSubjectAvailabilitySource availabilitySource)
        {
            _availabilitySource = availabilitySource ?? throw new ArgumentNullException(nameof(availabilitySource));
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                AttachScene(SceneManager.GetSceneAt(index));
            }
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _attachedConsumers.Clear();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachScene(scene);
        }

        internal void AttachScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            AttachRoots(scene.GetRootGameObjects());
        }

        internal void AttachRoots(IReadOnlyList<GameObject> roots)
        {
            if (roots == null) return;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null) continue;
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is ICameraSubjectAvailabilityConsumer consumer)
                    {
                        AttachExact(consumer);
                    }
                }
            }
        }

        internal void AttachExact(
            ICameraSubjectAvailabilityConsumer consumer)
        {
            if (consumer == null)
            {
                throw new ArgumentNullException(nameof(consumer));
            }

            for (int index = 0; index < _attachedConsumers.Count; index++)
            {
                if (ReferenceEquals(_attachedConsumers[index], consumer))
                {
                    return;
                }
            }

            consumer.AttachCameraSubjectAvailability(_availabilitySource);
            _attachedConsumers.Add(consumer);
        }
    }
}
