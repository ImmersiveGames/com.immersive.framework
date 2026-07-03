using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable evidence record for one Activity-owned scene release operation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25D Activity scene release evidence; unloads ReleaseOnActivityChange scenes.")]
    internal readonly struct ActivitySceneReleaseResultEntry
    {
        public ActivitySceneReleaseResultEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            ActivitySceneReleaseEntryStatus status,
            bool blocksRelease,
            string message)
        {
            PlanEntry = planEntry;
            Status = status;
            BlocksRelease = blocksRelease;
            Message = Normalize(message);
        }

        public ActivitySceneCompositionPlanEntry PlanEntry { get; }

        public ActivitySceneReleaseEntryStatus Status { get; }

        public bool BlocksRelease { get; }

        public string Message { get; }

        public FrameworkContentIdentity ContentIdentity => PlanEntry.ContentIdentity;

        public string ContentId => PlanEntry.ContentId;

        public string SceneName => PlanEntry.SceneName;

        public string ScenePath => PlanEntry.ScenePath;

        public FrameworkContentRequiredness Requiredness => PlanEntry.Requiredness;

        public ActivityContentReleasePolicy ReleasePolicy => PlanEntry.ReleasePolicy;

        public bool Unloaded => Status == ActivitySceneReleaseEntryStatus.Unloaded;

        public bool Skipped => Status is ActivitySceneReleaseEntryStatus.SkippedKeepOnActivityChange or ActivitySceneReleaseEntryStatus.SkippedNotLoaded;

        public bool Failed => Status == ActivitySceneReleaseEntryStatus.Failed;

        public bool HasIssue => Failed || BlocksRelease;

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
            return $"identity='{identity}' id='{contentId}' scene='{scene}' requiredness='{Requiredness}' releasePolicy='{ReleasePolicy}' status='{Status}' blocksRelease='{BlocksRelease}' message='{message}'";
        }

        public static ActivitySceneReleaseResultEntry UnloadedEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            SceneLifecycleUnloadResult unloadResult)
        {
            return new ActivitySceneReleaseResultEntry(
                planEntry,
                ActivitySceneReleaseEntryStatus.Unloaded,
                false,
                unloadResult.Message);
        }

        public static ActivitySceneReleaseResultEntry SkippedKeepOnActivityChangeEntry(ActivitySceneCompositionPlanEntry planEntry)
        {
            return new ActivitySceneReleaseResultEntry(
                planEntry,
                ActivitySceneReleaseEntryStatus.SkippedKeepOnActivityChange,
                false,
                "Activity-owned scene kept loaded because Release Policy is KeepOnActivityChange for Activity changes.");
        }

        public static ActivitySceneReleaseResultEntry SkippedNotLoadedEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            SceneLifecycleUnloadResult unloadResult)
        {
            return new ActivitySceneReleaseResultEntry(
                planEntry,
                ActivitySceneReleaseEntryStatus.SkippedNotLoaded,
                false,
                unloadResult.Message);
        }

        public static ActivitySceneReleaseResultEntry FailedEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            SceneLifecycleUnloadResult unloadResult)
        {
            return new ActivitySceneReleaseResultEntry(
                planEntry,
                ActivitySceneReleaseEntryStatus.Failed,
                true,
                unloadResult.Message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
