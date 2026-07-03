using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26A Activity content discovery scope for Route primary and Activity-owned loaded scenes.")]
    internal readonly struct ActivityContentDiscoveryScope
    {
        private readonly ActivityContentDiscoveryScene[] _activityOwnedScenes;

        internal ActivityContentDiscoveryScope(
            RouteAsset route,
            IReadOnlyList<ActivityContentDiscoveryScene> activityOwnedScenes)
        {
            Route = route;
            _activityOwnedScenes = CopyScenes(activityOwnedScenes);
        }

        internal RouteAsset Route { get; }

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
