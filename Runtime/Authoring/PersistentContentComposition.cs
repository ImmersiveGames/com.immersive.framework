using System;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Concrete application-level composition for content that survives Route and
    /// Activity scene changes.
    ///
    /// The Content Scene is the complete visual authoring boundary. Runtime loads
    /// that scene, retains its authored root hierarchies for the application
    /// lifetime and unloads the source scene.
    ///
    /// Prefabs may be used inside the scene through normal Unity authoring, but
    /// prefab origin is not part of this contract.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.")]
    public sealed class PersistentContentComposition
    {
        [SerializeField]
        [Tooltip("Direct reference to the Unity scene containing the complete application-persistent composition.")]
        private UnityEngine.Object containerScene;

        public UnityEngine.Object ContainerScene =>
            containerScene;

        public string ContainerSceneName =>
            containerScene != null
                ? containerScene.name
                : string.Empty;

        public bool HasContainerScene =>
            containerScene != null;

        public bool IsComplete =>
            HasContainerScene;
    }
}
