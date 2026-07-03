using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 lifecycle-local operation stage evidence vocabulary; not a shared stage taxonomy.")]
    internal enum FrameworkLifecycleOperationStage
    {
        Unknown = 0,
        TransitionBefore = 10,
        LoadingBefore = 20,
        SceneComposition = 30,
        RouteRelease = 40,
        ActivitySceneComposition = 50,
        ActivitySceneRelease = 60,
        ContentExit = 70,
        ContentEnter = 80,
        RuntimeScopeExit = 90,
        RuntimeScopeEnter = 100,
        Readiness = 110,
        TransitionAfter = 120,
        LoadingAfter = 130,
        RouteExit = 140,
        ContentAnchorBindingCleanup = 150,
        ActivityContentExecution = 160,
        ActivitySceneLedger = 170
    }
}
