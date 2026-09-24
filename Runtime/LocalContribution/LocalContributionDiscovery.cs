using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Identity;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Composition-scoped scene-authored contribution discovery for F5.
    /// This discovery requires explicit local ids and emits structured issues instead of falling back to GameObject names or paths.
    /// It does not materialize, release, load, unload, reset, snapshot or own lifecycle state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Composition-scoped scene-authored local contribution discovery introduced by F5D and extended with requiredness metadata in F5F.")]
    internal static class LocalContributionDiscovery
    {
        public static LocalContributionDiscoveryResult Discover(
            RouteContentDiscoveryScope scope)
        {
            var handles = new List<LocalContributionHandle>();
            var issues = new List<LocalContributionDiscoveryIssue>();
            RouteAsset route = scope.Route;

            if (route == null)
            {
                issues.Add(new LocalContributionDiscoveryIssue(
                    LocalContributionDiscoveryIssueKind.MissingOwner,
                    "Route local contribution discovery requires a Route owner."));
                return new LocalContributionDiscoveryResult(LocalContributionSet.Empty(), issues);
            }

            CollectRouteBindings(scope, handles, issues);
            AddDuplicateIssues(handles, issues);

            SortHandles(handles);
            return new LocalContributionDiscoveryResult(LocalContributionSet.FromHandles(handles), issues);
        }

        public static LocalContributionDiscoveryResult Discover(
            ActivityContentDiscoveryScope scope,
            ActivityAsset activity)
        {
            var handles = new List<LocalContributionHandle>();
            var issues = new List<LocalContributionDiscoveryIssue>();

            if (activity == null)
            {
                issues.Add(new LocalContributionDiscoveryIssue(
                    LocalContributionDiscoveryIssueKind.MissingOwner,
                    "Activity local contribution discovery requires an Activity owner."));
                return new LocalContributionDiscoveryResult(LocalContributionSet.Empty(), issues);
            }

            CollectActivityContributions(scope, activity, handles, issues);
            AddDuplicateIssues(handles, issues);

            SortHandles(handles);
            return new LocalContributionDiscoveryResult(LocalContributionSet.FromHandles(handles), issues);
        }

        private static void CollectRouteBindings(
            RouteContentDiscoveryScope scope,
            List<LocalContributionHandle> handles,
            List<LocalContributionDiscoveryIssue> issues)
        {
            RouteAsset route = scope.Route;
            IReadOnlyList<RouteContentContribution> bindings =
                SceneCompositionComponentQuery.GetComponents<RouteContentContribution>(
                    scope);
            if (bindings == null || bindings.Count == 0)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null || !binding.IsSceneBinding)
                {
                    continue;
                }

                if (!binding.MatchesRoute(route))
                {
                    continue;
                }

                if (!binding.TryGetLocalContentId(out var localId))
                {
                    issues.Add(new LocalContributionDiscoveryIssue(
                        LocalContributionDiscoveryIssueKind.MissingLocalContentId,
                        "RouteContentContribution requires an explicit Local Content Id. GameObject names and hierarchy paths are diagnostics only.",
                        sceneName: binding.SceneName,
                        objectName: binding.ObjectName));
                    continue;
                }

                TryAddHandle(
                    handles,
                    issues,
                    FrameworkContentScope.Route,
                    CreateRouteOwnerKey(route),
                    binding.LocalScopeKind,
                    localId,
                    LocalContributionSourceKind.RouteContentContribution,
                    binding.Requiredness,
                    binding.SceneName,
                    binding.ObjectName,
                    nameof(RouteContentContribution));
            }
        }

        private static void CollectActivityContributions(
            ActivityContentDiscoveryScope scope,
            ActivityAsset activityFilter,
            List<LocalContributionHandle> handles,
            List<LocalContributionDiscoveryIssue> issues)
        {
            IReadOnlyList<ActivityContentContribution> contributions =
                SceneCompositionComponentQuery.GetComponents<ActivityContentContribution>(
                    scope,
                    activityFilter);
            if (contributions == null || contributions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < contributions.Count; i++)
            {
                var contribution = contributions[i];
                if (contribution == null || !contribution.IsSceneBinding)
                {
                    continue;
                }

                if (!contribution.TryValidate(out string validationReason))
                {
                    issues.Add(new LocalContributionDiscoveryIssue(
                        LocalContributionDiscoveryIssueKind.MissingOwner,
                        $"ActivityContentContribution is invalid: {validationReason}. Activity ownership and local identity must be explicit.",
                        sceneName: contribution.SceneName,
                        objectName: contribution.ObjectName));
                    continue;
                }

                if (!contribution.MatchesActivity(activityFilter))
                {
                    continue;
                }

                var activity = contribution.Activity;

                if (!contribution.TryGetLocalContentId(out var localId))
                {
                    issues.Add(new LocalContributionDiscoveryIssue(
                        LocalContributionDiscoveryIssueKind.MissingLocalContentId,
                        "ActivityContentContribution requires an explicit Local Content Id. GameObject names and hierarchy paths are diagnostics only.",
                        sceneName: contribution.SceneName,
                        objectName: contribution.ObjectName));
                    continue;
                }

                TryAddHandle(
                    handles,
                    issues,
                    FrameworkContentScope.Activity,
                    CreateActivityOwnerKey(activity),
                    contribution.LocalScopeKind,
                    localId,
                    LocalContributionSourceKind.ActivityContentContribution,
                    contribution.Requiredness,
                    contribution.SceneName,
                    contribution.ObjectName,
                    nameof(ActivityContentContribution));
            }
        }

        private static void TryAddHandle(
            List<LocalContributionHandle> handles,
            List<LocalContributionDiscoveryIssue> issues,
            FrameworkContentScope contentScope,
            FrameworkIdentityKey ownerKey,
            LocalContentScopeKind localScopeKind,
            LocalContentId localId,
            LocalContributionSourceKind sourceKind,
            FrameworkContentRequiredness requiredness,
            string sceneName,
            string objectName,
            string componentType)
        {
            try
            {
                var identity = new LocalContentIdentity(contentScope, ownerKey, localScopeKind, localId);
                handles.Add(new LocalContributionHandle(identity, sourceKind, requiredness, sceneName, objectName, componentType));
            }
            catch (Exception exception)
            {
                issues.Add(new LocalContributionDiscoveryIssue(
                    LocalContributionDiscoveryIssueKind.InvalidLocalContentIdentity,
                    exception.Message,
                    sceneName: sceneName,
                    objectName: objectName));
            }
        }

        private static void AddDuplicateIssues(
            IReadOnlyList<LocalContributionHandle> handles,
            List<LocalContributionDiscoveryIssue> issues)
        {
            if (handles == null || handles.Count <= 1)
            {
                return;
            }

            var seen = new Dictionary<string, LocalContributionHandle>(StringComparer.Ordinal);
            for (int i = 0; i < handles.Count; i++)
            {
                var handle = handles[i];
                string identityText = handle.Identity.StableText;
                if (!seen.TryGetValue(identityText, out var previous))
                {
                    seen.Add(identityText, handle);
                    continue;
                }

                issues.Add(new LocalContributionDiscoveryIssue(
                    LocalContributionDiscoveryIssueKind.DuplicateLocalContentIdentity,
                    $"Duplicate LocalContentIdentity. First object='{FormatValue(previous.ObjectName)}' scene='{FormatValue(previous.SceneName)}'.",
                    identityText,
                    handle.SceneName,
                    handle.ObjectName));
            }
        }

        private static FrameworkIdentityKey CreateRouteOwnerKey(RouteAsset route)
        {
            return FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, CreateRouteOwnerId(route));
        }

        private static FrameworkIdentityKey CreateActivityOwnerKey(ActivityAsset activity)
        {
            return FrameworkIdentityKey.From(FrameworkIdentityDomain.Activity, CreateActivityOwnerId(activity));
        }

        private static string CreateRouteOwnerId(RouteAsset route)
        {
            if (route == null)
            {
                return string.Empty;
            }

            if (!route.HasValidRouteId)
            {
                throw new ArgumentException("Local Route contribution ownership requires a valid RouteId.", nameof(route));
            }

            return route.RouteId.StableText;
        }

        private static string CreateActivityOwnerId(ActivityAsset activity)
        {
            if (activity == null)
            {
                return string.Empty;
            }

            if (!activity.HasValidActivityId)
            {
                throw new ArgumentException("Local Activity contribution ownership requires a valid ActivityId.", nameof(activity));
            }

            return activity.ActivityId.StableText;
        }

        private static void SortHandles(List<LocalContributionHandle> handles)
        {
            if (handles == null || handles.Count <= 1)
            {
                return;
            }

            handles.Sort((left, right) => string.Compare(left.Identity.StableText, right.Identity.StableText, StringComparison.Ordinal));
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\'");
        }
    }
}
