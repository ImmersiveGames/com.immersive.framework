using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Diagnostic issue kinds for Activity operation planning.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation issue kind; planning diagnostics only.")]
    internal enum ActivityOperationIssueKind
    {
        Unknown = 0,
        MissingOperationKind = 10,
        MissingTargetActivity = 20,
        FadeWithLoadingWithoutSceneSideEffects = 50,
        InvalidSceneEntry = 60,
        StaleTrackedScene = 70
    }
}
