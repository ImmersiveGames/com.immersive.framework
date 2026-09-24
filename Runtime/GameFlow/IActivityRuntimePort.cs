using System.Threading.Tasks;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.GameFlow
{
    internal interface IActivityRuntimePort
    {
        Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason);

        Task<FrameworkActivityRequestResult> ClearActivityAsync(
            string source,
            string reason);
    }
}
