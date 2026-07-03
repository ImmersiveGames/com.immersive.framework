using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Inert, side-effect-free plan for the scenes declared by a Route.
    /// The plan remains side-effect free and is consumed by F6E route scene composition execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Route scene composition plan consumed by F6E; no release/unload side effects.")]
    internal readonly struct RouteSceneCompositionPlan
    {
        private readonly RouteSceneCompositionPlanEntry[] _additionalScenes;

        public RouteSceneCompositionPlan(
            RouteAsset route,
            RouteContentProfileAsset profile,
            string routeOwnerId,
            string routeOwnerName,
            RouteSceneCompositionPlanEntry primaryScene,
            IReadOnlyList<RouteSceneCompositionPlanEntry> additionalScenes,
            RouteSceneActiveScenePolicy activeScenePolicy,
            string source,
            string reason)
        {
            Route = route;
            Profile = profile;
            RouteOwnerId = Normalize(routeOwnerId);
            RouteOwnerName = Normalize(routeOwnerName);
            PrimaryScene = primaryScene;
            ActiveScenePolicy = activeScenePolicy;
            Source = Normalize(source);
            Reason = Normalize(reason);

            if (additionalScenes == null || additionalScenes.Count == 0)
            {
                _additionalScenes = Array.Empty<RouteSceneCompositionPlanEntry>();
            }
            else
            {
                _additionalScenes = new RouteSceneCompositionPlanEntry[additionalScenes.Count];
                for (int i = 0; i < additionalScenes.Count; i++)
                {
                    _additionalScenes[i] = additionalScenes[i];
                }
            }
        }

        public RouteAsset Route { get; }

        public RouteContentProfileAsset Profile { get; }

        public bool HasProfile => Profile != null;

        public string ProfileId => Profile != null ? Profile.ProfileId : string.Empty;

        public string RouteOwnerId { get; }

        public string RouteOwnerName { get; }

        public RouteSceneCompositionPlanEntry PrimaryScene { get; }

        public IReadOnlyList<RouteSceneCompositionPlanEntry> AdditionalScenes => _additionalScenes ?? Array.Empty<RouteSceneCompositionPlanEntry>();

        public RouteSceneActiveScenePolicy ActiveScenePolicy { get; }

        public FrameworkContentScope Scope => FrameworkContentScope.Route;

        public string Source { get; }

        public string Reason { get; }

        public bool HasRoute => Route != null;

        public bool HasPrimaryScene => PrimaryScene.HasScene;

        public bool PrimarySceneExecutionReady => PrimaryScene.IsExecutionReady;

        public int AdditionalSceneCount => _additionalScenes?.Length ?? 0;

        public bool HasAdditionalScenes => AdditionalSceneCount > 0;

        public int EntryCount => 1 + AdditionalSceneCount;

        public int RequiredSceneCount => CountRequiredness(FrameworkContentRequiredness.Required);

        public int OptionalSceneCount => CountRequiredness(FrameworkContentRequiredness.Optional);

        public bool HasExecutionBlockingPlanIssue => !PrimarySceneExecutionReady || HasBlockingAdditionalSceneIssue();

        public static RouteSceneCompositionPlan FromRoute(RouteAsset route, string source, string reason)
        {
            string ownerId = CreateRouteOwnerId(route);
            string ownerName = route != null ? route.RouteName : string.Empty;
            var primaryScene = RouteSceneCompositionPlanEntry.Primary(route, ownerId);
            var profile = route != null ? route.RouteContentProfile : null;

            if (profile == null || profile.AdditionalScenes == null || profile.AdditionalScenes.Count == 0)
            {
                return new RouteSceneCompositionPlan(
                    route,
                    profile,
                    ownerId,
                    ownerName,
                    primaryScene,
                    Array.Empty<RouteSceneCompositionPlanEntry>(),
                    RouteSceneActiveScenePolicy.PrimarySceneActive,
                    source,
                    reason);
            }

            var plannedAdditionalScenes = new List<RouteSceneCompositionPlanEntry>(profile.AdditionalScenes.Count);
            for (int i = 0; i < profile.AdditionalScenes.Count; i++)
            {
                plannedAdditionalScenes.Add(RouteSceneCompositionPlanEntry.Additive(profile.AdditionalScenes[i], i, ownerId));
            }

            return new RouteSceneCompositionPlan(
                route,
                profile,
                ownerId,
                ownerName,
                primaryScene,
                plannedAdditionalScenes,
                RouteSceneActiveScenePolicy.PrimarySceneActive,
                source,
                reason);
        }

        public string ToDiagnosticString()
        {
            string routeName = RouteOwnerName.ToDiagnosticText("<missing>");
            var builder = new StringBuilder();
            builder.Append($"Route Scene Composition Plan route='{routeName}' owner='{RouteOwnerId}' entries='{EntryCount}' required='{RequiredSceneCount}' optional='{OptionalSceneCount}' activeScenePolicy='{ActiveScenePolicy}' sideEffects='False' profile='{ProfileId}' blockingIssues='{HasExecutionBlockingPlanIssue}' details=[");
            builder.Append(PrimaryScene.ToDiagnosticString());

            for (int i = 0; i < AdditionalScenes.Count; i++)
            {
                builder.Append(" | ");
                builder.Append(AdditionalScenes[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
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

        private int CountRequiredness(FrameworkContentRequiredness requiredness)
        {
            int count = PrimaryScene.Requiredness == requiredness ? 1 : 0;
            for (int i = 0; i < AdditionalScenes.Count; i++)
            {
                if (AdditionalScenes[i].Requiredness == requiredness)
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasBlockingAdditionalSceneIssue()
        {
            for (int i = 0; i < AdditionalScenes.Count; i++)
            {
                var entry = AdditionalScenes[i];
                if (entry is { Requiredness: FrameworkContentRequiredness.Required, IsExecutionReady: false })
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
