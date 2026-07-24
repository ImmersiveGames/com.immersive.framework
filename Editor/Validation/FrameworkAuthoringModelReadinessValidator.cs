using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Loading;
using Immersive.Framework.Pause;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Validation
{
    internal static class FrameworkAuthoringModelReadinessValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateProjectReadiness(
            ImmersiveFrameworkSettingsAsset settings,
            bool includeOpenSceneBindings)
        {
            FrameworkValidationMode validationMode =
                ResolveValidationMode(settings);
            var report =
                new FrameworkAuthoringValidationReport(validationMode);

            report.AddRange(
                FrameworkAuthoringValidator.ValidateProjectSettings(
                    settings,
                    includeOpenSceneBindings));

            if (settings == null ||
                settings.ActiveGameApplication == null)
            {
                AddReadinessSummary(report);
                return report;
            }

            GameApplicationAsset gameApplication =
                settings.ActiveGameApplication;

            ValidateGameApplicationModel(
                report,
                gameApplication);
            ValidateRouteModel(
                report,
                gameApplication.StartupRoute,
                "Startup Route");
            ValidatePersistentContentSurfaceModel(
                report,
                gameApplication);
            AddReadinessSummary(report);

            return report;
        }

        private static void ValidateGameApplicationModel(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset gameApplication)
        {
            ValidateEnumField<FrameworkValidationMode>(
                report,
                gameApplication,
                "validationMode",
                "Game Application Validation Mode");

            SerializedObject serializedApplication =
                new SerializedObject(gameApplication);
            SerializedProperty persistentContent =
                serializedApplication.FindProperty("persistentContent");

            if (persistentContent == null)
            {
                report.AddError(
                    "Model Readiness: Game Application Persistent Content composition field could not be found.",
                    gameApplication);
                return;
            }

            if (gameApplication.PersistentContent == null)
            {
                report.AddError(
                    "Model Readiness: Game Application Persistent Content composition is missing.",
                    gameApplication);
            }
        }

        private static void ValidateRouteModel(
            FrameworkAuthoringValidationReport report,
            RouteAsset route,
            string label)
        {
            if (route == null)
            {
                return;
            }

            ValidateSceneBuildSettings(
                report,
                route,
                route.PrimaryScenePath,
                $"{label} Primary Scene",
                true);
            ValidateEnumField<TransitionGateMode>(
                report,
                route,
                "transitionGateMode",
                $"{label} Transition Gate");

            if (route.StartupActivity == null)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no Startup Activity. This is valid for menu/no-activity routes because no route-level 'requires startup activity' policy exists yet.",
                    route);
            }
            else
            {
                ValidateActivityModel(
                    report,
                    route.StartupActivity,
                    $"{label} Startup Activity");
            }

            if (route.RouteContentProfile == null)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no Route Content Profile. Route-owned additive content scene validation is skipped.",
                    route);
                return;
            }

            ValidateRouteContentProfile(
                report,
                route.RouteContentProfile,
                $"{label} Route Content Profile");
        }

        private static void ValidateActivityModel(
            FrameworkAuthoringValidationReport report,
            ActivityAsset activity,
            string label)
        {
            if (activity == null)
            {
                report.AddError(
                    $"Model Readiness: {label} is missing.",
                    null);
                return;
            }

            ValidateEnumField<ActivityVisualTransitionMode>(
                report,
                activity,
                "visualTransitionMode",
                $"{label} Visual Transition Mode");
            ValidateEnumField<TransitionGateMode>(
                report,
                activity,
                "transitionGateMode",
                $"{label} Transition Gate");

            if (activity.ActivityContentProfile == null)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no Activity Content Profile. Activity-owned scene/content validation is skipped.",
                    activity);
                return;
            }

            ValidateActivityContentProfileReadiness(
                report,
                activity.ActivityContentProfile,
                $"{label} Activity Content Profile");
        }

        private static void ValidateRouteContentProfile(
            FrameworkAuthoringValidationReport report,
            RouteContentProfileAsset profile,
            string label)
        {
            if (profile == null)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} is absent. Route content scene validation is skipped.",
                    null);
                return;
            }

            if (!profile.HasAdditionalScenes)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no additional scenes. Route content scene validation is skipped.",
                    profile);
                return;
            }

            for (int index = 0;
                 index < profile.AdditionalScenes.Count;
                 index++)
            {
                var entry =
                    profile.AdditionalScenes[index];
                string entryLabel =
                    $"{label} scene index {index}";

                if (entry == null)
                {
                    report.AddError(
                        $"Model Readiness: {entryLabel} is null.",
                        profile);
                    continue;
                }

                if (!entry.HasExplicitContentId)
                {
                    report.AddError(
                        $"Model Readiness: {entryLabel} has no explicit Content Id. Scene names/paths are diagnostics only and must not be used as content identity.",
                        profile);
                }

                ValidateSceneEntry(
                    report,
                    profile,
                    entry.ScenePath,
                    entry.HasScene,
                    entry.Requiredness,
                    entryLabel);
            }
        }

        private static void ValidateActivityContentProfileReadiness(
            FrameworkAuthoringValidationReport report,
            ActivityContentProfileAsset profile,
            string label)
        {
            if (profile == null)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} is absent. Activity content scene validation is skipped.",
                    null);
                return;
            }

            if (!profile.HasScenes)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no scenes. Activity content scene validation is skipped.",
                    profile);
                return;
            }

            for (int index = 0;
                 index < profile.Scenes.Count;
                 index++)
            {
                var entry =
                    profile.Scenes[index];
                string entryLabel =
                    $"{label} scene index {index}";

                if (entry == null)
                {
                    report.AddError(
                        $"Model Readiness: {entryLabel} is null.",
                        profile);
                    continue;
                }

                if (!entry.HasExplicitContentId)
                {
                    report.AddError(
                        $"Model Readiness: {entryLabel} has no explicit Content Id. Scene names/paths are diagnostics only and must not be used as content identity.",
                        profile);
                }

                ValidateSceneEntry(
                    report,
                    profile,
                    entry.ScenePath,
                    entry.HasScene,
                    entry.Requiredness,
                    entryLabel);
            }
        }

        private static void ValidateSceneEntry(
            FrameworkAuthoringValidationReport report,
            UnityEngine.Object context,
            string scenePath,
            bool hasScene,
            FrameworkContentRequiredness requiredness,
            string label)
        {
            bool required =
                requiredness == FrameworkContentRequiredness.Required;

            if (!hasScene)
            {
                if (required)
                {
                    report.AddError(
                        $"Model Readiness: {label} is Required but has no scene assigned.",
                        context);
                }
                else
                {
                    report.AddOptionalSkip(
                        $"Model Readiness: {label} has no scene assigned and is Optional.",
                        context);
                }

                return;
            }

            ValidateSceneAsset(
                report,
                context,
                scenePath,
                label,
                required);
            ValidateSceneBuildSettings(
                report,
                context,
                scenePath,
                label,
                required);
        }

        private static void ValidatePersistentContentSurfaceModel(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset gameApplication)
        {
            PersistentContentComposition composition =
                gameApplication.PersistentContent;

            if (composition == null ||
                composition.ContainerScene == null)
            {
                return;
            }

            SceneAsset sceneAsset =
                composition.ContainerScene as SceneAsset;
            if (sceneAsset == null)
            {
                return;
            }

            string scenePath =
                AssetDatabase.GetAssetPath(sceneAsset);
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return;
            }

            var sceneScope =
                default(SceneValidationScope);

            try
            {
                sceneScope =
                    FrameworkEditorSceneValidationUtility
                        .OpenSceneForValidation(scenePath);
                UnityEngine.SceneManagement.Scene scene =
                    sceneScope.Scene;

                if (!scene.IsValid() ||
                    !scene.isLoaded)
                {
                    report.AddError(
                        $"Model Readiness: Persistent Content Container Scene '{scenePath}' could not be loaded for readiness validation.",
                        gameApplication);
                    return;
                }

                int transitionAdapterCount =
                    CountSceneAdapters<ITransitionEffectAdapter>(scene);
                int loadingAdapterCount =
                    CountSceneAdapters<ILoadingSurfaceAdapter>(scene);
                int pauseAdapterCount =
                    CountSceneAdapters<IPauseSurfaceAdapter>(scene);

                report.AddInfo(
                    $"Model Readiness: Persistent Content Container Scene '{scenePath}' contains transitionAdapters='{transitionAdapterCount}' loadingAdapters='{loadingAdapterCount}'.",
                    gameApplication);

                if (pauseAdapterCount == 0)
                {
                    report.AddOptionalSkip(
                        $"Model Readiness: Persistent Content Container Scene '{scenePath}' has no resident Pause adapter. This is skipped because the current model has no serialized 'Pause expected' policy.",
                        gameApplication);
                }
                else
                {
                    report.AddInfo(
                        $"Model Readiness: Persistent Content Container Scene '{scenePath}' contains {pauseAdapterCount} resident Pause adapter(s).",
                        gameApplication);
                }

                if (transitionAdapterCount == 0 &&
                    RequiresTransitionInteractionBlocking(gameApplication))
                {
                    report.AddWarning(
                        $"Model Readiness: Persistent Content Container Scene '{scenePath}' has no Transition adapter, but Route/Activity Transition Gate policy expects interaction blocking during visual transitions.",
                        gameApplication);
                }
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Model Readiness: Persistent Content Container Scene '{scenePath}' could not be validated. {exception.Message}",
                    gameApplication);
            }
            finally
            {
                sceneScope.CloseIfOwned();
            }
        }

        private static bool RequiresTransitionInteractionBlocking(
            GameApplicationAsset gameApplication)
        {
            if (gameApplication == null ||
                gameApplication.StartupRoute == null)
            {
                return false;
            }

            RouteAsset route =
                gameApplication.StartupRoute;
            if (TransitionGateBlockerPolicy
                .BlocksInteractionAcceptance(
                    route.TransitionGateMode))
            {
                return true;
            }

            ActivityAsset activity =
                route.StartupActivity;
            return activity != null &&
                   activity.VisualTransitionMode !=
                       ActivityVisualTransitionMode.Seamless &&
                   TransitionGateBlockerPolicy
                       .BlocksInteractionAcceptance(
                           activity.TransitionGateMode);
        }

        private static void ValidateEnumField<TEnum>(
            FrameworkAuthoringValidationReport report,
            UnityEngine.Object owner,
            string serializedFieldName,
            string label)
            where TEnum : struct, Enum
        {
            if (owner == null)
            {
                return;
            }

            var serializedObject =
                new SerializedObject(owner);
            SerializedProperty property =
                serializedObject.FindProperty(serializedFieldName);

            if (property == null)
            {
                report.AddError(
                    $"Model Readiness: {label} field '{serializedFieldName}' could not be found.",
                    owner);
                return;
            }

            TEnum value =
                (TEnum)Enum.ToObject(
                    typeof(TEnum),
                    property.intValue);

            if (!Enum.IsDefined(
                    typeof(TEnum),
                    value))
            {
                report.AddError(
                    $"Model Readiness: {label} has invalid value '{property.intValue}'.",
                    owner);
            }
        }

        private static void ValidateSceneAsset(
            FrameworkAuthoringValidationReport report,
            UnityEngine.Object context,
            string scenePath,
            string label,
            bool required)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                if (required)
                {
                    report.AddError(
                        $"Model Readiness: {label} path is empty.",
                        context);
                }
                else
                {
                    report.AddOptionalSkip(
                        $"Model Readiness: {label} path is empty and optional.",
                        context);
                }

                return;
            }

            if (!scenePath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal) ||
                !scenePath.EndsWith(
                    ".unity",
                    StringComparison.Ordinal))
            {
                report.AddError(
                    $"Model Readiness: {label} path must be a project-relative Unity scene under Assets. Current path: '{scenePath}'.",
                    context);
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    scenePath) == null)
            {
                report.AddError(
                    $"Model Readiness: {label} scene asset could not be found at '{scenePath}'.",
                    context);
            }
        }

        private static void ValidateSceneBuildSettings(
            FrameworkAuthoringValidationReport report,
            UnityEngine.Object context,
            string scenePath,
            string label,
            bool required)
        {
            if (string.IsNullOrWhiteSpace(scenePath) ||
                IsSceneInBuildSettings(scenePath))
            {
                return;
            }

            if (required)
            {
                report.AddError(
                    $"Model Readiness: {label} scene '{scenePath}' is not included in Build Settings.",
                    context);
            }
            else
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} scene '{scenePath}' is optional and not included in Build Settings.",
                    context);
            }
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
            UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return 0;
            }

            GameObject[] roots =
                scene.GetRootGameObjects();
            if (roots == null)
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

        private static void AddReadinessSummary(
            FrameworkAuthoringValidationReport report)
        {
            report.AddInfo(
                $"Model Readiness completed. totalIssues='{report.TotalIssueCount}' blockingIssues='{report.ErrorCount}' warnings='{report.WarningCount}' optionalSkips='{report.OptionalSkipCount}'.",
                null);
        }

        private static FrameworkValidationMode ResolveValidationMode(
            ImmersiveFrameworkSettingsAsset settings)
        {
            return settings != null &&
                   settings.ActiveGameApplication != null
                ? settings.ActiveGameApplication.ValidationMode
                : FrameworkValidationMode.Strict;
        }
    }
}
