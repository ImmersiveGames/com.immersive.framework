using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Loading;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.TransitionEffects;
using Immersive.Logging.Records;
using UnityEngine;
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
        private readonly IPauseSurfaceAdapter[] _pauseAdapters;
        private readonly GameObject[] _persistedRoots;

        private GlobalUiSceneRuntime(
            UnityEngine.Object containerScene,
            string label,
            IReadOnlyList<GameObject> persistedRoots,
            IReadOnlyList<ITransitionEffectAdapter> transitionAdapters,
            IReadOnlyList<ILoadingSurfaceAdapter> loadingAdapters,
            IReadOnlyList<IPauseSurfaceAdapter> pauseAdapters,
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
            _transitionAdapters =
                FrameworkCollectionCopy.ToArrayOrEmpty(transitionAdapters);
            _loadingAdapters =
                FrameworkCollectionCopy.ToArrayOrEmpty(loadingAdapters);
            _pauseAdapters =
                FrameworkCollectionCopy.ToArrayOrEmpty(pauseAdapters);
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

        public int PauseAdapterCount =>
            _pauseAdapters.Length;

        public bool HasBlockingConfigurationIssue { get; }

        public string BlockingConfigurationMessage { get; }

        public string Message { get; }

        public IReadOnlyList<ITransitionEffectAdapter> TransitionAdapters =>
            _transitionAdapters;

        public IReadOnlyList<ILoadingSurfaceAdapter> LoadingAdapters =>
            _loadingAdapters;

        public IReadOnlyList<IPauseSurfaceAdapter> PauseAdapters =>
            _pauseAdapters;

        internal GlobalUiPauseRequestTriggerBindingResult
            TryBindPauseRequestTriggers(
                IPauseProductRequestPort pauseProductRequest)
        {
            return TryBindPauseRequestTriggers(
                _persistedRoots,
                pauseProductRequest);
        }

        internal RouteRequestTriggerBindingResult
            TryBindRouteRequestTriggers(
                IRouteRuntimePort routeRuntime)
        {
            return RouteRequestTriggerBinding.TryBind(
                _persistedRoots,
                routeRuntime);
        }

        internal ActivityRequestTriggerBindingResult
            TryBindActivityRequestTriggers(
                IActivityRuntimePort activityRuntime)
        {
            return ActivityRequestTriggerBinding.TryBind(
                _persistedRoots,
                activityRuntime);
        }

        internal RouteCycleResetTriggerBindingResult
            TryBindRouteCycleResetTriggers(
                IRouteCycleResetRuntimePort routeCycleResetRuntime)
        {
            return RouteCycleResetTriggerBinding.TryBind(
                _persistedRoots,
                routeCycleResetRuntime);
        }

        internal ActivityCycleResetTriggerBindingResult
            TryBindActivityCycleResetTriggers(
                IActivityCycleResetRuntimePort activityCycleResetRuntime)
        {
            return ActivityCycleResetTriggerBinding.TryBind(
                _persistedRoots,
                activityCycleResetRuntime);
        }

        internal ActivityRestartTriggerBindingResult
            TryBindActivityRestartTriggers(
                IActivityRestartRuntimePort activityRestartRuntime)
        {
            return ActivityRestartTriggerBinding.TryBind(
                _persistedRoots,
                activityRestartRuntime);
        }

        internal LocalPlayerActorSelectionRequestAuthoringBindingResult
            TryBindLocalPlayerActorSelectionRequests(
                IPlayerActorSelectionRuntimePort selectionRuntime)
        {
            return LocalPlayerActorSelectionRequestAuthoringBinding.TryBind(
                _persistedRoots,
                selectionRuntime);
        }

        internal LocalPlayerActorSelectionRequestAuthoringReleaseResult
            TryReleaseLocalPlayerActorSelectionRequests(
                IPlayerActorSelectionRuntimePort selectionRuntime)
        {
            return LocalPlayerActorSelectionRequestAuthoringBinding.TryRelease(
                _persistedRoots,
                selectionRuntime);
        }

        internal static GlobalUiPauseRequestTriggerBindingResult
            TryBindPauseRequestTriggers(
                IReadOnlyList<GameObject> persistentRoots,
                IPauseProductRequestPort pauseProductRequest)
        {
            int rootCount =
                CountRoots(persistentRoots);

            if (pauseProductRequest == null)
            {
                return GlobalUiPauseRequestTriggerBindingResult.Rejected(
                    "RejectedMissingPauseProductRequest",
                    $"Persistent Content Pause request trigger binding requires a Pause product request port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<PauseRequestTrigger> triggers =
                CollectPauseRequestTriggers(persistentRoots);

            if (triggers.Count == 0)
            {
                return GlobalUiPauseRequestTriggerBindingResult.OptionalAbsent(
                    rootCount);
            }

            int boundCount = 0;
            int idempotentCount = 0;
            int rejectedCount = 0;
            var issues = new List<string>();

            for (int index = 0;
                 index < triggers.Count;
                 index++)
            {
                PauseRequestTrigger trigger =
                    triggers[index];
                bool wasBound =
                    trigger.HasPauseProductRequestBinding;

                if (trigger.TryBindPauseProductRequest(
                        pauseProductRequest,
                        out string issue))
                {
                    if (wasBound)
                    {
                        idempotentCount++;
                    }
                    else
                    {
                        boundCount++;
                    }

                    continue;
                }

                rejectedCount++;
                string sceneName =
                    trigger.gameObject.scene.name
                        .NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"trigger='{trigger.name}' scene='{sceneName}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            if (rejectedCount > 0)
            {
                return GlobalUiPauseRequestTriggerBindingResult.Rejected(
                    "RejectedTriggerBinding",
                    $"Persistent Content Pause request trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{boundCount}' idempotent='{idempotentCount}' rejected='{rejectedCount}'. {string.Join(" ", issues)}",
                    rootCount,
                    triggers.Count,
                    boundCount,
                    idempotentCount,
                    rejectedCount);
            }

            return GlobalUiPauseRequestTriggerBindingResult.Completed(
                rootCount,
                triggers.Count,
                boundCount,
                idempotentCount);
        }

        internal bool TryResolveCameraPresentation(
            out CameraOutputSessionBinding outputSession,
            out SessionCameraOverrideBinding sessionOverride,
            out string diagnostic)
        {
            List<CameraOutputSessionBinding> outputCandidates =
                FindAll<CameraOutputSessionBinding>();
            List<SessionCameraOverrideBinding> overrideCandidates =
                FindAll<SessionCameraOverrideBinding>();

            outputSession =
                outputCandidates.Count == 1
                    ? outputCandidates[0]
                    : null;
            sessionOverride =
                overrideCandidates.Count == 1
                    ? overrideCandidates[0]
                    : null;

            if (outputSession == null ||
                sessionOverride == null)
            {
                diagnostic =
                    $"Persistent Content requires exactly one CameraOutputSessionBinding and one SessionCameraOverrideBinding. outputSessions='{outputCandidates.Count}' sessionOverrides='{overrideCandidates.Count}'.";
                return false;
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

            List<LocalPlayerProvisioningHostRegistration> registrations =
                FindAll<LocalPlayerProvisioningHostRegistration>();

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
                !composition.IsComplete)
            {
                const string message =
                    "Persistent Content composition is incomplete. Container Scene, Camera Output Prefab and Presentation Canvas Prefab are required.";
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
            List<IPauseSurfaceAdapter> pauseAdapters =
                CollectAdapters<IPauseSurfaceAdapter>(persistedRoots);

            string blockingMessage =
                BuildBlockingMessageIfRequired(
                    sceneName,
                    transitionAdapters.Count,
                    loadingAdapters.Count);

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

            if (!string.IsNullOrWhiteSpace(
                    blockingMessage))
            {
                logger.Error(
                    $"Persistent Content Container Scene '{label}' loaded and its roots were retained, but required presentation adapters are missing. {blockingMessage}");

                return new GlobalUiSceneRuntime(
                    containerScene,
                    label,
                    persistedRoots,
                    transitionAdapters,
                    loadingAdapters,
                    pauseAdapters,
                    true,
                    blockingMessage,
                    $"Persistent Content loaded with rootCount='{persistedRoots.Count}' transitionAdapterCount='{transitionAdapters.Count}' loadingAdapterCount='{loadingAdapters.Count}' pauseAdapterCount='{pauseAdapters.Count}'.");
            }

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
                        loadingAdapters.Count),
                    LogFields.Field(
                        "pauseAdapterCount",
                        pauseAdapters.Count)));

            return new GlobalUiSceneRuntime(
                containerScene,
                label,
                persistedRoots,
                transitionAdapters,
                loadingAdapters,
                pauseAdapters,
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
                Array.Empty<IPauseSurfaceAdapter>(),
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

        private static List<PauseRequestTrigger>
            CollectPauseRequestTriggers(
                IReadOnlyList<GameObject> roots)
        {
            var triggers =
                new List<PauseRequestTrigger>();
            var seen =
                new HashSet<PauseRequestTrigger>();

            if (roots == null)
            {
                return triggers;
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

                PauseRequestTrigger[] candidates =
                    root.GetComponentsInChildren<PauseRequestTrigger>(true);

                for (int candidateIndex = 0;
                     candidateIndex < candidates.Length;
                     candidateIndex++)
                {
                    PauseRequestTrigger candidate =
                        candidates[candidateIndex];
                    if (candidate != null &&
                        seen.Add(candidate))
                    {
                        triggers.Add(candidate);
                    }
                }
            }

            return triggers;
        }

        private static int CountRoots(
            IReadOnlyList<GameObject> roots)
        {
            if (roots == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0;
                 index < roots.Count;
                 index++)
            {
                if (roots[index] != null)
                {
                    count++;
                }
            }

            return count;
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

        private static string BuildBlockingMessageIfRequired(
            string label,
            int transitionAdapterCount,
            int loadingAdapterCount)
        {
            var missing = new List<string>(2);

            if (transitionAdapterCount == 0)
            {
                missing.Add("Transition adapter");
            }

            if (loadingAdapterCount == 0)
            {
                missing.Add("Loading adapter");
            }

            if (missing.Count == 0)
            {
                return string.Empty;
            }

            return
                $"Persistent Content Container Scene '{label}' is missing {string.Join(" and ", missing)}.";
        }
    }
}
