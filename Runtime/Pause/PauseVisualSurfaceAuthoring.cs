using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Authored Pause visual surface contract.
    /// This component declares the data a future Pause visual consumer may use to request ContentAnchor binding/materialization.
    /// It does not materialize, bind, register, release, listen to Pause input, change Time.timeScale or control Route/Activity lifecycle.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Pause/Pause Visual Surface Authoring")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10C Pause visual surface authoring contract; records anchor owner for binding request derivation.")]
    public sealed class PauseVisualSurfaceAuthoring : MonoBehaviour
    {
        private const string DefaultSurfaceId = "pause.visual.surface";
        private const string DefaultRuntimeOwnerId = "pause.visual.owner";
        private const string DefaultRuntimeOwnerName = "Pause Visual Surface";
        private const string DefaultAnchorOwnerId = "pause.visual.anchor-owner";
        private const string DefaultAnchorId = "pause.visual.overlay";
        private const string DefaultRuntimeContentId = "pause.visual.content";
        private const string DefaultResourceKey = "pause.visual.prefab";
        private const string DefaultSource = nameof(PauseVisualSurfaceAuthoring);
        private const string DefaultReason = "pause.visual.surface.authoring.contract";

        [Header("Pause Visual Surface")]
        [SerializeField] private string surfaceId = DefaultSurfaceId;
        [SerializeField] private PauseVisualSurfaceKind surfaceKind = PauseVisualSurfaceKind.OverlayRoot;
        [SerializeField] private PauseState pauseState = PauseState.Paused;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private bool resetLocalTransform = true;

        [Header("Runtime Content")]
        [SerializeField] private RuntimeContentScope runtimeScope = RuntimeContentScope.Transient;
        [SerializeField] private string runtimeOwnerId = DefaultRuntimeOwnerId;
        [SerializeField] private string runtimeOwnerName = DefaultRuntimeOwnerName;
        [SerializeField] private string runtimeContentId = DefaultRuntimeContentId;
        [SerializeField] private string resourceKey = DefaultResourceKey;
        [SerializeField] private RuntimeReleasePolicy releasePolicy = RuntimeReleasePolicy.MarkReleasedAndUnregister;

        [Header("Content Anchor Requirement")]
        [SerializeField] private ContentAnchorScope anchorScope = ContentAnchorScope.Local;
        [SerializeField] private ContentAnchorKind anchorKind = ContentAnchorKind.Root;
        [SerializeField] private ContentAnchorRequiredness requiredness = ContentAnchorRequiredness.Required;
        [SerializeField] private string anchorOwnerId = DefaultAnchorOwnerId;
        [SerializeField] private string anchorId = DefaultAnchorId;

        [Header("Diagnostics")]
        [SerializeField] private string displayName = "Pause Visual Surface";
        [SerializeField]
        [TextArea(2, 4)]
        private string description = "Passive Pause visual surface contract. No materialization side effects.";
        [SerializeField] private string reason = DefaultReason;

        public string SurfaceIdText => surfaceId.NormalizeText();

        public PauseVisualSurfaceKind SurfaceKind => surfaceKind;

        public PauseState PauseState => pauseState;

        public GameObject VisualPrefab => visualPrefab;

        public bool ResetLocalTransform => resetLocalTransform;

        public RuntimeContentScope RuntimeScope => runtimeScope;

        public string RuntimeOwnerId => runtimeOwnerId.NormalizeText();

        public string RuntimeOwnerName => runtimeOwnerName.NormalizeText();

        public string RuntimeContentIdText => runtimeContentId.NormalizeText();

        public string ResourceKey => resourceKey.NormalizeText();

        public RuntimeReleasePolicy ReleasePolicy => releasePolicy;

        public ContentAnchorScope AnchorScope => anchorScope;

        public ContentAnchorKind AnchorKind => anchorKind;

        public ContentAnchorRequiredness Requiredness => requiredness;

        public string AnchorOwnerId => anchorOwnerId.NormalizeText();

        public string AnchorId => anchorId.NormalizeText();

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public string Reason => reason.NormalizeText();

        public bool HasSurfaceId => !string.IsNullOrWhiteSpace(surfaceId);

        public bool HasVisualPrefab => visualPrefab != null;

        public bool HasRuntimeOwnerId => !string.IsNullOrWhiteSpace(runtimeOwnerId);

        public bool HasRuntimeContentId => !string.IsNullOrWhiteSpace(runtimeContentId);

        public bool HasResourceKey => !string.IsNullOrWhiteSpace(resourceKey);

        public bool HasAnchorOwnerId => !string.IsNullOrWhiteSpace(anchorOwnerId);

        public bool HasAnchorId => !string.IsNullOrWhiteSpace(anchorId);

        public bool HasValidAuthoring => HasSurfaceId
            && surfaceKind != PauseVisualSurfaceKind.Unknown
            && pauseState == PauseState.Paused
            && HasVisualPrefab
            && runtimeScope != RuntimeContentScope.Unknown
            && HasRuntimeOwnerId
            && HasRuntimeContentId
            && HasResourceKey
            && anchorScope != ContentAnchorScope.Unknown
            && anchorKind != ContentAnchorKind.Unknown
            && HasAnchorOwnerId
            && HasAnchorId;

        public bool TryCreateContract(out PauseVisualSurfaceContract contract, out string message)
        {
            contract = default;
            message = string.Empty;

            if (!HasSurfaceId)
            {
                message = "Pause visual surface authoring requires an explicit surface id.";
                return false;
            }

            if (!Enum.IsDefined(typeof(PauseVisualSurfaceKind), surfaceKind) || surfaceKind == PauseVisualSurfaceKind.Unknown)
            {
                message = "Pause visual surface authoring requires an explicit visual surface kind.";
                return false;
            }

            if (!Enum.IsDefined(typeof(PauseState), pauseState) || pauseState != PauseState.Paused)
            {
                message = "Pause visual surface authoring currently supports only the Paused visual state.";
                return false;
            }

            if (visualPrefab == null)
            {
                message = "Pause visual surface authoring requires an explicit visual prefab/template.";
                return false;
            }

            if (!Enum.IsDefined(typeof(RuntimeContentScope), runtimeScope) || runtimeScope == RuntimeContentScope.Unknown)
            {
                message = "Pause visual surface authoring requires an explicit RuntimeContent scope.";
                return false;
            }

            if (!HasRuntimeOwnerId)
            {
                message = "Pause visual surface authoring requires an explicit RuntimeContent owner id.";
                return false;
            }

            if (!HasRuntimeContentId)
            {
                message = "Pause visual surface authoring requires an explicit RuntimeContent id.";
                return false;
            }

            if (!HasResourceKey)
            {
                message = "Pause visual surface authoring requires an explicit runtime resource key.";
                return false;
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), releasePolicy) || releasePolicy == RuntimeReleasePolicy.Unknown)
            {
                message = "Pause visual surface authoring requires an explicit RuntimeContent release policy.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ContentAnchorScope), anchorScope) || anchorScope == ContentAnchorScope.Unknown)
            {
                message = "Pause visual surface authoring requires an explicit ContentAnchor scope.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ContentAnchorKind), anchorKind) || anchorKind == ContentAnchorKind.Unknown)
            {
                message = "Pause visual surface authoring requires an explicit ContentAnchor kind.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), requiredness))
            {
                message = "Pause visual surface authoring requires a defined ContentAnchor requiredness value.";
                return false;
            }

            if (!HasAnchorOwnerId)
            {
                message = "Pause visual surface authoring requires an explicit ContentAnchor owner id.";
                return false;
            }

            if (!HasAnchorId)
            {
                message = "Pause visual surface authoring requires an explicit ContentAnchor id.";
                return false;
            }

            var owner = CreateRuntimeOwner(runtimeScope, RuntimeOwnerId, RuntimeOwnerName);
            var requirement = new PauseContentRequirement(
                PauseContentRequirementId.From(SurfaceIdText),
                ToRequirementPurpose(surfaceKind),
                pauseState,
                runtimeScope,
                owner,
                anchorScope,
                anchorKind,
                ContentAnchorId.From(AnchorId),
                requiredness,
                DefaultSource,
                Reason.NormalizeTextOrFallback(DefaultReason));
            var resource = RuntimeMaterializationResource.From(
                "UnityPrefab",
                ResourceKey,
                visualPrefab.name,
                GetPrefabResourcePath(visualPrefab));

            contract = new PauseVisualSurfaceContract(
                PauseContentRequirementId.From(SurfaceIdText),
                surfaceKind,
                visualPrefab,
                requirement,
                ContentAnchorDeclaration.CreateOwnerKey(anchorScope, AnchorOwnerId),
                owner,
                RuntimeContentId.From(RuntimeContentIdText),
                resource,
                releasePolicy,
                resetLocalTransform,
                DefaultSource,
                Reason.NormalizeTextOrFallback(DefaultReason));
            message = contract.ToDiagnosticString();
            return true;
        }

        internal void ConfigureForDiagnostics(
            GameObject configuredVisualPrefab,
            PauseVisualSurfaceKind configuredSurfaceKind,
            PauseState configuredPauseState,
            RuntimeContentScope configuredRuntimeScope,
            string configuredRuntimeOwnerId,
            string configuredRuntimeOwnerName,
            ContentAnchorScope configuredAnchorScope,
            ContentAnchorKind configuredAnchorKind,
            ContentAnchorRequiredness configuredRequiredness,
            string configuredAnchorOwnerId,
            string configuredAnchorId,
            string configuredRuntimeContentId,
            string configuredResourceKey,
            RuntimeReleasePolicy configuredReleasePolicy,
            bool configuredResetLocalTransform)
        {
            visualPrefab = configuredVisualPrefab;
            surfaceKind = configuredSurfaceKind;
            pauseState = configuredPauseState;
            runtimeScope = configuredRuntimeScope;
            runtimeOwnerId = configuredRuntimeOwnerId.NormalizeTextOrFallback(DefaultRuntimeOwnerId);
            runtimeOwnerName = configuredRuntimeOwnerName.NormalizeTextOrFallback(DefaultRuntimeOwnerName);
            anchorScope = configuredAnchorScope;
            anchorKind = configuredAnchorKind;
            requiredness = configuredRequiredness;
            anchorOwnerId = configuredAnchorOwnerId.NormalizeTextOrFallback(DefaultAnchorOwnerId);
            anchorId = configuredAnchorId.NormalizeTextOrFallback(DefaultAnchorId);
            runtimeContentId = configuredRuntimeContentId.NormalizeTextOrFallback(DefaultRuntimeContentId);
            resourceKey = configuredResourceKey.NormalizeTextOrFallback(DefaultResourceKey);
            releasePolicy = configuredReleasePolicy;
            resetLocalTransform = configuredResetLocalTransform;
        }

        public string ToDiagnosticString()
        {
            if (TryCreateContract(out var contract, out _))
            {
                return contract.ToDiagnosticString();
            }

            return $"Pause Visual Surface invalid object='{gameObject.ToDiagnosticText(x => x.name, "<missing>")}' surfaceId='{SurfaceIdText}' kind='{surfaceKind}' pauseState='{pauseState}' prefab='{visualPrefab.ToDiagnosticText(x => x.name, "<missing>")}' runtimeScope='{runtimeScope}' runtimeOwner='{RuntimeOwnerId}' runtimeContent='{RuntimeContentIdText}' anchorScope='{anchorScope}' anchorKind='{anchorKind}' anchorOwner='{AnchorOwnerId}' anchor='{AnchorId}' requiredness='{requiredness}'.";
        }

        private static RuntimeContentOwner CreateRuntimeOwner(
            RuntimeContentScope scope,
            string ownerId,
            string ownerName)
        {
            string resolvedOwnerId = ownerId.NormalizeText();
            string resolvedOwnerName = ownerName.NormalizeText();
            switch (scope)
            {
                case RuntimeContentScope.Session:
                    return RuntimeContentOwner.Session(resolvedOwnerId, resolvedOwnerName);
                case RuntimeContentScope.Route:
                    return RuntimeContentOwner.Route(resolvedOwnerId, resolvedOwnerName);
                case RuntimeContentScope.Activity:
                    return RuntimeContentOwner.Activity(resolvedOwnerId, resolvedOwnerName);
                case RuntimeContentScope.Transient:
                    return RuntimeContentOwner.Transient(resolvedOwnerId, resolvedOwnerName);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Pause visual surface runtime scope must be explicit.");
            }
        }

        private static PauseContentRequirementPurpose ToRequirementPurpose(PauseVisualSurfaceKind kind)
        {
            switch (kind)
            {
                case PauseVisualSurfaceKind.OverlayRoot:
                    return PauseContentRequirementPurpose.PresentationRoot;
                case PauseVisualSurfaceKind.MenuRoot:
                    return PauseContentRequirementPurpose.MenuRoot;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Pause visual surface kind must map to a Pause content requirement purpose.");
            }
        }

        private static string GetPrefabResourcePath(GameObject prefab)
        {
            return prefab != null ? prefab.name : string.Empty;
        }
    }
}
