using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26A loaded Activity-owned scene reference for Activity content discovery.")]
    internal readonly struct ActivityContentDiscoveryScene
    {
        internal ActivityContentDiscoveryScene(
            ActivityAsset activity,
            string scenePath,
            string sceneName)
        {
            Activity = activity;
            ScenePath = scenePath.NormalizeText();
            SceneName = sceneName.NormalizeText();
        }

        private ActivityAsset Activity { get; }

        internal string ScenePath { get; }

        internal string SceneName { get; }

        internal bool MatchesActivity(ActivityAsset activity)
        {
            return Activity != null && Activity.HasSameIdentity(activity);
        }
    }
}
