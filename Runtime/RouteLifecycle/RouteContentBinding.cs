using Immersive.Framework.Authoring;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.LocalContribution;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Scene-authored boundary for Route-scoped content.
    /// Route Lifecycle uses this component to notify local receivers; it does not own GameObject visibility.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Route Content Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route local content callbacks are active in the F3 Route baseline and may still change before stabilization.")]
    public sealed class RouteContentBinding : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Route asset that owns this scene. Use the Route whose Primary Scene is this scene.")]
        private RouteAsset route;

        [SerializeField]
        [Tooltip("Explicit local content id for this scene-authored Route contribution. Required for F5 local identity. GameObject names and hierarchy paths are diagnostics only and are not used as fallback.")]
        private string localContentId = string.Empty;

        [SerializeField]
        [Tooltip("Declares whether this local Route contribution should be treated as required by future contribution consumers. F5F records this policy but does not validate absence yet.")]
        private FrameworkContentRequiredness requiredness = FrameworkContentRequiredness.Required;

        public RouteAsset Route => route;

        public FrameworkContentRequiredness Requiredness => requiredness;

        public LocalContentScopeKind LocalScopeKind => LocalContentScopeKind.SceneAuthored;

        public string LocalContentIdText => !string.IsNullOrWhiteSpace(localContentId)
            ? localContentId.NormalizeText()
            : string.Empty;

        public bool HasExplicitLocalContentId => !string.IsNullOrWhiteSpace(localContentId);

        public bool TryGetLocalContentId(out LocalContentId localId)
        {
            if (!HasExplicitLocalContentId)
            {
                localId = default;
                return false;
            }

            localId = LocalContentId.From(localContentId);
            return true;
        }

        internal bool IsSceneBinding => gameObject.scene.IsValid() && gameObject.scene.isLoaded;

        internal string ObjectName => gameObject.ToDiagnosticText(x => x.name, "<missing>");

        internal string SceneName => gameObject != null && gameObject.scene.IsValid()
            ? gameObject.scene.name
            : "<no-scene>";

        internal bool MatchesRoute(RouteAsset candidateRoute)
        {
            if (candidateRoute == null || !IsSceneBinding)
            {
                return false;
            }

            return route != null && route.HasSameIdentity(candidateRoute);
        }
    }
}
