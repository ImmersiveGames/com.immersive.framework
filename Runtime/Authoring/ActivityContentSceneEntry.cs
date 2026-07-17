using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Authoring declaration for one scene owned by an Activity content profile.
    /// Entries are validated, planned and executed by Activity scene composition when execution-ready.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity content scene entries are loaded/released by Activity scene composition operations.")]
    public sealed class ActivityContentSceneEntry
    {
        [SerializeField]
        [Tooltip("Stable content id within the Activity Content Profile. Required for execution identity. GameObject names, scene names and scene paths are diagnostics only.")]
        private string contentId = string.Empty;

        [SerializeField]
        [Tooltip("Project-relative path of the Activity-owned Unity scene declared by this profile. Managed by the Profile Inspector.")]
        private string scenePath = string.Empty;

        [SerializeField]
        [Tooltip("Cached human-readable scene name shown in framework diagnostics.")]
        private string sceneName = string.Empty;

        [SerializeField]
        [Tooltip("Whether this scene declaration is required once Activity scene composition execution exists.")]
        private FrameworkContentRequiredness requiredness = FrameworkContentRequiredness.Optional;

        [SerializeField]
        [Tooltip("How this Activity scene is loaded by Activity scene composition. Current runtime supports Additive only.")]
        private ActivityContentSceneLoadMode loadMode = ActivityContentSceneLoadMode.Additive;

        [SerializeField]
        [Tooltip("Whether this Activity-owned scene is released or kept when the active Activity changes. Route changes always release Activity-owned scenes regardless of this policy.")]
        private ActivityContentReleasePolicy releasePolicy = ActivityContentReleasePolicy.ReleaseOnActivityChange;

        public string ExplicitContentId
        {
            get
            {
                return !string.IsNullOrWhiteSpace(contentId) ? contentId.Trim() : string.Empty;
            }
        }

        public bool HasExplicitContentId => !string.IsNullOrWhiteSpace(ExplicitContentId);

        /// <summary>
        /// Functional content id. Empty when not authored; never falls back to scene name/path.
        /// </summary>
        public string ContentId => ExplicitContentId;

        /// <summary>
        /// Diagnostics-only label when explicit content id is missing. Not used as runtime identity.
        /// </summary>
        public string DiagnosticContentId
        {
            get
            {
                if (HasExplicitContentId)
                {
                    return ExplicitContentId;
                }

                if (!string.IsNullOrWhiteSpace(SceneName))
                {
                    return SceneName;
                }

                return "activity-scene";
            }
        }

        public string ScenePath => scenePath ?? string.Empty;

        public string SceneName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    return sceneName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(scenePath))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        return fileName;
                    }
                }

                return string.Empty;
            }
        }

        public FrameworkContentRequiredness Requiredness => requiredness;

        public ActivityContentSceneLoadMode LoadMode
        {
            get
            {
                return Enum.IsDefined(typeof(ActivityContentSceneLoadMode), loadMode)
                    ? loadMode
                    : ActivityContentSceneLoadMode.Additive;
            }
        }

        public ActivityContentReleasePolicy ReleasePolicy
        {
            get
            {
                return Enum.IsDefined(typeof(ActivityContentReleasePolicy), releasePolicy)
                    ? releasePolicy
                    : ActivityContentReleasePolicy.ReleaseOnActivityChange;
            }
        }

        public bool HasScene => !string.IsNullOrWhiteSpace(ScenePath) || !string.IsNullOrWhiteSpace(SceneName);
    }
}
