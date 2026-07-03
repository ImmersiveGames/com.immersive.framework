using UnityEngine;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.LocalContribution;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored local visibility adapter between one GameObject and one Activity.
    /// Activity Flow uses this component to toggle local scene-authored content for the active Activity.
    /// It is not canonical Activity materialization.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Local Visibility Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityLocalVisibilityAdapter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Activity that owns this local visibility adapter. This only toggles this GameObject when that Activity is active; it is not canonical Activity materialization.")]
        private ActivityAsset activity;

        [SerializeField]
        [Tooltip("Explicit local content id for this scene-authored Activity contribution. Required for F5 local identity. GameObject names and hierarchy paths are diagnostics only and are not used as fallback.")]
        private string localContentId = string.Empty;

        [SerializeField]
        [Tooltip("Declares whether this local Activity contribution should be treated as required by future contribution consumers. F5F records this policy but does not validate absence yet.")]
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

        internal bool SetContentActive(bool active)
        {
            if (gameObject.activeSelf == active)
            {
                return false;
            }

            gameObject.SetActive(active);
            return true;
        }
    }
}
