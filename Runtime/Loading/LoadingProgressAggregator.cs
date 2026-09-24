using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Pure helper for aggregating LoadingStep observations into a LoadingProgressAggregationResult.
    /// This is not a scheduler, executor, SceneLifecycle adapter, Transition adapter or visual loading screen controller.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22C pure Loading progress aggregator; no Unity side effects.")]
    public static class LoadingProgressAggregator
    {
        public static LoadingProgressAggregationResult Aggregate(
            LoadingOperationId operationId,
            IReadOnlyList<LoadingStep> steps,
            string source,
            string reason,
            string message)
        {
            return LoadingProgressAggregationResult.FromSteps(operationId, steps, source, reason, message);
        }
    }
}
