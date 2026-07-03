using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Scene-authored, passive Route-scoped Content Anchor declaration.
    /// This component declares an authored anchor. Route-scoped discovery and authoring validation can inspect it,
    /// but it does not register, materialize, bind, move or instantiate runtime content.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Route Content Anchor")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route-scoped Content Anchor authoring component introduced by F7D.")]
    public sealed class RouteContentAnchor : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Route asset that owns this Content Anchor. The owner is explicit; scene, GameObject name and hierarchy path are diagnostics only.")]
        private RouteAsset route;

        [SerializeField]
        [Tooltip("Explicit stable Content Anchor Id. GameObject names, hierarchy paths, scene names and scene paths are diagnostics only and are not fallback identities.")]
        private string anchorId = string.Empty;

        [SerializeField]
        [Tooltip("Authoring intent for this anchor. Root marks a semantic container, Slot marks a future placement/mount slot, and Point marks a semantic reference point.")]
        private ContentAnchorKind kind = ContentAnchorKind.Slot;

        [SerializeField]
        [Tooltip("Authoring validation policy for this anchor. F7H reports invalid authoring, but required anchors do not block Route lifecycle yet.")]
        private ContentAnchorRequiredness requiredness = ContentAnchorRequiredness.Optional;

        [SerializeField]
        [Tooltip("Optional human-readable label for diagnostics. It is not functional identity.")]
        private string displayName = string.Empty;

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for this anchor. It has no runtime behavior.")]
        private string description = string.Empty;

        public RouteAsset Route => route;

        public ContentAnchorScope Scope => ContentAnchorScope.Route;

        public ContentAnchorKind Kind => kind;

        public ContentAnchorRequiredness Requiredness => requiredness;

        public string AnchorIdText => anchorId.NormalizeText();

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public bool HasRoute => route != null;

        public bool HasExplicitAnchorId => !string.IsNullOrWhiteSpace(anchorId);

        public bool HasExplicitKind => kind != ContentAnchorKind.Unknown;

        public bool HasValidAuthoring => HasRoute && HasExplicitAnchorId && HasExplicitKind;

        public bool IsRequired => requiredness == ContentAnchorRequiredness.Required;

        public bool IsOptional => requiredness == ContentAnchorRequiredness.Optional;

        internal bool MatchesRoute(RouteAsset candidateRoute)
        {
            return candidateRoute != null && route != null && ReferenceEquals(route, candidateRoute);
        }

        public bool IsSceneAuthored => gameObject.scene.IsValid() && gameObject.scene.isLoaded;

        public string ObjectName => gameObject.ToDiagnosticText(x => x.name, "<missing>");

        public string SceneName => gameObject != null && gameObject.scene.IsValid()
            ? gameObject.scene.name
            : "<no-scene>";

        public string ScenePath => gameObject != null && gameObject.scene.IsValid()
            ? gameObject.scene.path
            : string.Empty;

        public string ResourceName => ObjectName;

        public string ResourcePath => !string.IsNullOrWhiteSpace(ScenePath)
            ? $"{ScenePath}::{GetHierarchyPath(transform)}"
            : GetHierarchyPath(transform);

        public bool TryGetContentAnchorId(out ContentAnchorId contentAnchorId)
        {
            if (!HasExplicitAnchorId)
            {
                contentAnchorId = default;
                return false;
            }

            contentAnchorId = ContentAnchorId.From(anchorId);
            return true;
        }

        public bool TryCreateDeclaration(out ContentAnchorDeclaration declaration)
        {
            if (!HasValidAuthoring)
            {
                declaration = default;
                return false;
            }

            declaration = CreateDeclaration();
            return true;
        }

        public ContentAnchorDeclaration CreateDeclaration()
        {
            var owner = ContentAnchorDeclaration.CreateOwnerKey(ContentAnchorScope.Route, CreateRouteOwnerId(route));
            return new ContentAnchorDeclaration(
                owner,
                ContentAnchorScope.Route,
                kind,
                ContentAnchorId.From(anchorId),
                requiredness,
                DisplayName,
                Description,
                ResourceName,
                ResourcePath);
        }

        public string ToDiagnosticString()
        {
            if (TryCreateDeclaration(out var declaration))
            {
                return declaration.ToDiagnosticString();
            }

            return $"Route Content Anchor invalid object='{ObjectName}' scene='{SceneName}' route='{GetRouteName(route)}' anchorId='{AnchorIdText}' kind='{kind}' requiredness='{requiredness}'";
        }

        private static string CreateRouteOwnerId(RouteAsset route)
        {
            if (route == null)
            {
                return string.Empty;
            }

            return route.PrimaryScenePath.NormalizeTextOrFallback(route.RouteName);
        }

        private static string GetRouteName(RouteAsset route)
        {
            return route.ToDiagnosticText(x => x.RouteName);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            var parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
