using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Boundary implemented by physical materialization adapters outside the RuntimeContent core.
    /// The framework core owns request, result, handle, guard and release state; adapter implementations interpret resources and perform physical work in their own layer.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8I materialization adapter boundary; no prefab, scene, Addressables, pooling or UnityEngine implementation in RuntimeContent core.")]
    public interface IRuntimeMaterializationAdapter
    {
        /// <summary>
        /// Executes one materialization request in the adapter implementation and returns a canonical result.
        /// The adapter must not invent ownership, create fallback roots or replace the request identity.
        /// </summary>
        /// <param name="request">The explicit runtime materialization request produced by the framework runtime.</param>
        /// <returns>A materialization result carrying status, diagnostics and a matching handle on success.</returns>
        RuntimeMaterializationResult Materialize(RuntimeMaterializationRequest request);
    }
}
