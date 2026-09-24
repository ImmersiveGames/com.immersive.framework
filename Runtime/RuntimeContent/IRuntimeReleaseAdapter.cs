using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Boundary implemented by physical release adapters outside the RuntimeContent core.
    /// The framework core owns release request/result/handle state; adapter implementations perform physical cleanup in their own layer when needed.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8J release adapter boundary; no prefab, scene, Addressables, pooling or UnityEngine implementation in RuntimeContent core.")]
    public interface IRuntimeReleaseAdapter
    {
        /// <summary>
        /// Executes physical release in the adapter implementation and returns a canonical release result.
        /// The adapter must not invent ownership, remove scope roots or replace the request identity.
        /// </summary>
        /// <param name="request">The explicit runtime release request produced by the framework runtime.</param>
        /// <returns>A release result carrying status and diagnostics. The framework runtime applies logical handle state after adapter success.</returns>
        RuntimeReleaseResult Release(RuntimeReleaseRequest request);
    }
}
