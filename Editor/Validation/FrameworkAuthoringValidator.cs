using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Editor.Authoring;
using Immersive.Framework.Loading;
using Immersive.Framework.Pause;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Editor.Validation
{
    internal static class FrameworkAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateProjectSettings(
            ImmersiveFrameworkSettingsAsset settings,
            bool includeOpenSceneBindings)
        {
            var validationMode = ResolveValidationMode(settings);
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (settings == null)
            {
                report.AddError(
                    "Framework Settings asset is missing. Open Project Settings > Immersive Framework to create it.",
                    null);
                return report;
            }

            report.AddInfo(
                $"Validation Mode policy: {FrameworkValidationModePolicy.GetSummary(validationMode)}",
                settings);

            if (settings.EditorPlayModeStartup ==
                FrameworkEditorPlayModeStartup.CurrentSceneOnly)
            {
                report.AddInfo(
                    "Editor Play Mode Startup is Current Scene Only. Framework boot validation is skipped in Play Mode, but authoring assets can still be checked.",
                    settings);
            }

            if (settings.ActiveGameApplication == null)
            {
                report.AddError(
                    "Active Game Application is missing in Project Settings > Immersive Framework.",
                    settings);
            }
            else
            {
                report.AddRange(
                    ValidateGameApplication(
                        settings.ActiveGameApplication,
                        true,
                        validationMode));
            }

            if (includeOpenSceneBindings)
            {
                ValidateOpenSceneActivityLocalVisibilityAdapters(
                    report,
                    validationMode);
                ValidateOpenSceneRouteContentBindings(
                    report,
                    validationMode);
                ValidateOpenSceneCycleResetTriggers(
                    report,
                    validationMode);
                FrameworkLocalPlayerCameraPublicationValidator
                    .ValidateOpenScenes(report);
                FrameworkResetRestartAuthoringValidator
                    .ValidateOpenScenes(report);
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    "Authoring validation passed with no findings.",
                    settings);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication,
            bool validateDependencies)
        {
            return ValidateGameApplication(
                gameApplication,
                validateDependencies,
                ResolveValidationMode(gameApplication));
        }

        internal static FrameworkAuthoringValidationReport
            ValidatePersistentContentTemplateScene(
                Scene scene,
                Object context)
        {
            var report =
                new FrameworkAuthoringValidationReport(
                    FrameworkValidationMode.Standard);

            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                report.AddError(
                    "Persistent Content template scene is invalid or not loaded.",
                    context);
                return report;
            }

            string sceneLabel =
                !string.IsNullOrWhiteSpace(scene.path)
                    ? scene.path
                    : !string.IsNullOrWhiteSpace(scene.name)
                        ? scene.name
                        : "<untitled>";

            ValidatePersistentContentSceneContents(
                report,
                context,
                scene,
                sceneLabel);

            if (report.ErrorCount == 0)
            {
                report.AddInfo(
                    "Persistent Content template scene satisfies the current composition contract.",
                    context);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateRoute(
            RouteAsset route,
            bool validateDependencies)
        {
            return ValidateRoute(
                route,
                validateDependencies,
                FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivity(
            ActivityAsset activity)
        {
            return ValidateActivity(
                activity,
                FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport
            ValidateActivityContentProfile(
                ActivityContentProfileAsset profile)
        {
            return ValidateActivityContentProfile(
                profile,
                FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport
            ValidateActivityLocalVisibilityAdapter(
                ActivityLocalVisibilityAdapter binding)
        {
            return ValidateActivityLocalVisibilityAdapter(
                binding,
                FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport
            ValidateRouteContentBinding(
                RouteContentBinding binding)
        {
            return ValidateRouteContentBinding(
                binding,
                FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport
            ValidateRouteCycleResetTrigger(
                RouteCycleResetTrigger trigger)
        {
            return ValidateRouteCycleResetTrigger(
                trigger,
                FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport
            ValidateActivityCycleResetTrigger(
                ActivityCycleResetTrigger trigger)
        {
            return ValidateActivityCycleResetTrigger(
                trigger,
                FrameworkValidationMode.Standard);
        }

        private static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication,
            bool validateDependencies,
            FrameworkValidationMode validationMode)
        {
            var report =
                new FrameworkAuthoringValidationReport(validationMode);

            if (gameApplication == null)
            {
                report.AddError(
                    "Game Application is missing.",
                    null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(
                    gameApplication.ApplicationName))
            {
                report.AddWarning(
                    "Game Application has no display name. The asset name will be used in diagnostics.",
                    gameApplication);
            }

            if (gameApplication.StartupRoute == null)
            {
                report.AddError(
                    "Startup Route is missing. Assign the first Route in this Game Application.",
                    gameApplication);
            }
            else if (validateDependencies)
            {
                report.AddRange(
                    ValidateRoute(
                        gameApplication.StartupRoute,
                        true,
                        validationMode));
            }

            ValidatePersistentContentComposition(
                report,
                gameApplication,
                validateDependencies);

            report.AddRange(
                ApplicationFrameRateAuthoringValidator
                    .Validate(gameApplication));

            // IF-ID-06: Startup identity chain only (Startup Route + Startup Activity).
            report.AddRange(
                FrameworkIdentityAuthoringValidator
                    .ValidateGameApplicationIdentity(
                        gameApplication,
                        validationMode));

            if (!report.HasIssues)
            {
                report.AddInfo(
                    "Game Application authoring is valid for the current framework scope.",
                    gameApplication);
            }

            return report;
        }

        private static void ValidatePersistentContentComposition(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset gameApplication,
            bool validateDependencies)
        {
            PersistentContentComposition composition =
                gameApplication.PersistentContent;

            if (composition == null)
            {
                report.AddError(
                    "Persistent Content composition is missing.",
                    gameApplication);
                return;
            }

            SceneAsset sceneAsset =
                composition.ContainerScene as SceneAsset;
            if (sceneAsset == null)
            {
                report.AddError(
                    composition.ContainerScene == null
                        ? "Persistent Content Scene is missing."
                        : "Persistent Content Scene must directly reference a Unity Scene asset.",
                    gameApplication);
                return;
            }

            string scenePath =
                AssetDatabase.GetAssetPath(sceneAsset);

            ValidateSceneAssetReference(
                report,
                gameApplication,
                scenePath,
                sceneAsset.name,
                "Persistent Content Scene");

            if (!IsSceneInBuildSettings(scenePath))
            {
                report.AddError(
                    $"Persistent Content Scene '{scenePath}' is not enabled in the Build Profile.",
                    gameApplication);
            }

            ValidateUniqueBuildSceneName(
                report,
                gameApplication,
                sceneAsset.name,
                scenePath);

            if (!validateDependencies)
            {
                return;
            }

            ValidatePersistentContentScene(
                report,
                gameApplication,
                scenePath);
        }

        private static void ValidateUniqueBuildSceneName(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset owner,
            string sceneName,
            string expectedPath)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                report.AddError(
                    "Persistent Content Scene has no valid scene name.",
                    owner);
                return;
            }

            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;
            int enabledNameMatches = 0;

            if (scenes != null)
            {
                for (int index = 0;
                     index < scenes.Length;
                     index++)
                {
                    EditorBuildSettingsScene scene =
                        scenes[index];
                    if (scene == null ||
                        !scene.enabled)
                    {
                        continue;
                    }

                    string candidateName =
                        System.IO.Path.GetFileNameWithoutExtension(
                            scene.path);
                    if (string.Equals(
                            candidateName,
                            sceneName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        enabledNameMatches++;
                    }
                }
            }

            if (enabledNameMatches != 1)
            {
                report.AddError(
                    $"Persistent Content Scene name '{sceneName}' must be unique among enabled Build Profile scenes. matches='{enabledNameMatches}' expectedPath='{expectedPath}'.",
                    owner);
            }
        }

        private static void ValidatePersistentContentScene(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset owner,
            string scenePath)
        {
            SceneValidationScope sceneScope = default;

            try
            {
                sceneScope =
                    FrameworkEditorSceneValidationUtility
                        .OpenSceneForValidation(scenePath);
                Scene scene =
                    sceneScope.Scene;

                if (!scene.IsValid() ||
                    !scene.isLoaded)
                {
                    report.AddError(
                        $"Persistent Content Scene '{scenePath}' could not be opened for composition validation.",
                        owner);
                    return;
                }

                ValidatePersistentContentSceneContents(
                    report,
                    owner,
                    scene,
                    scenePath);
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Persistent Content Scene '{scenePath}' could not be validated. {exception.Message}",
                    owner);
            }
            finally
            {
                sceneScope.CloseIfOwned();
            }
        }

        private static void ValidatePersistentContentSceneContents(
            FrameworkAuthoringValidationReport report,
            Object owner,
            Scene scene,
            string sceneLabel)
        {
            GameObject[] roots =
                scene.GetRootGameObjects();

            ValidateExactSceneComponentCount<CameraOutputSessionBinding>(
                report,
                owner,
                scene,
                nameof(CameraOutputSessionBinding));
            ValidateMaximumSceneComponentCount<SessionCameraOverrideBinding>(
                report,
                owner,
                scene,
                nameof(SessionCameraOverrideBinding),
                1);

            CameraOutputSessionBinding[] outputBindings =
                GetSceneComponents<CameraOutputSessionBinding>(
                    scene);

            if (outputBindings.Length == 1)
            {
                CameraOutputSessionBinding binding =
                    outputBindings[0];

                if (string.IsNullOrWhiteSpace(
                        binding.OutputIdText))
                {
                    report.AddError(
                        "Persistent Content Camera Output requires an explicit Output ID.",
                        binding);
                }

                if (binding.UnityCamera == null)
                {
                    report.AddError(
                        "Persistent Content Camera Output requires an explicit Unity Camera reference.",
                        binding);
                }

                if (binding.CinemachineBrain == null)
                {
                    report.AddError(
                        "Persistent Content Camera Output requires an explicit Cinemachine Brain reference.",
                        binding);
                }

                if (binding.UnityCamera != null &&
                    binding.CinemachineBrain != null &&
                    binding.UnityCamera.gameObject !=
                    binding.CinemachineBrain.gameObject)
                {
                    report.AddError(
                        "Persistent Content Unity Camera and Cinemachine Brain must belong to the same physical output GameObject.",
                        binding);
                }
            }

            SessionCameraOverrideBinding[] sessionBindings =
                GetSceneComponents<SessionCameraOverrideBinding>(
                    scene);

            if (sessionBindings.Length == 1)
            {
                ValidateSessionCameraOverrideBinding(
                    report,
                    outputBindings.Length == 1
                        ? outputBindings[0]
                        : null,
                    sessionBindings[0]);
            }

            EventSystem[] eventSystems =
                GetSceneComponents<EventSystem>(
                    scene);
            InputSystemUIInputModule[] inputModules =
                GetSceneComponents<InputSystemUIInputModule>(
                    scene);
            UnityPauseSurfaceAdapter[] builtInPauseAdapters =
                GetSceneComponents<UnityPauseSurfaceAdapter>(
                    scene);
            PauseRequestTrigger[] pauseRequestTriggers =
                GetSceneComponents<PauseRequestTrigger>(
                    scene);

            for (int index = 0;
                 index < builtInPauseAdapters.Length;
                 index++)
            {
                ValidatePersistentPauseSurfaceAdapter(
                    report,
                    scene,
                    builtInPauseAdapters[index]);
            }

            if (eventSystems.Length == 1)
            {
                EventSystem eventSystem =
                    eventSystems[0];

                if (!eventSystem.enabled)
                {
                    report.AddError(
                        "Persistent Content EventSystem must be enabled.",
                        eventSystem);
                }

                if (!eventSystem.sendNavigationEvents)
                {
                    report.AddError(
                        "Persistent Content EventSystem must send navigation events for Move, Submit and Cancel UI input.",
                        eventSystem);
                }
            }

            if (inputModules.Length == 1)
            {
                ValidatePersistentContentUiInputModule(
                    report,
                    inputModules[0]);
            }

            if (eventSystems.Length == 1 &&
                inputModules.Length == 1 &&
                eventSystems[0].gameObject !=
                inputModules[0].gameObject)
            {
                report.AddError(
                    "Persistent Content EventSystem and InputSystemUIInputModule must belong to the same GameObject.",
                    inputModules[0]);
            }

            int legacyInputModuleCount =
                CountSceneComponents<StandaloneInputModule>(
                    scene);
            if (legacyInputModuleCount > 0)
            {
                report.AddError(
                    $"Persistent Content Scene '{sceneLabel}' must not contain StandaloneInputModule when the Input System UI module is authoritative. found='{legacyInputModuleCount}'.",
                    owner);
            }

            int missingScriptCount =
                CountMissingScripts(
                    scene,
                    out GameObject firstMissingScriptObject);
            if (missingScriptCount > 0)
            {
                report.AddError(
                    $"Persistent Content Scene '{sceneLabel}' contains missing MonoBehaviour scripts. count='{missingScriptCount}'.",
                    firstMissingScriptObject != null
                        ? firstMissingScriptObject
                        : owner);
            }

            int canvasCount =
                CountSceneComponents<Canvas>(
                    scene);
            int transitionAdapterCount =
                CountSceneAdapters<ITransitionEffectAdapter>(
                    scene);
            int loadingAdapterCount =
                CountSceneAdapters<ILoadingSurfaceAdapter>(
                    scene);
            int pauseAdapterCount =
                CountSceneAdapters<IPauseSurfaceAdapter>(
                    scene);
            int resumeButtonCount =
                CountResumeButtons(
                    pauseRequestTriggers);

            report.AddInfo(
                $"Persistent Content Scene composition scanned. roots='{roots.Length}' canvases='{canvasCount}' eventSystems='{eventSystems.Length}' inputSystemUiModules='{inputModules.Length}' legacyInputModules='{legacyInputModuleCount}' transitionAdapters='{transitionAdapterCount}' loadingAdapters='{loadingAdapterCount}' pauseAdapters='{pauseAdapterCount}' pauseRequestTriggers='{pauseRequestTriggers.Length}' resumeButtons='{resumeButtonCount}' missingScripts='{missingScriptCount}'.",
                owner);
        }

        private static void ValidatePersistentPauseSurfaceAdapter(
            FrameworkAuthoringValidationReport report,
            Scene expectedScene,
            UnityPauseSurfaceAdapter adapter)
        {
            if (adapter == null)
            {
                return;
            }

            if (adapter.CanvasGroup == null)
            {
                report.AddError(
                    "Persistent Content Unity Pause Surface Adapter requires an explicit CanvasGroup.",
                    adapter);
            }

            if (adapter.SurfaceRoot == null)
            {
                report.AddError(
                    "Persistent Content Unity Pause Surface Adapter requires an explicit Surface Root.",
                    adapter);
            }

            if (adapter.CanvasGroup != null &&
                adapter.SurfaceRoot != null &&
                adapter.CanvasGroup.gameObject !=
                adapter.SurfaceRoot)
            {
                report.AddError(
                    "Persistent Content Unity Pause Surface Adapter requires its CanvasGroup on the configured Surface Root.",
                    adapter);
            }

            if (adapter.SurfaceRoot != null &&
                adapter.SurfaceRoot.scene !=
                expectedScene)
            {
                report.AddError(
                    "Persistent Content Unity Pause Surface Adapter Surface Root must belong to the same Content Scene.",
                    adapter);
            }

            if (!adapter.ApplyRunningStateOnAwake)
            {
                report.AddError(
                    "Persistent Content Unity Pause Surface Adapter must apply the initial Running presentation on Awake.",
                    adapter);
            }
        }

        private static int CountResumeButtons(
            IReadOnlyList<PauseRequestTrigger> triggers)
        {
            if (triggers == null ||
                triggers.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int triggerIndex = 0;
                 triggerIndex < triggers.Count;
                 triggerIndex++)
            {
                PauseRequestTrigger trigger =
                    triggers[triggerIndex];
                if (trigger == null)
                {
                    continue;
                }

                Button button =
                    trigger.GetComponent<Button>();
                if (button == null ||
                    !button.interactable)
                {
                    continue;
                }

                int eventCount =
                    button.onClick.GetPersistentEventCount();
                for (int eventIndex = 0;
                     eventIndex < eventCount;
                     eventIndex++)
                {
                    if (button.onClick.GetPersistentTarget(eventIndex) ==
                            trigger &&
                        string.Equals(
                            button.onClick.GetPersistentMethodName(eventIndex),
                            nameof(PauseRequestTrigger.RequestResume),
                            StringComparison.Ordinal))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private static void ValidatePersistentContentUiInputModule(
            FrameworkAuthoringValidationReport report,
            InputSystemUIInputModule inputModule)
        {
            if (inputModule == null)
            {
                return;
            }

            if (!inputModule.enabled)
            {
                report.AddError(
                    "Persistent Content InputSystemUIInputModule must be enabled.",
                    inputModule);
            }

            if (inputModule.actionsAsset == null)
            {
                report.AddError(
                    "Persistent Content InputSystemUIInputModule requires an explicit Actions Asset.",
                    inputModule);
            }

            if (inputModule.point == null)
            {
                report.AddError(
                    "Persistent Content InputSystemUIInputModule requires an explicit Point action.",
                    inputModule);
            }

            if (inputModule.leftClick == null)
            {
                report.AddError(
                    "Persistent Content InputSystemUIInputModule requires an explicit Left Click action.",
                    inputModule);
            }

            if (inputModule.scrollWheel == null)
            {
                report.AddError(
                    "Persistent Content InputSystemUIInputModule requires an explicit Scroll Wheel action.",
                    inputModule);
            }

            if (inputModule.move == null)
            {
                report.AddError(
                    "Persistent Content InputSystemUIInputModule requires an explicit Move action.",
                    inputModule);
            }

            if (inputModule.submit == null)
            {
                report.AddError(
                    "Persistent Content InputSystemUIInputModule requires an explicit Submit action.",
                    inputModule);
            }

            if (inputModule.cancel == null)
            {
                report.AddError(
                    "Persistent Content InputSystemUIInputModule requires an explicit Cancel action.",
                    inputModule);
            }
        }

        private static void ValidateSessionCameraOverrideBinding(
            FrameworkAuthoringValidationReport report,
            CameraOutputSessionBinding expectedOutput,
            SessionCameraOverrideBinding binding)
        {
            if (binding == null)
            {
                return;
            }

            if (binding.PersistentOutputSession == null)
            {
                report.AddError(
                    "Persistent Content Session Camera Override requires an explicit Camera Output Session Binding.",
                    binding);
            }
            else if (expectedOutput != null &&
                     binding.PersistentOutputSession != expectedOutput)
            {
                report.AddError(
                    "Persistent Content Session Camera Override must reference the unique Camera Output Session Binding in the same Content Scene.",
                    binding);
            }

            if (string.IsNullOrWhiteSpace(
                    binding.ScopeId))
            {
                report.AddError(
                    "Persistent Content Session Camera Override requires an explicit Scope ID.",
                    binding);
            }

            if (string.IsNullOrWhiteSpace(
                    binding.RequestIdText))
            {
                report.AddError(
                    "Persistent Content Session Camera Override requires an explicit Request ID.",
                    binding);
            }

            if (string.IsNullOrWhiteSpace(
                    binding.TieBreakerId))
            {
                report.AddError(
                    "Persistent Content Session Camera Override requires an explicit Tie Breaker ID.",
                    binding);
            }

            if (binding.RigComposer == null)
            {
                report.AddError(
                    "Persistent Content Session Camera Override requires an explicit Camera Rig Composer.",
                    binding);
            }
            else
            {
                ValidatePersistentSessionRigComposer(
                    report,
                    binding.RigComposer);
            }

            if (binding.TargetSource == null)
            {
                report.AddError(
                    "Persistent Content Session Camera Override requires an explicit Target Source Transform.",
                    binding);
            }
        }

        private static void ValidatePersistentSessionRigComposer(
            FrameworkAuthoringValidationReport report,
            CameraRigComposer composer)
        {
            if (composer == null)
            {
                return;
            }

            if (!composer.TryValidateForApply(
                    out string issue))
            {
                report.AddError(
                    $"Persistent Content Session Camera Rig is not authorable. {issue}",
                    composer);
            }

            if (composer.CinemachineCamera == null)
            {
                report.AddError(
                    "Persistent Content Session Camera Rig requires an explicit Cinemachine Camera reference.",
                    composer);
            }

            if (composer.TargetSourceBehaviour == null &&
                composer.TargetSourceKind ==
                CameraTargetSourceKind.ExplicitTransform)
            {
                if (composer.FollowRequirement ==
                        CameraTargetRequirement.Required &&
                    composer.ExplicitFollowTarget == null)
                {
                    report.AddError(
                        "Persistent Content Session Camera Rig requires an explicit Follow Target.",
                        composer);
                }

                if (composer.LookAtRequirement ==
                        CameraTargetRequirement.Required &&
                    composer.ExplicitLookAtTarget == null)
                {
                    report.AddError(
                        "Persistent Content Session Camera Rig requires an explicit Look At Target.",
                        composer);
                }
            }
        }

        private static int CountMissingScripts(
            Scene scene,
            out GameObject firstContext)
        {
            firstContext = null;

            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return 0;
            }

            int count = 0;
            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Transform[] transforms =
                    roots[rootIndex]
                        .GetComponentsInChildren<Transform>(
                            true);

                for (int transformIndex = 0;
                     transformIndex < transforms.Length;
                     transformIndex++)
                {
                    GameObject gameObject =
                        transforms[transformIndex].gameObject;
                    int objectMissingCount =
                        GameObjectUtility
                            .GetMonoBehavioursWithMissingScriptCount(
                                gameObject);

                    if (objectMissingCount <= 0)
                    {
                        continue;
                    }

                    if (firstContext == null)
                    {
                        firstContext =
                            gameObject;
                    }

                    count +=
                        objectMissingCount;
                }
            }

            return count;
        }

        private static TComponent[] GetSceneComponents<TComponent>(
            Scene scene)
            where TComponent : Component
        {
            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return Array.Empty<TComponent>();
            }

            var components =
                new List<TComponent>();
            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                TComponent[] rootComponents =
                    roots[index]
                        .GetComponentsInChildren<TComponent>(
                            true);

                if (rootComponents != null &&
                    rootComponents.Length > 0)
                {
                    components.AddRange(
                        rootComponents);
                }
            }

            return components.ToArray();
        }

        private static void ValidateExactSceneComponentCount<TComponent>(
            FrameworkAuthoringValidationReport report,
            Object owner,
            Scene scene,
            string label)
            where TComponent : Component
        {
            int count =
                CountSceneComponents<TComponent>(scene);

            if (count == 1)
            {
                return;
            }

            report.AddError(
                $"Persistent Content Scene requires exactly one {label}, but found '{count}'.",
                owner);
        }

        private static void ValidateMaximumSceneComponentCount<TComponent>(
            FrameworkAuthoringValidationReport report,
            Object owner,
            Scene scene,
            string label,
            int maximum)
            where TComponent : Component
        {
            int count = CountSceneComponents<TComponent>(scene);

            if (count <= maximum)
            {
                return;
            }

            report.AddError(
                $"Persistent Content Scene permits at most '{maximum}' {label}, but found '{count}'.",
                owner);
        }

        private static int CountSceneComponents<TComponent>(
            Scene scene)
            where TComponent : Component
        {
            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return 0;
            }

            int count = 0;
            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                count += roots[index]
                    .GetComponentsInChildren<TComponent>(true)
                    .Length;
            }

            return count;
        }

        private static bool IsSceneInBuildSettings(
            string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;
            if (scenes == null)
            {
                return false;
            }

            for (int index = 0;
                 index < scenes.Length;
                 index++)
            {
                EditorBuildSettingsScene scene =
                    scenes[index];
                if (scene != null &&
                    scene.enabled &&
                    string.Equals(
                        scene.path,
                        scenePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountSceneAdapters<TAdapter>(
            Scene scene)
        {
            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return 0;
            }

            GameObject[] roots =
                scene.GetRootGameObjects();
            if (roots == null ||
                roots.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                GameObject root =
                    roots[rootIndex];
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
                    if (behaviours[behaviourIndex] is TAdapter)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static FrameworkAuthoringValidationReport ValidateRoute(
            RouteAsset route,
            bool validateDependencies,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (route == null)
            {
                report.AddError("Route is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(route.RouteName))
            {
                report.AddWarning(
                    "Route has no display name. The asset name will be used in diagnostics.",
                    route);
            }

            if (!route.HasValidRouteId)
            {
                report.AddError(
                    "Route ID is missing or invalid. Route identity must be authored explicitly and cannot fall back to name or scene path.",
                    route);
            }

            if (string.IsNullOrWhiteSpace(route.PrimaryScenePath))
            {
                report.AddError(
                    "Primary Scene is missing. A Route must declare one Primary Scene for Scene Lifecycle.",
                    route);
            }
            else
            {
                ValidatePrimarySceneReference(report, route);
            }

            if (!Enum.IsDefined(typeof(TransitionGateMode), route.TransitionGateMode))
            {
                report.AddError(
                    "Route Transition Gate has an invalid value.",
                    route);
            }

            if (route.StartupActivity == null)
            {
                report.AddInfo(
                    "Route has no Startup Activity. This is valid for menu/no-activity routes.",
                    route);
            }
            else
            {
                ValidateRouteStartupActivityEntryReadiness(
                    report,
                    route);

                if (validateDependencies)
                {
                    report.AddRange(
                        ValidateActivity(
                            route.StartupActivity,
                            validationMode));
                }
            }

            if (!report.HasIssues)
            {
                report.AddInfo("Route authoring is valid for the current framework scope.", route);
            }

            return report;
        }

        private static void ValidateRouteStartupActivityEntryReadiness(
            FrameworkAuthoringValidationReport report,
            RouteAsset route)
        {
            ActivityAsset startupActivity =
                route.StartupActivity;

            if (startupActivity == null)
            {
                return;
            }

            if (!startupActivity.HasDefinedEntryReadinessPolicy)
            {
                report.AddError(
                    $"Route Startup Activity '{startupActivity.ActivityName}' has an invalid Activity Entry Readiness Policy.",
                    route);
                return;
            }

            if (!startupActivity.WaitsForEntryReadiness)
            {
                report.AddInfo(
                    $"Route Startup Activity '{startupActivity.ActivityName}' uses Entry Readiness = ObserveOnly. The Route transition is not retained for Activity readiness.",
                    route);
                return;
            }

            if (route.TransitionGateMode !=
                TransitionGateMode.InputInteractionAndGameplay)
            {
                report.AddError(
                    $"Route Startup Activity '{startupActivity.ActivityName}' uses Entry Readiness = {startupActivity.EntryReadinessPolicy}, but Route Transition Gate is '{route.TransitionGateMode}'. Waiting requires InputInteractionAndGameplay because the Route transition owns Startup Activity entry.",
                    route);
                return;
            }

            report.AddInfo(
                $"Route Startup Activity entry-readiness policy '{startupActivity.EntryReadinessPolicy}' is compatible with Route Transition Gate '{route.TransitionGateMode}'.",
                route);
        }

        private static FrameworkAuthoringValidationReport ValidateActivity(
            ActivityAsset activity,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (activity == null)
            {
                report.AddError("Activity is missing.", null);
                return report;
            }

            if (!activity.HasValidActivityId)
            {
                report.AddError(
                    "Activity ID is required and must remain stable across cosmetic renames.",
                    activity);
            }

            ValidateActivityEntryReadinessPolicy(
                report,
                activity);

            switch (activity.VisualTransitionMode)
            {
                case ActivityVisualTransitionMode.Seamless:
                    report.AddInfo(
                        "Activity Transition Mode is Seamless. Activity requests skip the Session TransitionSurface and canonical LoadingSurface, including when the operation performs Activity scene load/release side-effects.",
                        activity);
                    break;
                case ActivityVisualTransitionMode.Fade:
                    report.AddInfo(
                        "Activity Transition Mode is Fade. Activity requests use the Session TransitionSurface and skip the canonical LoadingSurface, including when the operation performs Activity scene load/release side-effects.",
                        activity);
                    break;
                case ActivityVisualTransitionMode.FadeWithLoading:
                    report.AddInfo(
                        "Activity Transition Mode is FadeWithLoading. Activity requests use the Session TransitionSurface and canonical LoadingSurface when the operation performs Activity scene load/release side-effects.",
                        activity);
                    break;
            }

            if (!Enum.IsDefined(typeof(TransitionGateMode), activity.TransitionGateMode))
            {
                report.AddError(
                    "Activity Transition Gate has an invalid value.",
                    activity);
            }
            else if (!activity.WaitsForEntryReadiness &&
                activity.VisualTransitionMode != ActivityVisualTransitionMode.Seamless &&
                activity.TransitionGateMode != TransitionGateMode.InputInteractionAndGameplay)
            {
                report.AddWarning(
                    "Activity uses a visible Transition Mode. Transition Gate = InputInteractionAndGameplay is recommended to block repeated UI/input/gameplay during the fade.",
                    activity);
            }

            if (activity.ActivityContentProfile == null)
            {
                report.AddInfo(
                    "Activity has no Activity Content Profile. Activity scene/content loading remains absent and Loading will be skipped for Activity requests.",
                    activity);
            }
            else
            {
                ValidateActivityOperationGuards(report, activity, activity.ActivityContentProfile);
                report.AddRange(ValidateActivityContentProfile(activity.ActivityContentProfile, validationMode));
            }

            if (!report.HasIssues)
            {
                report.AddInfo("Activity authoring is valid for the current framework scope.", activity);
            }

            return report;
        }

        private static void ValidateActivityEntryReadinessPolicy(
            FrameworkAuthoringValidationReport report,
            ActivityAsset activity)
        {
            if (!activity.HasDefinedEntryReadinessPolicy)
            {
                report.AddError(
                    $"Activity '{activity.ActivityName}' has an invalid Activity Entry Readiness Policy.",
                    activity);
                return;
            }

            ActivityEntryReadinessPolicy policy =
                activity.EntryReadinessPolicy;

            switch (policy)
            {
                case ActivityEntryReadinessPolicy.ObserveOnly:
                    report.AddInfo(
                        "Activity Entry Readiness is ObserveOnly. Readiness remains observable and does not retain visual cover or the operation capability gate.",
                        activity);
                    return;

                case ActivityEntryReadinessPolicy.WaitCovered:
                    if (activity.VisualTransitionMode ==
                        ActivityVisualTransitionMode.Seamless)
                    {
                        report.AddError(
                            "Activity Entry Readiness = WaitCovered is incompatible with Transition Presentation = Seamless. Select Fade or FadeWithLoading; no visual fallback is applied.",
                            activity);
                    }
                    break;

                case ActivityEntryReadinessPolicy.WaitVisible:
                    break;
            }

            if (activity.TransitionGateMode !=
                TransitionGateMode.InputInteractionAndGameplay)
            {
                report.AddError(
                    $"Activity Entry Readiness = {policy} requires Transition Gate = InputInteractionAndGameplay. The framework does not silently strengthen the authored gate.",
                    activity);
                return;
            }

            report.AddInfo(
                $"Activity Entry Readiness policy '{policy}' is compatible with Transition Presentation '{activity.VisualTransitionMode}' and Transition Gate '{activity.TransitionGateMode}'.",
                activity);
        }

        private static void ValidateActivityOperationGuards(
            FrameworkAuthoringValidationReport report,
            ActivityAsset activity,
            ActivityContentProfileAsset profile)
        {
            int sceneSideEffectDeclarations = CountActivitySceneSideEffectDeclarations(profile);
            if (sceneSideEffectDeclarations <= 0)
            {
                return;
            }

            switch (activity.VisualTransitionMode)
            {
                case ActivityVisualTransitionMode.Seamless:
                    report.AddInfo(
                        $"Activity '{activity.ActivityName}' declares {sceneSideEffectDeclarations} Activity content scene(s) and uses Seamless. Runtime may load/release those scenes without TransitionSurface or LoadingSurface.",
                        activity);
                    break;
                case ActivityVisualTransitionMode.Fade:
                    report.AddInfo(
                        $"Activity '{activity.ActivityName}' declares {sceneSideEffectDeclarations} Activity content scene(s) and uses Fade. Runtime may load/release those scenes inside the TransitionSurface without the canonical LoadingSurface.",
                        activity);
                    break;
                case ActivityVisualTransitionMode.FadeWithLoading:
                    report.AddInfo(
                        $"Activity '{activity.ActivityName}' declares {sceneSideEffectDeclarations} Activity content scene(s) and uses FadeWithLoading. Runtime may load/release those scenes inside the TransitionSurface with the canonical LoadingSurface.",
                        activity);
                    break;
            }
        }

        private static int CountActivitySceneSideEffectDeclarations(ActivityContentProfileAsset profile)
        {
            if (profile == null || !profile.HasScenes)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < profile.Scenes.Count; i++)
            {
                var entry = profile.Scenes[i];
                if (entry != null && entry.HasScene)
                {
                    count++;
                }
            }

            return count;
        }

        private static FrameworkAuthoringValidationReport ValidateActivityContentProfile(
            ActivityContentProfileAsset profile,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (profile == null)
            {
                report.AddError("Activity Content Profile is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(profile.ProfileId))
            {
                report.AddWarning(
                    "Activity Content Profile has no explicit Profile Id. The asset name will be used in diagnostics.",
                    profile);
            }

            if (!profile.HasScenes)
            {
                report.AddWarning(
                    "Activity Content Profile has no scene declarations. This is valid as a placeholder, but it does not enable Activity content loading.",
                    profile);
            }

            var contentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < profile.Scenes.Count; i++)
            {
                ValidateActivityContentSceneEntry(report, profile, profile.Scenes[i], i, contentIds);
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Content Profile '{profile.ProfileId}' is valid for Activity scene composition authoring.",
                    profile);
            }

            return report;
        }

        private static void ValidateActivityContentSceneEntry(
            FrameworkAuthoringValidationReport report,
            ActivityContentProfileAsset profile,
            ActivityContentSceneEntry entry,
            int index,
            HashSet<string> contentIds)
        {
            if (entry == null)
            {
                report.AddError(
                    $"Activity Content Profile '{profile.ProfileId}' has a null scene entry at index {index}.",
                    profile);
                return;
            }

            var label = $"Activity Content Scene {index + 1}";
            if (!entry.HasExplicitContentId)
            {
                report.AddError(
                    $"{label} in profile '{profile.ProfileId}' has no explicit Content Id. F25 Activity content identity must not fall back to scene path/name.",
                    profile);
            }
            else if (!contentIds.Add(entry.ExplicitContentId))
            {
                report.AddError(
                    $"{label} in profile '{profile.ProfileId}' duplicates Content Id '{entry.ExplicitContentId}'. Content ids must be unique within an Activity Content Profile.",
                    profile);
            }

            if (!entry.HasScene)
            {
                if (entry.Requiredness == FrameworkContentRequiredness.Required)
                {
                    report.AddError(
                        $"{label} in profile '{profile.ProfileId}' is Required but has no scene assigned.",
                        profile);
                }
                else
                {
                    report.AddWarning(
                        $"{label} in profile '{profile.ProfileId}' has no scene assigned. Optional entries are skipped by Activity scene composition execution.",
                        profile);
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(entry.ScenePath))
            {
                report.AddError(
                    $"{label} in profile '{profile.ProfileId}' has a cached scene name but no scene path. Reassign the scene in the Inspector.",
                    profile);
                return;
            }

            ValidateSceneAssetReference(
                report,
                profile,
                entry.ScenePath,
                entry.SceneName,
                label);

            if (!IsSceneInBuildSettings(entry.ScenePath))
            {
                report.AddWarning(
                    $"{label} scene '{entry.ScenePath}' is not included in Build Settings. Activity scene composition execution requires Activity content scenes to be build-loadable.",
                    profile);
            }

            if (entry.LoadMode != ActivityContentSceneLoadMode.Additive)
            {
                report.AddError(
                    $"{label} in profile '{profile.ProfileId}' has unsupported load mode '{entry.LoadMode}'. Activity scene composition only supports Additive Activity scenes.",
                    profile);
            }
        }

        private static FrameworkAuthoringValidationReport ValidateActivityLocalVisibilityAdapter(
            ActivityLocalVisibilityAdapter binding,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (binding == null)
            {
                report.AddError("Activity Local Visibility Adapter is missing.", null);
                return report;
            }

            string objectName = binding.gameObject != null ? binding.gameObject.name : "<missing>";

            ActivityVisibilityEvaluation visibilityEvaluation =
                binding.EvaluateVisibility(null);
            if (!visibilityEvaluation.IsValid)
            {
                report.AddError(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' has an invalid Activity Rule: {visibilityEvaluation.DiagnosticReason}. Correct the indicated schema, list entry, enum or Activity identity without relying on automatic repair.",
                    binding);
            }

            if (!binding.HasExplicitLocalContentId)
            {
                report.AddError(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' has no Local Content Id. F5 local identity requires an explicit id; GameObject names and hierarchy paths are diagnostics only.",
                    binding);
            }

            var parentBinding = FindParentActivityLocalVisibilityAdapter(binding);
            if (parentBinding != null)
            {
                report.AddWarning(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' is nested under '{parentBinding.gameObject.name}'. Nested Activity local visibility policy is not defined yet.",
                    binding);
            }

            int childBindingCount = CountChildActivityLocalVisibilityAdapters(binding);
            if (childBindingCount > 0)
            {
                report.AddWarning(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' has {childBindingCount} child Activity Local Visibility Adapter component(s). Keep Activity local visibility adapter roots flat for now.",
                    binding);
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' is valid for the current framework scope.",
                    binding);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateRouteContentBinding(
            RouteContentBinding binding,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (binding == null)
            {
                report.AddError("Route Content Binding is missing.", null);
                return report;
            }

            string objectName = binding.gameObject != null ? binding.gameObject.name : "<missing>";

            if (binding.Route == null)
            {
                report.AddError(
                    $"Route Content Binding on GameObject '{objectName}' has no Route assigned.",
                    binding);
            }
            else
            {
                ValidateRouteContentBindingSceneRoute(report, binding, objectName);
            }

            if (!binding.HasExplicitLocalContentId)
            {
                report.AddError(
                    $"Route Content Binding on GameObject '{objectName}' has no Local Content Id. F5 local identity requires an explicit id; GameObject names and hierarchy paths are diagnostics only.",
                    binding);
            }

            var parentBinding = FindParentRouteContentBinding(binding);
            if (parentBinding != null)
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' is nested under '{parentBinding.gameObject.name}'. Nested Route content policy is not defined in F3; keep Route content roots flat.",
                    binding);
            }

            int childBindingCount = CountChildRouteContentBindings(binding);
            if (childBindingCount > 0)
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' has {childBindingCount} child Route Content Binding component(s). Keep Route content roots flat for the F3 callback baseline.",
                    binding);
            }

            int receiverCount = CountRouteContentLifecycleReceivers(binding);
            if (receiverCount == 0)
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' has no IRouteContentLifecycleReceiver in itself or its children. Route Content Runtime will dispatch with zero receivers, and Route Callback Smoke cannot use this binding as callback proof.",
                    binding);
            }
            else
            {
                report.AddInfo(
                    $"Route Content Binding on GameObject '{objectName}' has {receiverCount} Route content lifecycle receiver(s).",
                    binding);
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Route Content Binding on GameObject '{objectName}' is valid for the F3 Route callback baseline.",
                    binding);
            }

            return report;
        }

        private static void ValidateRouteContentBindingSceneRoute(
            FrameworkAuthoringValidationReport report,
            RouteContentBinding binding,
            string objectName)
        {
            var route = binding.Route;
            var scene = binding.gameObject != null ? binding.gameObject.scene : default;

            if (!scene.IsValid())
            {
                report.AddInfo(
                    $"Route Content Binding on GameObject '{objectName}' is not in a valid scene. Scene-route validation is skipped for prefabs or disconnected objects.",
                    binding);
                return;
            }

            if (!scene.isLoaded)
            {
                report.AddInfo(
                    $"Route Content Binding on GameObject '{objectName}' is in scene '{scene.name}', but the scene is not loaded. Scene-route validation only checks loaded scenes.",
                    binding);
                return;
            }

            string scenePath = scene.path;
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' is in an unsaved scene. Save the scene so it can be compared against Route.PrimaryScenePath.",
                    binding);
                return;
            }

            if (string.IsNullOrWhiteSpace(route.PrimaryScenePath))
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' points to Route '{GetRouteLabel(route)}', but that Route has no Primary Scene path.",
                    binding);
                return;
            }

            if (!string.Equals(scenePath, route.PrimaryScenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' points to Route '{GetRouteLabel(route)}', but it is authored in scene '{scenePath}'. The Route primary scene is '{route.PrimaryScenePath}'. This will cause Route callbacks and Route Callback Smoke to resolve the binding for the wrong Route.",
                    binding);
            }
        }

        private static FrameworkAuthoringValidationReport ValidateRouteCycleResetTrigger(
            RouteCycleResetTrigger trigger,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (trigger == null)
            {
                report.AddError("Route Cycle Reset Trigger is missing.", null);
                return report;
            }

            string objectName = trigger.gameObject != null ? trigger.gameObject.name : "<missing>";
            ValidateCycleResetTriggerCommon(report, trigger, objectName, "Route Cycle Reset Trigger", trigger.AuthoringReason);

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Route Cycle Reset Trigger on GameObject '{objectName}' is valid for the F12 Cycle Reset authoring UX scope.",
                    trigger);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateActivityCycleResetTrigger(
            ActivityCycleResetTrigger trigger,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (trigger == null)
            {
                report.AddError("Activity Cycle Reset Trigger is missing.", null);
                return report;
            }

            string objectName = trigger.gameObject != null ? trigger.gameObject.name : "<missing>";
            ValidateCycleResetTriggerCommon(report, trigger, objectName, "Activity Cycle Reset Trigger", trigger.AuthoringReason);

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Cycle Reset Trigger on GameObject '{objectName}' is valid for the F12 Cycle Reset authoring UX scope.",
                    trigger);
            }

            return report;
        }

        private static void ValidateCycleResetTriggerCommon(
            FrameworkAuthoringValidationReport report,
            MonoBehaviour trigger,
            string objectName,
            string triggerLabel,
            string reason)
        {
            if (trigger == null)
            {
                return;
            }

            if (!trigger.gameObject.scene.IsValid())
            {
                report.AddInfo(
                    $"{triggerLabel} on GameObject '{objectName}' is not in a valid scene. Scene authoring validation is skipped for prefabs or disconnected objects.",
                    trigger);
            }

            if (!trigger.gameObject.activeInHierarchy)
            {
                report.AddInfo(
                    $"{triggerLabel} on GameObject '{objectName}' is inactive in hierarchy. It will not submit requests until active.",
                    trigger);
            }

            if (CycleResetTriggerAuthoringText.ContainsFutureResetVocabulary(reason))
            {
                report.AddWarning(
                    $"{triggerLabel} on GameObject '{objectName}' uses reason '{reason}'. Cycle Reset is Route/Activity-level only; object/component/player/actor/pool/save/reload wording belongs to later reset phases.",
                    trigger);
            }

            bool hasRouteTrigger = trigger.GetComponent<RouteCycleResetTrigger>() != null;
            bool hasActivityTrigger = trigger.GetComponent<ActivityCycleResetTrigger>() != null;
            if (hasRouteTrigger && hasActivityTrigger)
            {
                report.AddWarning(
                    $"GameObject '{objectName}' has both Route and Activity Cycle Reset Triggers. This is allowed for tooling, but separate buttons/objects are clearer for authoring.",
                    trigger);
            }
        }

        private static void ValidatePrimarySceneReference(FrameworkAuthoringValidationReport report, RouteAsset route)
        {
            ValidateSceneAssetReference(
                report,
                route,
                route.PrimaryScenePath,
                route.PrimarySceneName,
                "Primary Scene");
        }

        private static void ValidateSceneAssetReference(
            FrameworkAuthoringValidationReport report,
            Object owner,
            string scenePath,
            string cachedSceneName,
            string label)
        {
            if (!scenePath.StartsWith("Assets/"))
            {
                report.AddError(
                    $"{label} path must be project-relative under Assets. Current path: '{scenePath}'.",
                    owner);
                return;
            }

            if (!scenePath.EndsWith(".unity"))
            {
                report.AddError(
                    $"{label} path must reference a Unity scene asset. Current path: '{scenePath}'.",
                    owner);
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                report.AddError(
                    $"{label} asset could not be found at '{scenePath}'. Reassign the scene in the Inspector.",
                    owner);
                return;
            }

            if (!string.Equals(sceneAsset.name, cachedSceneName, System.StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(cachedSceneName))
            {
                report.AddWarning(
                    $"{label} cached name '{cachedSceneName}' does not match scene asset name '{sceneAsset.name}'. Reassign the scene to refresh diagnostics.",
                    owner);
            }
        }

        private static void ValidateOpenSceneActivityLocalVisibilityAdapters(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            ActivityLocalVisibilityAdapter[] bindings = Object.FindObjectsByType<ActivityLocalVisibilityAdapter>(FindObjectsInactive.Include);
            if (bindings == null || bindings.Length == 0)
            {
                report.AddInfo("No Activity Local Visibility Adapter components were found in open scenes.", null);
                return;
            }

            int sceneBindingCount = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || !binding.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!binding.gameObject.scene.isLoaded)
                {
                    continue;
                }

                sceneBindingCount++;
                report.AddRange(ValidateActivityLocalVisibilityAdapter(binding, validationMode));
            }

            if (sceneBindingCount == 0)
            {
                report.AddInfo("No scene-authored Activity Local Visibility Adapter components were found in loaded scenes.", null);
            }
        }

        private static void ValidateOpenSceneRouteContentBindings(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            RouteContentBinding[] bindings = Object.FindObjectsByType<RouteContentBinding>(FindObjectsInactive.Include);
            if (bindings == null || bindings.Length == 0)
            {
                report.AddInfo("No Route Content Binding components were found in open scenes.", null);
                return;
            }

            int sceneBindingCount = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || !binding.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!binding.gameObject.scene.isLoaded)
                {
                    continue;
                }

                sceneBindingCount++;
                report.AddRange(ValidateRouteContentBinding(binding, validationMode));
            }

            if (sceneBindingCount == 0)
            {
                report.AddInfo("No scene-authored Route Content Binding components were found in loaded scenes.", null);
            }
        }

        private static void ValidateOpenSceneCycleResetTriggers(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            RouteCycleResetTrigger[] routeTriggers = Object.FindObjectsByType<RouteCycleResetTrigger>(FindObjectsInactive.Include);
            ActivityCycleResetTrigger[] activityTriggers = Object.FindObjectsByType<ActivityCycleResetTrigger>(FindObjectsInactive.Include);

            int routeTriggerCount = 0;
            if (routeTriggers != null)
            {
                for (int i = 0; i < routeTriggers.Length; i++)
                {
                    var trigger = routeTriggers[i];
                    if (trigger == null || trigger.gameObject == null || !trigger.gameObject.scene.IsValid() || !trigger.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    routeTriggerCount++;
                    report.AddRange(ValidateRouteCycleResetTrigger(trigger, validationMode));
                }
            }

            int activityTriggerCount = 0;
            if (activityTriggers != null)
            {
                for (int i = 0; i < activityTriggers.Length; i++)
                {
                    var trigger = activityTriggers[i];
                    if (trigger == null || trigger.gameObject == null || !trigger.gameObject.scene.IsValid() || !trigger.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    activityTriggerCount++;
                    report.AddRange(ValidateActivityCycleResetTrigger(trigger, validationMode));
                }
            }

            if (routeTriggerCount == 0 && activityTriggerCount == 0)
            {
                report.AddInfo("No scene-authored Cycle Reset Trigger components were found in loaded scenes.", null);
                return;
            }

            report.AddInfo(
                $"Cycle Reset Trigger validation scanned routeTriggers='{routeTriggerCount}' activityTriggers='{activityTriggerCount}'.",
                null);
        }

        private static FrameworkValidationMode ResolveValidationMode(ImmersiveFrameworkSettingsAsset settings)
        {
            return settings != null && settings.ActiveGameApplication != null
                ? settings.ActiveGameApplication.ValidationMode
                : FrameworkValidationMode.Strict;
        }

        private static FrameworkValidationMode ResolveValidationMode(GameApplicationAsset gameApplication)
        {
            return gameApplication != null
                ? gameApplication.ValidationMode
                : FrameworkValidationMode.Strict;
        }

        private static ActivityLocalVisibilityAdapter FindParentActivityLocalVisibilityAdapter(ActivityLocalVisibilityAdapter binding)
        {
            var parent = binding.transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<ActivityLocalVisibilityAdapter>(out var parentBinding))
                {
                    return parentBinding;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static int CountChildActivityLocalVisibilityAdapters(ActivityLocalVisibilityAdapter binding)
        {
            ActivityLocalVisibilityAdapter[] all = binding.GetComponentsInChildren<ActivityLocalVisibilityAdapter>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != binding)
                {
                    count++;
                }
            }

            return count;
        }

        private static RouteContentBinding FindParentRouteContentBinding(RouteContentBinding binding)
        {
            var parent = binding.transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<RouteContentBinding>(out var parentBinding))
                {
                    return parentBinding;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static int CountChildRouteContentBindings(RouteContentBinding binding)
        {
            RouteContentBinding[] all = binding.GetComponentsInChildren<RouteContentBinding>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != binding)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountRouteContentLifecycleReceivers(RouteContentBinding binding)
        {
            MonoBehaviour[] behaviours = binding.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IRouteContentLifecycleReceiver)
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetRouteLabel(RouteAsset route)
        {
            if (route == null)
            {
                return "<none>";
            }

            return string.IsNullOrWhiteSpace(route.RouteName)
                ? route.name
                : route.RouteName;
        }

    }
}
