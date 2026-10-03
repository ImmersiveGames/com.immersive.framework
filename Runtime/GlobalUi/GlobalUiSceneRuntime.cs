using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Internal runtime loader for the application Persistent Content Container Scene.
    ///
    /// The source scene is loaded additively, its complete authored root hierarchies are moved
    /// to Unity's persistent runtime scene, and the source scene is unloaded. The source scene
    /// is an authoring container; the retained objects own the application lifetime.
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

        private GlobalUiSceneRuntime(
            UnityEngine.Object containerScene,
            string label,
            IReadOnlyList<GameObject> persistedRoots,
            IReadOnlyList<ITransitionEffectAdapter> transitionAdapters,
            IReadOnlyList<ILoadingSurfaceAdapter> loadingAdapters,
            bool hasBlockingConfigurationIssue,
            string blockingConfigurationMessage,
            string message)
        {
            ContainerScene = containerScene;
            SceneName = containerScene != null
                ? containerScene.name.NormalizeText()
                : string.Empty;
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

        public UnityEngine.Object ContainerScene { get; }

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
                return Failed(null, message);
            }

            PersistentContentComposition composition =
                application.PersistentContent;

            if (composition == null ||
                !composition.HasContainerScene)
            {
                const string message =
                    "Persistent Content composition is incomplete. A Content Scene is required.";
                logger.Error(message);
                return Failed(application, message);
            }

            UnityEngine.Object containerScene =
                composition.ContainerScene;
            string sceneName =
                composition.ContainerSceneName;

            if (containerScene == null ||
                string.IsNullOrWhiteSpace(sceneName))
            {
                const string message =
                    "Persistent Content Container Scene reference is missing or invalid.";
                logger.Error(message);
                return Failed(application, message);
            }

            AsyncOperation asyncOperation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive);

            if (asyncOperation == null)
            {
                string message =
                    $"Persistent Content Container Scene '{sceneName}' could not be loaded. Ensure the directly referenced scene is enabled in the Build Profile and has a unique scene name.";
                logger.Error(message);
                return Failed(application, message);
            }

            while (!asyncOperation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            Scene scene =
                SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                string message =
                    $"Persistent Content Container Scene '{sceneName}' finished loading but could not be resolved as a loaded scene by its validated unique name.";
                logger.Error(message);
                return Failed(application, message);
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

                    // Preserve the complete authored root hierarchy. Do not flatten or re-parent
                    // Camera, Canvas, Audio or future Lighting content under the runtime host.
                    UnityEngine.Object.DontDestroyOnLoad(root);
                    persistedRoots.Add(root);
                }
            }

            List<ITransitionEffectAdapter> transitionAdapters =
                CollectAdapters<ITransitionEffectAdapter>(persistedRoots);
            List<ILoadingSurfaceAdapter> loadingAdapters =
                CollectAdapters<ILoadingSurfaceAdapter>(persistedRoots);
            AsyncOperation unloadOperation =
                SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
            }

            string label =
                sceneName.NormalizeTextOrFallback(
                    "Persistent Content");

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
                containerScene,
                label,
                persistedRoots,
                transitionAdapters,
                loadingAdapters,
                false,
                string.Empty,
                "Persistent Content loaded and retained for the application lifetime.");
        }

        private static GlobalUiSceneRuntime Failed(
            GameApplicationAsset application,
            string message)
        {
            UnityEngine.Object sceneReference =
                application?.PersistentContent?.ContainerScene;
            string label =
                application?.PersistentContent?.ContainerSceneName;

            return new GlobalUiSceneRuntime(
                sceneReference,
                label,
                Array.Empty<GameObject>(),
                Array.Empty<ITransitionEffectAdapter>(),
                Array.Empty<ILoadingSurfaceAdapter>(),
                true,
                message,
                message);
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
