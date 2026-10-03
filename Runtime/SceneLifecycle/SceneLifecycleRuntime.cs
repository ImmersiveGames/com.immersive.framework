using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.Loading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Minimal owner for scene lifecycle operations.
    /// It resolves, loads and activates the Startup Route primary scene when requested by Game Flow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class SceneLifecycleRuntime
    {
        private const string AlreadyLoadedMode = "AlreadyLoaded";
        private const string SingleLoadMode = "Single";
        private const string AdditiveLoadMode = "Additive";
        private readonly ISceneLifecycleParticipant[] _participants;
        private readonly List<ISceneLifecycleParticipant> _sessionParticipants = new();
        private readonly HashSet<SceneCompositionScope> _availableScopes = new();

        internal SceneLifecycleRuntime(params ISceneLifecycleParticipant[] participants)
        {
            _participants = participants ?? Array.Empty<ISceneLifecycleParticipant>();
        }

        internal async Task<SceneLifecycleLoadResult> LoadPrimarySceneAsync(RouteAsset route)
        {
            return await LoadPrimarySceneAsync(route, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<SceneLifecycleLoadResult> LoadPrimarySceneAsync(
            RouteAsset route,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            if (route == null)
            {
                return SceneLifecycleLoadResult.Failed("Route is missing.");
            }

            if (!route.HasPrimaryScene)
            {
                return SceneLifecycleLoadResult.Failed("Route Primary Scene is missing.");
            }

            string sceneName = route.PrimarySceneName;
            string scenePath = route.PrimaryScenePath;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return SceneLifecycleLoadResult.Failed("Route Primary Scene name is empty.");
            }

            var activeScene = SceneManager.GetActiveScene();
            if (IsSceneMatch(activeScene, scenePath, sceneName) && activeScene.isLoaded)
            {
                if (!NotifySceneAvailable(activeScene, out string lifecycleIssue))
                {
                    return SceneLifecycleLoadResult.Failed(lifecycleIssue);
                }
                return SceneLifecycleLoadResult.LoadedPrimaryScene(sceneName, scenePath, true, AlreadyLoadedMode);
            }

            var loadedScene = FindLoadedScene(scenePath, sceneName);
            bool alreadyLoaded = loadedScene.IsValid() && loadedScene.isLoaded;
            string loadMode = AlreadyLoadedMode;

            if (!alreadyLoaded)
            {
                if (!ReleaseLoadedScenesForSingleLoad(activeScene, out string releaseIssue))
                {
                    return SceneLifecycleLoadResult.Failed(releaseIssue);
                }
                var loadResult = await TryLoadSceneSingleAsync(scenePath, sceneName, progressReporter);
                if (!loadResult.Loaded)
                {
                    TryCompensateWithSceneAvailable(activeScene, out string replacementCompensationDiagnostic);
                    return replacementCompensationDiagnostic.Length == 0
                        ? loadResult
                        : SceneLifecycleLoadResult.Failed(loadResult.Message + replacementCompensationDiagnostic);
                }

                loadMode = SingleLoadMode;
                loadedScene = FindLoadedScene(scenePath, sceneName);
            }

            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle could not resolve loaded Primary Scene '{sceneName}' after load.");
            }

            if (!IsSceneMatch(SceneManager.GetActiveScene(), scenePath, sceneName))
            {
                if (!SceneManager.SetActiveScene(loadedScene))
                {
                    var currentActiveScene = SceneManager.GetActiveScene();
                    if (!IsSceneMatch(currentActiveScene, scenePath, sceneName) || !currentActiveScene.isLoaded)
                    {
                        return SceneLifecycleLoadResult.Failed(
                            $"Scene Lifecycle failed to set Primary Scene '{sceneName}' as active.");
                    }
                }
            }

            if (!NotifySceneAvailable(loadedScene, out string loadedAvailableIssue))
            {
                return SceneLifecycleLoadResult.Failed(loadedAvailableIssue);
            }

            return SceneLifecycleLoadResult.LoadedPrimaryScene(sceneName, scenePath, alreadyLoaded, loadMode);
        }


        internal async Task<SceneLifecycleLoadResult> LoadAdditiveSceneAsync(string sceneName, string scenePath)
        {
            return await LoadAdditiveSceneAsync(sceneName, scenePath, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<SceneLifecycleLoadResult> LoadAdditiveSceneAsync(
            string sceneName,
            string scenePath,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            sceneName = Normalize(sceneName);
            scenePath = Normalize(scenePath);
            if (string.IsNullOrWhiteSpace(sceneName) && string.IsNullOrWhiteSpace(scenePath))
            {
                return SceneLifecycleLoadResult.Failed("Scene Lifecycle cannot load Additive Scene because scene name and path are empty.");
            }

            var loadedScene = FindLoadedScene(scenePath, sceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                if (!NotifySceneAvailable(loadedScene, out string availableIssue))
                {
                    return SceneLifecycleLoadResult.Failed(availableIssue);
                }
                return SceneLifecycleLoadResult.LoadedAdditiveScene(
                    GetSceneNameForDiagnostics(loadedScene, sceneName),
                    GetScenePathForDiagnostics(loadedScene, scenePath),
                    true,
                    AlreadyLoadedMode);
            }

            var loadResult = await TryLoadSceneAdditiveAsync(scenePath, sceneName, progressReporter);
            if (!loadResult.Loaded)
            {
                return loadResult;
            }

            loadedScene = FindLoadedScene(scenePath, sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle could not resolve loaded Additive Scene '{ResolveSceneLabel(scenePath, sceneName)}' after load.");
            }

            if (!NotifySceneAvailable(loadedScene, out string loadedAdditiveAvailableIssue))
            {
                return SceneLifecycleLoadResult.Failed(loadedAdditiveAvailableIssue);
            }

            return SceneLifecycleLoadResult.LoadedAdditiveScene(
                GetSceneNameForDiagnostics(loadedScene, sceneName),
                GetScenePathForDiagnostics(loadedScene, scenePath),
                false,
                AdditiveLoadMode);
        }


        internal async Task<SceneLifecycleUnloadResult> UnloadSceneAsync(string sceneName, string scenePath)
        {
            return await UnloadSceneAsync(sceneName, scenePath, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<SceneLifecycleUnloadResult> UnloadSceneAsync(
            string sceneName,
            string scenePath,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            sceneName = Normalize(sceneName);
            scenePath = Normalize(scenePath);
            if (string.IsNullOrWhiteSpace(sceneName) && string.IsNullOrWhiteSpace(scenePath))
            {
                return SceneLifecycleUnloadResult.Failed("Scene Lifecycle cannot unload Scene because scene name and path are empty.");
            }

            var loadedScene = FindLoadedScene(scenePath, sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return SceneLifecycleUnloadResult.SkippedScene(
                    sceneName,
                    scenePath,
                    $"Scene Lifecycle skipped unload for Scene '{ResolveSceneLabel(scenePath, sceneName)}' because it is not loaded.");
            }

            if (IsSceneMatch(SceneManager.GetActiveScene(), scenePath, sceneName))
            {
                return SceneLifecycleUnloadResult.Failed(
                    $"Scene Lifecycle cannot unload active Scene '{ResolveSceneLabel(scenePath, sceneName)}'. Active Primary Scene is controlled by Single load.");
            }

            if (!NotifySceneReleasing(loadedScene, "scene-unload", out string releaseIssue))
            {
                return SceneLifecycleUnloadResult.Failed(releaseIssue);
            }

            try
            {
                string sceneLabel = ResolveSceneLabel(scenePath, sceneName);
                var operation = SceneManager.UnloadSceneAsync(loadedScene);
                if (operation == null)
                {
                    TryCompensateWithSceneAvailable(loadedScene, out string startCompensationDiagnostic);
                    return SceneLifecycleUnloadResult.Failed(
                        $"Scene Lifecycle failed to start unloading Scene '{sceneLabel}'.{startCompensationDiagnostic}");
                }

                await ReportDeterminateProgressAsync(
                    progressReporter,
                    0f,
                    "SceneUnload",
                    $"Unloading Scene '{sceneLabel}'.");

                float lastReportedProgress = 0f;
                while (!operation.isDone)
                {
                    float normalizedProgress = NormalizeAsyncOperationProgress(operation.progress, divideByActivationGate: false);
                    if (ShouldReportProgress(lastReportedProgress, normalizedProgress))
                    {
                        lastReportedProgress = normalizedProgress;
                        await ReportDeterminateProgressAsync(
                            progressReporter,
                            normalizedProgress,
                            "SceneUnload",
                            $"Unloading Scene '{sceneLabel}'.");
                    }

                    await Awaitable.NextFrameAsync();
                }

                await ReportDeterminateProgressAsync(
                    progressReporter,
                    1f,
                    "SceneUnload",
                    $"Scene '{sceneLabel}' unloaded.");

                var remainingScene = FindLoadedScene(scenePath, sceneName);
                if (remainingScene.IsValid() && remainingScene.isLoaded)
                {
                    TryCompensateWithSceneAvailable(remainingScene, out string stillLoadedCompensationDiagnostic);
                    return SceneLifecycleUnloadResult.Failed(
                        $"Scene Lifecycle could not confirm Scene '{ResolveSceneLabel(scenePath, sceneName)}' was unloaded.{stillLoadedCompensationDiagnostic}");
                }

                return SceneLifecycleUnloadResult.UnloadedScene(
                    GetSceneNameForDiagnostics(loadedScene, sceneName),
                    GetScenePathForDiagnostics(loadedScene, scenePath));
            }
            catch (Exception exception)
            {
                TryCompensateWithSceneAvailable(loadedScene, out string exceptionCompensationDiagnostic);
                return SceneLifecycleUnloadResult.Failed(
                    $"Scene Lifecycle failed to unload Scene '{ResolveSceneLabel(scenePath, sceneName)}'. {exception.GetType().Name}: {exception.Message}{exceptionCompensationDiagnostic}");
            }
        }

        internal bool IsSceneLoaded(string sceneName, string scenePath)
        {
            sceneName = Normalize(sceneName);
            scenePath = Normalize(scenePath);
            var loadedScene = FindLoadedScene(scenePath, sceneName);
            return loadedScene.IsValid() && loadedScene.isLoaded;
        }


        private static Task<SceneLifecycleLoadResult> TryLoadSceneSingleAsync(
            string scenePath,
            string sceneName,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return TryLoadSceneAsync(
                scenePath,
                sceneName,
                LoadSceneMode.Single,
                "Primary Scene",
                SingleLoadMode,
                SceneLifecycleLoadResult.LoadedPrimaryScene,
                progressReporter);
        }

        private static Task<SceneLifecycleLoadResult> TryLoadSceneAdditiveAsync(
            string scenePath,
            string sceneName,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return TryLoadSceneAsync(
                scenePath,
                sceneName,
                LoadSceneMode.Additive,
                "Additive Scene",
                AdditiveLoadMode,
                SceneLifecycleLoadResult.LoadedAdditiveScene,
                progressReporter);
        }

        private static async Task<SceneLifecycleLoadResult> TryLoadSceneAsync(
            string scenePath,
            string sceneName,
            LoadSceneMode sceneLoadMode,
            string sceneRoleLabel,
            string resultLoadMode,
            Func<string, string, bool, string, SceneLifecycleLoadResult> createLoadedResult,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            string sceneLabel = ResolveSceneLabel(scenePath, sceneName);
            if (!TryGetLoadSceneIdentifier(scenePath, sceneName, out string sceneIdentifier))
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle cannot load {sceneRoleLabel} '{sceneLabel}'. Add it to the active Build Profile or Shared Scene List.");
            }

            try
            {
                var operation = SceneManager.LoadSceneAsync(sceneIdentifier, sceneLoadMode);
                if (operation == null)
                {
                    return SceneLifecycleLoadResult.Failed(
                        $"Scene Lifecycle failed to start loading {sceneRoleLabel} '{sceneLabel}'. Make sure the scene is included in Build Settings.");
                }

                await ReportDeterminateProgressAsync(
                    progressReporter,
                    0f,
                    "SceneLoad",
                    $"Loading {sceneRoleLabel} '{sceneLabel}'.");

                float lastReportedProgress = 0f;
                while (!operation.isDone)
                {
                    float normalizedProgress = NormalizeAsyncOperationProgress(operation.progress, divideByActivationGate: true);
                    if (ShouldReportProgress(lastReportedProgress, normalizedProgress))
                    {
                        lastReportedProgress = normalizedProgress;
                        await ReportDeterminateProgressAsync(
                            progressReporter,
                            normalizedProgress,
                            "SceneLoad",
                            $"Loading {sceneRoleLabel} '{sceneLabel}'.");
                    }

                    await Awaitable.NextFrameAsync();
                }

                await ReportDeterminateProgressAsync(
                    progressReporter,
                    1f,
                    "SceneLoad",
                    $"{sceneRoleLabel} '{sceneLabel}' loaded.");

                return createLoadedResult(sceneName, scenePath, false, resultLoadMode);
            }
            catch (Exception exception)
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle failed to load {sceneRoleLabel} '{sceneLabel}'. {exception.GetType().Name}: {exception.Message}");
            }
        }

        private static async Awaitable ReportDeterminateProgressAsync(
            IFrameworkLoadingProgressReporter progressReporter,
            float value01,
            string phase,
            string message)
        {
            if (progressReporter == null || !progressReporter.IsEnabled)
            {
                return;
            }

            await progressReporter.ReportAsync(FrameworkLoadingProgress.Determinate(value01, phase, message));
        }

        private static float NormalizeAsyncOperationProgress(float operationProgress, bool divideByActivationGate)
        {
            if (float.IsNaN(operationProgress) || float.IsInfinity(operationProgress))
            {
                return 0f;
            }

            float normalized = divideByActivationGate
                ? operationProgress / 0.9f
                : operationProgress;

            return Clamp01(normalized);
        }

        private static bool ShouldReportProgress(float lastReportedProgress, float currentProgress)
        {
            if (currentProgress >= 1f && lastReportedProgress < 1f)
            {
                return true;
            }

            return currentProgress > lastReportedProgress
                && currentProgress - lastReportedProgress >= 0.01f;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private static bool TryGetLoadSceneIdentifier(string scenePath, string sceneName, out string sceneIdentifier)
        {
            if (!string.IsNullOrWhiteSpace(scenePath) && Application.CanStreamedLevelBeLoaded(scenePath))
            {
                sceneIdentifier = scenePath;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            {
                sceneIdentifier = sceneName;
                return true;
            }

            sceneIdentifier = null;
            return false;
        }

        private static Scene FindLoadedScene(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                var sceneByPath = SceneManager.GetSceneByPath(scenePath);
                if (sceneByPath.IsValid() && sceneByPath.isLoaded)
                {
                    return sceneByPath;
                }

                // Path was the requested identity and failed. Do not resolve a different
                // scene that merely shares the same name.
                return default;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return default;
            }

            var sceneByName = SceneManager.GetSceneByName(sceneName);
            return sceneByName.IsValid() && sceneByName.isLoaded ? sceneByName : default;
        }

        private static string GetSceneNameForDiagnostics(Scene scene, string fallbackSceneName)
        {
            if (scene.IsValid() && !string.IsNullOrWhiteSpace(scene.name))
            {
                return scene.name;
            }

            return Normalize(fallbackSceneName);
        }

        private static string GetScenePathForDiagnostics(Scene scene, string fallbackScenePath)
        {
            if (scene.IsValid() && !string.IsNullOrWhiteSpace(scene.path))
            {
                return scene.path;
            }

            return Normalize(fallbackScenePath);
        }

        private static string ResolveSceneLabel(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                return sceneName;
            }

            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return scenePath;
            }

            return "<missing>";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }

        private static bool IsSceneMatch(Scene scene, string scenePath, string sceneName)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            // Path wins as functional identity when present; name is legacy-only when path is empty.
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase);
            }

            return !string.IsNullOrWhiteSpace(sceneName)
                && string.Equals(scene.name, sceneName, StringComparison.OrdinalIgnoreCase);
        }

        private bool ReleaseLoadedScenesForSingleLoad(Scene activeScene, out string issue)
        {
            issue = string.Empty;
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return true;
            }
            return NotifySceneReleasing(activeScene, "single-scene-replacement", out issue);
        }

        /// <summary>
        /// Reconciles Scene Lifecycle participants with a Scene that remained really loaded
        /// after NotifySceneReleasing already succeeded for it but the subsequent Unity scene
        /// operation (unload or Single-load replacement) failed. This does not give
        /// ISceneLifecycleParticipant an exactly-once contract: NotifySceneAvailable already
        /// fires more than once per physical Scene lifetime today (every AlreadyLoaded re-entry
        /// in LoadPrimarySceneAsync/LoadAdditiveSceneAsync), so re-notifying availability here is
        /// the same, already-existing notification semantics applied to one more real case, not a
        /// new one. It never changes a failed operation into a success; it only restores
        /// participant composition to match the Scene that factually remained valid and loaded.
        /// If the Scene is no longer valid/loaded, there is nothing to reconcile and this is a
        /// no-op (empty diagnostic, returns true).
        /// </summary>
        private bool TryCompensateWithSceneAvailable(Scene scene, out string compensationDiagnostic)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                compensationDiagnostic = string.Empty;
                return true;
            }

            if (!NotifySceneAvailable(scene, out string issue))
            {
                compensationDiagnostic =
                    $" Scene Lifecycle composition restoration also failed for Scene '{scene.name.NormalizeTextOrFallback("<unnamed>")}' that remained loaded after the failed operation. {issue}";
                return false;
            }

            compensationDiagnostic =
                $" Scene Lifecycle composition was restored for Scene '{scene.name.NormalizeTextOrFallback("<unnamed>")}' that remained loaded after the failed operation.";
            return true;
        }

        private bool NotifySceneAvailable(Scene scene, out string issue)
        {
            issue = string.Empty;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return true;
            }
            IReadOnlyList<GameObject> roots = scene.GetRootGameObjects();
            SceneCompositionResult result = NotifyAvailable(
                SceneCompositionScope.ForScene(scene),
                roots);
            issue = result.Diagnostic;
            return result.Succeeded;
        }

        private bool NotifySceneReleasing(Scene scene, string reason, out string issue)
        {
            issue = string.Empty;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return true;
            }
            IReadOnlyList<GameObject> roots = scene.GetRootGameObjects();
            SceneCompositionResult result = NotifyReleasing(
                SceneCompositionScope.ForScene(scene),
                roots,
                reason);
            issue = result.Diagnostic;
            return result.Succeeded;
        }

        internal SceneCompositionResult ComposeSessionScope(
            UnityEngine.Object sessionOwner,
            IReadOnlyList<GameObject> roots)
        {
            SceneCompositionScope scope = SceneCompositionScope.ForSession(sessionOwner);
            if (!scope.IsValid)
            {
                return SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Available,
                    "Session composition requires an explicit live Session owner.");
            }

            List<ISceneLifecycleParticipant> discoveredParticipants =
                CollectCompositionParticipants(roots);
            ISceneLifecycleParticipant[] participants =
                CreateParticipantSnapshot(discoveredParticipants);
            SceneCompositionResult result = NotifyAvailable(scope, roots, participants);
            if (result.Succeeded)
            {
                for (int index = 0; index < discoveredParticipants.Count; index++)
                {
                    _sessionParticipants.Add(discoveredParticipants[index]);
                }
            }

            return result;
        }

        internal SceneCompositionResult ReleaseSessionScope(
            UnityEngine.Object sessionOwner,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            SceneCompositionScope scope = SceneCompositionScope.ForSession(sessionOwner);
            if (!scope.IsValid)
            {
                return SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Releasing,
                    "Session composition release requires the exact live Session owner.");
            }

            SceneCompositionResult result = NotifyReleasing(
                scope,
                roots,
                reason,
                CreateParticipantSnapshot());
            if (result.Succeeded)
            {
                _sessionParticipants.Clear();
            }

            return result;
        }

        private SceneCompositionResult NotifyAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            return NotifyAvailable(scope, roots, CreateParticipantSnapshot());
        }

        private SceneCompositionResult NotifyAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            IReadOnlyList<ISceneLifecycleParticipant> participants)
        {
            if (!scope.IsValid)
            {
                return SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Available,
                    "Scene composition rejected an invalid scope.");
            }

            if (participants == null || participants.Count == 0)
            {
                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Available,
                    $"Scene Lifecycle composition completed for scope '{scope.Label}' with no participants.");
            }

            bool wasAlreadyAvailable = _availableScopes.Contains(scope);
            var completedParticipants = new List<ISceneLifecycleParticipant>();
            for (int i = 0; i < participants.Count; i++)
            {
                ISceneLifecycleParticipant participant = participants[i];
                if (participant == null)
                {
                    continue;
                }

                SceneCompositionResult participantResult =
                    participant.OnSceneAvailable(scope, roots);
                if (!participantResult.Succeeded ||
                    !participantResult.Scope.Equals(scope) ||
                    participantResult.Operation != SceneCompositionOperation.Available)
                {
                    string issue = participantResult.Diagnostic.NormalizeTextOrFallback(
                        "Participant rejected scene composition.");
                    var rollbackIssues = new List<string>();
                    for (int rollbackIndex = wasAlreadyAvailable ? -1 : completedParticipants.Count - 1;
                         rollbackIndex >= 0;
                         rollbackIndex--)
                    {
                        ISceneLifecycleParticipant completed = completedParticipants[rollbackIndex];
                        SceneCompositionResult rollback = completed.OnSceneReleasing(
                            scope, roots, "composition-bind-rollback");
                        if (!rollback.Succeeded)
                            rollbackIssues.Add($"participant='{completed.GetType().Name}' {rollback.Diagnostic}");
                    }
                    return SceneCompositionResult.Rejected(
                        scope,
                        SceneCompositionOperation.Available,
                        $"Scene Lifecycle composition rejected scope '{scope.Label}' at participant '{participant.GetType().Name}'. {issue}" +
                        (wasAlreadyAvailable
                            ? " Reentry rollback was limited to the failing feature's local bind pass."
                            : rollbackIssues.Count == 0
                                ? " Rollback='Succeeded'."
                                : $" Rollback='Failed'. {string.Join(" | ", rollbackIssues)}"));
                }

                completedParticipants.Add(participant);
            }

            _availableScopes.Add(scope);

            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Available,
                $"Scene Lifecycle composition completed for scope '{scope.Label}'.");
        }

        private SceneCompositionResult NotifyReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            return NotifyReleasing(scope, roots, reason, CreateParticipantSnapshot());
        }

        private SceneCompositionResult NotifyReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason,
            IReadOnlyList<ISceneLifecycleParticipant> participants)
        {
            if (!scope.IsValid)
            {
                return SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Releasing,
                    "Scene composition release rejected an invalid scope.");
            }

            if (participants == null || participants.Count == 0)
            {
                _availableScopes.Remove(scope);
                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Releasing,
                    $"Scene Lifecycle release completed for scope '{scope.Label}' with no participants.");
            }

            var issues = new List<string>();
            for (int i = participants.Count - 1; i >= 0; i--)
            {
                ISceneLifecycleParticipant participant = participants[i];
                if (participant == null)
                {
                    continue;
                }

                SceneCompositionResult participantResult =
                    participant.OnSceneReleasing(scope, roots, reason);
                if (!participantResult.Succeeded ||
                    !participantResult.Scope.Equals(scope) ||
                    participantResult.Operation != SceneCompositionOperation.Releasing)
                {
                    string issue = participantResult.Diagnostic.NormalizeTextOrFallback(
                        "Participant rejected scene release.");
                    issues.Add($"participant='{participant.GetType().Name}' {issue}");
                }
            }

            if (issues.Count > 0)
            {
                string compensation = string.Empty;
                if (scope.Kind == SceneCompositionScopeKind.Scene)
                {
                    _availableScopes.Remove(scope);
                    SceneCompositionResult restored = NotifyAvailable(scope, roots);
                    compensation = restored.Succeeded
                        ? " Composition was restored because the Scene remains loaded."
                        : $" Composition restoration failed: {restored.Diagnostic}";
                }

                return SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Releasing,
                    $"Scene Lifecycle release rejected scope '{scope.Label}'. {string.Join(" | ", issues)}{compensation}");
            }

            _availableScopes.Remove(scope);

            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Releasing,
                $"Scene Lifecycle release completed for scope '{scope.Label}' reason='{reason.NormalizeTextOrFallback("scene-release")}'.");
        }

        private List<ISceneLifecycleParticipant> CollectCompositionParticipants(
            IReadOnlyList<GameObject> roots)
        {
            var discovered = new List<ISceneLifecycleParticipant>();
            if (roots == null)
            {
                return discovered;
            }

            var seen = new HashSet<ISceneLifecycleParticipant>();
            ISceneLifecycleParticipant[] existing = CreateParticipantSnapshot();
            for (int index = 0; index < existing.Length; index++)
            {
                if (existing[index] != null)
                {
                    seen.Add(existing[index]);
                }
            }

            var seenRoots = new HashSet<GameObject>();
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root))
                {
                    continue;
                }

                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is ISceneLifecycleParticipant participant &&
                        seen.Add(participant))
                    {
                        discovered.Add(participant);
                    }
                }
            }

            return discovered;
        }

        private ISceneLifecycleParticipant[] CreateParticipantSnapshot(
            IReadOnlyList<ISceneLifecycleParticipant> additional = null)
        {
            var result = new List<ISceneLifecycleParticipant>();
            var seen = new HashSet<ISceneLifecycleParticipant>();
            AddParticipants(_participants, result, seen);
            AddParticipants(_sessionParticipants, result, seen);
            AddParticipants(additional, result, seen);
            return result.ToArray();
        }

        private static void AddParticipants(
            IReadOnlyList<ISceneLifecycleParticipant> candidates,
            List<ISceneLifecycleParticipant> result,
            HashSet<ISceneLifecycleParticipant> seen)
        {
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                ISceneLifecycleParticipant candidate = candidates[index];
                if (candidate != null &&
                    (candidate is not UnityEngine.Object unityObject || unityObject != null) &&
                    seen.Add(candidate))
                {
                    result.Add(candidate);
                }
            }
        }
    }
}
