using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Read-only diagnostics snapshot for an authored ContentAnchor materialization bridge set.
    /// It is a query surface only: creating a snapshot does not materialize, release, bind, place or mutate Unity objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-J authored ContentAnchor bridge set diagnostics snapshot; query-only, no runtime side effects.")]
    public sealed class UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot
    {
        private UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot(
            string source,
            string message,
            int bridgeCount,
            int registryEntries,
            int registryActive,
            int physicalReleaseRequests,
            int contentHandleCount,
            bool hasLastMaterializeAllResult,
            bool hasLastReleaseAllResult,
            UnityContentAnchorMaterializationBridgeSetStatus lastMaterializeAllStatus,
            UnityContentAnchorMaterializationBridgeSetStatus lastReleaseAllStatus,
            int lastMaterializedCount,
            int lastReleasedCount,
            int lastFailedCount,
            UnityContentAnchorMaterializationAuthoringValidationStatus authoringStatus,
            int authoringBlockingIssues,
            bool authoredBridgeSet,
            bool explicitSubmit,
            bool runtimeUsesAuthoringValidation,
            bool batchPreflight,
            bool noRuntimeSideEffects,
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
            Source = source.NormalizeText();
            Message = message.NormalizeText();
            BridgeCount = bridgeCount;
            RegistryEntries = registryEntries;
            RegistryActive = registryActive;
            PhysicalReleaseRequests = physicalReleaseRequests;
            ContentHandleCount = contentHandleCount;
            HasLastMaterializeAllResult = hasLastMaterializeAllResult;
            HasLastReleaseAllResult = hasLastReleaseAllResult;
            LastMaterializeAllStatus = lastMaterializeAllStatus;
            LastReleaseAllStatus = lastReleaseAllStatus;
            LastMaterializedCount = lastMaterializedCount;
            LastReleasedCount = lastReleasedCount;
            LastFailedCount = lastFailedCount;
            AuthoringStatus = authoringStatus;
            AuthoringBlockingIssues = authoringBlockingIssues;
            AuthoredBridgeSet = authoredBridgeSet;
            ExplicitSubmit = explicitSubmit;
            RuntimeUsesAuthoringValidation = runtimeUsesAuthoringValidation;
            BatchPreflight = batchPreflight;
            NoRuntimeSideEffects = noRuntimeSideEffects;
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

        public string Source { get; }

        public string Message { get; }

        public int BridgeCount { get; }

        public int RegistryEntries { get; }

        public int RegistryActive { get; }

        public int PhysicalReleaseRequests { get; }

        public int ContentHandleCount { get; }

        public bool HasLastMaterializeAllResult { get; }

        public bool HasLastReleaseAllResult { get; }

        public UnityContentAnchorMaterializationBridgeSetStatus LastMaterializeAllStatus { get; }

        public UnityContentAnchorMaterializationBridgeSetStatus LastReleaseAllStatus { get; }

        public int LastMaterializedCount { get; }

        public int LastReleasedCount { get; }

        public int LastFailedCount { get; }

        public UnityContentAnchorMaterializationAuthoringValidationStatus AuthoringStatus { get; }

        public int AuthoringBlockingIssues { get; }

        public bool AuthoredBridgeSet { get; }

        public bool ExplicitSubmit { get; }

        public bool RuntimeUsesAuthoringValidation { get; }

        public bool BatchPreflight { get; }

        public bool NoRuntimeSideEffects { get; }

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

        public bool AuthoringValid => AuthoringStatus == UnityContentAnchorMaterializationAuthoringValidationStatus.Succeeded;

        public bool HasRuntimeContent => RegistryEntries > 0 || RegistryActive > 0 || ContentHandleCount > 0;

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"source='{sourceText}' bridgeCount='{BridgeCount}' registryEntries='{RegistryEntries}' registryActive='{RegistryActive}' physicalReleaseRequests='{PhysicalReleaseRequests}' contentHandles='{ContentHandleCount}' hasLastMaterializeAll='{HasLastMaterializeAllResult}' hasLastReleaseAll='{HasLastReleaseAllResult}' lastMaterializeAll='{LastMaterializeAllStatus}' lastReleaseAll='{LastReleaseAllStatus}' lastMaterialized='{LastMaterializedCount}' lastReleased='{LastReleasedCount}' lastFailed='{LastFailedCount}' authoringStatus='{AuthoringStatus}' authoringValid='{AuthoringValid}' authoringBlockingIssues='{AuthoringBlockingIssues}' hasRuntimeContent='{HasRuntimeContent}' authoredBridgeSet='{AuthoredBridgeSet}' explicitSubmit='{ExplicitSubmit}' runtimeUsesAuthoringValidation='{RuntimeUsesAuthoringValidation}' batchPreflight='{BatchPreflight}' noRuntimeSideEffects='{NoRuntimeSideEffects}' automaticLifecycleWiring='{AutomaticLifecycleWiring}' routeActivityAutoMaterialization='{RouteActivityAutoMaterialization}' addressables='{Addressables}' pooling='{Pooling}' actorSpawn='{ActorSpawn}' playerJoin='{PlayerJoin}' gameplayConsumer='{GameplayConsumer}' cameraConsumer='{CameraConsumer}' audioConsumer='{AudioConsumer}' saveConsumer='{SaveConsumer}' message='{messageText}'";
        }

        internal static UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot Create(
            string source,
            UnityContentAnchorMaterializationBridgeSet bridgeSet,
            UnityContentAnchorMaterializationAuthoringValidationResult authoringValidation)
        {
            if (bridgeSet == null)
            {
                return new UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot(
                    source,
                    "Content Anchor materialization bridge set diagnostics snapshot failed because the bridge set is missing.",
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    UnityContentAnchorMaterializationBridgeSetStatus.Unknown,
                    UnityContentAnchorMaterializationBridgeSetStatus.Unknown,
                    0,
                    0,
                    0,
                    UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeSetMissing,
                    1,
                    true,
                    true,
                    true,
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

            var materializeResult = bridgeSet.LastMaterializeAllResult;
            var releaseResult = bridgeSet.LastReleaseAllResult;
            int contentHandleCount = releaseResult != null
                ? releaseResult.ContentHandleCount
                : materializeResult != null
                    ? materializeResult.ContentHandleCount
                    : 0;
            int failedCount = 0;
            if (materializeResult != null)
            {
                failedCount += materializeResult.FailedCount;
            }

            if (releaseResult != null)
            {
                failedCount += releaseResult.FailedCount;
            }

            var authoringStatus = authoringValidation != null
                ? authoringValidation.Status
                : UnityContentAnchorMaterializationAuthoringValidationStatus.Unknown;
            int blockingIssues = authoringValidation != null ? authoringValidation.BlockingIssueCount : 0;
            string message = authoringValidation != null
                ? authoringValidation.Message.NormalizeTextOrFallback("Content Anchor materialization bridge set diagnostics snapshot completed.")
                : "Content Anchor materialization bridge set diagnostics snapshot completed without authoring validation details.";

            return new UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot(
                source,
                message,
                bridgeSet.BridgeCount,
                bridgeSet.RegistryEntries,
                bridgeSet.RegistryActive,
                bridgeSet.PhysicalReleaseRequestedCount,
                contentHandleCount,
                bridgeSet.HasLastMaterializeAllResult,
                bridgeSet.HasLastReleaseAllResult,
                materializeResult != null ? materializeResult.Status : UnityContentAnchorMaterializationBridgeSetStatus.Unknown,
                releaseResult != null ? releaseResult.Status : UnityContentAnchorMaterializationBridgeSetStatus.Unknown,
                materializeResult != null ? materializeResult.MaterializedCount : 0,
                releaseResult != null ? releaseResult.ReleasedCount : 0,
                failedCount,
                authoringStatus,
                blockingIssues,
                true,
                true,
                true,
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
