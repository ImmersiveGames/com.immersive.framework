using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Loading;
using Immersive.Framework.Pause;
using Immersive.Framework.TransitionEffects;
using Immersive.Framework.Common;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.GlobalUi
{
    /// <summary>
    /// API status: Internal. Loads the canonical app/session-scoped UIGlobal scene additively after the
    /// Startup Route Primary Scene is prepared, moves its authored UI roots under the persistent
    /// FrameworkRuntimeHost, and exposes visual adapters.
    /// It does not own RouteLifecycle, ActivityFlow, SceneLifecycle, Loading lifecycle or Transition lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24E canonical UIGlobal scene loader; scene-authored visual surfaces only.")]
    internal sealed class GlobalUiSceneRuntime
    {
        private readonly ITransitionEffectAdapter[] _transitionAdapters;
        private readonly ILoadingSurfaceAdapter[] _loadingAdapters;
        private readonly IPauseSurfaceAdapter[] _pauseAdapters;
        private readonly GameObject[] _persistedRoots;

        private GlobalUiSceneRuntime(
            GlobalUiScenePolicy policy,
            string scenePath,
            string sceneName,
            string label,
            IReadOnlyList<GameObject> persistedRoots,
            IReadOnlyList<ITransitionEffectAdapter> transitionAdapters,
            IReadOnlyList<ILoadingSurfaceAdapter> loadingAdapters,
            IReadOnlyList<IPauseSurfaceAdapter> pauseAdapters,
            bool hasBlockingConfigurationIssue,
            string blockingConfigurationMessage,
            string message)
        {
            Policy = policy;
            ScenePath = Normalize(scenePath);
            SceneName = Normalize(sceneName);
            Label = label.NormalizeTextOrFallback("UIGlobal");
            PersistedRootCount = persistedRoots?.Count ?? 0;
            _persistedRoots = FrameworkCollectionCopy.ToArrayOrEmpty(persistedRoots);
            _transitionAdapters = FrameworkCollectionCopy.ToArrayOrEmpty(transitionAdapters);
            _loadingAdapters = FrameworkCollectionCopy.ToArrayOrEmpty(loadingAdapters);
            _pauseAdapters = FrameworkCollectionCopy.ToArrayOrEmpty(pauseAdapters);
            HasBlockingConfigurationIssue = hasBlockingConfigurationIssue;
            BlockingConfigurationMessage = Normalize(blockingConfigurationMessage);
            Message = Normalize(message);
        }

        public GlobalUiScenePolicy Policy { get; }
        public string ScenePath { get; }
        public string SceneName { get; }
        public string Label { get; }
        public int PersistedRootCount { get; }
        public int TransitionAdapterCount => _transitionAdapters.Length;
        public int LoadingAdapterCount => _loadingAdapters.Length;
        public int PauseAdapterCount => _pauseAdapters.Length;
        public bool HasBlockingConfigurationIssue { get; }
        public string BlockingConfigurationMessage { get; }
        public string Message { get; }
        public IReadOnlyList<ITransitionEffectAdapter> TransitionAdapters => _transitionAdapters;
        public IReadOnlyList<ILoadingSurfaceAdapter> LoadingAdapters => _loadingAdapters;
        public IReadOnlyList<IPauseSurfaceAdapter> PauseAdapters => _pauseAdapters;

        internal bool TryResolveCameraPresentation(
            out CameraOutputSessionBinding outputSession,
            out SessionCameraOverrideBinding sessionOverride,
            out string diagnostic)
        {
            outputSession = FindSingle<CameraOutputSessionBinding>();
            sessionOverride = FindSingle<SessionCameraOverrideBinding>();
            if (outputSession == null || sessionOverride == null)
            {
                diagnostic = "UIGlobal requires exactly one CameraOutputSessionBinding and one SessionCameraOverrideBinding.";
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
                    "UIGlobal has no Local Player Provisioning Host Registration. Local Player provisioning is explicitly unavailable.";
                return true;
            }

            isConfigured = true;
            if (registrations.Count != 1)
            {
                diagnostic =
                    $"UIGlobal requires exactly one Local Player Provisioning Host Registration when provisioning is configured, but found '{registrations.Count}'.";
                return false;
            }

            if (!registrations[0].TryResolveAuthoring(
                    out authoring,
                    out string issue))
            {
                diagnostic =
                    $"UIGlobal Local Player Provisioning Host Registration is invalid. {issue}";
                return false;
            }

            diagnostic =
                $"Resolved Local Player provisioning authoring '{authoring.name}' through the explicit UIGlobal Host Registration.";
            return true;
        }

        internal static async Awaitable<GlobalUiSceneRuntime> LoadAndPersistAsync(
            GameApplicationAsset application,
            Transform persistentParent,
            FrameworkLogger logger)
        {
            logger ??= FrameworkLogger.Create<GlobalUiSceneRuntime>();

            if (application == null)
            {
                const string message = "UIGlobal scene is not configured because the Game Application is missing.";
                logger.Warning(message);
                return new GlobalUiSceneRuntime(
                    GlobalUiScenePolicy.NoneConfigured,
                    string.Empty,
                    string.Empty,
                    "UIGlobal",
                    Array.Empty<GameObject>(),
                    Array.Empty<ITransitionEffectAdapter>(),
                    Array.Empty<ILoadingSurfaceAdapter>(),
                    Array.Empty<IPauseSurfaceAdapter>(),
                    false,
                    string.Empty,
                    message);
            }

            if (application.GlobalUiScenePolicyValue == GlobalUiScenePolicy.NoneConfigured)
            {
                if (application.HasGlobalUiScene)
                {
                    logger.Warning($"UIGlobal scene '{application.GlobalUiScenePath}' is assigned but Global UI Scene Policy is NoneConfigured. The scene will not be loaded and the runtime will keep explicit NoOp behavior.");
                }
                else
                {
                    logger.Info("UIGlobal scene is not configured. The runtime will keep explicit NoOp behavior.");
                }

                return new GlobalUiSceneRuntime(
                    application.GlobalUiScenePolicyValue,
                    application.GlobalUiScenePath,
                    application.GlobalUiSceneName,
                    "UIGlobal",
                    Array.Empty<GameObject>(),
                    Array.Empty<ITransitionEffectAdapter>(),
                    Array.Empty<ILoadingSurfaceAdapter>(),
                    Array.Empty<IPauseSurfaceAdapter>(),
                    false,
                    string.Empty,
                    "UIGlobal scene is explicit NoOp.");
            }

            if (!application.HasGlobalUiScene)
            {
                const string message = "Global UI Scene Policy is Required, but UIGlobal Scene is missing.";
                logger.Error(message);
                return Failed(application, message);
            }

            string scenePath = application.GlobalUiScenePath;
            var asyncOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            if (asyncOperation == null)
            {
                string message = $"UIGlobal scene '{scenePath}' could not be loaded. Ensure it is in Build Settings.";
                logger.Error(message);
                return Failed(application, message);
            }

            while (!asyncOperation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            // Resolve only by the path used for LoadSceneAsync. Name fallback can attach to a
            // different loaded scene that happens to share GlobalUiSceneName.
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                string message = $"UIGlobal scene '{scenePath}' finished loading but could not be resolved as a loaded scene by path.";
                logger.Error(message);
                return Failed(application, message);
            }

            GameObject[] roots = scene.GetRootGameObjects();
            var persistedRoots = new List<GameObject>();
            if (roots != null)
            {
                for (int i = 0; i < roots.Length; i++)
                {
                    var root = roots[i];
                    if (root == null)
                    {
                        continue;
                    }

                    root.name = root.name.EndsWith(" (UIGlobal)", StringComparison.Ordinal)
                        ? root.name
                        : root.name + " (UIGlobal)";
                    root.transform.SetParent(persistentParent, false);
                    persistedRoots.Add(root);
                }
            }

            List<ITransitionEffectAdapter> transitionAdapters = CollectAdapters<ITransitionEffectAdapter>(persistedRoots);
            List<ILoadingSurfaceAdapter> loadingAdapters = CollectAdapters<ILoadingSurfaceAdapter>(persistedRoots);
            List<IPauseSurfaceAdapter> pauseAdapters = CollectAdapters<IPauseSurfaceAdapter>(persistedRoots);
            string blockingMessage = BuildBlockingMessageIfRequired(
                application.GlobalUiScenePolicyValue,
                application.GlobalUiSceneName,
                transitionAdapters.Count,
                loadingAdapters.Count);

            var unloadOperation = SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
            }

            string label = string.IsNullOrWhiteSpace(application.GlobalUiSceneName)
                ? "UIGlobal"
                : application.GlobalUiSceneName;
            if (!string.IsNullOrWhiteSpace(blockingMessage))
            {
                logger.Error($"UIGlobal scene '{label}' loaded and persisted, but required adapters are missing. {blockingMessage}");
                return new GlobalUiSceneRuntime(
                    application.GlobalUiScenePolicyValue,
                    application.GlobalUiScenePath,
                    application.GlobalUiSceneName,
                    label,
                    persistedRoots,
                    transitionAdapters,
                    loadingAdapters,
                    pauseAdapters,
                    true,
                    blockingMessage,
                    $"UIGlobal scene '{label}' loaded and persisted with rootCount='{persistedRoots.Count}' transitionAdapterCount='{transitionAdapters.Count}' loadingAdapterCount='{loadingAdapters.Count}' pauseAdapterCount='{pauseAdapters.Count}'.");
            }

            logger.Info("UIGlobal scene loaded.", LogFields.Field("scene", label));
            logger.Debug(
                "UIGlobal scene diagnostics.",
                LogFields.Of(
                    LogFields.Field("scene", label),
                    LogFields.Field("rootCount", persistedRoots.Count),
                    LogFields.Field("transitionAdapterCount", transitionAdapters.Count),
                    LogFields.Field("loadingAdapterCount", loadingAdapters.Count),
                    LogFields.Field("pauseAdapterCount", pauseAdapters.Count)));

            return new GlobalUiSceneRuntime(
                application.GlobalUiScenePolicyValue,
                application.GlobalUiScenePath,
                application.GlobalUiSceneName,
                label,
                persistedRoots,
                transitionAdapters,
                loadingAdapters,
                pauseAdapters,
                false,
                string.Empty,
                "UIGlobal scene loaded and persisted.");
        }

        private static GlobalUiSceneRuntime Failed(GameApplicationAsset application, string message)
        {
            return new GlobalUiSceneRuntime(
                application != null ? application.GlobalUiScenePolicyValue : GlobalUiScenePolicy.NoneConfigured,
                application != null ? application.GlobalUiScenePath : string.Empty,
                application != null ? application.GlobalUiSceneName : string.Empty,
                application != null ? application.GlobalUiSceneName : "UIGlobal",
                Array.Empty<GameObject>(),
                Array.Empty<ITransitionEffectAdapter>(),
                Array.Empty<ILoadingSurfaceAdapter>(),
                Array.Empty<IPauseSurfaceAdapter>(),
                true,
                message,
                message);
        }

        private static List<TAdapter> CollectAdapters<TAdapter>(IReadOnlyList<GameObject> roots)
        {
            var adapters = new List<TAdapter>();
            if (roots == null || roots.Count == 0)
            {
                return adapters;
            }

            for (int i = 0; i < roots.Count; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours == null)
                {
                    continue;
                }

                for (int j = 0; j < behaviours.Length; j++)
                {
                    if (behaviours[j] is TAdapter adapter)
                    {
                        adapters.Add(adapter);
                    }
                }
            }

            return adapters;
        }

        private T FindSingle<T>() where T : MonoBehaviour
        {
            List<T> candidates = FindAll<T>();
            return candidates.Count == 1 ? candidates[0] : null;
        }

        private List<T> FindAll<T>() where T : MonoBehaviour
        {
            var resolved = new List<T>();
            for (int rootIndex = 0; rootIndex < _persistedRoots.Length; rootIndex++)
            {
                T[] candidates = _persistedRoots[rootIndex]
                    .GetComponentsInChildren<T>(true);
                for (int index = 0; index < candidates.Length; index++)
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
            GlobalUiScenePolicy policy,
            string label,
            int transitionAdapterCount,
            int loadingAdapterCount)
        {
            if (policy != GlobalUiScenePolicy.Required)
            {
                return string.Empty;
            }

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

            return $"UIGlobal scene '{label}' is required, but missing {string.Join(" and ", missing)}.";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
