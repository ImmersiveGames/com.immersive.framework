using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Loading;
using Immersive.Framework.Editor.Editor.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.TransitionEffects;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Immersive.Framework.Editor.Editor.Validation
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

            report.AddInfo($"Validation Mode policy: {FrameworkValidationModePolicy.GetSummary(validationMode)}", settings);

            if (settings.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.CurrentSceneOnly)
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
                report.AddRange(ValidateGameApplication(settings.ActiveGameApplication, true, validationMode));
            }

            if (includeOpenSceneBindings)
            {
                ValidateOpenSceneActivityLocalVisibilityAdapters(report, validationMode);
                ValidateOpenSceneRouteContentBindings(report, validationMode);
                ValidateOpenSceneRouteContentAnchors(report, validationMode);
                ValidateOpenSceneActivityContentAnchors(report, validationMode);
                ValidateOpenSceneUnityContentAnchorMaterializationBridges(report, validationMode);
                ValidateOpenSceneCycleResetTriggers(report, validationMode);
                FrameworkResetRestartAuthoringValidator.ValidateOpenScenes(report);
            }

            if (!report.HasIssues)
            {
                report.AddInfo("Authoring validation passed with no findings.", settings);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication,
            bool validateDependencies)
        {
            return ValidateGameApplication(gameApplication, validateDependencies, ResolveValidationMode(gameApplication));
        }

        internal static FrameworkAuthoringValidationReport ValidateRoute(RouteAsset route, bool validateDependencies)
        {
            return ValidateRoute(route, validateDependencies, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivity(ActivityAsset activity)
        {
            return ValidateActivity(activity, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivityContentProfile(ActivityContentProfileAsset profile)
        {
            return ValidateActivityContentProfile(profile, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivityLocalVisibilityAdapter(ActivityLocalVisibilityAdapter binding)
        {
            return ValidateActivityLocalVisibilityAdapter(binding, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateRouteContentBinding(RouteContentBinding binding)
        {
            return ValidateRouteContentBinding(binding, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateRouteContentAnchor(RouteContentAnchor anchor)
        {
            return ValidateRouteContentAnchor(anchor, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivityContentAnchor(ActivityContentAnchor anchor)
        {
            return ValidateActivityContentAnchor(anchor, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateUnityContentAnchorMaterializationBridge(UnityContentAnchorMaterializationBridge bridge)
        {
            return ValidateUnityContentAnchorMaterializationBridge(bridge, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateUnityContentAnchorMaterializationBridgeSet(UnityContentAnchorMaterializationBridgeSet bridgeSet)
        {
            return ValidateUnityContentAnchorMaterializationBridgeSet(bridgeSet, FrameworkValidationMode.Standard);
        }
        internal static FrameworkAuthoringValidationReport ValidateRouteCycleResetTrigger(RouteCycleResetTrigger trigger)
        {
            return ValidateRouteCycleResetTrigger(trigger, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivityCycleResetTrigger(ActivityCycleResetTrigger trigger)
        {
            return ValidateActivityCycleResetTrigger(trigger, FrameworkValidationMode.Standard);
        }


        private static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication,
            bool validateDependencies,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (gameApplication == null)
            {
                report.AddError("Game Application is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(gameApplication.ApplicationName))
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
                report.AddRange(ValidateRoute(gameApplication.StartupRoute, true, validationMode));
            }

            ValidateGlobalUiSceneConfiguration(report, gameApplication, validateDependencies);

            if (!report.HasIssues)
            {
                report.AddInfo("Game Application authoring is valid for the current framework scope.", gameApplication);
            }

            return report;
        }

        private static void ValidateGlobalUiSceneConfiguration(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset gameApplication,
            bool validateDependencies)
        {
            if (gameApplication.GlobalUiScenePolicyValue == GlobalUiScenePolicy.NoneConfigured)
            {
                if (gameApplication.HasGlobalUiScene)
                {
                    report.AddWarning(
                        "UIGlobal Scene is assigned, but Global UI Scene Policy is NoneConfigured. The scene will not be loaded and explicit NoOp behavior remains active.",
                        gameApplication);
                }
                else
                {
                    report.AddInfo(
                        "UIGlobal scene is configured as explicit NoOp.",
                        gameApplication);
                }

                return;
            }

            if (!gameApplication.HasGlobalUiScene)
            {
                report.AddError(
                    "Global UI Scene Policy is Required, but UIGlobal Scene is missing.",
                    gameApplication);
                return;
            }

            ValidateSceneAssetReference(
                report,
                gameApplication,
                gameApplication.GlobalUiScenePath,
                gameApplication.GlobalUiSceneName,
                "UIGlobal Scene");

            ValidateGlobalUiSceneBuildSettings(report, gameApplication);

            if (validateDependencies)
            {
                ValidateGlobalUiSceneAdapters(report, gameApplication);
            }
        }

        private static void ValidateGlobalUiSceneBuildSettings(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset gameApplication)
        {
            var scenePath = gameApplication.GlobalUiScenePath;
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                report.AddError(
                    "Global UI Scene Policy is Required, but UIGlobal Scene path is empty.",
                    gameApplication);
                return;
            }

            var scenes = EditorBuildSettings.scenes;
            var found = false;
            if (scenes != null)
            {
                for (var i = 0; i < scenes.Length; i++)
                {
                    var scene = scenes[i];
                    if (scene != null && string.Equals(scene.path, scenePath, System.StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                report.AddError(
                    $"UIGlobal Scene '{scenePath}' is not included in Build Settings.",
                    gameApplication);
            }
        }

        private static bool IsSceneInBuildSettings(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            var scenes = EditorBuildSettings.scenes;
            if (scenes == null)
            {
                return false;
            }

            for (var i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                if (scene != null && string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateGlobalUiSceneAdapters(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset gameApplication)
        {
            var scenePath = gameApplication.GlobalUiScenePath;
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return;
            }

            var sceneScope = default(SceneValidationScope);
            try
            {
                sceneScope = FrameworkEditorSceneValidationUtility.OpenSceneForValidation(scenePath);
                var scene = sceneScope.Scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    report.AddError(
                        $"UIGlobal Scene '{scenePath}' could not be loaded for adapter validation.",
                        gameApplication);
                    return;
                }

                var transitionAdapterCount = CountSceneAdapters<ITransitionEffectAdapter>(scene);
                var loadingAdapterCount = CountSceneAdapters<ILoadingSurfaceAdapter>(scene);

                if (transitionAdapterCount == 0)
                {
                    report.AddError(
                        $"UIGlobal Scene '{scenePath}' must contain at least one ITransitionEffectAdapter implementation.",
                        gameApplication);
                }
                else
                {
                    report.AddInfo(
                        $"UIGlobal Scene '{scenePath}' contains {transitionAdapterCount} Transition adapter(s).",
                        gameApplication);
                }

                if (loadingAdapterCount == 0)
                {
                    report.AddError(
                        $"UIGlobal Scene '{scenePath}' must contain at least one ILoadingSurfaceAdapter implementation.",
                        gameApplication);
                }
                else
                {
                    report.AddInfo(
                        $"UIGlobal Scene '{scenePath}' contains {loadingAdapterCount} Loading adapter(s).",
                        gameApplication);
                }
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"UIGlobal Scene '{scenePath}' could not be validated. {exception.Message}",
                    gameApplication);
            }
            finally
            {
                sceneScope.CloseIfOwned();
            }
        }

        private static int CountSceneAdapters<TAdapter>(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            var roots = scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours == null)
                {
                    continue;
                }

                for (var j = 0; j < behaviours.Length; j++)
                {
                    if (behaviours[j] is TAdapter)
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
            else if (validateDependencies)
            {
                report.AddRange(ValidateActivity(route.StartupActivity, validationMode));
            }

            if (!report.HasIssues)
            {
                report.AddInfo("Route authoring is valid for the current framework scope.", route);
            }

            return report;
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

            if (string.IsNullOrWhiteSpace(activity.ActivityName))
            {
                report.AddWarning(
                    "Activity has no display name. The asset name will be used in diagnostics.",
                    activity);
            }

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
            else if (activity.VisualTransitionMode != ActivityVisualTransitionMode.Seamless
                && activity.TransitionGateMode != TransitionGateMode.InputInteractionAndGameplay)
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

            if (binding.Activity == null)
            {
                report.AddError(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' has no Activity assigned.",
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

        private static FrameworkAuthoringValidationReport ValidateRouteContentAnchor(
            RouteContentAnchor anchor,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (anchor == null)
            {
                report.AddError("Route Content Anchor is missing.", null);
                return report;
            }

            string objectName = anchor.gameObject != null ? anchor.gameObject.name : "<missing>";

            if (anchor.Route == null)
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{objectName}' has no Route assigned.",
                    anchor);
            }
            else
            {
                ValidateRouteContentAnchorSceneRoute(report, anchor, objectName);
            }

            if (!anchor.HasExplicitAnchorId)
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{objectName}' has no Anchor Id. Content Anchor identity must be explicit; GameObject names and hierarchy paths are diagnostics only.",
                    anchor);
            }

            if (!anchor.HasExplicitKind)
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{objectName}' has Kind set to Unknown. Choose Root, Slot or Point.",
                    anchor);
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), anchor.Requiredness))
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{objectName}' has an invalid Requiredness value.",
                    anchor);
            }

            if (anchor.HasValidAuthoring)
            {
                try
                {
                    if (anchor.TryCreateDeclaration(out var declaration))
                    {
                        report.AddInfo(
                            $"Route Content Anchor on GameObject '{objectName}' declares '{declaration.AnchorId.StableText}' as {declaration.Kind}/{declaration.Requiredness}.",
                            anchor);
                    }
                }
                catch (Exception exception)
                {
                    report.AddError(
                        $"Route Content Anchor on GameObject '{objectName}' could not create a ContentAnchorDeclaration. {exception.Message}",
                        anchor);
                }
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Route Content Anchor on GameObject '{objectName}' is valid for the current F7 authoring-validation scope.",
                    anchor);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateActivityContentAnchor(
            ActivityContentAnchor anchor,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (anchor == null)
            {
                report.AddError("Activity Content Anchor is missing.", null);
                return report;
            }

            string objectName = anchor.gameObject != null ? anchor.gameObject.name : "<missing>";

            if (anchor.Activity == null)
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{objectName}' has no Activity assigned.",
                    anchor);
            }

            if (!anchor.HasExplicitAnchorId)
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{objectName}' has no Anchor Id. Content Anchor identity must be explicit; GameObject names and hierarchy paths are diagnostics only.",
                    anchor);
            }

            if (!anchor.HasExplicitKind)
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{objectName}' has Kind set to Unknown. Choose Root, Slot or Point.",
                    anchor);
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), anchor.Requiredness))
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{objectName}' has an invalid Requiredness value.",
                    anchor);
            }

            if (anchor.HasValidAuthoring)
            {
                try
                {
                    if (anchor.TryCreateDeclaration(out var declaration))
                    {
                        report.AddInfo(
                            $"Activity Content Anchor on GameObject '{objectName}' declares '{declaration.AnchorId.StableText}' as {declaration.Kind}/{declaration.Requiredness}.",
                            anchor);
                    }
                }
                catch (Exception exception)
                {
                    report.AddError(
                        $"Activity Content Anchor on GameObject '{objectName}' could not create a ContentAnchorDeclaration. {exception.Message}",
                        anchor);
                }
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Content Anchor on GameObject '{objectName}' is valid for the current F9G authoring-validation scope.",
                    anchor);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateUnityContentAnchorMaterializationBridge(
            UnityContentAnchorMaterializationBridge bridge,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            var validation = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridge(
                bridge,
                "Unity Content Anchor Materialization Bridge");
            AddContentAnchorMaterializationAuthoringValidation(report, validation, bridge);
            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateUnityContentAnchorMaterializationBridgeSet(
            UnityContentAnchorMaterializationBridgeSet bridgeSet,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            var validation = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridgeSet(
                bridgeSet,
                "Unity Content Anchor Materialization Bridge Set");
            AddContentAnchorMaterializationAuthoringValidation(report, validation, bridgeSet);
            return report;
        }

        private static void AddContentAnchorMaterializationAuthoringValidation(
            FrameworkAuthoringValidationReport report,
            UnityContentAnchorMaterializationAuthoringValidationResult validation,
            Object context)
        {
            if (validation == null)
            {
                report.AddError("Content Anchor materialization authoring validation produced no result.", context);
                return;
            }

            if (validation.Succeeded)
            {
                report.AddInfo($"Content Anchor materialization authoring validation passed. {validation.ToDiagnosticString()}", context);
                return;
            }

            if (validation.NullBridgeCount > 0)
            {
                report.AddError($"Content Anchor materialization authoring has missing bridge references. count='{validation.NullBridgeCount}'.", context);
            }

            if (validation.DuplicateBridgeCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge set references the same bridge more than once. count='{validation.DuplicateBridgeCount}'.", context);
            }

            if (validation.DuplicateMaterializationKeyCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge set has duplicate runtime materialization keys. count='{validation.DuplicateMaterializationKeyCount}'. RuntimeContent identity must be explicit and unique in the set.", context);
            }

            if (validation.MissingPrefabCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires an explicit prefab/template. missingPrefabs='{validation.MissingPrefabCount}'.", context);
            }

            if (validation.MissingAnchorTransformCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires an explicit anchor Transform. missingAnchorTransforms='{validation.MissingAnchorTransformCount}'.", context);
            }

            if (validation.InvalidRuntimeScopeCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit RuntimeContent scope. invalidRuntimeScopes='{validation.InvalidRuntimeScopeCount}'.", context);
            }

            if (validation.InvalidAnchorScopeCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit ContentAnchor scope. invalidAnchorScopes='{validation.InvalidAnchorScopeCount}'.", context);
            }

            if (validation.InvalidAnchorKindCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit ContentAnchor kind. invalidAnchorKinds='{validation.InvalidAnchorKindCount}'.", context);
            }

            if (validation.InvalidReleasePolicyCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit RuntimeContent release policy. invalidReleasePolicies='{validation.InvalidReleasePolicyCount}'.", context);
            }

            if (validation.MissingRuntimeOwnerIdCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit Runtime Owner Id. missingRuntimeOwnerIds='{validation.MissingRuntimeOwnerIdCount}'.", context);
            }

            if (validation.MissingAnchorOwnerIdCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit Anchor Owner Id. missingAnchorOwnerIds='{validation.MissingAnchorOwnerIdCount}'.", context);
            }

            if (validation.MissingAnchorIdCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit Anchor Id. missingAnchorIds='{validation.MissingAnchorIdCount}'.", context);
            }

            if (validation.MissingRuntimeContentIdCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit Runtime Content Id. missingRuntimeContentIds='{validation.MissingRuntimeContentIdCount}'.", context);
            }

            if (validation.MissingResourceKeyCount > 0)
            {
                report.AddError($"Content Anchor materialization bridge requires explicit Resource Key. missingResourceKeys='{validation.MissingResourceKeyCount}'.", context);
            }

            bool hasDetailedIssue = validation.NullBridgeCount > 0
                || validation.DuplicateBridgeCount > 0
                || validation.DuplicateMaterializationKeyCount > 0
                || validation.MissingPrefabCount > 0
                || validation.MissingAnchorTransformCount > 0
                || validation.InvalidRuntimeScopeCount > 0
                || validation.InvalidAnchorScopeCount > 0
                || validation.InvalidAnchorKindCount > 0
                || validation.InvalidReleasePolicyCount > 0
                || validation.MissingRuntimeOwnerIdCount > 0
                || validation.MissingAnchorOwnerIdCount > 0
                || validation.MissingAnchorIdCount > 0
                || validation.MissingRuntimeContentIdCount > 0
                || validation.MissingResourceKeyCount > 0;

            if (validation.BlockingIssueCount > 0 && !hasDetailedIssue)
            {
                report.AddError($"Content Anchor materialization authoring has blocking issues. {validation.ToDiagnosticString()}", context);
            }

            if (validation.BlockingIssueCount == 0)
            {
                report.AddWarning($"Content Anchor materialization authoring validation did not pass cleanly. {validation.ToDiagnosticString()}", context);
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

        private static void ValidateRouteContentAnchorSceneRoute(
            FrameworkAuthoringValidationReport report,
            RouteContentAnchor anchor,
            string objectName)
        {
            var route = anchor.Route;
            var scene = anchor.gameObject != null ? anchor.gameObject.scene : default;

            if (!scene.IsValid())
            {
                report.AddInfo(
                    $"Route Content Anchor on GameObject '{objectName}' is not in a valid scene. Scene-route validation is skipped for prefabs or disconnected objects.",
                    anchor);
                return;
            }

            if (!scene.isLoaded)
            {
                report.AddInfo(
                    $"Route Content Anchor on GameObject '{objectName}' is in scene '{scene.name}', but the scene is not loaded. Scene-route validation only checks loaded scenes.",
                    anchor);
                return;
            }

            string scenePath = scene.path;
            string sceneName = scene.name;
            if (string.IsNullOrWhiteSpace(scenePath) && string.IsNullOrWhiteSpace(sceneName))
            {
                report.AddWarning(
                    $"Route Content Anchor on GameObject '{objectName}' is in an unidentified scene. Save the scene so it can be compared against Route scene composition.",
                    anchor);
                return;
            }

            if (!DoesRouteDeclareScene(route, scenePath, sceneName))
            {
                report.AddWarning(
                    $"Route Content Anchor on GameObject '{objectName}' points to Route '{GetRouteLabel(route)}', but it is authored in scene '{GetSceneLabel(scenePath, sceneName)}'. That scene is not declared as the Route primary scene or an additional Route Content Profile scene.",
                    anchor);
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

        private static void ValidateOpenSceneRouteContentAnchors(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            RouteContentAnchor[] anchors = Object.FindObjectsByType<RouteContentAnchor>(FindObjectsInactive.Include);
            if (anchors == null || anchors.Length == 0)
            {
                report.AddInfo("No Route Content Anchor components were found in open scenes.", null);
                return;
            }

            int sceneAnchorCount = 0;
            var declarations = new List<ContentAnchorDeclaration>(anchors.Length);

            for (int i = 0; i < anchors.Length; i++)
            {
                var anchor = anchors[i];
                if (anchor == null || anchor.gameObject == null || !anchor.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!anchor.gameObject.scene.isLoaded)
                {
                    continue;
                }

                sceneAnchorCount++;
                report.AddRange(ValidateRouteContentAnchor(anchor, validationMode));
                TryCollectContentAnchorDeclaration(anchor, declarations, report);
            }

            if (sceneAnchorCount == 0)
            {
                report.AddInfo("No scene-authored Route Content Anchor components were found in loaded scenes.", null);
                return;
            }

            ValidateContentAnchorSetIssues(report, declarations);
        }

        private static void ValidateOpenSceneActivityContentAnchors(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            ActivityContentAnchor[] anchors = Object.FindObjectsByType<ActivityContentAnchor>(FindObjectsInactive.Include);
            if (anchors == null || anchors.Length == 0)
            {
                report.AddInfo("No Activity Content Anchor components were found in open scenes.", null);
                return;
            }

            int sceneAnchorCount = 0;
            var declarations = new List<ContentAnchorDeclaration>(anchors.Length);

            for (int i = 0; i < anchors.Length; i++)
            {
                var anchor = anchors[i];
                if (anchor == null || anchor.gameObject == null || !anchor.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!anchor.gameObject.scene.isLoaded)
                {
                    continue;
                }

                sceneAnchorCount++;
                report.AddRange(ValidateActivityContentAnchor(anchor, validationMode));
                TryCollectContentAnchorDeclaration(anchor, declarations, report);
            }

            if (sceneAnchorCount == 0)
            {
                report.AddInfo("No scene-authored Activity Content Anchor components were found in loaded scenes.", null);
                return;
            }

            ValidateContentAnchorSetIssues(report, declarations, "Activity Content Anchor");
        }

        private static void ValidateOpenSceneUnityContentAnchorMaterializationBridges(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            UnityContentAnchorMaterializationBridge[] bridges = Object.FindObjectsByType<UnityContentAnchorMaterializationBridge>(FindObjectsInactive.Include);
            UnityContentAnchorMaterializationBridgeSet[] bridgeSets = Object.FindObjectsByType<UnityContentAnchorMaterializationBridgeSet>(FindObjectsInactive.Include);

            int bridgeCount = 0;
            if (bridges != null)
            {
                for (int i = 0; i < bridges.Length; i++)
                {
                    var bridge = bridges[i];
                    if (bridge == null || bridge.gameObject == null || !bridge.gameObject.scene.IsValid() || !bridge.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    bridgeCount++;
                    report.AddRange(ValidateUnityContentAnchorMaterializationBridge(bridge, validationMode));
                }
            }

            int bridgeSetCount = 0;
            if (bridgeSets != null)
            {
                for (int i = 0; i < bridgeSets.Length; i++)
                {
                    var bridgeSet = bridgeSets[i];
                    if (bridgeSet == null || bridgeSet.gameObject == null || !bridgeSet.gameObject.scene.IsValid() || !bridgeSet.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    bridgeSetCount++;
                    report.AddRange(ValidateUnityContentAnchorMaterializationBridgeSet(bridgeSet, validationMode));
                }
            }

            if (bridgeCount == 0 && bridgeSetCount == 0)
            {
                report.AddInfo("No Unity Content Anchor Materialization Bridge or Bridge Set components were found in loaded scenes.", null);
                return;
            }

            report.AddInfo(
                $"Unity Content Anchor materialization authoring validation scanned bridges='{bridgeCount}' bridgeSets='{bridgeSetCount}'.",
                null);
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

        private static void TryCollectContentAnchorDeclaration(
            RouteContentAnchor anchor,
            List<ContentAnchorDeclaration> declarations,
            FrameworkAuthoringValidationReport report)
        {
            if (anchor == null || !anchor.HasValidAuthoring)
            {
                return;
            }

            try
            {
                if (anchor.TryCreateDeclaration(out var declaration))
                {
                    declarations.Add(declaration);
                }
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{anchor.ObjectName}' could not be collected for duplicate validation. {exception.Message}",
                    anchor);
            }
        }

        private static void TryCollectContentAnchorDeclaration(
            ActivityContentAnchor anchor,
            List<ContentAnchorDeclaration> declarations,
            FrameworkAuthoringValidationReport report)
        {
            if (anchor == null || !anchor.HasValidAuthoring)
            {
                return;
            }

            try
            {
                if (anchor.TryCreateDeclaration(out var declaration))
                {
                    declarations.Add(declaration);
                }
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{anchor.ObjectName}' could not be collected for duplicate validation. {exception.Message}",
                    anchor);
            }
        }

        private static void ValidateContentAnchorSetIssues(
            FrameworkAuthoringValidationReport report,
            IReadOnlyList<ContentAnchorDeclaration> declarations,
            string label = "Route Content Anchor")
        {
            if (declarations == null || declarations.Count == 0)
            {
                return;
            }

            var set = ContentAnchorSet.FromDeclarations(declarations);
            if (!set.HasIssues)
            {
                report.AddInfo(
                    $"{label} duplicate validation passed. anchors='{set.Count}' required='{set.RequiredCount}' optional='{set.OptionalCount}'.",
                    null);
                return;
            }

            var issues = set.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                switch (issue.Kind)
                {
                    case ContentAnchorSetIssueKind.DuplicateIdentity:
                    case ContentAnchorSetIssueKind.DuplicateAnchorId:
                    case ContentAnchorSetIssueKind.InvalidDeclaration:
                        report.AddError($"{label} duplicate validation issue: {issue.ToDiagnosticString()}.", null);
                        break;
                    default:
                        report.AddWarning($"{label} validation issue: {issue.ToDiagnosticString()}.", null);
                        break;
                }
            }
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

        private static bool DoesRouteDeclareScene(RouteAsset route, string scenePath, string sceneName)
        {
            if (route == null)
            {
                return false;
            }

            if (MatchesScene(route.PrimaryScenePath, route.PrimarySceneName, scenePath, sceneName))
            {
                return true;
            }

            var profile = route.RouteContentProfile;
            if (profile == null || profile.AdditionalScenes == null)
            {
                return false;
            }

            for (int i = 0; i < profile.AdditionalScenes.Count; i++)
            {
                var entry = profile.AdditionalScenes[i];
                if (entry != null && MatchesScene(entry.ScenePath, entry.SceneName, scenePath, sceneName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesScene(string declaredPath, string declaredName, string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(declaredPath)
                && !string.IsNullOrWhiteSpace(scenePath)
                && string.Equals(declaredPath.Trim(), scenePath.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(declaredName)
                && !string.IsNullOrWhiteSpace(sceneName)
                && string.Equals(declaredName.Trim(), sceneName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSceneLabel(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return scenePath.Trim();
            }

            return string.IsNullOrWhiteSpace(sceneName) ? "<none>" : sceneName.Trim();
        }
    }
}
