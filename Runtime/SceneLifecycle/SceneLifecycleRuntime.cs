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
                    return loadResult;
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
                    return SceneLifecycleUnloadResult.Failed(
                        $"Scene Lifecycle failed to start unloading Scene '{sceneLabel}'.");
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
                    return SceneLifecycleUnloadResult.Failed(
                        $"Scene Lifecycle could not confirm Scene '{ResolveSceneLabel(scenePath, sceneName)}' was unloaded.");
                }

                return SceneLifecycleUnloadResult.UnloadedScene(
                    GetSceneNameForDiagnostics(loadedScene, sceneName),
                    GetScenePathForDiagnostics(loadedScene, scenePath));
            }
            catch (Exception exception)
            {
                return SceneLifecycleUnloadResult.Failed(
                    $"Scene Lifecycle failed to unload Scene '{ResolveSceneLabel(scenePath, sceneName)}'. {exception.GetType().Name}: {exception.Message}");
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

        private bool NotifySceneAvailable(Scene scene, out string issue)
        {
            issue = string.Empty;
            if (_participants.Length == 0 || !scene.IsValid() || !scene.isLoaded)
            {
                return true;
            }
            IReadOnlyList<GameObject> roots = scene.GetRootGameObjects();
            for (int i = 0; i < _participants.Length; i++)
            {
                if (_participants[i] != null && !_participants[i].OnSceneAvailable(scene, roots, out issue))
                {
                    issue = $"Scene Lifecycle composition rejected scene '{scene.name}'. {issue}";
                    return false;
                }
            }
            return true;
        }

        private bool NotifySceneReleasing(Scene scene, string reason, out string issue)
        {
            issue = string.Empty;
            if (_participants.Length == 0 || !scene.IsValid() || !scene.isLoaded)
            {
                return true;
            }
            IReadOnlyList<GameObject> roots = scene.GetRootGameObjects();
            for (int i = 0; i < _participants.Length; i++)
            {
                if (_participants[i] != null && !_participants[i].OnSceneReleasing(scene, roots, reason, out issue))
                {
                    issue = $"Scene Lifecycle release rejected scene '{scene.name}'. {issue}";
                    return false;
                }
            }
            return true;
        }
    }
}
