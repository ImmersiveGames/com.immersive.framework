using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Loading;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.TransitionEffects;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.GlobalUi
{
    internal readonly struct PersistentContentOwnershipCommitResult
    {
        private PersistentContentOwnershipCommitResult(bool succeeded, string diagnostic)
        {
            Succeeded = succeeded;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal bool Succeeded { get; }

        internal string Diagnostic { get; }

        internal static PersistentContentOwnershipCommitResult SucceededWithDiagnostic(
            string diagnostic)
        {
            return new PersistentContentOwnershipCommitResult(true, diagnostic);
        }

        internal static PersistentContentOwnershipCommitResult Failed(string diagnostic)
        {
            return new PersistentContentOwnershipCommitResult(false, diagnostic);
        }
    }

    /// <summary>
    /// Internal runtime loader for the application Persistent Content Container Scene.
    ///
    /// The source scene is loaded additively and its complete authored roots remain there until
    /// Session composition succeeds. It then moves those roots to Unity's persistent runtime
    /// scene and unloads the source scene.
    ///
    /// This class keeps its historical internal name until the runtime namespace migration cut.
    /// It is not a public product surface.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "Persistent Content Container Scene loader. Historical internal type name retained until runtime namespace migration.")]
    internal sealed class GlobalUiSceneRuntime
    {
        private readonly ITransitionEffectAdapter[] _transitionAdapters;
        private readonly ILoadingSurfaceAdapter[] _loadingAdapters;
        private readonly GameObject[] _persistedRoots;
        private readonly IReadOnlyList<GameObject> _persistedRootsView;
        private readonly Scene _sourceScene;

        private GlobalUiSceneRuntime(
            string scenePath,
            string sceneName,
            Scene sourceScene,
            string label,
            IReadOnlyList<GameObject> persistedRoots,
            IReadOnlyList<ITransitionEffectAdapter> transitionAdapters,
            IReadOnlyList<ILoadingSurfaceAdapter> loadingAdapters,
            bool hasBlockingConfigurationIssue,
            string blockingConfigurationMessage,
            string message)
        {
            ScenePath = scenePath.NormalizeText();
            SceneName = sceneName.NormalizeText();
            _sourceScene = sourceScene;
            Label = label.NormalizeTextOrFallback("Persistent Content");
            PersistedRootCount = persistedRoots?.Count ?? 0;
            _persistedRoots =
                FrameworkCollectionCopy.ToArrayOrEmpty(persistedRoots);
            _persistedRootsView =
                Array.AsReadOnly(_persistedRoots);
            _transitionAdapters =
                FrameworkCollectionCopy.ToArrayOrEmpty(transitionAdapters);
            _loadingAdapters =
                FrameworkCollectionCopy.ToArrayOrEmpty(loadingAdapters);
            HasBlockingConfigurationIssue =
                hasBlockingConfigurationIssue;
            BlockingConfigurationMessage =
                blockingConfigurationMessage.NormalizeText();
            Message = message.NormalizeText();
        }

        public string ScenePath { get; }

        public string SceneName { get; }

        public string Label { get; }

        public int PersistedRootCount { get; }

        public int TransitionAdapterCount =>
            _transitionAdapters.Length;

        public int LoadingAdapterCount =>
            _loadingAdapters.Length;

        public bool HasBlockingConfigurationIssue { get; }

        public string BlockingConfigurationMessage { get; }

        public string Message { get; }

        public IReadOnlyList<ITransitionEffectAdapter> TransitionAdapters =>
            _transitionAdapters;

        public IReadOnlyList<ILoadingSurfaceAdapter> LoadingAdapters =>
            _loadingAdapters;

        internal IReadOnlyList<GameObject> PersistedRoots =>
            _persistedRootsView;

        internal int AttachActivityEntryCompletionReceivers(
            GameFlowRuntime gameFlowRuntime)
        {
            if (gameFlowRuntime == null)
            {
                throw new ArgumentNullException(nameof(gameFlowRuntime));
            }

            int attachedCount = 0;
            for (int rootIndex = 0;
                 rootIndex < _persistedRoots.Length;
                 rootIndex++)
            {
                GameObject root = _persistedRoots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours =
                    root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int behaviourIndex = 0;
                     behaviourIndex < behaviours.Length;
                     behaviourIndex++)
                {
                    if (behaviours[behaviourIndex] is not
                        IActivityContentEntryCompletionReceiver receiver)
                    {
                        continue;
                    }

                    gameFlowRuntime.AttachActivityEntryCompletionReceiver(
                        receiver);
                    attachedCount++;
                }
            }

            return attachedCount;
        }

        internal bool TryResolveCameraSessionEnvironment(
            out bool automaticSplitScreenEnabled,
            out string diagnostic)
        {
            automaticSplitScreenEnabled = false;

            List<CameraOutputAuthoring> legacyOutputs =
                FindAll<CameraOutputAuthoring>();
            if (legacyOutputs.Count > 0)
            {
                diagnostic =
                    $"Persistent Content contains '{legacyOutputs.Count}' CameraOutputAuthoring component(s). IF-ADR-038 requires physical Camera Outputs to be configured as GameApplication Camera Session Output prefabs.";
                return false;
            }

            List<PlayerInputManager> playerInputManagers =
                FindAll<PlayerInputManager>();
            for (int index = 0;
                 index < playerInputManagers.Count;
                 index++)
            {
                if (playerInputManagers[index].splitScreen)
                {
                    automaticSplitScreenEnabled = true;
                    break;
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        internal bool TryResolveLocalPlayerProvisioning(
            out LocalPlayerProvisioningAuthoring authoring,
            out bool isConfigured,
            out string diagnostic)
        {
            authoring = null;
            isConfigured = false;

            List<LocalPlayerProvisioningEndpointRegistration> registrations =
                FindAll<LocalPlayerProvisioningEndpointRegistration>();

            if (registrations.Count == 0)
            {
                diagnostic =
                    "Persistent Content has no Local Player Provisioning Host Registration. Local Player provisioning is explicitly unavailable.";
                return true;
            }

            isConfigured = true;
            if (registrations.Count != 1)
            {
                diagnostic =
                    $"Persistent Content requires exactly one Local Player Provisioning Host Registration when provisioning is configured, but found '{registrations.Count}'.";
                return false;
            }

            if (!registrations[0].TryResolveAuthoring(
                    out authoring,
                    out string issue))
            {
                diagnostic =
                    $"Persistent Content Local Player Provisioning Host Registration is invalid. {issue}";
                return false;
            }

            diagnostic =
                $"Resolved Local Player provisioning authoring '{authoring.name}' through the explicit Persistent Content Host Registration.";
            return true;
        }

        internal static async Awaitable<GlobalUiSceneRuntime>
            LoadAndPersistAsync(
                GameApplicationAsset application,
                Transform persistentParent,
                FrameworkLogger logger)
        {
            logger ??=
                FrameworkLogger.Create<GlobalUiSceneRuntime>();

            if (application == null)
            {
                const string message =
                    "Persistent Content cannot load because the Game Application is missing.";
                logger.Error(message);
                return Failed(string.Empty, string.Empty, message);
            }

            PersistentContentComposition composition =
                application.PersistentContent;

            if (composition == null ||
                !composition.HasContainerScene)
            {
                const string message =
                    "Persistent Content composition is incomplete. A Content Scene is required.";
                logger.Error(message);
                return Failed(
                    composition?.ContainerScenePath,
                    composition?.ContainerSceneName,
                    message);
            }

            string scenePath = composition.ContainerScenePath;
            string sceneName =
                composition.ContainerSceneName;

            if (!TryResolveSceneLoadIdentifier(
                    scenePath,
                    sceneName,
                    out string sceneIdentifier,
                    out string resolutionDiagnostic))
            {
                string message =
                    $"Persistent Content scene reference is invalid. {resolutionDiagnostic}";
                logger.Error(message);
                return Failed(scenePath, sceneName, message);
            }

            string targetScenePath = sceneIdentifier;
            var previouslyLoadedHandles = GetLoadedSceneHandles();
            AsyncOperation asyncOperation;
            try
            {
                asyncOperation =
                    SceneManager.LoadSceneAsync(
                        sceneIdentifier,
                        LoadSceneMode.Additive);
            }
            catch (Exception exception)
            {
                string message =
                    $"Persistent Content scene '{sceneIdentifier}' failed before its load operation could be observed. {exception.GetType().Name}: {exception.Message}";
                logger.Error(message);
                return Failed(scenePath, sceneName, message);
            }

            if (asyncOperation == null)
            {
                string message =
                    $"Persistent Content scene '{sceneIdentifier}' could not start loading. Confirm that the exact scene path is enabled in the active Build Profile or Shared Scene List.";
                logger.Error(message);
                return Failed(scenePath, sceneName, message);
            }

            while (!asyncOperation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            if (!TryFindNewlyLoadedScene(
                    targetScenePath,
                    sceneName,
                    previouslyLoadedHandles,
                    out Scene scene,
                    out Scene homonymousScene,
                    out string resolutionIssue))
            {
                if (homonymousScene.IsValid() && homonymousScene.isLoaded)
                {
                    string unloadIssue =
                        await TryUnloadNewSceneAsync(homonymousScene);
                    resolutionIssue += string.IsNullOrWhiteSpace(unloadIssue)
                        ? " The exact newly loaded homonymous scene was unloaded."
                        : $" The exact newly loaded homonymous scene could not be unloaded: {unloadIssue}";
                }

                string message =
                    $"Persistent Content scene '{targetScenePath}' finished loading but could not be resolved by its requested identity. {resolutionIssue}";
                logger.Error(message);
                return Failed(scenePath, sceneName, message);
            }

            GameObject[] roots =
                scene.GetRootGameObjects();
            var persistedRoots =
                new List<GameObject>();

            if (roots != null)
            {
                for (int index = 0;
                     index < roots.Length;
                     index++)
                {
                    GameObject root = roots[index];
                    if (root == null)
                    {
                        continue;
                    }

                    persistedRoots.Add(root);
                }
            }

            List<ITransitionEffectAdapter> transitionAdapters =
                CollectAdapters<ITransitionEffectAdapter>(persistedRoots);
            List<ILoadingSurfaceAdapter> loadingAdapters =
                CollectAdapters<ILoadingSurfaceAdapter>(persistedRoots);
            string label =
                sceneName.NormalizeTextOrFallback(
                    Path.GetFileNameWithoutExtension(targetScenePath)
                        .NormalizeTextOrFallback("Persistent Content"));

            logger.Debug(
                "Persistent Content loaded.",
                LogFields.Field("scene", label));
            logger.Debug(
                "Persistent Content diagnostics.",
                LogFields.Of(
                    LogFields.Field("scene", label),
                    LogFields.Field(
                        "rootCount",
                        persistedRoots.Count),
                    LogFields.Field(
                        "transitionAdapterCount",
                        transitionAdapters.Count),
                    LogFields.Field(
                        "loadingAdapterCount",
                        loadingAdapters.Count)));

            return new GlobalUiSceneRuntime(
                targetScenePath,
                scene.name,
                scene,
                label,
                persistedRoots,
                transitionAdapters,
                loadingAdapters,
                false,
                string.Empty,
                "Persistent Content source scene loaded; roots remain in the source until Session composition succeeds.");
        }

        private static GlobalUiSceneRuntime Failed(
            string scenePath,
            string sceneName,
            string message)
        {
            return new GlobalUiSceneRuntime(
                scenePath,
                sceneName,
                default,
                sceneName,
                Array.Empty<GameObject>(),
                Array.Empty<ITransitionEffectAdapter>(),
                Array.Empty<ILoadingSurfaceAdapter>(),
                true,
                message,
                message);
        }

        internal async Awaitable<PersistentContentOwnershipCommitResult>
            CommitApplicationLifetimeAsync(FrameworkLogger logger)
        {
            if (!_sourceScene.IsValid() || !_sourceScene.isLoaded)
            {
                return PersistentContentOwnershipCommitResult.Failed(
                    $"Persistent Content source scene '{ScenePath}' is no longer loaded before application-lifetime commit.");
            }

            for (int index = 0; index < _persistedRoots.Length; index++)
            {
                if (_persistedRoots[index] == null)
                {
                    return PersistentContentOwnershipCommitResult.Failed(
                        $"Persistent Content root at index '{index}' was destroyed before application-lifetime commit. Source scene '{ScenePath}' remains loaded.");
                }
            }

            try
            {
                // Keep the complete authored root hierarchies intact. Move them only after
                // Session-scope composition has succeeded and before unloading their source.
                for (int index = 0; index < _persistedRoots.Length; index++)
                    UnityEngine.Object.DontDestroyOnLoad(_persistedRoots[index]);

                AsyncOperation unloadOperation =
                    SceneManager.UnloadSceneAsync(_sourceScene);
                if (unloadOperation == null)
                {
                    if (!_sourceScene.isLoaded)
                    {
                        return PersistentContentOwnershipCommitResult.SucceededWithDiagnostic(
                            "Unity removed the source scene; the composed roots remain application-scoped.");
                    }

                    string restoreDiagnostic = RestoreRootsToSourceScene();
                    return PersistentContentOwnershipCommitResult.Failed(
                        $"Persistent Content could not start unloading source scene '{ScenePath}'. Roots were returned to the source. {restoreDiagnostic}");
                }

                while (!unloadOperation.isDone)
                    await Awaitable.NextFrameAsync();

                if (!_sourceScene.isLoaded)
                {
                    return PersistentContentOwnershipCommitResult.SucceededWithDiagnostic(string.Empty);
                }

                string failedUnloadRestoreDiagnostic = RestoreRootsToSourceScene();
                return PersistentContentOwnershipCommitResult.Failed(
                    $"Persistent Content unload operation completed but source scene '{ScenePath}' remains loaded. Roots were returned to the source. {failedUnloadRestoreDiagnostic}");
            }
            catch (Exception exception)
            {
                if (!_sourceScene.isLoaded)
                {
                    string diagnostic =
                        $"Persistent Content source scene was unloaded despite '{exception.GetType().Name}' during unload completion; composed roots remain application-scoped.";
                    logger?.Warning(diagnostic);
                    return PersistentContentOwnershipCommitResult.SucceededWithDiagnostic(diagnostic);
                }

                string restoreDiagnostic = RestoreRootsToSourceScene();
                return PersistentContentOwnershipCommitResult.Failed(
                    $"Persistent Content application-lifetime commit failed. {exception.GetType().Name}: {exception.Message} Roots were returned to the source. {restoreDiagnostic}");
            }
        }

        internal static bool TryResolveSceneLoadIdentifier(
            string scenePath,
            string sceneName,
            out string sceneIdentifier,
            out string diagnostic)
        {
            scenePath = scenePath.NormalizeText();
            sceneName = sceneName.NormalizeText();
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                    !IsScenePathInEffectiveBuildSet(scenePath) ||
                    !Application.CanStreamedLevelBeLoaded(scenePath))
                {
                    sceneIdentifier = string.Empty;
                    diagnostic =
                        $"Explicit scenePath '{scenePath}' is invalid or unavailable in the effective Player scene set. Name fallback is forbidden.";
                    return false;
                }

                sceneIdentifier = scenePath;
                diagnostic = string.Empty;
                return true;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                sceneIdentifier = string.Empty;
                diagnostic = "Neither scenePath nor legacy sceneName is authored.";
                return false;
            }

            var candidatePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                string candidatePath = SceneUtility.GetScenePathByBuildIndex(index);
                if (HasSceneName(candidatePath, sceneName))
                    candidatePaths.Add(candidatePath);
            }

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(index);
                if (loadedScene.IsValid() && loadedScene.isLoaded &&
                    HasSceneName(loadedScene.path, sceneName))
                {
                    candidatePaths.Add(loadedScene.path);
                }
            }

            if (candidatePaths.Count != 1)
            {
                sceneIdentifier = string.Empty;
                diagnostic = candidatePaths.Count == 0
                    ? $"Legacy sceneName '{sceneName}' has no candidate in the effective build or loaded scene set."
                    : $"Legacy sceneName '{sceneName}' is ambiguous across {candidatePaths.Count} scene paths.";
                return false;
            }

            foreach (string candidatePath in candidatePaths)
            {
                if (!Application.CanStreamedLevelBeLoaded(candidatePath))
                {
                    sceneIdentifier = string.Empty;
                    diagnostic =
                        $"The unique legacy scene candidate '{candidatePath}' cannot be loaded from the effective Player scene set.";
                    return false;
                }

                sceneIdentifier = candidatePath;
                diagnostic = string.Empty;
                return true;
            }

            sceneIdentifier = string.Empty;
            diagnostic = $"Legacy sceneName '{sceneName}' could not be resolved.";
            return false;
        }

        private static HashSet<int> GetLoadedSceneHandles()
        {
            var handles = new HashSet<int>();
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid() && scene.isLoaded)
                    handles.Add(scene.handle);
            }

            return handles;
        }

        private static bool TryFindNewlyLoadedScene(
            string expectedPath,
            string expectedName,
            ISet<int> previouslyLoadedHandles,
            out Scene scene,
            out Scene homonymousScene,
            out string diagnostic)
        {
            Scene matched = default;
            int matches = 0;
            Scene homonym = default;
            int homonyms = 0;

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene candidate = SceneManager.GetSceneAt(index);
                if (!candidate.IsValid() || !candidate.isLoaded ||
                    previouslyLoadedHandles.Contains(candidate.handle))
                {
                    continue;
                }

                if (string.Equals(candidate.path, expectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    matched = candidate;
                    matches++;
                }

                if (HasSceneName(candidate.path, expectedName))
                {
                    homonym = candidate;
                    homonyms++;
                }
            }

            if (matches == 1)
            {
                scene = matched;
                homonymousScene = default;
                diagnostic = string.Empty;
                return true;
            }

            scene = default;
            homonymousScene = default;
            if (matches > 1)
            {
                diagnostic = $"More than one new loaded Scene matched requested path '{expectedPath}'.";
                return false;
            }

            if (homonyms == 1)
            {
                homonymousScene = homonym;
                diagnostic =
                    $"Loaded path '{homonym.path}' did not match requested path '{expectedPath}'.";
                return false;
            }

            diagnostic =
                $"No unique newly loaded Scene matched requested path '{expectedPath}'.";
            return false;
        }

        private static async Awaitable<string> TryUnloadNewSceneAsync(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return string.Empty;

            try
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
                if (unloadOperation == null)
                {
                    return "Unity did not start the unload operation.";
                }

                while (!unloadOperation.isDone)
                    await Awaitable.NextFrameAsync();

                return scene.isLoaded
                    ? "The unload operation completed but the exact scene remains loaded."
                    : string.Empty;
            }
            catch (Exception exception)
            {
                return $"{exception.GetType().Name}: {exception.Message}";
            }
        }

        private string RestoreRootsToSourceScene()
        {
            if (!_sourceScene.IsValid() || !_sourceScene.isLoaded)
                return $"Source scene '{ScenePath}' is no longer loaded; roots could not be returned.";

            var issues = new List<string>();
            for (int index = 0; index < _persistedRoots.Length; index++)
            {
                GameObject root = _persistedRoots[index];
                if (root == null)
                {
                    issues.Add($"rootIndex='{index}' was destroyed before source restoration");
                    continue;
                }

                if (root.scene == _sourceScene)
                    continue;

                try
                {
                    SceneManager.MoveGameObjectToScene(root, _sourceScene);
                }
                catch (Exception exception)
                {
                    issues.Add($"root='{root.name}' {exception.GetType().Name}: {exception.Message}");
                }
            }

            return issues.Count == 0
                ? ""
                : $"Root restoration issues: {string.Join(" | ", issues)}";
        }

        private static bool HasSceneName(string scenePath, string sceneName)
        {
            return !string.IsNullOrWhiteSpace(scenePath) &&
                   string.Equals(
                       Path.GetFileNameWithoutExtension(scenePath),
                       sceneName,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsScenePathInEffectiveBuildSet(string scenePath)
        {
            for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                string candidatePath = SceneUtility.GetScenePathByBuildIndex(index);
                if (string.Equals(
                        candidatePath,
                        scenePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<TAdapter> CollectAdapters<TAdapter>(
            IReadOnlyList<GameObject> roots)
        {
            var adapters = new List<TAdapter>();
            if (roots == null ||
                roots.Count == 0)
            {
                return adapters;
            }

            for (int rootIndex = 0;
                 rootIndex < roots.Count;
                 rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours =
                    root.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours == null)
                {
                    continue;
                }

                for (int behaviourIndex = 0;
                     behaviourIndex < behaviours.Length;
                     behaviourIndex++)
                {
                    if (behaviours[behaviourIndex] is TAdapter adapter)
                    {
                        adapters.Add(adapter);
                    }
                }
            }

            return adapters;
        }

        private List<T> FindAll<T>()
            where T : MonoBehaviour
        {
            var resolved = new List<T>();

            for (int rootIndex = 0;
                 rootIndex < _persistedRoots.Length;
                 rootIndex++)
            {
                GameObject root =
                    _persistedRoots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                T[] candidates =
                    root.GetComponentsInChildren<T>(true);

                for (int index = 0;
                     index < candidates.Length;
                     index++)
                {
                    if (candidates[index] != null)
                    {
                        resolved.Add(candidates[index]);
                    }
                }
            }

            return resolved;
        }

    }
}
