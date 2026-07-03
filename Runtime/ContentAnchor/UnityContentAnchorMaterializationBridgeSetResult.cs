using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Diagnostic result for explicit authored ContentAnchor bridge set submissions.
    /// It aggregates bridge-level results without exposing Unity object references as functional API.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-F authored ContentAnchor bridge set diagnostic result; aggregates explicit bridge calls only.")]
    public sealed class UnityContentAnchorMaterializationBridgeSetResult
    {
        private UnityContentAnchorMaterializationBridgeSetResult(
            UnityContentAnchorMaterializationBridgeSetStatus status,
            string operation,
            string source,
            string reason,
            string message,
            int bridgeCount,
            int materializedCount,
            int releasedCount,
            int failedCount,
            int registryEntries,
            int registryActive,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int bindingRemovedCount,
            int contentHandleCount,
            bool authoredBridgeSet,
            bool explicitSubmit,
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
            if (!Enum.IsDefined(typeof(UnityContentAnchorMaterializationBridgeSetStatus), status)
                || status == UnityContentAnchorMaterializationBridgeSetStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "ContentAnchor materialization bridge set status must be explicit.");
            }

            Status = status;
            Operation = operation.NormalizeTextOrFallback("Unknown");
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
            BridgeCount = bridgeCount;
            MaterializedCount = materializedCount;
            ReleasedCount = releasedCount;
            FailedCount = failedCount;
            RegistryEntries = registryEntries;
            RegistryActive = registryActive;
            PhysicalReleaseRequests = physicalReleaseRequests;
            LogicalReleaseResults = logicalReleaseResults;
            BindingRemovedCount = bindingRemovedCount;
            ContentHandleCount = contentHandleCount;
            AuthoredBridgeSet = authoredBridgeSet;
            ExplicitSubmit = explicitSubmit;
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

        public UnityContentAnchorMaterializationBridgeSetStatus Status { get; }

        public string Operation { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public int BridgeCount { get; }

        public int MaterializedCount { get; }

        public int ReleasedCount { get; }

        public int FailedCount { get; }

        public int RegistryEntries { get; }

        public int RegistryActive { get; }

        public int PhysicalReleaseRequests { get; }

        public int LogicalReleaseResults { get; }

        public int BindingRemovedCount { get; }

        public int ContentHandleCount { get; }

        public bool AuthoredBridgeSet { get; }

        public bool ExplicitSubmit { get; }

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

        public bool Succeeded => Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
            || Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
            || Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleaseNoContent;

        public bool Failed => !Succeeded;

        public bool MaterializedAll => Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll;

        public bool ReleasedAll => Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
            || Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleaseNoContent;

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
            return $"status='{Status}' succeeded='{Succeeded}' operation='{operationText}' bridgeCount='{BridgeCount}' materialized='{MaterializedCount}' released='{ReleasedCount}' failed='{FailedCount}' registryEntries='{RegistryEntries}' registryActive='{RegistryActive}' physicalReleaseRequests='{PhysicalReleaseRequests}' logicalReleaseResults='{LogicalReleaseResults}' bindingRemoved='{BindingRemovedCount}' contentHandles='{ContentHandleCount}' authoredBridgeSet='{AuthoredBridgeSet}' explicitSubmit='{ExplicitSubmit}' automaticLifecycleWiring='{AutomaticLifecycleWiring}' routeActivityAutoMaterialization='{RouteActivityAutoMaterialization}' addressables='{Addressables}' pooling='{Pooling}' actorSpawn='{ActorSpawn}' playerJoin='{PlayerJoin}' gameplayConsumer='{GameplayConsumer}' cameraConsumer='{CameraConsumer}' audioConsumer='{AudioConsumer}' saveConsumer='{SaveConsumer}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        internal static UnityContentAnchorMaterializationBridgeSetResult Create(
            UnityContentAnchorMaterializationBridgeSetStatus status,
            string operation,
            string source,
            string reason,
            string message,
            int bridgeCount,
            int materializedCount,
            int releasedCount,
            int failedCount,
            int registryEntries,
            int registryActive,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int bindingRemovedCount,
            int contentHandleCount)
        {
            return new UnityContentAnchorMaterializationBridgeSetResult(
                status,
                operation,
                source,
                reason,
                message,
                bridgeCount,
                materializedCount,
                releasedCount,
                failedCount,
                registryEntries,
                registryActive,
                physicalReleaseRequests,
                logicalReleaseResults,
                bindingRemovedCount,
                contentHandleCount,
                true,
                true,
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
