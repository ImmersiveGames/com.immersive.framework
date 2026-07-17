using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Scene-authored, passive Activity-scoped Content Anchor declaration.
    /// This component declares an authored anchor owned by one Activity. Activity-scoped discovery and authoring
    /// validation can inspect it, but it does not register, materialize, bind, move or instantiate runtime content.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Content Anchor")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity-scoped Content Anchor authoring component introduced by F9G.")]
    public sealed class ActivityContentAnchor : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Activity asset that owns this Content Anchor. The owner is explicit; scene, GameObject name and hierarchy path are diagnostics only.")]
        private ActivityAsset activity;

        [SerializeField]
        [Tooltip("Explicit stable Content Anchor Id. GameObject names, hierarchy paths, scene names and scene paths are diagnostics only and are not fallback identities.")]
        private string anchorId = string.Empty;

        [SerializeField]
        [Tooltip("Authoring intent for this anchor. Root marks a semantic container, Slot marks a future placement/mount slot, and Point marks a semantic reference point.")]
        private ContentAnchorKind kind = ContentAnchorKind.Slot;

        [SerializeField]
        [Tooltip("Authoring validation policy for this anchor. Required anchors are reported diagnostically only; they do not block Activity lifecycle yet.")]
        private ContentAnchorRequiredness requiredness = ContentAnchorRequiredness.Optional;

        [SerializeField]
        [Tooltip("Optional human-readable label for diagnostics. It is not functional identity.")]
        private string displayName = string.Empty;

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for this anchor. It has no runtime behavior.")]
        private string description = string.Empty;

        public ActivityAsset Activity => activity;

        public ContentAnchorScope Scope => ContentAnchorScope.Activity;

        public ContentAnchorKind Kind => kind;

        public ContentAnchorRequiredness Requiredness => requiredness;

        public string AnchorIdText => Normalize(anchorId);

        public string DisplayName => Normalize(displayName);

        public string Description => Normalize(description);

        public bool HasActivity => activity != null;

        public bool HasExplicitAnchorId => !string.IsNullOrWhiteSpace(anchorId);

        public bool HasExplicitKind => kind != ContentAnchorKind.Unknown;

        public bool HasValidAuthoring => HasActivity && activity.HasValidActivityId && HasExplicitAnchorId && HasExplicitKind;

        public bool IsRequired => requiredness == ContentAnchorRequiredness.Required;

        public bool IsOptional => requiredness == ContentAnchorRequiredness.Optional;

        internal bool MatchesActivity(ActivityAsset candidateActivity)
        {
            return candidateActivity != null && activity != null && ReferenceEquals(activity, candidateActivity);
        }

        /// <summary>
        /// Internal diagnostics hook used by QA smokes to create temporary positive-path fixtures.
        /// This is not gameplay authoring API and does not materialize, bind or place runtime content.
        /// </summary>
        internal void ConfigureForDiagnostics(
            ActivityAsset ownerActivity,
            string explicitAnchorId,
            ContentAnchorKind anchorKind,
            ContentAnchorRequiredness anchorRequiredness,
            string diagnosticDisplayName,
            string diagnosticDescription)
        {
            activity = ownerActivity;
            anchorId = Normalize(explicitAnchorId);
            kind = anchorKind;
            requiredness = anchorRequiredness;
            displayName = Normalize(diagnosticDisplayName);
            description = Normalize(diagnosticDescription);
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
            var owner = ContentAnchorDeclaration.CreateOwnerKey(ContentAnchorScope.Activity, CreateActivityOwnerId(activity));
            return new ContentAnchorDeclaration(
                owner,
                ContentAnchorScope.Activity,
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

            return $"Activity Content Anchor invalid object='{ObjectName}' scene='{SceneName}' activity='{GetActivityName(activity)}' anchorId='{AnchorIdText}' kind='{kind}' requiredness='{requiredness}'";
        }

        private static string CreateActivityOwnerId(ActivityAsset activity)
        {
            if (activity == null)
            {
                return string.Empty;
            }

            if (!activity.HasValidActivityId)
            {
                throw new System.ArgumentException("Activity Content Anchor ownership requires a valid ActivityId.", nameof(activity));
            }

            return activity.ActivityId.StableText;
        }

        private static string GetActivityName(ActivityAsset activity)
        {
            return activity.ToDiagnosticText(x => x.ActivityName);
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

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
