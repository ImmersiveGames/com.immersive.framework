using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Contract for transition orchestration around framework Route/Activity requests.
    /// Implementations must not take ownership of Route/Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24B Transition orchestration contract.")]
    public interface ITransitionOrchestrator
    {
        TransitionResult Execute(TransitionRequest request);

        /// <summary>
        /// Executes the transition and completes only after the visual phase has settled.
        /// No-op implementations may complete immediately.
        /// </summary>
        Awaitable<TransitionResult> ExecuteAsync(TransitionRequest request);
    }
}
