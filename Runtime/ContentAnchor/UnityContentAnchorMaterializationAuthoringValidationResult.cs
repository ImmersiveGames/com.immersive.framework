using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    public sealed class UnityContentAnchorMaterializationAuthoringValidationResult
    {
        private UnityContentAnchorMaterializationAuthoringValidationResult(
            UnityContentAnchorMaterializationAuthoringValidationStatus status,
            string scope,
            string message,
            int bridgeCount,
            int blockingIssueCount,
            int warningCount,
            int nullBridgeCount,
            int duplicateBridgeCount,
            int duplicateMaterializationKeyCount,
            int missingPrefabCount,
            int missingAnchorTransformCount,
            int invalidRuntimeScopeCount,
            int invalidAnchorScopeCount,
            int invalidAnchorKindCount,
            int invalidReleasePolicyCount,
            int missingRuntimeOwnerIdCount,
            int missingAnchorOwnerIdCount,
            int missingAnchorIdCount,
            int missingRuntimeContentIdCount,
            int missingResourceKeyCount,
            bool automaticLifecycleWiring,
            bool routeActivityAutoMaterialization)
        {
            Status = status;
            Scope = scope.NormalizeTextOrFallback("<none>");
            Message = message.NormalizeTextOrFallback("<none>");
            BridgeCount = bridgeCount;
            BlockingIssueCount = blockingIssueCount;
            WarningCount = warningCount;
            NullBridgeCount = nullBridgeCount;
            DuplicateBridgeCount = duplicateBridgeCount;
            DuplicateMaterializationKeyCount = duplicateMaterializationKeyCount;
            MissingPrefabCount = missingPrefabCount;
            MissingAnchorTransformCount = missingAnchorTransformCount;
            InvalidRuntimeScopeCount = invalidRuntimeScopeCount;
            InvalidAnchorScopeCount = invalidAnchorScopeCount;
            InvalidAnchorKindCount = invalidAnchorKindCount;
            InvalidReleasePolicyCount = invalidReleasePolicyCount;
            MissingRuntimeOwnerIdCount = missingRuntimeOwnerIdCount;
            MissingAnchorOwnerIdCount = missingAnchorOwnerIdCount;
            MissingAnchorIdCount = missingAnchorIdCount;
            MissingRuntimeContentIdCount = missingRuntimeContentIdCount;
            MissingResourceKeyCount = missingResourceKeyCount;
            AutomaticLifecycleWiring = automaticLifecycleWiring;
            RouteActivityAutoMaterialization = routeActivityAutoMaterialization;
        }

        public UnityContentAnchorMaterializationAuthoringValidationStatus Status { get; }

        public string Scope { get; }

        public string Message { get; }

        public int BridgeCount { get; }

        public int BlockingIssueCount { get; }

        public int WarningCount { get; }

        public int NullBridgeCount { get; }

        public int DuplicateBridgeCount { get; }

        public int DuplicateMaterializationKeyCount { get; }

        public int MissingPrefabCount { get; }

        public int MissingAnchorTransformCount { get; }

        public int InvalidRuntimeScopeCount { get; }

        public int InvalidAnchorScopeCount { get; }

        public int InvalidAnchorKindCount { get; }

        public int InvalidReleasePolicyCount { get; }

        public int MissingRuntimeOwnerIdCount { get; }

        public int MissingAnchorOwnerIdCount { get; }

        public int MissingAnchorIdCount { get; }

        public int MissingRuntimeContentIdCount { get; }

        public int MissingResourceKeyCount { get; }

        public bool AutomaticLifecycleWiring { get; }

        public bool RouteActivityAutoMaterialization { get; }

        public bool Succeeded => Status == UnityContentAnchorMaterializationAuthoringValidationStatus.Succeeded;

        public bool Failed => !Succeeded;

        public static UnityContentAnchorMaterializationAuthoringValidationResult Create(
            UnityContentAnchorMaterializationAuthoringValidationStatus status,
            string scope,
            string message,
            int bridgeCount,
            int blockingIssueCount,
            int warningCount,
            int nullBridgeCount,
            int duplicateBridgeCount,
            int duplicateMaterializationKeyCount,
            int missingPrefabCount,
            int missingAnchorTransformCount,
            int invalidRuntimeScopeCount,
            int invalidAnchorScopeCount,
            int invalidAnchorKindCount,
            int invalidReleasePolicyCount,
            int missingRuntimeOwnerIdCount,
            int missingAnchorOwnerIdCount,
            int missingAnchorIdCount,
            int missingRuntimeContentIdCount,
            int missingResourceKeyCount,
            bool automaticLifecycleWiring = false,
            bool routeActivityAutoMaterialization = false)
        {
            return new UnityContentAnchorMaterializationAuthoringValidationResult(
                status,
                scope,
                message,
                bridgeCount,
                blockingIssueCount,
                warningCount,
                nullBridgeCount,
                duplicateBridgeCount,
                duplicateMaterializationKeyCount,
                missingPrefabCount,
                missingAnchorTransformCount,
                invalidRuntimeScopeCount,
                invalidAnchorScopeCount,
                invalidAnchorKindCount,
                invalidReleasePolicyCount,
                missingRuntimeOwnerIdCount,
                missingAnchorOwnerIdCount,
                missingAnchorIdCount,
                missingRuntimeContentIdCount,
                missingResourceKeyCount,
                automaticLifecycleWiring,
                routeActivityAutoMaterialization);
        }

        public string ToDiagnosticString()
        {
            return $"status='{Status}' scope='{Scope.ToDiagnosticText()}' bridgeCount='{BridgeCount}' blockingIssues='{BlockingIssueCount}' warnings='{WarningCount}' nullBridges='{NullBridgeCount}' duplicateBridges='{DuplicateBridgeCount}' duplicateMaterializationKeys='{DuplicateMaterializationKeyCount}' missingPrefabs='{MissingPrefabCount}' missingAnchorTransforms='{MissingAnchorTransformCount}' invalidRuntimeScopes='{InvalidRuntimeScopeCount}' invalidAnchorScopes='{InvalidAnchorScopeCount}' invalidAnchorKinds='{InvalidAnchorKindCount}' invalidReleasePolicies='{InvalidReleasePolicyCount}' missingRuntimeOwnerIds='{MissingRuntimeOwnerIdCount}' missingAnchorOwnerIds='{MissingAnchorOwnerIdCount}' missingAnchorIds='{MissingAnchorIdCount}' missingRuntimeContentIds='{MissingRuntimeContentIdCount}' missingResourceKeys='{MissingResourceKeyCount}' automaticLifecycleWiring='{AutomaticLifecycleWiring}' routeActivityAutoMaterialization='{RouteActivityAutoMaterialization}' message='{Message.ToDiagnosticText()}'.";
        }
    }
}
