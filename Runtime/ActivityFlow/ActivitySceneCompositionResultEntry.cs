using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable evidence record for one Activity scene composition plan entry.
    /// F25C records additive load execution; release/unload is deferred to a later cut.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25C Activity scene composition result entry; additive load evidence, release deferred.")]
    internal readonly struct ActivitySceneCompositionResultEntry
    {
        public ActivitySceneCompositionResultEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            ActivitySceneCompositionEntryStatus status,
            bool blocksComposition,
            string message)
        {
            PlanEntry = planEntry;
            Status = status;
            BlocksComposition = blocksComposition;
            Message = Normalize(message);
        }

        public ActivitySceneCompositionPlanEntry PlanEntry { get; }

        public ActivitySceneCompositionEntryStatus Status { get; }

        public bool BlocksComposition { get; }

        public string Message { get; }

        public FrameworkContentIdentity ContentIdentity => PlanEntry.ContentIdentity;

        public string ContentId => PlanEntry.ContentId;

        public string SceneName => PlanEntry.SceneName;

        public string ScenePath => PlanEntry.ScenePath;

        public FrameworkContentRequiredness Requiredness => PlanEntry.Requiredness;

        public ActivityContentSceneLoadMode LoadMode => PlanEntry.LoadMode;

        public ActivityContentReleasePolicy ReleasePolicy => PlanEntry.ReleasePolicy;

        public int ExecutionOrder => PlanEntry.ExecutionOrder;

        public bool Planned => Status == ActivitySceneCompositionEntryStatus.Planned;

        public bool NotExecutionReady => Status == ActivitySceneCompositionEntryStatus.NotExecutionReady;

        public bool Loaded => Status == ActivitySceneCompositionEntryStatus.Loaded;

        public bool AlreadyLoaded => Status == ActivitySceneCompositionEntryStatus.AlreadyLoaded;

        public bool Failed => Status == ActivitySceneCompositionEntryStatus.Failed;

        public bool Skipped => Status == ActivitySceneCompositionEntryStatus.Skipped;

        public bool HasIssue => NotExecutionReady || Failed || Skipped || BlocksComposition;

        public string ToDiagnosticString()
        {
            string scene = !string.IsNullOrWhiteSpace(SceneName) ? SceneName : ScenePath;
            if (string.IsNullOrWhiteSpace(scene))
            {
                scene = "<missing>";
            }

            string identity = ContentIdentity.IsValid ? ContentIdentity.StableText : "<missing>";
            string contentId = ContentId.ToDiagnosticText("<missing>");
            string message = Message.ToDiagnosticText();
            return $"identity='{identity}' id='{contentId}' scene='{scene}' requiredness='{Requiredness}' loadMode='{LoadMode}' releasePolicy='{ReleasePolicy}' order='{ExecutionOrder}' status='{Status}' blocksComposition='{BlocksComposition}' message='{message}'";
        }

        public static ActivitySceneCompositionResultEntry PlannedEntry(ActivitySceneCompositionPlanEntry planEntry)
        {
            return new ActivitySceneCompositionResultEntry(
                planEntry,
                ActivitySceneCompositionEntryStatus.Planned,
                false,
                "Activity scene declaration is execution-ready. F25B records planning evidence only; loading is deferred.");
        }

        public static ActivitySceneCompositionResultEntry NotExecutionReadyEntry(ActivitySceneCompositionPlanEntry planEntry)
        {
            bool blocksComposition = planEntry.Requiredness == FrameworkContentRequiredness.Required;
            string message = blocksComposition
                ? "Required Activity scene declaration is not execution-ready. Scene, explicit content id and content identity are required."
                : "Optional Activity scene declaration is not execution-ready and was skipped.";

            return new ActivitySceneCompositionResultEntry(
                planEntry,
                ActivitySceneCompositionEntryStatus.NotExecutionReady,
                blocksComposition,
                message);
        }

        public static ActivitySceneCompositionResultEntry LoadedEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            SceneLifecycleLoadResult loadResult)
        {
            return new ActivitySceneCompositionResultEntry(
                planEntry,
                loadResult.AlreadyLoaded ? ActivitySceneCompositionEntryStatus.AlreadyLoaded : ActivitySceneCompositionEntryStatus.Loaded,
                false,
                loadResult.Message);
        }

        public static ActivitySceneCompositionResultEntry AlreadyLoadedTrackedEntry(ActivitySceneCompositionPlanEntry planEntry)
        {
            return new ActivitySceneCompositionResultEntry(
                planEntry,
                ActivitySceneCompositionEntryStatus.AlreadyLoaded,
                false,
                "Activity scene composition skipped load because the Activity-owned scene is already tracked and loaded.");
        }

        public static ActivitySceneCompositionResultEntry FailedEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            SceneLifecycleLoadResult loadResult)
        {
            bool blocksComposition = planEntry.Requiredness == FrameworkContentRequiredness.Required;
            return new ActivitySceneCompositionResultEntry(
                planEntry,
                ActivitySceneCompositionEntryStatus.Failed,
                blocksComposition,
                loadResult.Message);
        }

        public static ActivitySceneCompositionResultEntry SkippedEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            string message)
        {
            return new ActivitySceneCompositionResultEntry(
                planEntry,
                ActivitySceneCompositionEntryStatus.Skipped,
                false,
                message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
