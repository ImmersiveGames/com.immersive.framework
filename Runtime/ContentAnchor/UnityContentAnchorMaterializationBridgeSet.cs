using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Explicit authored set for submitting multiple ContentAnchor materialization bridges.
    /// It batches authored bridge calls only; it does not subscribe to Route/Activity lifecycle or discover bridges implicitly at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Content Anchor/Unity Content Anchor Materialization Bridge Set")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-L authored opt-in ContentAnchor bridge set proof; explicit batch submit/release with partial materialization rollback, no automatic lifecycle wiring.")]
    public sealed class UnityContentAnchorMaterializationBridgeSet : MonoBehaviour
    {
        private const string DefaultSource = nameof(UnityContentAnchorMaterializationBridgeSet);
        private const string DefaultReason = "content-anchor.materialization.bridge-set";

        private FrameworkLogger _logger;
        private UnityContentAnchorMaterializationBridgeSetResult _lastMaterializeAllResult;
        private UnityContentAnchorMaterializationBridgeSetResult _lastReleaseAllResult;

        [SerializeField] private UnityContentAnchorMaterializationBridge[] bridges = System.Array.Empty<UnityContentAnchorMaterializationBridge>();
        [SerializeField] private string reason = DefaultReason;
        [SerializeField] private bool logResults = true;

        public UnityContentAnchorMaterializationBridgeSetResult LastMaterializeAllResult => _lastMaterializeAllResult;

        public UnityContentAnchorMaterializationBridgeSetResult LastReleaseAllResult => _lastReleaseAllResult;

        public bool HasLastMaterializeAllResult => _lastMaterializeAllResult != null;

        public bool HasLastReleaseAllResult => _lastReleaseAllResult != null;

        public int BridgeCount => bridges != null ? bridges.Length : 0;

        public int RegistryEntries => SumRegistryEntries();

        public int RegistryActive => SumRegistryActive();

        public int PhysicalReleaseRequestedCount => SumPhysicalReleaseRequests();

        public UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot CreateDiagnosticsSnapshot()
        {
            return CreateDiagnosticsSnapshot(DefaultSource);
        }

        internal UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot CreateDiagnosticsSnapshotForDiagnostics(string source)
        {
            return CreateDiagnosticsSnapshot(source);
        }

        public UnityContentAnchorMaterializationBridge[] CopyBridgesForAuthoringValidation()
        {
            if (bridges == null || bridges.Length == 0)
            {
                return System.Array.Empty<UnityContentAnchorMaterializationBridge>();
            }

            var copy = new UnityContentAnchorMaterializationBridge[bridges.Length];
            System.Array.Copy(bridges, copy, bridges.Length);
            return copy;
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<UnityContentAnchorMaterializationBridgeSet>();
        }

        [ContextMenu("Immersive Framework/Content Anchor/Materialize All Bridges")]
        public void MaterializeAllBridges()
        {
            _lastMaterializeAllResult = SubmitMaterializeAll(DefaultSource, ResolveReason("content-anchor.materialization.bridge-set.materialize-all"));
            LogResult("Content Anchor Materialization Bridge Set materialize completed.", _lastMaterializeAllResult);
        }

        [ContextMenu("Immersive Framework/Content Anchor/Release All Bridges")]
        public void ReleaseAllBridges()
        {
            _lastReleaseAllResult = SubmitReleaseAll(DefaultSource, ResolveReason("content-anchor.materialization.bridge-set.release-all"));
            LogResult("Content Anchor Materialization Bridge Set release completed.", _lastReleaseAllResult);
        }

        [ContextMenu("Immersive Framework/Content Anchor/Log Bridge Set Diagnostics Snapshot")]
        public void LogDiagnosticsSnapshot()
        {
            var snapshot = CreateDiagnosticsSnapshot(DefaultSource);
            EnsureLogger();
            _logger.Info(
                "Content Anchor Materialization Bridge Set diagnostics snapshot created.",
                LogFields.Of(
                    LogFields.Field("bridgeCount", snapshot.BridgeCount),
                    LogFields.Field("registryEntries", snapshot.RegistryEntries),
                    LogFields.Field("registryActive", snapshot.RegistryActive),
                    LogFields.Field("physicalReleaseRequests", snapshot.PhysicalReleaseRequests),
                    LogFields.Field("contentHandles", snapshot.ContentHandleCount),
                    LogFields.Field("authoringStatus", snapshot.AuthoringStatus.ToString()),
                    LogFields.Field("diagnostics", snapshot.ToDiagnosticString())));
        }

        internal void ConfigureForDiagnostics(UnityContentAnchorMaterializationBridge[] configuredBridges, bool configuredLogResults)
        {
            bridges = configuredBridges ?? System.Array.Empty<UnityContentAnchorMaterializationBridge>();
            logResults = configuredLogResults;
        }

        internal UnityContentAnchorMaterializationBridgeSetResult SubmitMaterializeAllForDiagnostics(string source, string requestReason)
        {
            _lastMaterializeAllResult = SubmitMaterializeAll(source, requestReason);
            return _lastMaterializeAllResult;
        }

        internal UnityContentAnchorMaterializationBridgeSetResult SubmitReleaseAllForDiagnostics(string source, string requestReason)
        {
            _lastReleaseAllResult = SubmitReleaseAll(source, requestReason);
            return _lastReleaseAllResult;
        }

        private UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot CreateDiagnosticsSnapshot(string source)
        {
            string resolvedSource = source.NormalizeTextOrFallback(DefaultSource);
            var validation = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridgeSet(
                this,
                "DiagnosticsSnapshot");
            return UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot.Create(
                resolvedSource,
                this,
                validation);
        }

        private UnityContentAnchorMaterializationBridgeSetResult SubmitMaterializeAll(string source, string requestReason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(DefaultSource);
            string resolvedReason = requestReason.NormalizeTextOrFallback(ResolveReason("content-anchor.materialization.bridge-set.materialize-all"));

            if (!TryValidateBridges(resolvedSource, resolvedReason, out var failure))
            {
                return failure;
            }

            if (!TryValidateAuthoringForMaterialization(resolvedSource, resolvedReason, out var authoringFailure))
            {
                return authoringFailure;
            }

            if (!TryPreflightMaterializeAll(resolvedSource, resolvedReason, out var preflightFailure))
            {
                return preflightFailure;
            }

            int materializedCount = 0;
            int failedCount = 0;
            string message = "All authored ContentAnchor materialization bridges were materialized.";
            var materializedBridges = new List<UnityContentAnchorMaterializationBridge>();

            for (int i = 0; i < bridges.Length; i++)
            {
                var bridge = bridges[i];
                var result = bridge.SubmitMaterializationForDiagnostics(
                    resolvedSource,
                    resolvedReason + ".bridge." + i);
                if (!result.Succeeded)
                {
                    failedCount++;
                    message = result.Message.NormalizeTextOrFallback("A ContentAnchor materialization bridge failed to materialize.");

                    if (materializedBridges.Count == 0)
                    {
                        return CreateResult(
                            UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterialization,
                            "MaterializeAll",
                            resolvedSource,
                            resolvedReason,
                            message,
                            materializedCount,
                            0,
                            failedCount);
                    }

                    RollbackMaterializedBridges(
                        materializedBridges,
                        resolvedSource,
                        resolvedReason,
                        out int rollbackReleasedCount,
                        out int rollbackFailedCount,
                        out string rollbackMessage);

                    failedCount += rollbackFailedCount;
                    var rollbackStatus = rollbackFailedCount == 0
                        ? UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterializationRolledBack
                        : UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterializationRollbackFailed;
                    string rollbackSummary = rollbackMessage.NormalizeTextOrFallback(
                        rollbackFailedCount == 0
                            ? "Partial bridge set materialization was rolled back."
                            : "Partial bridge set materialization rollback failed.");

                    return CreateResult(
                        rollbackStatus,
                        "MaterializeAllRollback",
                        resolvedSource,
                        resolvedReason,
                        message + " " + rollbackSummary,
                        materializedCount,
                        rollbackReleasedCount,
                        failedCount);
                }

                materializedCount++;
                materializedBridges.Add(bridge);
            }

            return CreateResult(
                UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll,
                "MaterializeAll",
                resolvedSource,
                resolvedReason,
                message,
                materializedCount,
                0,
                failedCount);
        }

        private UnityContentAnchorMaterializationBridgeSetResult SubmitReleaseAll(string source, string requestReason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(DefaultSource);
            string resolvedReason = requestReason.NormalizeTextOrFallback(ResolveReason("content-anchor.materialization.bridge-set.release-all"));

            if (!TryValidateBridges(resolvedSource, resolvedReason, out var failure))
            {
                return failure;
            }

            int releasedCount = 0;
            int failedCount = 0;
            int beforeReleaseRequests = SumPhysicalReleaseRequests();
            int beforeActive = SumRegistryActive();
            string message = "All authored ContentAnchor materialization bridges were released.";

            for (int i = 0; i < bridges.Length; i++)
            {
                var bridge = bridges[i];
                var result = bridge.SubmitScopeReleaseForDiagnostics(
                    resolvedSource,
                    resolvedReason + ".bridge." + i);
                if (!result.Succeeded)
                {
                    failedCount++;
                    message = result.Message.NormalizeTextOrFallback("A ContentAnchor materialization bridge failed to release.");
                    return CreateResult(
                        UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeRelease,
                        "ReleaseAll",
                        resolvedSource,
                        resolvedReason,
                        message,
                        0,
                        releasedCount,
                        failedCount);
                }

                if (result.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleased)
                {
                    releasedCount++;
                }
            }

            var status = beforeActive > 0 || SumPhysicalReleaseRequests() > beforeReleaseRequests
                ? UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                : UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleaseNoContent;
            if (status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleaseNoContent)
            {
                message = "No authored ContentAnchor materialization bridge had active content to release.";
            }

            return CreateResult(
                status,
                "ReleaseAll",
                resolvedSource,
                resolvedReason,
                message,
                0,
                releasedCount,
                failedCount);
        }

        private void RollbackMaterializedBridges(
            IReadOnlyList<UnityContentAnchorMaterializationBridge> materializedBridges,
            string source,
            string requestReason,
            out int releasedCount,
            out int failedCount,
            out string message)
        {
            releasedCount = 0;
            failedCount = 0;
            message = string.Empty;

            if (materializedBridges == null || materializedBridges.Count == 0)
            {
                message = "No partial ContentAnchor materialization bridge set entries required rollback.";
                return;
            }

            for (int i = materializedBridges.Count - 1; i >= 0; i--)
            {
                var bridge = materializedBridges[i];
                if (bridge == null)
                {
                    failedCount++;
                    message = "Content Anchor materialization bridge set rollback encountered a missing bridge reference.";
                    continue;
                }

                var rollbackResult = bridge.SubmitScopeReleaseForDiagnostics(
                    source,
                    requestReason + ".rollback.bridge." + i);
                if (!rollbackResult.Succeeded)
                {
                    failedCount++;
                    message = rollbackResult.Message.NormalizeTextOrFallback("A ContentAnchor materialization bridge set rollback release failed.");
                    continue;
                }

                if (rollbackResult.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleased)
                {
                    releasedCount++;
                }
            }

            if (failedCount == 0)
            {
                message = "Partial ContentAnchor materialization bridge set entries were rolled back.";
            }
            else if (string.IsNullOrWhiteSpace(message))
            {
                message = "Partial ContentAnchor materialization bridge set rollback failed.";
            }
        }

        private bool TryValidateAuthoringForMaterialization(
            string source,
            string requestReason,
            out UnityContentAnchorMaterializationBridgeSetResult failure)
        {
            failure = null;
            var validation = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridgeSet(
                this,
                "RuntimeMaterializeAll");
            if (validation.Succeeded)
            {
                return true;
            }

            failure = CreateResult(
                UnityContentAnchorMaterializationBridgeSetStatus.FailedAuthoringValidation,
                "AuthoringValidation",
                source,
                requestReason,
                validation.ToDiagnosticString(),
                0,
                0,
                validation.BlockingIssueCount > 0 ? validation.BlockingIssueCount : 1);
            return false;
        }

        private bool TryPreflightMaterializeAll(
            string source,
            string requestReason,
            out UnityContentAnchorMaterializationBridgeSetResult failure)
        {
            failure = null;
            var materializationKeys = new HashSet<string>();
            for (int i = 0; i < bridges.Length; i++)
            {
                var bridge = bridges[i];
                string materializationKey = bridge.BridgeSetPreflightKey.NormalizeTextOrFallback("bridge." + i);
                if (!materializationKeys.Add(materializationKey))
                {
                    failure = CreateResult(
                        UnityContentAnchorMaterializationBridgeSetStatus.FailedDuplicateMaterializationKey,
                        "MaterializeAllPreflight",
                        source,
                        requestReason,
                        "Content Anchor materialization bridge set contains duplicate runtime materialization keys.",
                        0,
                        0,
                        1);
                    return false;
                }

                if (!bridge.TryPreflightMaterializationForBridgeSet(
                        source,
                        requestReason + ".preflight.bridge." + i,
                        out string message))
                {
                    failure = CreateResult(
                        UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterializationPreflight,
                        "MaterializeAllPreflight",
                        source,
                        requestReason,
                        message.NormalizeTextOrFallback("A ContentAnchor materialization bridge failed preflight before batch side effects."),
                        0,
                        0,
                        1);
                    return false;
                }
            }

            return true;
        }

        private bool TryValidateBridges(
            string source,
            string requestReason,
            out UnityContentAnchorMaterializationBridgeSetResult failure)
        {
            failure = null;
            if (bridges == null || bridges.Length == 0)
            {
                failure = CreateResult(
                    UnityContentAnchorMaterializationBridgeSetStatus.FailedNoBridges,
                    "Validate",
                    source,
                    requestReason,
                    "Content Anchor materialization bridge set requires at least one explicit bridge reference.",
                    0,
                    0,
                    1);
                return false;
            }

            var unique = new HashSet<UnityContentAnchorMaterializationBridge>();
            for (int i = 0; i < bridges.Length; i++)
            {
                var bridge = bridges[i];
                if (bridge == null)
                {
                    failure = CreateResult(
                        UnityContentAnchorMaterializationBridgeSetStatus.FailedNullBridge,
                        "Validate",
                        source,
                        requestReason,
                        "Content Anchor materialization bridge set contains a missing bridge reference.",
                        0,
                        0,
                        1);
                    return false;
                }

                if (!unique.Add(bridge))
                {
                    failure = CreateResult(
                        UnityContentAnchorMaterializationBridgeSetStatus.FailedDuplicateBridge,
                        "Validate",
                        source,
                        requestReason,
                        "Content Anchor materialization bridge set contains the same bridge reference more than once.",
                        0,
                        0,
                        1);
                    return false;
                }
            }

            return true;
        }

        private UnityContentAnchorMaterializationBridgeSetResult CreateResult(
            UnityContentAnchorMaterializationBridgeSetStatus status,
            string operation,
            string source,
            string requestReason,
            string message,
            int materializedCount,
            int releasedCount,
            int failedCount)
        {
            return UnityContentAnchorMaterializationBridgeSetResult.Create(
                status,
                operation,
                source,
                requestReason,
                message,
                BridgeCount,
                materializedCount,
                releasedCount,
                failedCount,
                SumRegistryEntries(),
                SumRegistryActive(),
                SumPhysicalReleaseRequests(),
                SumLogicalReleaseResults(),
                SumBindingRemovedCount(),
                SumContentHandleCount());
        }

        private int SumRegistryEntries()
        {
            int count = 0;
            if (bridges == null)
            {
                return 0;
            }

            for (int i = 0; i < bridges.Length; i++)
            {
                if (bridges[i] != null)
                {
                    count += bridges[i].RegistryCount;
                }
            }

            return count;
        }

        private int SumRegistryActive()
        {
            int count = 0;
            if (bridges == null)
            {
                return 0;
            }

            for (int i = 0; i < bridges.Length; i++)
            {
                if (bridges[i] != null)
                {
                    count += bridges[i].RegistryActiveCount;
                }
            }

            return count;
        }

        private int SumPhysicalReleaseRequests()
        {
            int count = 0;
            if (bridges == null)
            {
                return 0;
            }

            for (int i = 0; i < bridges.Length; i++)
            {
                if (bridges[i] != null)
                {
                    count += bridges[i].PhysicalReleaseRequestedCount;
                }
            }

            return count;
        }

        private int SumLogicalReleaseResults()
        {
            int count = 0;
            if (bridges == null)
            {
                return 0;
            }

            for (int i = 0; i < bridges.Length; i++)
            {
                if (bridges[i] != null && bridges[i].HasLastReleaseResult)
                {
                    count += bridges[i].LastReleaseResult.LogicalReleaseResults;
                }
            }

            return count;
        }

        private int SumBindingRemovedCount()
        {
            int count = 0;
            if (bridges == null)
            {
                return 0;
            }

            for (int i = 0; i < bridges.Length; i++)
            {
                if (bridges[i] != null && bridges[i].HasLastReleaseResult)
                {
                    count += bridges[i].LastReleaseResult.BindingRemovedCount;
                }
            }

            return count;
        }

        private int SumContentHandleCount()
        {
            int count = 0;
            if (bridges == null)
            {
                return 0;
            }

            for (int i = 0; i < bridges.Length; i++)
            {
                if (bridges[i] != null)
                {
                    int handles = 0;
                    if (bridges[i].HasLastMaterializationResult)
                    {
                        handles = bridges[i].LastMaterializationResult.ContentHandleCount;
                    }

                    if (bridges[i].HasLastReleaseResult)
                    {
                        handles = bridges[i].LastReleaseResult.ContentHandleCount;
                    }

                    count += handles;
                }
            }

            return count;
        }

        private string ResolveReason(string fallback)
        {
            return reason.NormalizeTextOrFallback(fallback.NormalizeTextOrFallback(DefaultReason));
        }

        private void LogResult(string message, UnityContentAnchorMaterializationBridgeSetResult result)
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
                    LogFields.Field("bridgeCount", result.BridgeCount),
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
            _logger ??= FrameworkLogger.Create<UnityContentAnchorMaterializationBridgeSet>();
        }
    }
}
