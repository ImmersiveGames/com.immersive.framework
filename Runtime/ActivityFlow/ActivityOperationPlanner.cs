using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free Activity operation planner introduced by F25F.
    /// It reconciles target Activity scene loads, previous Activity scene releases and visual policy into one ActivityOperationPlan.
    /// It does not execute transition, loading, scene load, scene release or Activity lifecycle changes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25F Activity operation planner; side-effect-free preview only.")]
    internal sealed class ActivityOperationPlanner
    {
        private readonly ActivitySceneCompositionRuntime _activitySceneCompositionRuntime;

        internal ActivityOperationPlanner(ActivitySceneCompositionRuntime activitySceneCompositionRuntime)
        {
            _activitySceneCompositionRuntime = activitySceneCompositionRuntime ?? throw new ArgumentNullException(nameof(activitySceneCompositionRuntime));
        }

        internal ActivityOperationPlan CreatePlan(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            string source,
            string reason)
        {
            return _activitySceneCompositionRuntime.CreateActivityOperationPlan(
                operationKind,
                previousActivity,
                targetActivity,
                visualMode,
                source,
                reason);
        }

        internal ActivityOperationResult Preview(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            string source,
            string reason)
        {
            return ActivityOperationResult.FromPlan(CreatePlan(
                operationKind,
                previousActivity,
                targetActivity,
                visualMode,
                source,
                reason));
        }
    }
}
