using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    public static class UnityContentAnchorMaterializationAuthoringValidator
    {
        public static UnityContentAnchorMaterializationAuthoringValidationResult ValidateBridge(
            UnityContentAnchorMaterializationBridge bridge,
            string scope = "ContentAnchorMaterializationBridge")
        {
            if (bridge == null)
            {
                return UnityContentAnchorMaterializationAuthoringValidationResult.Create(
                    UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeMissing,
                    scope,
                    "Content Anchor materialization bridge is missing.",
                    0,
                    1,
                    0,
                    1,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0);
            }

            var counters = BridgeValidationCounters.FromBridge(bridge);
            var status = counters.BlockingIssueCount == 0
                ? UnityContentAnchorMaterializationAuthoringValidationStatus.Succeeded
                : UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeConfiguration;
            string message = counters.BlockingIssueCount == 0
                ? "Content Anchor materialization bridge authoring is valid for explicit submit/release."
                : "Content Anchor materialization bridge has blocking authoring issues.";

            return counters.ToResult(status, scope, message, 1, 0, 0);
        }

        public static UnityContentAnchorMaterializationAuthoringValidationResult ValidateBridgeSet(
            UnityContentAnchorMaterializationBridgeSet bridgeSet,
            string scope = "ContentAnchorMaterializationBridgeSet")
        {
            if (bridgeSet == null)
            {
                return UnityContentAnchorMaterializationAuthoringValidationResult.Create(
                    UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeSetMissing,
                    scope,
                    "Content Anchor materialization bridge set is missing.",
                    0,
                    1,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0);
            }

            var bridges = bridgeSet.CopyBridgesForAuthoringValidation();
            var counters = new BridgeValidationCounters();
            int bridgeCount = bridges.Length;
            if (bridgeCount == 0)
            {
                counters.BlockingIssueCount++;
            }

            var uniqueBridgeReferences = new HashSet<UnityContentAnchorMaterializationBridge>();
            var materializationKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < bridges.Length; i++)
            {
                var bridge = bridges[i];
                if (bridge == null)
                {
                    counters.NullBridgeCount++;
                    counters.BlockingIssueCount++;
                    continue;
                }

                if (!uniqueBridgeReferences.Add(bridge))
                {
                    counters.DuplicateBridgeCount++;
                    counters.BlockingIssueCount++;
                }

                string materializationKey = bridge.AuthoringMaterializationKey.NormalizeTextOrFallback("bridge." + i);
                if (!materializationKeys.Add(materializationKey))
                {
                    counters.DuplicateMaterializationKeyCount++;
                    counters.BlockingIssueCount++;
                }

                counters.Add(BridgeValidationCounters.FromBridge(bridge));
            }

            var status = counters.BlockingIssueCount == 0
                ? UnityContentAnchorMaterializationAuthoringValidationStatus.Succeeded
                : UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeSetConfiguration;
            string message = counters.BlockingIssueCount == 0
                ? "Content Anchor materialization bridge set authoring is valid for explicit batch submit/release."
                : "Content Anchor materialization bridge set has blocking authoring issues.";

            return counters.ToResult(status, scope, message, bridgeCount, 0, 0);
        }

        private struct BridgeValidationCounters
        {
            public int BlockingIssueCount;
            public int WarningCount;
            public int NullBridgeCount;
            public int MissingPrefabCount;
            public int MissingAnchorTransformCount;
            public int InvalidRuntimeScopeCount;
            public int InvalidAnchorScopeCount;
            public int InvalidAnchorKindCount;
            public int InvalidReleasePolicyCount;
            public int MissingRuntimeOwnerIdCount;
            public int MissingAnchorOwnerIdCount;
            public int MissingAnchorIdCount;
            public int MissingRuntimeContentIdCount;
            public int MissingResourceKeyCount;
            public int DuplicateBridgeCount;
            public int DuplicateMaterializationKeyCount;

            public static BridgeValidationCounters FromBridge(UnityContentAnchorMaterializationBridge bridge)
            {
                var counters = new BridgeValidationCounters();
                if (bridge == null)
                {
                    counters.NullBridgeCount++;
                    counters.BlockingIssueCount++;
                    return counters;
                }

                if (!bridge.HasConfiguredPrefab)
                {
                    counters.MissingPrefabCount++;
                    counters.BlockingIssueCount++;
                }

                if (!bridge.HasConfiguredAnchorTransform)
                {
                    counters.MissingAnchorTransformCount++;
                    counters.BlockingIssueCount++;
                }

                if (!Enum.IsDefined(typeof(RuntimeContentScope), bridge.ConfiguredRuntimeScope)
                    || bridge.ConfiguredRuntimeScope == RuntimeContentScope.Unknown)
                {
                    counters.InvalidRuntimeScopeCount++;
                    counters.BlockingIssueCount++;
                }

                if (!Enum.IsDefined(typeof(ContentAnchorScope), bridge.ConfiguredAnchorScope)
                    || bridge.ConfiguredAnchorScope == ContentAnchorScope.Unknown)
                {
                    counters.InvalidAnchorScopeCount++;
                    counters.BlockingIssueCount++;
                }

                if (!Enum.IsDefined(typeof(ContentAnchorKind), bridge.ConfiguredAnchorKind)
                    || bridge.ConfiguredAnchorKind == ContentAnchorKind.Unknown)
                {
                    counters.InvalidAnchorKindCount++;
                    counters.BlockingIssueCount++;
                }

                if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), bridge.ConfiguredReleasePolicy)
                    || bridge.ConfiguredReleasePolicy == RuntimeReleasePolicy.Unknown)
                {
                    counters.InvalidReleasePolicyCount++;
                    counters.BlockingIssueCount++;
                }

                if (string.IsNullOrWhiteSpace(bridge.ConfiguredRuntimeOwnerId))
                {
                    counters.MissingRuntimeOwnerIdCount++;
                    counters.BlockingIssueCount++;
                }

                if (string.IsNullOrWhiteSpace(bridge.ConfiguredAnchorOwnerId))
                {
                    counters.MissingAnchorOwnerIdCount++;
                    counters.BlockingIssueCount++;
                }

                if (string.IsNullOrWhiteSpace(bridge.ConfiguredAnchorId))
                {
                    counters.MissingAnchorIdCount++;
                    counters.BlockingIssueCount++;
                }

                if (string.IsNullOrWhiteSpace(bridge.ConfiguredRuntimeContentId))
                {
                    counters.MissingRuntimeContentIdCount++;
                    counters.BlockingIssueCount++;
                }

                if (string.IsNullOrWhiteSpace(bridge.ConfiguredResourceKey))
                {
                    counters.MissingResourceKeyCount++;
                    counters.BlockingIssueCount++;
                }

                return counters;
            }

            public void Add(BridgeValidationCounters other)
            {
                BlockingIssueCount += other.BlockingIssueCount;
                WarningCount += other.WarningCount;
                NullBridgeCount += other.NullBridgeCount;
                MissingPrefabCount += other.MissingPrefabCount;
                MissingAnchorTransformCount += other.MissingAnchorTransformCount;
                InvalidRuntimeScopeCount += other.InvalidRuntimeScopeCount;
                InvalidAnchorScopeCount += other.InvalidAnchorScopeCount;
                InvalidAnchorKindCount += other.InvalidAnchorKindCount;
                InvalidReleasePolicyCount += other.InvalidReleasePolicyCount;
                MissingRuntimeOwnerIdCount += other.MissingRuntimeOwnerIdCount;
                MissingAnchorOwnerIdCount += other.MissingAnchorOwnerIdCount;
                MissingAnchorIdCount += other.MissingAnchorIdCount;
                MissingRuntimeContentIdCount += other.MissingRuntimeContentIdCount;
                MissingResourceKeyCount += other.MissingResourceKeyCount;
                DuplicateBridgeCount += other.DuplicateBridgeCount;
                DuplicateMaterializationKeyCount += other.DuplicateMaterializationKeyCount;
            }

            public UnityContentAnchorMaterializationAuthoringValidationResult ToResult(
                UnityContentAnchorMaterializationAuthoringValidationStatus status,
                string scope,
                string message,
                int bridgeCount,
                int duplicateBridgeCount,
                int duplicateMaterializationKeyCount)
            {
                int resolvedDuplicateBridgeCount = DuplicateBridgeCount + duplicateBridgeCount;
                int resolvedDuplicateMaterializationKeyCount = DuplicateMaterializationKeyCount + duplicateMaterializationKeyCount;
                return UnityContentAnchorMaterializationAuthoringValidationResult.Create(
                    status,
                    scope,
                    message,
                    bridgeCount,
                    BlockingIssueCount,
                    WarningCount,
                    NullBridgeCount,
                    resolvedDuplicateBridgeCount,
                    resolvedDuplicateMaterializationKeyCount,
                    MissingPrefabCount,
                    MissingAnchorTransformCount,
                    InvalidRuntimeScopeCount,
                    InvalidAnchorScopeCount,
                    InvalidAnchorKindCount,
                    InvalidReleasePolicyCount,
                    MissingRuntimeOwnerIdCount,
                    MissingAnchorOwnerIdCount,
                    MissingAnchorIdCount,
                    MissingRuntimeContentIdCount,
                    MissingResourceKeyCount,
                    false,
                    false);
            }
        }
    }
}
