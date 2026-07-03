using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Loading;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Executes Activity-owned additive scene composition plans and releases Activity-owned scenes on Activity change.
    /// F25C added additive load. F25D adds ReleaseOnActivityChange unload.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25D Activity scene composition execution and Activity-change release.")]
    internal sealed class ActivitySceneCompositionRuntime
    {
        private readonly SceneLifecycleRuntime _sceneLifecycleRuntime;
        private readonly ActivitySceneLedger _ledger = new ActivitySceneLedger();
        private RouteAsset _currentRoute;
        private string _currentRouteInstanceId = string.Empty;

        internal ActivitySceneCompositionRuntime(SceneLifecycleRuntime sceneLifecycleRuntime)
        {
            _sceneLifecycleRuntime = sceneLifecycleRuntime ?? throw new ArgumentNullException(nameof(sceneLifecycleRuntime));
        }

        internal void SetRouteContext(RouteAsset route, string routeInstanceId)
        {
            _currentRoute = route;
            _currentRouteInstanceId = Normalize(routeInstanceId);
        }

        internal int LedgerEntryCount => _ledger.EntryCount;

        internal int LedgerLoadedCount => _ledger.LoadedCount;

        internal int LedgerReleasedCount => _ledger.ReleasedCount;

        internal int LedgerStaleCount => _ledger.StaleCount;

        internal int PreviewReleaseForRouteChangeCount()
        {
            return _ledger.CollectLoadedForRouteInstance(_currentRouteInstanceId).Count;
        }

        internal int PreviewReleaseForActivityChangeCount(ActivityAsset activity)
        {
            if (activity == null)
            {
                return 0;
            }

            List<ActivitySceneLedgerEntry> records = _ledger.CollectLoadedForActivityRouteInstance(activity, _currentRouteInstanceId);
            int count = 0;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].PlanEntry.ReleasePolicy != ActivityContentReleasePolicy.KeepOnActivityChange)
                {
                    count++;
                }
            }

            return count;
        }

        internal ActivityContentDiscoveryScope CreateActivityContentDiscoveryScope(ActivityAsset activity)
        {
            return CreateActivityContentDiscoveryScope(activity, null);
        }

        internal ActivityContentDiscoveryScope CreateActivityContentDiscoveryScope(ActivityAsset firstActivity, ActivityAsset secondActivity)
        {
            var scenes = new List<ActivityContentDiscoveryScene>();
            var sceneKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddActivityOwnedDiscoveryScenes(firstActivity, scenes, sceneKeys);
            if (!ReferenceEquals(firstActivity, secondActivity))
            {
                AddActivityOwnedDiscoveryScenes(secondActivity, scenes, sceneKeys);
            }

            return new ActivityContentDiscoveryScope(_currentRoute, scenes);
        }

        internal ActivityOperationPlan CreateActivityOperationPlan(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            string source,
            string reason)
        {
            var scenes = new List<ActivityOperationPlanSceneEntry>();
            var issues = new List<ActivityOperationIssue>();

            AddReleaseEntries(operationKind, previousActivity, targetActivity, scenes, issues);
            AddLoadEntries(operationKind, targetActivity, scenes);

            return new ActivityOperationPlan(
                operationKind,
                previousActivity,
                targetActivity,
                visualMode,
                scenes,
                issues,
                source,
                reason);
        }

        internal Task<ActivitySceneCompositionResult> ExecuteAsync(ActivitySceneCompositionPlan plan)
        {
            return ExecuteAsync(plan, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<ActivitySceneCompositionResult> ExecuteAsync(
            ActivitySceneCompositionPlan plan,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            if (!plan.HasActivity || !plan.HasProfile || !plan.HasScenes)
            {
                return ActivitySceneCompositionResult.FromPlan(plan, plan.Source, plan.Reason);
            }

            var entries = new List<ActivitySceneCompositionResultEntry>(plan.SceneCount);
            int progressStepCount = CountExecutionReadyScenes(plan);
            int progressStepIndex = 0;
            for (int i = 0; i < plan.Scenes.Count; i++)
            {
                var scene = plan.Scenes[i];
                if (!scene.IsExecutionReady)
                {
                    entries.Add(ActivitySceneCompositionResultEntry.NotExecutionReadyEntry(scene));
                    continue;
                }

                if (_ledger.TryGetLoaded(plan.Activity, scene, _currentRouteInstanceId, out var ledgerEntry))
                {
                    if (_sceneLifecycleRuntime.IsSceneLoaded(scene.SceneName, scene.ScenePath))
                    {
                        entries.Add(ActivitySceneCompositionResultEntry.AlreadyLoadedTrackedEntry(scene));
                        continue;
                    }

                    _ledger.MarkStale(ledgerEntry);
                }

                var sceneProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedStepReporter(
                    progressReporter,
                    progressStepIndex,
                    progressStepCount,
                    "ActivitySceneComposition",
                    "Activity scene composition progress.");
                progressStepIndex++;

                var loadResult = await _sceneLifecycleRuntime.LoadAdditiveSceneAsync(
                    scene.SceneName,
                    scene.ScenePath,
                    sceneProgressReporter);
                if (loadResult.Loaded)
                {
                    var loadedEntry = ActivitySceneCompositionResultEntry.LoadedEntry(scene, loadResult);
                    entries.Add(loadedEntry);
                    TrackLoadedScene(plan.Activity, loadedEntry);
                    continue;
                }

                entries.Add(ActivitySceneCompositionResultEntry.FailedEntry(scene, loadResult));
            }

            await FrameworkLoadingProgressReporterUtility.ReportCompletedIfAnyAsync(
                progressReporter,
                "ActivitySceneComposition",
                "Activity scene composition progress completed.");

            return ActivitySceneCompositionResult.ExecutedResult(plan, entries, plan.Source, plan.Reason);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseOnActivityChangeAsync(
            ActivityAsset activity,
            string source,
            string reason)
        {
            return ReleaseOnActivityChangeAsync(activity, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseOnActivityChangeAsync(
            ActivityAsset activity,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return ReleaseTrackedScenesAsync(activity, forceReleaseAll: false, source, reason, progressReporter);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseForRouteChangeAsync(
            string source,
            string reason)
        {
            return ReleaseForRouteChangeAsync(source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseForRouteChangeAsync(
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return ReleaseTrackedScenesAsync(activity: null, forceReleaseAll: true, source, reason, progressReporter);
        }

        private async Task<ActivitySceneReleaseResult> ReleaseTrackedScenesAsync(
            ActivityAsset activity,
            bool forceReleaseAll,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            if (activity == null && !forceReleaseAll)
            {
                return ActivitySceneReleaseResult.NotRequested(
                    null,
                    source,
                    reason,
                    "Activity scene release was not requested because Activity is missing.");
            }

            List<ActivitySceneLedgerEntry> records = forceReleaseAll && activity == null
                ? _ledger.CollectLoadedForRouteInstance(_currentRouteInstanceId)
                : _ledger.CollectLoadedForActivityRouteInstance(activity, _currentRouteInstanceId);
            if (records.Count == 0)
            {
                return ActivitySceneReleaseResult.NotRequested(
                    activity,
                    source,
                    reason,
                    forceReleaseAll
                        ? "Activity scene release skipped because no Activity-owned scenes are tracked for route change."
                        : "Activity scene release skipped because no Activity-owned scenes are tracked for this Activity.");
            }

            var entries = new List<ActivitySceneReleaseResultEntry>(records.Count);
            int progressStepCount = CountReleasableRecords(records, forceReleaseAll);
            int progressStepIndex = 0;
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var planEntry = record.PlanEntry;
                if (!forceReleaseAll && planEntry.ReleasePolicy == ActivityContentReleasePolicy.KeepOnActivityChange)
                {
                    entries.Add(ActivitySceneReleaseResultEntry.SkippedKeepOnActivityChangeEntry(planEntry));
                    continue;
                }

                var sceneProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedStepReporter(
                    progressReporter,
                    progressStepIndex,
                    progressStepCount,
                    forceReleaseAll ? "RouteActivitySceneRelease" : "ActivitySceneRelease",
                    forceReleaseAll ? "Route activity scene release progress." : "Activity scene release progress.");
                progressStepIndex++;

                var unloadResult = await _sceneLifecycleRuntime.UnloadSceneAsync(
                    planEntry.SceneName,
                    planEntry.ScenePath,
                    sceneProgressReporter);
                if (unloadResult is { Completed: true, Unloaded: true })
                {
                    entries.Add(ActivitySceneReleaseResultEntry.UnloadedEntry(planEntry, unloadResult));
                    _ledger.MarkReleased(record);
                    continue;
                }

                if (unloadResult is { Completed: true, Skipped: true })
                {
                    entries.Add(ActivitySceneReleaseResultEntry.SkippedNotLoadedEntry(planEntry, unloadResult));
                    _ledger.MarkStale(record);
                    continue;
                }

                entries.Add(ActivitySceneReleaseResultEntry.FailedEntry(planEntry, unloadResult));
            }

            await FrameworkLoadingProgressReporterUtility.ReportCompletedIfAnyAsync(
                progressReporter,
                forceReleaseAll ? "RouteActivitySceneRelease" : "ActivitySceneRelease",
                forceReleaseAll ? "Route activity scene release progress completed." : "Activity scene release progress completed.");

            return ActivitySceneReleaseResult.FromEntries(activity, entries, source, reason);
        }

        private static int CountExecutionReadyScenes(ActivitySceneCompositionPlan plan)
        {
            if (!plan.HasScenes)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < plan.Scenes.Count; i++)
            {
                if (plan.Scenes[i].IsExecutionReady)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountReleasableRecords(IReadOnlyList<ActivitySceneLedgerEntry> records, bool forceReleaseAll)
        {
            if (records == null || records.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < records.Count; i++)
            {
                var planEntry = records[i].PlanEntry;
                if (forceReleaseAll || planEntry.ReleasePolicy != ActivityContentReleasePolicy.KeepOnActivityChange)
                {
                    count++;
                }
            }

            return count;
        }

        internal static bool ShouldExecute(ActivitySceneCompositionPlan plan)
        {
            return plan is { HasActivity: true, HasProfile: true, HasScenes: true, ExecutionReadySceneCount: > 0 };
        }

        private void AddLoadEntries(
            ActivityOperationKind operationKind,
            ActivityAsset targetActivity,
            List<ActivityOperationPlanSceneEntry> scenes)
        {
            if (!ShouldPlanTargetLoad(operationKind, targetActivity))
            {
                return;
            }

            var compositionPlan = ActivitySceneCompositionPlan.FromActivity(targetActivity, source: string.Empty, reason: string.Empty);
            if (!ShouldExecute(compositionPlan))
            {
                return;
            }

            for (int i = 0; i < compositionPlan.Scenes.Count; i++)
            {
                var scene = compositionPlan.Scenes[i];
                if (!scene.IsExecutionReady)
                {
                    continue;
                }

                bool isAlreadyLoaded = _sceneLifecycleRuntime.IsSceneLoaded(scene.SceneName, scene.ScenePath);
                scenes.Add(ActivityOperationPlanSceneEntry.FromCompositionPlanEntry(scene, isAlreadyLoaded));
            }
        }

        private void AddReleaseEntries(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            List<ActivityOperationPlanSceneEntry> scenes,
            List<ActivityOperationIssue> issues)
        {
            if (!ShouldPlanRelease(operationKind, previousActivity, targetActivity))
            {
                return;
            }

            List<ActivitySceneLedgerEntry> records = operationKind == ActivityOperationKind.RouteExitCleanup
                ? _ledger.CollectLoadedForRouteInstance(_currentRouteInstanceId)
                : _ledger.CollectLoadedForActivityRouteInstance(previousActivity, _currentRouteInstanceId);

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var planEntry = record.PlanEntry;
                if (operationKind != ActivityOperationKind.RouteExitCleanup
                    && planEntry.ReleasePolicy == ActivityContentReleasePolicy.KeepOnActivityChange)
                {
                    continue;
                }

                if (!_sceneLifecycleRuntime.IsSceneLoaded(planEntry.SceneName, planEntry.ScenePath))
                {
                    _ledger.MarkStale(record);
                    issues.Add(ActivityOperationIssue.Warning(
                        ActivityOperationIssueKind.StaleTrackedScene,
                        $"Ledger Activity scene '{ResolveSceneLabel(planEntry)}' is marked stale because Unity no longer reports it as loaded; release side-effect is not planned."));
                    continue;
                }

                scenes.Add(ActivityOperationPlanSceneEntry.ForRelease(
                    planEntry.ContentIdentity,
                    planEntry.ContentId,
                    planEntry.SceneName,
                    planEntry.ScenePath,
                    planEntry.Requiredness,
                    planEntry.LoadMode,
                    planEntry.ReleasePolicy,
                    planEntry.ExecutionOrder));
            }
        }

        private static bool ShouldPlanTargetLoad(ActivityOperationKind operationKind, ActivityAsset targetActivity)
        {
            return targetActivity != null
                && operationKind is ActivityOperationKind.Start or ActivityOperationKind.Switch or ActivityOperationKind.RouteStartup;
        }

        private static bool ShouldPlanRelease(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity)
        {
            if (operationKind == ActivityOperationKind.RouteExitCleanup)
            {
                return true;
            }

            if (previousActivity == null || ReferenceEquals(previousActivity, targetActivity))
            {
                return false;
            }

            return operationKind is ActivityOperationKind.Switch or ActivityOperationKind.Clear;
        }

        private static string ResolveSceneLabel(ActivitySceneCompositionPlanEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.SceneName))
            {
                return entry.SceneName;
            }

            if (!string.IsNullOrWhiteSpace(entry.ScenePath))
            {
                return entry.ScenePath;
            }

            return "<missing>";
        }

        private void TrackLoadedScene(ActivityAsset activity, ActivitySceneCompositionResultEntry entry)
        {
            _ledger.RegisterLoaded(
                _currentRouteInstanceId,
                _currentRoute,
                activity,
                entry);
        }

        private void AddActivityOwnedDiscoveryScenes(
            ActivityAsset activity,
            List<ActivityContentDiscoveryScene> scenes,
            HashSet<string> sceneKeys)
        {
            if (activity == null || scenes == null || sceneKeys == null)
            {
                return;
            }

            List<ActivitySceneLedgerEntry> records = _ledger.CollectLoadedForActivityRouteInstance(activity, _currentRouteInstanceId);
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (!_sceneLifecycleRuntime.IsSceneLoaded(record.SceneName, record.ScenePath))
                {
                    continue;
                }

                string sceneKey = CreateSceneKey(record.ScenePath, record.SceneName);
                if (string.IsNullOrWhiteSpace(sceneKey) || !sceneKeys.Add(sceneKey))
                {
                    continue;
                }

                scenes.Add(new ActivityContentDiscoveryScene(
                    record.Activity,
                    record.ScenePath,
                    record.SceneName));
            }
        }

        private static string CreateSceneKey(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return scenePath.Trim();
            }

            return sceneName.NormalizeText();
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
