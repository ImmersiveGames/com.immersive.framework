using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.LocalContribution;
using UnityEngine;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored boundary for content owned by one Activity scope.
    /// Activity Flow uses this component for local contribution discovery and lifecycle callbacks;
    /// it does not decide GameObject visibility.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Content Contribution")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity local contribution callbacks are active in the F4 baseline and may still change before stabilization.")]
    public sealed class ActivityContentContribution : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Activity asset that owns this scene-authored content boundary.")]
        private ActivityAsset activity;

        [SerializeField]
        [Tooltip("Explicit local content id for this scene-authored Activity contribution. GameObject names and hierarchy paths are diagnostics only and are not used as fallback.")]
        private string localContentId = string.Empty;

        [SerializeField]
        [Tooltip("Declares whether this local Activity contribution is required by content/readiness consumers.")]
        private FrameworkContentRequiredness requiredness = FrameworkContentRequiredness.Required;

        public ActivityAsset Activity => activity;

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

        internal bool MatchesActivity(ActivityAsset candidateActivity)
        {
            return candidateActivity != null &&
                   IsSceneBinding &&
                   activity != null &&
                   ReferenceEquals(activity, candidateActivity);
        }

        internal bool TryValidate(out string diagnosticReason)
        {
            if (activity == null)
            {
                diagnosticReason = "MissingActivity";
                return false;
            }

            if (!activity.HasValidActivityId)
            {
                diagnosticReason = "ActivityIdentityInvalid";
                return false;
            }

            if (!HasExplicitLocalContentId)
            {
                diagnosticReason = "MissingLocalContentId";
                return false;
            }

            if (requiredness != FrameworkContentRequiredness.Required &&
                requiredness != FrameworkContentRequiredness.Optional)
            {
                diagnosticReason = "UnsupportedRequiredness";
                return false;
            }

            diagnosticReason = "Valid";
            return true;
        }
    }
}
