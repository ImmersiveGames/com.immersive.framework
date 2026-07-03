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
    /// API status: Internal. Loaded scene-authored contribution discovery for F5.
    /// This discovery requires explicit local ids and emits structured issues instead of falling back to GameObject names or paths.
    /// It does not materialize, release, load, unload, reset, snapshot or own lifecycle state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Loaded scene-authored local contribution discovery introduced by F5D and extended with requiredness metadata in F5F.")]
    internal static class LocalContributionDiscovery
    {
        public static LocalContributionDiscoveryResult DiscoverLoadedSceneAuthored()
        {
            var handles = new List<LocalContributionHandle>();
            var issues = new List<LocalContributionDiscoveryIssue>();

            CollectRouteBindings(null, handles, issues);
            CollectActivityAdapters(null, null, handles, issues);
            AddDuplicateIssues(handles, issues);

            SortHandles(handles);
            return new LocalContributionDiscoveryResult(LocalContributionSet.FromHandles(handles), issues);
        }

        public static LocalContributionDiscoveryResult DiscoverLoadedRoute(RouteAsset route)
        {
            var handles = new List<LocalContributionHandle>();
            var issues = new List<LocalContributionDiscoveryIssue>();

            if (route == null)
            {
                issues.Add(new LocalContributionDiscoveryIssue(
                    LocalContributionDiscoveryIssueKind.MissingOwner,
                    "Route local contribution discovery requires a Route owner."));
                return new LocalContributionDiscoveryResult(LocalContributionSet.Empty(), issues);
            }

            CollectRouteBindings(route, handles, issues);
            AddDuplicateIssues(handles, issues);

            SortHandles(handles);
            return new LocalContributionDiscoveryResult(LocalContributionSet.FromHandles(handles), issues);
        }

        public static LocalContributionDiscoveryResult DiscoverLoadedActivity(ActivityAsset activity)
        {
            return DiscoverLoadedActivity(activity, null);
        }

        public static LocalContributionDiscoveryResult DiscoverLoadedActivity(ActivityAsset activity, RouteAsset routeScope)
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

            CollectActivityAdapters(activity, routeScope, handles, issues);
            AddDuplicateIssues(handles, issues);

            SortHandles(handles);
            return new LocalContributionDiscoveryResult(LocalContributionSet.FromHandles(handles), issues);
        }

        private static void CollectRouteBindings(
            RouteAsset routeFilter,
            List<LocalContributionHandle> handles,
            List<LocalContributionDiscoveryIssue> issues)
        {
            IReadOnlyList<RouteContentBinding> bindings = routeFilter != null
                ? SceneScopedComponentQuery.GetComponentsInRoutePrimaryScene<RouteContentBinding>(routeFilter)
                : SceneScopedComponentQuery.GetComponentsInLoadedScenes<RouteContentBinding>();
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

                if (routeFilter != null && !binding.MatchesRoute(routeFilter))
                {
                    continue;
                }

                var route = routeFilter != null ? routeFilter : binding.Route;
                if (route == null)
                {
                    issues.Add(new LocalContributionDiscoveryIssue(
                        LocalContributionDiscoveryIssueKind.MissingOwner,
                        "RouteContentBinding requires an explicit Route owner before it can produce a LocalContentIdentity.",
                        sceneName: binding.SceneName,
                        objectName: binding.ObjectName));
                    continue;
                }

                if (!binding.TryGetLocalContentId(out var localId))
                {
                    issues.Add(new LocalContributionDiscoveryIssue(
                        LocalContributionDiscoveryIssueKind.MissingLocalContentId,
                        "RouteContentBinding requires an explicit Local Content Id. GameObject names and hierarchy paths are diagnostics only.",
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
                    LocalContributionSourceKind.RouteContentBinding,
                    binding.Requiredness,
                    binding.SceneName,
                    binding.ObjectName,
                    nameof(RouteContentBinding));
            }
        }

        private static void CollectActivityAdapters(
            ActivityAsset activityFilter,
            RouteAsset routeScope,
            List<LocalContributionHandle> handles,
            List<LocalContributionDiscoveryIssue> issues)
        {
            IReadOnlyList<ActivityLocalVisibilityAdapter> adapters = routeScope != null
                ? SceneScopedComponentQuery.GetComponentsInRoutePrimaryScene<ActivityLocalVisibilityAdapter>(routeScope)
                : SceneScopedComponentQuery.GetComponentsInLoadedScenes<ActivityLocalVisibilityAdapter>();
            if (adapters == null || adapters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < adapters.Count; i++)
            {
                var adapter = adapters[i];
                if (adapter == null || !adapter.IsSceneBinding)
                {
                    continue;
                }

                if (activityFilter != null && !ReferenceEquals(adapter.Activity, activityFilter))
                {
                    continue;
                }

                var activity = activityFilter != null ? activityFilter : adapter.Activity;
                if (activity == null)
                {
                    issues.Add(new LocalContributionDiscoveryIssue(
                        LocalContributionDiscoveryIssueKind.MissingOwner,
                        "ActivityLocalVisibilityAdapter requires an explicit Activity owner before it can produce a LocalContentIdentity.",
                        sceneName: adapter.SceneName,
                        objectName: adapter.ObjectName));
                    continue;
                }

                if (!adapter.TryGetLocalContentId(out var localId))
                {
                    issues.Add(new LocalContributionDiscoveryIssue(
                        LocalContributionDiscoveryIssueKind.MissingLocalContentId,
                        "ActivityLocalVisibilityAdapter requires an explicit Local Content Id. GameObject names and hierarchy paths are diagnostics only.",
                        sceneName: adapter.SceneName,
                        objectName: adapter.ObjectName));
                    continue;
                }

                TryAddHandle(
                    handles,
                    issues,
                    FrameworkContentScope.Activity,
                    CreateActivityOwnerKey(activity),
                    adapter.LocalScopeKind,
                    localId,
                    LocalContributionSourceKind.ActivityLocalVisibilityAdapter,
                    adapter.Requiredness,
                    adapter.SceneName,
                    adapter.ObjectName,
                    nameof(ActivityLocalVisibilityAdapter));
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

            if (!string.IsNullOrWhiteSpace(route.PrimaryScenePath))
            {
                return route.PrimaryScenePath.Trim();
            }

            return route.RouteName;
        }

        private static string CreateActivityOwnerId(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : string.Empty;
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
