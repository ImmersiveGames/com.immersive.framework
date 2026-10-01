using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.Reset;

namespace Immersive.Framework.ActivityRestart
{
    internal interface IActivityRestartRuntimePort
    {
        Task<ActivityRestartRuntimeResult> RequestActivityRestartAsync(
            ActivityAsset targetActivity,
            bool useCurrentActivityWhenTargetMissing,
            bool requireTargetActivityIsCurrent,
            ResetTarget resetTarget,
            string source,
            string reason);
    }
}
