using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Activity content discovery scope for Route-owned and Activity-owned loaded scenes.")]
    internal readonly struct ActivityContentDiscoveryScope
    {
        private readonly ActivityContentDiscoveryScene[] _activityOwnedScenes;

        internal ActivityContentDiscoveryScope(
            RouteContentDiscoveryScope routeScope,
            IReadOnlyList<ActivityContentDiscoveryScene> activityOwnedScenes)
        {
            RouteScope = routeScope;
            _activityOwnedScenes = CopyScenes(activityOwnedScenes);
        }

        internal RouteContentDiscoveryScope RouteScope { get; }

        internal RouteAsset Route => RouteScope.Route;

        internal IReadOnlyList<ActivityContentDiscoveryScene> ActivityOwnedScenes => _activityOwnedScenes ?? Array.Empty<ActivityContentDiscoveryScene>();

        private static ActivityContentDiscoveryScene[] CopyScenes(IReadOnlyList<ActivityContentDiscoveryScene> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<ActivityContentDiscoveryScene>();
            }

            var copy = new ActivityContentDiscoveryScene[scenes.Count];
            for (int i = 0; i < scenes.Count; i++)
            {
                copy[i] = scenes[i];
            }

            return copy;
        }
    }
}
