using System;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Concrete application-level composition for content that survives Route and Activity scene changes.
    ///
    /// The Container Scene is a visual Unity authoring boundary. The framework loads that scene,
    /// retains its authored roots for the application lifetime, and unloads the source scene.
    /// The prefab references declare the reusable implementations expected inside the scene;
    /// they are validation inputs and are never instantiated or repaired automatically.
    /// </summary>
    [Serializable]
    public sealed class PersistentContentComposition
    {
        [SerializeField]
        [Tooltip("Direct reference to the Unity scene used as the visual container for application-persistent content.")]
        private UnityEngine.Object containerScene;

        [SerializeField]
        [Tooltip("Prefab implementation expected to provide the single physical Camera output for this application.")]
        private GameObject cameraOutputPrefab;

        [SerializeField]
        [Tooltip("Prefab implementation expected to provide the persistent Transition and Loading presentation surfaces.")]
        private GameObject presentationCanvasPrefab;

        public UnityEngine.Object ContainerScene => containerScene;

        public string ContainerSceneName =>
            containerScene != null
                ? containerScene.name
                : string.Empty;

        public GameObject CameraOutputPrefab => cameraOutputPrefab;

        public GameObject PresentationCanvasPrefab => presentationCanvasPrefab;

        public bool HasContainerScene => containerScene != null;

        public bool HasCameraOutputPrefab => cameraOutputPrefab != null;

        public bool HasPresentationCanvasPrefab => presentationCanvasPrefab != null;

        public bool IsComplete =>
            HasContainerScene &&
            HasCameraOutputPrefab &&
            HasPresentationCanvasPrefab;
    }
}
