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
        [Tooltip("Project-relative Unity scene path. This is the authoritative Persistent Content scene identity.")]
        private string scenePath = string.Empty;

        [SerializeField]
        [Tooltip("Cached scene name for presentation, diagnostics and migrated name-only compatibility.")]
        private string sceneName = string.Empty;

        [SerializeField]
        [Tooltip("Deprecated serialized reference retained for explicit asset migration and Stable API compatibility. Runtime identity comes only from the scene path/name strings.")]
        private UnityEngine.Object containerScene;

        /// <summary>
        /// Deprecated compatibility snapshot of the pre-path SceneAsset reference.
        /// It may be null for valid path-authored content and is not runtime identity.
        /// </summary>
        public UnityEngine.Object ContainerScene =>
            containerScene;

        public string ContainerSceneName =>
            sceneName ?? string.Empty;

        public string ContainerScenePath =>
            scenePath ?? string.Empty;

        public bool HasContainerScene =>
            !string.IsNullOrWhiteSpace(scenePath) ||
            !string.IsNullOrWhiteSpace(sceneName);

        public bool IsComplete =>
            HasContainerScene;
    }
}
