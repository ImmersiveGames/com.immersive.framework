using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Public diagnostic result for the authored, opt-in ContentAnchor materialization bridge.
    /// It deliberately exposes only status and diagnostic counters, not Unity object references as functional API.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-E authored ContentAnchor materialization bridge diagnostic result; physical Unity evidence remains adapter-side.")]
    public sealed class UnityContentAnchorMaterializationBridgeResult
    {
        private UnityContentAnchorMaterializationBridgeResult(
            UnityContentAnchorMaterializationBridgeStatus status,
            string operation,
            string source,
            string reason,
            string message,
            string pipelineStatus,
            string releaseStatus,
            int registryEntries,
            int registryActive,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int bindingRemovedCount,
            int contentHandleCount,
            bool materializationAttempted,
            bool physicalPlacementApplied,
            bool contentAnchorCreatesObject,
            bool contentAnchorDestroysObject,
            bool automaticLifecycleWiring,
            bool routeActivityAutoMaterialization,
            bool addressables,
            bool pooling,
            bool actorSpawn,
            bool playerJoin,
            bool gameplayConsumer,
            bool cameraConsumer,
            bool audioConsumer,
            bool saveConsumer)
        {
            if (!Enum.IsDefined(typeof(UnityContentAnchorMaterializationBridgeStatus), status)
                || status == UnityContentAnchorMaterializationBridgeStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "ContentAnchor materialization bridge status must be explicit.");
            }

            Status = status;
            Operation = operation.NormalizeTextOrFallback("Unknown");
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
            PipelineStatus = pipelineStatus.NormalizeText();
            ReleaseStatus = releaseStatus.NormalizeText();
            RegistryEntries = registryEntries;
            RegistryActive = registryActive;
            PhysicalReleaseRequests = physicalReleaseRequests;
            LogicalReleaseResults = logicalReleaseResults;
            BindingRemovedCount = bindingRemovedCount;
            ContentHandleCount = contentHandleCount;
            MaterializationAttempted = materializationAttempted;
            PhysicalPlacementApplied = physicalPlacementApplied;
            ContentAnchorCreatesObject = contentAnchorCreatesObject;
            ContentAnchorDestroysObject = contentAnchorDestroysObject;
            AutomaticLifecycleWiring = automaticLifecycleWiring;
            RouteActivityAutoMaterialization = routeActivityAutoMaterialization;
            Addressables = addressables;
            Pooling = pooling;
            ActorSpawn = actorSpawn;
            PlayerJoin = playerJoin;
            GameplayConsumer = gameplayConsumer;
            CameraConsumer = cameraConsumer;
            AudioConsumer = audioConsumer;
            SaveConsumer = saveConsumer;
        }

        public UnityContentAnchorMaterializationBridgeStatus Status { get; }

        public string Operation { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public string PipelineStatus { get; }

        public string ReleaseStatus { get; }

        public int RegistryEntries { get; }

        public int RegistryActive { get; }

        public int PhysicalReleaseRequests { get; }

        public int LogicalReleaseResults { get; }

        public int BindingRemovedCount { get; }

        public int ContentHandleCount { get; }

        public bool MaterializationAttempted { get; }

        public bool PhysicalPlacementApplied { get; }

        public bool ContentAnchorCreatesObject { get; }

        public bool ContentAnchorDestroysObject { get; }

        public bool AutomaticLifecycleWiring { get; }

        public bool RouteActivityAutoMaterialization { get; }

        public bool Addressables { get; }

        public bool Pooling { get; }

        public bool ActorSpawn { get; }

        public bool PlayerJoin { get; }

        public bool GameplayConsumer { get; }

        public bool CameraConsumer { get; }

        public bool AudioConsumer { get; }

        public bool SaveConsumer { get; }

        public bool Succeeded => Status == UnityContentAnchorMaterializationBridgeStatus.SucceededMaterialized
            || Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleased
            || Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleaseNoContent;

        public bool Failed => !Succeeded;

        public bool Materialized => Status == UnityContentAnchorMaterializationBridgeStatus.SucceededMaterialized;

        public bool Released => Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleased
            || Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleaseNoContent;

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string operationText = Operation.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            string pipelineText = PipelineStatus.ToDiagnosticText("None");
            string releaseText = ReleaseStatus.ToDiagnosticText("None");

            return $"status='{Status}' succeeded='{Succeeded}' operation='{operationText}' pipeline='{pipelineText}' release='{releaseText}' registryEntries='{RegistryEntries}' registryActive='{RegistryActive}' physicalReleaseRequests='{PhysicalReleaseRequests}' logicalReleaseResults='{LogicalReleaseResults}' bindingRemoved='{BindingRemovedCount}' contentHandles='{ContentHandleCount}' materializationAttempted='{MaterializationAttempted}' physicalPlacementApplied='{PhysicalPlacementApplied}' contentAnchorCreatesObject='{ContentAnchorCreatesObject}' contentAnchorDestroysObject='{ContentAnchorDestroysObject}' automaticLifecycleWiring='{AutomaticLifecycleWiring}' routeActivityAutoMaterialization='{RouteActivityAutoMaterialization}' addressables='{Addressables}' pooling='{Pooling}' actorSpawn='{ActorSpawn}' playerJoin='{PlayerJoin}' gameplayConsumer='{GameplayConsumer}' cameraConsumer='{CameraConsumer}' audioConsumer='{AudioConsumer}' saveConsumer='{SaveConsumer}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        internal static UnityContentAnchorMaterializationBridgeResult FromMaterialization(
            UnityContentAnchorMaterializationBridgeStatus status,
            UnityContentAnchorMaterializationPipelineResult pipelineResult,
            int registryEntries,
            int registryActive,
            int contentHandleCount,
            string source,
            string reason,
            string message)
        {
            return new UnityContentAnchorMaterializationBridgeResult(
                status,
                "Materialize",
                source,
                reason,
                message,
                pipelineResult.Status.ToString(),
                string.Empty,
                registryEntries,
                registryActive,
                0,
                0,
                0,
                contentHandleCount,
                pipelineResult.HasMaterializationResult,
                pipelineResult.PhysicalPlacementApplied,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false);
        }

        internal static UnityContentAnchorMaterializationBridgeResult FromMaterialization(
            UnityContentAnchorMaterializationBridgeStatus status,
            string pipelineStatus,
            bool materializationAttempted,
            bool physicalPlacementApplied,
            int registryEntries,
            int registryActive,
            int contentHandleCount,
            string source,
            string reason,
            string message)
        {
            return new UnityContentAnchorMaterializationBridgeResult(
                status,
                "Materialize",
                source,
                reason,
                message,
                pipelineStatus,
                string.Empty,
                registryEntries,
                registryActive,
                0,
                0,
                0,
                contentHandleCount,
                materializationAttempted,
                physicalPlacementApplied,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false);
        }

        internal static UnityContentAnchorMaterializationBridgeResult FromScopeRelease(
            UnityContentAnchorMaterializationBridgeStatus status,
            string releaseStatus,
            int registryEntries,
            int registryActive,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int bindingRemovedCount,
            int contentHandleCount,
            string source,
            string reason,
            string message)
        {
            return new UnityContentAnchorMaterializationBridgeResult(
                status,
                "ReleaseScope",
                source,
                reason,
                message,
                string.Empty,
                releaseStatus,
                registryEntries,
                registryActive,
                physicalReleaseRequests,
                logicalReleaseResults,
                bindingRemovedCount,
                contentHandleCount,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false);
        }

        internal static UnityContentAnchorMaterializationBridgeResult Failure(
            UnityContentAnchorMaterializationBridgeStatus status,
            string operation,
            string pipelineStatus,
            string releaseStatus,
            int registryEntries,
            int registryActive,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int bindingRemovedCount,
            int contentHandleCount,
            bool materializationAttempted,
            bool physicalPlacementApplied,
            string source,
            string reason,
            string message)
        {
            return new UnityContentAnchorMaterializationBridgeResult(
                status,
                operation,
                source,
                reason,
                message,
                pipelineStatus,
                releaseStatus,
                registryEntries,
                registryActive,
                physicalReleaseRequests,
                logicalReleaseResults,
                bindingRemovedCount,
                contentHandleCount,
                materializationAttempted,
                physicalPlacementApplied,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false);
        }
    }
}
