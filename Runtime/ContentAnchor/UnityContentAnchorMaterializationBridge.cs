using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Scene-authored, opt-in bridge for explicit prefab materialization under a ContentAnchor Transform.
    /// It composes the F8R-E/F9R-B/F9R-C/F9R-D proof adapters without adding automatic Route/Activity lifecycle wiring.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Content Anchor/Unity Content Anchor Materialization Bridge")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-E authored opt-in ContentAnchor materialization bridge proof; explicit calls only, no automatic lifecycle wiring.")]
    public sealed class UnityContentAnchorMaterializationBridge : MonoBehaviour
    {
        private const string DefaultSource = nameof(UnityContentAnchorMaterializationBridge);
        private const string DefaultReason = "content-anchor.materialization.bridge";
        private const string DefaultRuntimeOwnerId = "content-anchor.materialization.bridge.owner";
        private const string DefaultRuntimeOwnerName = "Content Anchor Materialization Bridge";
        private const string DefaultAnchorOwnerId = "content-anchor.materialization.bridge.anchor-owner";
        private const string DefaultAnchorId = "content-anchor.materialization.bridge.anchor";
        private const string DefaultRuntimeContentId = "content-anchor.materialization.bridge.content";
        private const string DefaultResourceKey = "content-anchor.materialization.bridge.prefab";

        private FrameworkLogger _logger;
        private UnityRuntimeMaterializedObjectRegistry _registry;
        private UnityContentAnchorMaterializationBridgeResult _lastMaterializationResult;
        private UnityContentAnchorMaterializationBridgeResult _lastReleaseResult;

        [Header("Unity Physical Inputs")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Transform anchorTransform;
        [SerializeField] private bool resetLocalTransform = true;

        [Header("Runtime Owner")]
        [SerializeField] private RuntimeContentScope runtimeScope = RuntimeContentScope.Transient;
        [SerializeField] private string runtimeOwnerId = DefaultRuntimeOwnerId;
        [SerializeField] private string runtimeOwnerName = DefaultRuntimeOwnerName;
        [SerializeField] private bool createScopeRootIfMissing = true;

        [Header("Content Anchor")]
        [SerializeField] private ContentAnchorScope anchorScope = ContentAnchorScope.Route;
        [SerializeField] private ContentAnchorKind anchorKind = ContentAnchorKind.Slot;
        [SerializeField] private ContentAnchorRequiredness anchorRequiredness = ContentAnchorRequiredness.Required;
        [SerializeField] private string anchorOwnerId = DefaultAnchorOwnerId;
        [SerializeField] private string anchorId = DefaultAnchorId;
        [SerializeField] private string anchorDisplayName = "Content Anchor Materialization Bridge Anchor";
        [SerializeField] private string anchorDescription = "Explicit bridge anchor declaration.";

        [Header("Runtime Content")]
        [SerializeField] private string runtimeContentId = DefaultRuntimeContentId;
        [SerializeField] private string resourceKey = DefaultResourceKey;
        [SerializeField] private RuntimeReleasePolicy releasePolicy = RuntimeReleasePolicy.MarkReleasedAndUnregister;

        [Header("Diagnostics")]
        [SerializeField] private string reason = DefaultReason;
        [SerializeField] private bool logResults = true;

        public GameObject Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        public Transform AnchorTransform
        {
            get => anchorTransform;
            set => anchorTransform = value;
        }

        public UnityContentAnchorMaterializationBridgeResult LastMaterializationResult => _lastMaterializationResult;

        public UnityContentAnchorMaterializationBridgeResult LastReleaseResult => _lastReleaseResult;

        public bool HasLastMaterializationResult => _lastMaterializationResult != null;

        public bool HasLastReleaseResult => _lastReleaseResult != null;

        public bool LastMaterializationSucceeded => _lastMaterializationResult != null && _lastMaterializationResult.Succeeded;

        public bool LastReleaseSucceeded => _lastReleaseResult != null && _lastReleaseResult.Succeeded;

        public int RegistryCount => Registry.Count;

        public int RegistryActiveCount => Registry.ActiveCount;

        public int PhysicalReleaseRequestedCount => Registry.PhysicalReleaseRequestedCount;

        public bool HasConfiguredPrefab => prefab != null;

        public bool HasConfiguredAnchorTransform => anchorTransform != null;

        public RuntimeContentScope ConfiguredRuntimeScope => runtimeScope;

        public ContentAnchorScope ConfiguredAnchorScope => anchorScope;

        public ContentAnchorKind ConfiguredAnchorKind => anchorKind;

        public ContentAnchorRequiredness ConfiguredAnchorRequiredness => anchorRequiredness;

        public RuntimeReleasePolicy ConfiguredReleasePolicy => releasePolicy;

        public string ConfiguredRuntimeOwnerId => runtimeOwnerId.NormalizeText();

        public string ConfiguredRuntimeOwnerName => runtimeOwnerName.NormalizeText();

        public string ConfiguredAnchorOwnerId => anchorOwnerId.NormalizeText();

        public string ConfiguredAnchorId => anchorId.NormalizeText();

        public string ConfiguredRuntimeContentId => runtimeContentId.NormalizeText();

        public string ConfiguredResourceKey => resourceKey.NormalizeText();

        public string ConfiguredReason => reason.NormalizeText();

        public string AuthoringMaterializationKey => BridgeSetPreflightKey;

        internal UnityRuntimeMaterializedObjectRegistry Registry => _registry ??= new UnityRuntimeMaterializedObjectRegistry();

        private void Awake()
        {
            _logger = FrameworkLogger.Create<UnityContentAnchorMaterializationBridge>();
        }

        [ContextMenu("Immersive Framework/Content Anchor/Materialize Prefab At Anchor")]
        public void MaterializePrefabAtAnchor()
        {
            _lastMaterializationResult = SubmitMaterialization(DefaultSource, ResolveReason("content-anchor.materialization.bridge.materialize"));
            LogResult("Content Anchor Materialization Bridge materialize completed.", _lastMaterializationResult);
        }

        [ContextMenu("Immersive Framework/Content Anchor/Release Bridge Scope")]
        public void ReleaseBridgeScope()
        {
            _lastReleaseResult = SubmitScopeRelease(DefaultSource, ResolveReason("content-anchor.materialization.bridge.release"));
            LogResult("Content Anchor Materialization Bridge release completed.", _lastReleaseResult);
        }

        internal UnityContentAnchorMaterializationBridgeResult SubmitMaterializationForDiagnostics(string source, string requestReason)
        {
            _lastMaterializationResult = SubmitMaterialization(source, requestReason);
            return _lastMaterializationResult;
        }

        internal UnityContentAnchorMaterializationBridgeResult SubmitScopeReleaseForDiagnostics(string source, string requestReason)
        {
            _lastReleaseResult = SubmitScopeRelease(source, requestReason);
            return _lastReleaseResult;
        }

        internal void ConfigureForDiagnostics(
            GameObject configuredPrefab,
            Transform configuredAnchorTransform,
            RuntimeContentScope configuredRuntimeScope,
            string configuredRuntimeOwnerId,
            string configuredRuntimeOwnerName,
            bool configuredCreateScopeRootIfMissing,
            ContentAnchorScope configuredAnchorScope,
            ContentAnchorKind configuredAnchorKind,
            ContentAnchorRequiredness configuredAnchorRequiredness,
            string configuredAnchorOwnerId,
            string configuredAnchorId,
            string configuredRuntimeContentId,
            string configuredResourceKey,
            RuntimeReleasePolicy configuredReleasePolicy,
            bool configuredResetLocalTransform,
            bool configuredLogResults)
        {
            prefab = configuredPrefab;
            anchorTransform = configuredAnchorTransform;
            runtimeScope = configuredRuntimeScope;
            runtimeOwnerId = configuredRuntimeOwnerId.NormalizeTextOrFallback(DefaultRuntimeOwnerId);
            runtimeOwnerName = configuredRuntimeOwnerName.NormalizeTextOrFallback(DefaultRuntimeOwnerName);
            createScopeRootIfMissing = configuredCreateScopeRootIfMissing;
            anchorScope = configuredAnchorScope;
            anchorKind = configuredAnchorKind;
            anchorRequiredness = configuredAnchorRequiredness;
            anchorOwnerId = configuredAnchorOwnerId.NormalizeTextOrFallback(DefaultAnchorOwnerId);
            anchorId = configuredAnchorId.NormalizeTextOrFallback(DefaultAnchorId);
            runtimeContentId = configuredRuntimeContentId.NormalizeTextOrFallback(DefaultRuntimeContentId);
            resourceKey = configuredResourceKey.NormalizeTextOrFallback(DefaultResourceKey);
            releasePolicy = configuredReleasePolicy;
            resetLocalTransform = configuredResetLocalTransform;
            logResults = configuredLogResults;
        }

        internal string BridgeSetPreflightKey
        {
            get
            {
                string ownerId = runtimeOwnerId.NormalizeTextOrFallback(DefaultRuntimeOwnerId);
                string contentId = runtimeContentId.NormalizeTextOrFallback(DefaultRuntimeContentId);
                return runtimeScope + ":" + ownerId + ":" + contentId;
            }
        }

        internal bool TryPreflightMaterializationForBridgeSet(string source, string requestReason, out string message)
        {
            message = string.Empty;
            string resolvedSource = source.NormalizeTextOrFallback(DefaultSource);
            string resolvedReason = requestReason.NormalizeTextOrFallback(ResolveReason("content-anchor.materialization.bridge-set.preflight"));

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                message = "Content Anchor materialization bridge preflight requires FrameworkRuntimeHost.";
                return false;
            }

            if (runtimeHost.RuntimeContentRuntime == null)
            {
                message = "Content Anchor materialization bridge preflight requires RuntimeContentRuntime.";
                return false;
            }

            if (prefab == null)
            {
                message = "Content Anchor materialization bridge preflight requires an explicit prefab/template GameObject.";
                return false;
            }

            if (anchorTransform == null)
            {
                message = "Content Anchor materialization bridge preflight requires an explicit anchor Transform before materialization side effects.";
                return false;
            }

            if (!Enum.IsDefined(typeof(RuntimeContentScope), runtimeScope) || runtimeScope == RuntimeContentScope.Unknown)
            {
                message = "Content Anchor materialization bridge preflight requires an explicit RuntimeContent scope.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ContentAnchorScope), anchorScope) || anchorScope == ContentAnchorScope.Unknown)
            {
                message = "Content Anchor materialization bridge preflight requires an explicit ContentAnchor scope.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ContentAnchorKind), anchorKind) || anchorKind == ContentAnchorKind.Unknown)
            {
                message = "Content Anchor materialization bridge preflight requires an explicit ContentAnchor kind.";
                return false;
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), releasePolicy) || releasePolicy == RuntimeReleasePolicy.Unknown)
            {
                message = "Content Anchor materialization bridge preflight requires an explicit RuntimeContent release policy.";
                return false;
            }

            try
            {
                _ = CreateRuntimeOwner();
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }

            var declaration = ContentAnchorDeclaration.Create(
                ContentAnchorDeclaration.CreateOwnerKey(anchorScope, anchorOwnerId.NormalizeTextOrFallback(DefaultAnchorOwnerId)),
                anchorScope,
                anchorKind,
                anchorId.NormalizeTextOrFallback(DefaultAnchorId),
                anchorRequiredness,
                anchorDisplayName,
                anchorDescription,
                string.Empty,
                string.Empty);
            var anchorSet = ContentAnchorSet.FromDeclarations(new[] { declaration });
            if (!anchorSet.HasAnchors || anchorSet.HasIssues)
            {
                message = anchorSet.ToDiagnosticString();
                return false;
            }

            message = $"Content Anchor materialization bridge preflight succeeded. source='{resolvedSource.ToDiagnosticText()}' reason='{resolvedReason.ToDiagnosticText()}'.";
            return true;
        }

        private UnityContentAnchorMaterializationBridgeResult SubmitMaterialization(string source, string requestReason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(DefaultSource);
            string resolvedReason = requestReason.NormalizeTextOrFallback(ResolveReason("content-anchor.materialization.bridge.materialize"));

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                return Failure(
                    UnityContentAnchorMaterializationBridgeStatus.FailedRuntimeUnavailable,
                    "Materialize",
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Content Anchor materialization bridge requires FrameworkRuntimeHost.");
            }

            if (runtimeHost.RuntimeContentRuntime == null)
            {
                return Failure(
                    UnityContentAnchorMaterializationBridgeStatus.FailedRuntimeContentRuntimeUnavailable,
                    "Materialize",
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Content Anchor materialization bridge requires RuntimeContentRuntime.");
            }

            if (!TryBuildInputs(
                    runtimeHost,
                    resolvedSource,
                    resolvedReason,
                    out var context,
                    out var anchorSet,
                    out var bindingRequest,
                    out string buildMessage,
                    out var failureStatus))
            {
                return Failure(
                    failureStatus,
                    "Materialize",
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    CountHandles(runtimeHost, context),
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    buildMessage);
            }

            var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                prefab,
                Registry,
                null,
                resolvedSource);
            var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(Registry, resolvedSource);
            var placementAdapter = new UnityContentAnchorPlacementAdapter(resolvedSource);
            var service = new ContentAnchorMaterializationService(
                materializationAdapter,
                placementAdapter,
                releaseAdapter,
                resolvedSource);

            var materializationResult = service.MaterializeBindPlace(
                runtimeHost,
                anchorSet,
                bindingRequest,
                anchorTransform,
                resetLocalTransform,
                resolvedReason);

            if (!materializationResult.Succeeded)
            {
                return FromMaterialization(
                    UnityContentAnchorMaterializationBridgeStatus.FailedMaterializationPipeline,
                    materializationResult,
                    Registry.Count,
                    Registry.ActiveCount,
                    CountHandles(runtimeHost, context),
                    resolvedSource,
                    resolvedReason,
                    materializationResult.Message);
            }

            return FromMaterialization(
                UnityContentAnchorMaterializationBridgeStatus.SucceededMaterialized,
                materializationResult,
                Registry.Count,
                Registry.ActiveCount,
                CountHandles(runtimeHost, context),
                resolvedSource,
                resolvedReason,
                "Content Anchor materialization bridge materialized, logically bound and physically placed content.");
        }

        private UnityContentAnchorMaterializationBridgeResult SubmitScopeRelease(string source, string requestReason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(DefaultSource);
            string resolvedReason = requestReason.NormalizeTextOrFallback(ResolveReason("content-anchor.materialization.bridge.release"));

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                return Failure(
                    UnityContentAnchorMaterializationBridgeStatus.FailedRuntimeUnavailable,
                    "ReleaseScope",
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Content Anchor materialization bridge release requires FrameworkRuntimeHost.");
            }

            if (runtimeHost.RuntimeContentRuntime == null)
            {
                return Failure(
                    UnityContentAnchorMaterializationBridgeStatus.FailedRuntimeContentRuntimeUnavailable,
                    "ReleaseScope",
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Content Anchor materialization bridge release requires RuntimeContentRuntime.");
            }

            if (!TryResolveRuntimeContext(
                    runtimeHost.RuntimeContentRuntime,
                    resolvedSource,
                    resolvedReason,
                    out var context,
                    out string contextMessage,
                    out var status))
            {
                return Failure(
                    status,
                    "ReleaseScope",
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    contextMessage);
            }

            var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(Registry, resolvedSource);
            var scopeRelease = new UnityContentAnchorMaterializationScopeReleasePipeline(
                releaseAdapter,
                resolvedSource);
            var releaseResult = scopeRelease.ReleaseScope(
                runtimeHost,
                context,
                releasePolicy,
                resolvedReason);

            var bridgeStatus = releaseResult.Succeeded
                ? releaseResult.PhysicalReleaseRequests > 0 || releaseResult.BindingRemovedCount > 0
                    ? UnityContentAnchorMaterializationBridgeStatus.SucceededReleased
                    : UnityContentAnchorMaterializationBridgeStatus.SucceededReleaseNoContent
                : UnityContentAnchorMaterializationBridgeStatus.FailedScopeRelease;

            if (!releaseResult.Succeeded)
            {
                return UnityContentAnchorMaterializationBridgeResult.Failure(
                    bridgeStatus,
                    "ReleaseScope",
                    string.Empty,
                    releaseResult.Status.ToString(),
                    Registry.Count,
                    Registry.ActiveCount,
                    releaseResult.PhysicalReleaseRequests,
                    releaseResult.LogicalReleaseResults,
                    releaseResult.BindingRemovedCount,
                    CountHandles(runtimeHost, context),
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    releaseResult.Message);
            }

            return UnityContentAnchorMaterializationBridgeResult.FromScopeRelease(
                bridgeStatus,
                releaseResult.Status.ToString(),
                Registry.Count,
                Registry.ActiveCount,
                releaseResult.PhysicalReleaseRequests,
                releaseResult.LogicalReleaseResults,
                releaseResult.BindingRemovedCount,
                CountHandles(runtimeHost, context),
                resolvedSource,
                resolvedReason,
                releaseResult.Message);
        }

        private bool TryBuildInputs(
            FrameworkRuntimeHost runtimeHost,
            string source,
            string requestReason,
            out RuntimeScopeContext context,
            out ContentAnchorSet anchorSet,
            out ContentAnchorBindingRequest bindingRequest,
            out string message,
            out UnityContentAnchorMaterializationBridgeStatus status)
        {
            context = default;
            anchorSet = default;
            bindingRequest = default;
            message = string.Empty;
            status = UnityContentAnchorMaterializationBridgeStatus.FailedConfiguration;

            if (prefab == null)
            {
                message = "Content Anchor materialization bridge requires an explicit prefab/template GameObject.";
                return false;
            }

            if (anchorTransform == null)
            {
                message = "Content Anchor materialization bridge requires an explicit anchor Transform before materialization side effects.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ContentAnchorScope), anchorScope) || anchorScope == ContentAnchorScope.Unknown)
            {
                message = "Content Anchor materialization bridge requires an explicit ContentAnchor scope.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ContentAnchorKind), anchorKind) || anchorKind == ContentAnchorKind.Unknown)
            {
                message = "Content Anchor materialization bridge requires an explicit ContentAnchor kind.";
                return false;
            }

            if (!TryResolveRuntimeContext(runtimeHost.RuntimeContentRuntime, source, requestReason, out context, out message, out status))
            {
                return false;
            }

            var declaration = ContentAnchorDeclaration.Create(
                ContentAnchorDeclaration.CreateOwnerKey(anchorScope, anchorOwnerId.NormalizeTextOrFallback(DefaultAnchorOwnerId)),
                anchorScope,
                anchorKind,
                anchorId.NormalizeTextOrFallback(DefaultAnchorId),
                anchorRequiredness,
                anchorDisplayName,
                anchorDescription,
                string.Empty,
                string.Empty);
            anchorSet = ContentAnchorSet.FromDeclarations(new[] { declaration });
            if (!anchorSet.HasAnchors || anchorSet.HasIssues)
            {
                message = anchorSet.ToDiagnosticString();
                status = UnityContentAnchorMaterializationBridgeStatus.FailedAnchorSet;
                return false;
            }

            var resource = RuntimeMaterializationResource.From(
                UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                resourceKey.NormalizeTextOrFallback(DefaultResourceKey),
                prefab != null ? prefab.name : string.Empty,
                string.Empty);
            bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                context,
                declaration,
                runtimeContentId.NormalizeTextOrFallback(DefaultRuntimeContentId),
                resource,
                source,
                requestReason);
            return true;
        }

        private bool TryResolveRuntimeContext(
            RuntimeContentRuntime runtimeContentRuntime,
            string source,
            string requestReason,
            out RuntimeScopeContext context,
            out string message,
            out UnityContentAnchorMaterializationBridgeStatus status)
        {
            context = default;
            message = string.Empty;
            status = UnityContentAnchorMaterializationBridgeStatus.FailedConfiguration;

            RuntimeContentOwner owner;
            try
            {
                owner = CreateRuntimeOwner();
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }

            if (!runtimeContentRuntime.TryCreateScopeContext(owner, source, requestReason, out context))
            {
                if (!createScopeRootIfMissing)
                {
                    message = "Content Anchor materialization bridge requires an existing RuntimeContent scope root or explicit permission to create one.";
                    status = UnityContentAnchorMaterializationBridgeStatus.FailedScopeContext;
                    return false;
                }

                var createRootResult = runtimeContentRuntime.CreateScopeRoot(owner, source, requestReason);
                if (!createRootResult.Applied && createRootResult.Status != RuntimeRootRegistryOperationStatus.RootAlreadyExists)
                {
                    message = createRootResult.Message;
                    status = UnityContentAnchorMaterializationBridgeStatus.FailedScopeRoot;
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(owner, source, requestReason, out context))
                {
                    message = "Content Anchor materialization bridge failed to create a RuntimeContent scope context after root creation.";
                    status = UnityContentAnchorMaterializationBridgeStatus.FailedScopeContext;
                    return false;
                }
            }

            return true;
        }

        private RuntimeContentOwner CreateRuntimeOwner()
        {
            string ownerId = runtimeOwnerId.NormalizeTextOrFallback(DefaultRuntimeOwnerId);
            string ownerName = runtimeOwnerName.NormalizeTextOrFallback(DefaultRuntimeOwnerName);
            switch (runtimeScope)
            {
                case RuntimeContentScope.Session:
                    return RuntimeContentOwner.Session(ownerId, ownerName);
                case RuntimeContentScope.Route:
                    return RuntimeContentOwner.Route(ownerId, ownerName);
                case RuntimeContentScope.Activity:
                    return RuntimeContentOwner.Activity(ownerId, ownerName);
                case RuntimeContentScope.Transient:
                    return RuntimeContentOwner.Transient(ownerId, ownerName);
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtimeScope), runtimeScope, "Content Anchor materialization bridge requires an explicit RuntimeContent scope.");
            }
        }

        private int CountHandles(FrameworkRuntimeHost runtimeHost, RuntimeScopeContext context)
        {
            if (runtimeHost == null || runtimeHost.RuntimeContentRuntime == null || !context.IsValid)
            {
                return 0;
            }

            return runtimeHost.RuntimeContentRuntime.SnapshotHandles(context).Length;
        }

        private UnityContentAnchorMaterializationBridgeResult FromMaterialization(
            UnityContentAnchorMaterializationBridgeStatus status,
            ContentAnchorMaterializationResult materializationResult,
            int registryEntries,
            int registryActive,
            int contentHandleCount,
            string source,
            string requestReason,
            string message)
        {
            var pipelineStatus = UnityContentAnchorMaterializationPipeline.MapStatus(materializationResult.FailedStage);
            return UnityContentAnchorMaterializationBridgeResult.FromMaterialization(
                status,
                pipelineStatus.ToString(),
                materializationResult.HasMaterializationResult,
                materializationResult.PhysicalPlacementApplied,
                registryEntries,
                registryActive,
                contentHandleCount,
                source,
                requestReason,
                message);
        }

        private UnityContentAnchorMaterializationBridgeResult Failure(
            UnityContentAnchorMaterializationBridgeStatus status,
            string operation,
            string pipelineStatus,
            string releaseStatus,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int bindingRemovedCount,
            int contentHandleCount,
            bool materializationAttempted,
            bool physicalPlacementApplied,
            string source,
            string requestReason,
            string message)
        {
            return UnityContentAnchorMaterializationBridgeResult.Failure(
                status,
                operation,
                pipelineStatus,
                releaseStatus,
                Registry.Count,
                Registry.ActiveCount,
                physicalReleaseRequests,
                logicalReleaseResults,
                bindingRemovedCount,
                contentHandleCount,
                materializationAttempted,
                physicalPlacementApplied,
                source,
                requestReason,
                message);
        }

        private string ResolveReason(string fallback)
        {
            return reason.NormalizeTextOrFallback(fallback.NormalizeTextOrFallback(DefaultReason));
        }

        private void LogResult(string message, UnityContentAnchorMaterializationBridgeResult result)
        {
            if (!logResults || result == null)
            {
                return;
            }

            EnsureLogger();
            _logger.Info(
                message,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("operation", result.Operation),
                    LogFields.Field("registryEntries", result.RegistryEntries),
                    LogFields.Field("registryActive", result.RegistryActive),
                    LogFields.Field("physicalReleaseRequests", result.PhysicalReleaseRequests),
                    LogFields.Field("logicalReleaseResults", result.LogicalReleaseResults),
                    LogFields.Field("bindingRemoved", result.BindingRemovedCount),
                    LogFields.Field("contentHandles", result.ContentHandleCount),
                    LogFields.Field("diagnostics", result.ToDiagnosticString())));
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create<UnityContentAnchorMaterializationBridge>();
        }
    }
}
