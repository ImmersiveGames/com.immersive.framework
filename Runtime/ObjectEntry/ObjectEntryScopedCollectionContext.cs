using Immersive.Framework.ApiStatus;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.RouteLifecycle;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Internal immutable context used to collect scene-authored Object Entry declarations for the active lifecycle only.
    /// It carries explicit typed owners and Route/Activity scene composition evidence; it is not a registry or service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Composition-scoped Object Entry collection context; no physical binding or reset authority.")]
    internal readonly struct ObjectEntryScopedCollectionContext
    {
        internal ObjectEntryScopedCollectionContext(
            FrameworkIdentityKey sessionOwnerIdentity,
            RouteAsset route,
            FrameworkIdentityKey routeOwnerIdentity,
            RouteSceneCompositionResult routeSceneCompositionResult,
            ActivityAsset activity,
            FrameworkIdentityKey activityOwnerIdentity,
            ActivityContentDiscoveryScope activityContentDiscoveryScope,
            bool hasActivityContentDiscoveryScope)
        {
            SessionOwnerIdentity = sessionOwnerIdentity;
            Route = route;
            RouteOwnerIdentity = routeOwnerIdentity;
            RouteSceneCompositionResult = routeSceneCompositionResult;
            Activity = activity;
            ActivityOwnerIdentity = activityOwnerIdentity;
            ActivityContentDiscoveryScope = activityContentDiscoveryScope;
            HasActivityContentDiscoveryScope = hasActivityContentDiscoveryScope;
        }

        internal FrameworkIdentityKey SessionOwnerIdentity { get; }

        internal RouteAsset Route { get; }

        internal FrameworkIdentityKey RouteOwnerIdentity { get; }

        internal RouteSceneCompositionResult RouteSceneCompositionResult { get; }

        internal ActivityAsset Activity { get; }

        internal FrameworkIdentityKey ActivityOwnerIdentity { get; }

        internal ActivityContentDiscoveryScope ActivityContentDiscoveryScope { get; }

        internal bool HasActivityContentDiscoveryScope { get; }

        internal bool HasActiveActivity => Activity != null
            && ActivityOwnerIdentity is { IsValid: true, Domain: FrameworkIdentityDomain.Activity };

        internal bool TryValidate(out string issue)
        {
            if (!SessionOwnerIdentity.IsValid || SessionOwnerIdentity.Domain != FrameworkIdentityDomain.Session)
            {
                issue = "Scoped Object Entry collection requires a valid Session owner identity.";
                return false;
            }

            if (Route == null)
            {
                issue = "Scoped Object Entry collection requires an active Route.";
                return false;
            }

            if (!RouteOwnerIdentity.IsValid || RouteOwnerIdentity.Domain != FrameworkIdentityDomain.Route)
            {
                issue = "Scoped Object Entry collection requires a valid Route owner identity.";
                return false;
            }

            if (!RouteSceneCompositionResult.Succeeded || RouteSceneCompositionResult.LoadedCount == 0)
            {
                issue = "Scoped Object Entry collection requires successful Route scene composition with at least one loaded scene.";
                return false;
            }

            if (Activity != null
                && (!ActivityOwnerIdentity.IsValid || ActivityOwnerIdentity.Domain != FrameworkIdentityDomain.Activity))
            {
                issue = "Scoped Object Entry collection received an active Activity without a valid Activity owner identity.";
                return false;
            }

            if (Activity != null && !HasActivityContentDiscoveryScope)
            {
                issue = "Scoped Object Entry collection requires the active Activity scene composition scope.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        internal bool TryResolveOwnerIdentity(ObjectEntryScope scope, out FrameworkIdentityKey ownerIdentity)
        {
            switch (scope)
            {
                case ObjectEntryScope.Session:
                    ownerIdentity = SessionOwnerIdentity;
                    return ownerIdentity.IsValid;
                case ObjectEntryScope.Route:
                    ownerIdentity = RouteOwnerIdentity;
                    return ownerIdentity.IsValid;
                case ObjectEntryScope.Activity when HasActiveActivity:
                    ownerIdentity = ActivityOwnerIdentity;
                    return true;
                default:
                    ownerIdentity = default;
                    return false;
            }
        }
    }
}
