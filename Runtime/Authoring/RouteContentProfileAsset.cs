using System;
using System.Collections.Generic;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Authoring profile for Route-owned content declarations.
    /// Additional scenes declared here are consumed by Route scene composition in F6E.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Route Content Profile",
        menuName = "Immersive Framework/Route Content Profile",
        order = 11)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route content profile is consumed by F6E route scene composition; release remains deferred.")]
    public sealed class RouteContentProfileAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable profile id used in diagnostics. If empty, the asset name is used.")]
        private string profileId = string.Empty;

        [SerializeField]
        [Tooltip("Additional scenes declared by the Route and loaded additively by Route scene composition when execution-ready.")]
        private RouteContentSceneEntry[] additionalScenes = Array.Empty<RouteContentSceneEntry>();

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for this Route Content Profile. This has no runtime behavior.")]
        private string description = string.Empty;

        public string ProfileId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(profileId))
                {
                    return profileId.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "RouteContentProfile";
            }
        }

        public IReadOnlyList<RouteContentSceneEntry> AdditionalScenes => additionalScenes ?? Array.Empty<RouteContentSceneEntry>();

        public int AdditionalSceneCount => AdditionalScenes.Count;

        public bool HasAdditionalScenes => AdditionalSceneCount > 0;

        public string Description => description ?? string.Empty;
    }
}
