using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Route-owned content snapshot for the active Route scope.
    /// This baseline records the loaded primary scene with explicit ownership semantics; additional scenes are represented by F6 scene composition; release planning is introduced in F6F.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct RouteContentSet
    {
        private readonly RouteContentEntry[] _entries;

        public RouteContentSet(
            RouteAsset route,
            FrameworkContentSet contentSet,
            RouteContentMaterializationPlan contentPlan,
            IReadOnlyList<RouteContentEntry> entries)
        {
            Route = route;
            ContentSet = contentSet;
            ContentPlan = contentPlan;

            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<RouteContentEntry>();
                return;
            }

            _entries = new RouteContentEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                _entries[i] = entries[i];
            }
        }

        public RouteAsset Route { get; }

        public FrameworkContentSet ContentSet { get; }

        public RouteContentMaterializationPlan ContentPlan { get; }

        public IReadOnlyList<RouteContentEntry> Entries => _entries ?? Array.Empty<RouteContentEntry>();

        public bool HasContent => ContentSet.HasContent;

        public int Count => ContentSet.Count;

        public int RegisteredCount => CountOwnership(RouteContentOwnership.Registered);

        public int OwnedCount => CountOwnership(RouteContentOwnership.Owned);

        public int DiagnosticOnlyCount => CountOwnership(RouteContentOwnership.DiagnosticOnly);

        public bool HasOwnedContent => OwnedCount > 0;

        public string DiagnosticMessage
        {
            get
            {
                if (!HasContent)
                {
                    return Route != null
                        ? $"Route Content Set is empty for Route '{Route.RouteName}'."
                        : "Route Content Set is empty.";
                }

                string planMessage = ContentPlan.HasProfile ? $" {ContentPlan.ToDiagnosticString()}" : string.Empty;
                return $"Route Content Set registered {Count} handle(s). registered='{RegisteredCount}' owned='{OwnedCount}' diagnosticOnly='{DiagnosticOnlyCount}' {ContentSet.ToDiagnosticString()}.{planMessage}";
            }
        }

        public string OwnershipDiagnosticMessage
        {
            get
            {
                if (!HasContent)
                {
                    return "Route Content Set ownership is empty.";
                }

                var builder = new StringBuilder();
                builder.Append($"Route Content Set ownership registered='{RegisteredCount}' owned='{OwnedCount}' diagnosticOnly='{DiagnosticOnlyCount}' details=[");
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(" | ");
                    }

                    builder.Append(Entries[i].ToDiagnosticString());
                }

                builder.Append("]");
                return builder.ToString();
            }
        }

        public static RouteContentSet Empty(RouteAsset route)
        {
            string ownerName = route != null ? route.RouteName : string.Empty;
            string ownerId = CreateRouteOwnerId(route);
            return new RouteContentSet(
                route,
                FrameworkContentSet.Empty(FrameworkContentScope.Route, ownerId, ownerName),
                RouteContentMaterializationPlan.FromRoute(route),
                Array.Empty<RouteContentEntry>());
        }

        public static RouteContentSet FromPrimaryScene(
            RouteAsset route,
            RouteContentMaterializationPlan contentPlan,
            SceneLifecycleLoadResult sceneLifecycleResult,
            string source,
            string reason)
        {
            if (route == null || !sceneLifecycleResult.Loaded)
            {
                return Empty(route);
            }

            string ownerId = CreateRouteOwnerId(route);
            var handle = FrameworkContentHandle.RoutePrimaryScene(
                ownerId,
                route.RouteName,
                sceneLifecycleResult.SceneName,
                sceneLifecycleResult.ScenePath,
                true,
                source,
                reason,
                sceneLifecycleResult.Message);
            var entry = new RouteContentEntry(handle, RouteContentOwnership.Owned);

            return new RouteContentSet(
                route,
                FrameworkContentSet.Single(FrameworkContentScope.Route, ownerId, route.RouteName, handle),
                contentPlan,
                new[] { entry });
        }

        public static RouteContentSet FromSceneCompositionResult(
            RouteAsset route,
            RouteContentMaterializationPlan contentPlan,
            RouteSceneCompositionResult compositionResult,
            string source,
            string reason)
        {
            if (route == null || !compositionResult.Succeeded || compositionResult.LoadedCount == 0)
            {
                return Empty(route);
            }

            string ownerId = CreateRouteOwnerId(route);
            var handles = new List<FrameworkContentHandle>(compositionResult.LoadedCount);
            var entries = new List<RouteContentEntry>(compositionResult.LoadedCount);

            for (int i = 0; i < compositionResult.Entries.Count; i++)
            {
                var resultEntry = compositionResult.Entries[i];
                if (!resultEntry.Loaded || !resultEntry.ContentIdentity.IsValid)
                {
                    continue;
                }

                var handle = new FrameworkContentHandle(
                    resultEntry.ContentIdentity,
                    resultEntry.Requiredness,
                    route.RouteName,
                    resultEntry.SceneName,
                    resultEntry.ScenePath,
                    resultEntry.ActiveScene,
                    source,
                    reason,
                    resultEntry.Message);

                handles.Add(handle);
                entries.Add(new RouteContentEntry(handle, resultEntry.Ownership));
            }

            if (handles.Count == 0)
            {
                return Empty(route);
            }

            return new RouteContentSet(
                route,
                new FrameworkContentSet(FrameworkContentScope.Route, ownerId, route.RouteName, handles),
                contentPlan,
                entries);
        }


        public ContentReleasePlan CreateReleasePlan(
            string source,
            string reason)
        {
            if (!HasContent)
            {
                return ContentReleasePlan.Empty(
                    FrameworkContentScope.Route,
                    ContentSet.OwnerId,
                    ContentSet.OwnerName,
                    source,
                    reason,
                    "Route Content Set is empty; no Route content release entries were planned.");
            }

            var releaseEntries = new List<ContentReleasePlanEntry>(Entries.Count);
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                var releaseOwnership = ToReleaseOwnership(entry.Ownership);
                var action = DetermineReleaseAction(entry);
                releaseEntries.Add(new ContentReleasePlanEntry(
                    entry.Handle,
                    releaseOwnership,
                    action,
                    BuildReleasePlanEntryMessage(entry, action)));
            }

            return new ContentReleasePlan(
                FrameworkContentScope.Route,
                ContentSet.OwnerId,
                ContentSet.OwnerName,
                releaseEntries,
                source,
                reason,
                "Route content release plan created from active Route Content Set.");
        }

        private static ContentReleaseOwnership ToReleaseOwnership(RouteContentOwnership ownership)
        {
            switch (ownership)
            {
                case RouteContentOwnership.Owned:
                    return ContentReleaseOwnership.Owned;
                case RouteContentOwnership.DiagnosticOnly:
                    return ContentReleaseOwnership.DiagnosticOnly;
                default:
                    return ContentReleaseOwnership.Registered;
            }
        }

        private static ContentReleaseAction DetermineReleaseAction(RouteContentEntry entry)
        {
            if (!entry.IsOwned)
            {
                return ContentReleaseAction.None;
            }

            if (entry.Handle.Kind != FrameworkContentKind.Scene)
            {
                return ContentReleaseAction.None;
            }

            if (entry.Handle.Active)
            {
                return ContentReleaseAction.None;
            }

            if (!entry.Handle.HasResource)
            {
                return ContentReleaseAction.None;
            }

            return ContentReleaseAction.UnloadScene;
        }

        private static string BuildReleasePlanEntryMessage(
            RouteContentEntry entry,
            ContentReleaseAction action)
        {
            if (!entry.IsOwned)
            {
                return "Route content is not Owned; release action is not planned.";
            }

            if (entry.Handle.Kind != FrameworkContentKind.Scene)
            {
                return "Only scene unload release actions are planned in F6F.";
            }

            if (entry.Handle.Active)
            {
                return "Active Primary Scene is controlled by LoadSceneMode.Single; manual unload is not planned.";
            }

            if (!entry.Handle.HasResource)
            {
                return "Scene content has no resolvable resource; release action is not planned.";
            }

            if (action == ContentReleaseAction.UnloadScene)
            {
                return "Owned additive Route scene is planned for unload on Route release.";
            }

            return "Release action is not planned.";
        }

        private static string CreateRouteOwnerId(RouteAsset route)
        {
            if (route == null)
            {
                return string.Empty;
            }

            if (!route.HasValidRouteId)
            {
                throw new ArgumentException("Route content ownership requires a valid RouteId.", nameof(route));
            }

            return route.RouteId.StableText;
        }

        private int CountOwnership(RouteContentOwnership ownership)
        {
            if (_entries == null || _entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Ownership == ownership)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
