using System;
using Immersive.Framework.ContentFlow;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Authoring declaration for one additional scene owned by a Route content profile.
    /// F6E route scene composition executes declarations that have scene data and explicit content id.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route content scene entries are consumed by F6E route scene composition; release remains deferred.")]
    public sealed class RouteContentSceneEntry
    {
        [SerializeField]
        [Tooltip("Stable content id within the Route Content Profile. F6 execution requires this value to be explicit; the legacy fallback is diagnostics/planning-only.")]
        private string contentId = string.Empty;

        [SerializeField]
        [Tooltip("Project-relative path of the additional Unity scene declared by this route content profile. Managed by the Profile Inspector.")]
        private string scenePath = string.Empty;

        [SerializeField]
        [Tooltip("Cached human-readable scene name shown in framework diagnostics.")]
        private string sceneName = string.Empty;

        [SerializeField]
        [Tooltip("Whether this scene declaration is required once route scene composition execution is implemented.")]
        private FrameworkContentRequiredness requiredness = FrameworkContentRequiredness.Optional;

        public string ExplicitContentId
        {
            get
            {
                return !string.IsNullOrWhiteSpace(contentId) ? contentId.Trim() : string.Empty;
            }
        }

        public bool HasExplicitContentId => !string.IsNullOrWhiteSpace(ExplicitContentId);

        public string ContentId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(contentId))
                {
                    return contentId.Trim();
                }

                if (!string.IsNullOrWhiteSpace(SceneName))
                {
                    return SceneName;
                }

                return "route-scene";
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

        public bool HasScene => !string.IsNullOrWhiteSpace(ScenePath) || !string.IsNullOrWhiteSpace(SceneName);
    }
}
