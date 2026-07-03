using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Immutable evidence record for one scene after Route scene composition.
    /// This is evidence derived from scene loading and it does not authorize release/unload.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6E route scene composition result entry; additive execution evidence only, release deferred.")]
    internal readonly struct RouteSceneCompositionResultEntry
    {
        public RouteSceneCompositionResultEntry(
            RouteSceneCompositionPlanEntry planEntry,
            RouteSceneCompositionEntryStatus status,
            bool activeScene,
            bool blocksComposition,
            string message)
        {
            PlanEntry = planEntry;
            Status = status;
            ActiveScene = activeScene;
            BlocksComposition = blocksComposition;
            Message = Normalize(message);
        }

        public RouteSceneCompositionPlanEntry PlanEntry { get; }

        public RouteSceneCompositionEntryStatus Status { get; }

        public bool ActiveScene { get; }

        public bool BlocksComposition { get; }

        public string Message { get; }

        public FrameworkContentIdentity ContentIdentity => PlanEntry.ContentIdentity;

        public string ContentId => PlanEntry.ContentId;

        public string SceneName => PlanEntry.SceneName;

        public string ScenePath => PlanEntry.ScenePath;

        public RouteSceneRole SceneRole => PlanEntry.SceneRole;

        public FrameworkContentRequiredness Requiredness => PlanEntry.Requiredness;

        public RouteContentOwnership Ownership => PlanEntry.Ownership;

        public RouteSceneLoadMode LoadMode => PlanEntry.LoadMode;

        public int ExecutionOrder => PlanEntry.ExecutionOrder;

        public bool Loaded => Status is RouteSceneCompositionEntryStatus.Loaded or RouteSceneCompositionEntryStatus.AlreadyLoaded;

        public bool AlreadyLoaded => Status == RouteSceneCompositionEntryStatus.AlreadyLoaded;

        public bool Failed => Status == RouteSceneCompositionEntryStatus.Failed;

        public bool Skipped => Status == RouteSceneCompositionEntryStatus.Skipped;

        public bool NotExecuted => Status == RouteSceneCompositionEntryStatus.NotExecuted;

        public bool HasIssue => Failed || Skipped || NotExecuted || BlocksComposition;

        public bool IsOwnedLoaded => Loaded && Ownership == RouteContentOwnership.Owned;

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
            return $"identity='{identity}' id='{contentId}' scene='{scene}' role='{SceneRole}' requiredness='{Requiredness}' ownership='{Ownership}' loadMode='{LoadMode}' order='{ExecutionOrder}' status='{Status}' activeScene='{ActiveScene}' blocksComposition='{BlocksComposition}' message='{message}'";
        }

        public static RouteSceneCompositionResultEntry LoadedEntry(
            RouteSceneCompositionPlanEntry planEntry,
            bool alreadyLoaded,
            bool activeScene,
            string message)
        {
            return new RouteSceneCompositionResultEntry(
                planEntry,
                alreadyLoaded ? RouteSceneCompositionEntryStatus.AlreadyLoaded : RouteSceneCompositionEntryStatus.Loaded,
                activeScene,
                false,
                message);
        }

        public static RouteSceneCompositionResultEntry FailedEntry(
            RouteSceneCompositionPlanEntry planEntry,
            bool blocksComposition,
            string message)
        {
            return new RouteSceneCompositionResultEntry(
                planEntry,
                RouteSceneCompositionEntryStatus.Failed,
                false,
                blocksComposition,
                message);
        }

        public static RouteSceneCompositionResultEntry SkippedEntry(
            RouteSceneCompositionPlanEntry planEntry,
            bool blocksComposition,
            string message)
        {
            return new RouteSceneCompositionResultEntry(
                planEntry,
                RouteSceneCompositionEntryStatus.Skipped,
                false,
                blocksComposition,
                message);
        }

        public static RouteSceneCompositionResultEntry NotExecutedEntry(
            RouteSceneCompositionPlanEntry planEntry,
            string message)
        {
            return new RouteSceneCompositionResultEntry(
                planEntry,
                RouteSceneCompositionEntryStatus.NotExecuted,
                false,
                false,
                message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
