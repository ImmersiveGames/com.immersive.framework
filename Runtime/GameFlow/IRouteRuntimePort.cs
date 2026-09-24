using System.Threading.Tasks;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.GameFlow
{
    internal interface IRouteRuntimePort
    {
        Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason);
    }
}
