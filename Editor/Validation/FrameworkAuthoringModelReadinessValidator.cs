using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Loading;
using Immersive.Framework.Pause;
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
            var validationMode = ResolveValidationMode(settings);
            var report = new FrameworkAuthoringValidationReport(validationMode);

            report.AddRange(FrameworkAuthoringValidator.ValidateProjectSettings(settings, includeOpenSceneBindings));

            if (settings == null)
            {
                AddReadinessSummary(report);
                return report;
            }

            var gameApplication = settings.ActiveGameApplication;
            if (gameApplication == null)
            {
                AddReadinessSummary(report);
                return report;
            }

            ValidateGameApplicationModel(report, gameApplication);
            ValidateRouteModel(report, gameApplication.StartupRoute, "Startup Route");
            ValidateGlobalUiSurfaceModel(report, gameApplication);
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

            ValidateEnumField<GlobalUiScenePolicy>(
                report,
                gameApplication,
                "globalUiScenePolicy",
                "Game Application UIGlobal Scene Policy");

            if (gameApplication.GlobalUiScenePolicyValue == GlobalUiScenePolicy.NoneConfigured && gameApplication.HasGlobalUiScene)
            {
                report.AddWarning(
                    "Model Readiness: UIGlobal Scene is assigned while policy is NoneConfigured. This is explicit no-op behavior; switch policy to Required when shared surfaces are expected.",
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

            ValidateSceneBuildSettings(report, route, route.PrimaryScenePath, $"{label} Primary Scene", true);

            if (route.StartupActivity == null)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no Startup Activity. This is valid for menu/no-activity routes because no route-level 'requires startup activity' policy exists yet.",
                    route);
            }
            else
            {
                ValidateActivityModel(report, route.StartupActivity, $"{label} Startup Activity");
            }

            if (route.RouteContentProfile == null)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no Route Content Profile. Route-owned additive content scene validation is skipped.",
                    route);
                return;
            }

            ValidateRouteContentProfile(report, route.RouteContentProfile, $"{label} Route Content Profile");
        }

        private static void ValidateActivityModel(
            FrameworkAuthoringValidationReport report,
            ActivityAsset activity,
            string label)
        {
            if (activity == null)
            {
                report.AddError($"Model Readiness: {label} is missing.", null);
                return;
            }

            ValidateEnumField<ActivityVisualTransitionMode>(
                report,
                activity,
                "visualTransitionMode",
                $"{label} Visual Transition Mode");

            if (activity.ActivityContentProfile == null)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no Activity Content Profile. Activity-owned scene/content validation is skipped.",
                    activity);
                return;
            }

            ValidateActivityContentProfileReadiness(report, activity.ActivityContentProfile, $"{label} Activity Content Profile");
        }

        private static void ValidateRouteContentProfile(
            FrameworkAuthoringValidationReport report,
            RouteContentProfileAsset profile,
            string label)
        {
            if (profile == null)
            {
                report.AddOptionalSkip($"Model Readiness: {label} is absent. Route content scene validation is skipped.", null);
                return;
            }

            if (!profile.HasAdditionalScenes)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no additional scenes. Route content scene validation is skipped.",
                    profile);
                return;
            }

            for (int i = 0; i < profile.AdditionalScenes.Count; i++)
            {
                var entry = profile.AdditionalScenes[i];
                string entryLabel = $"{label} scene index {i}";
                if (entry == null)
                {
                    report.AddError($"Model Readiness: {entryLabel} is null.", profile);
                    continue;
                }

                ValidateSceneEntry(report, profile, entry.ScenePath, entry.HasScene, entry.Requiredness, entryLabel);
            }
        }

        private static void ValidateActivityContentProfileReadiness(
            FrameworkAuthoringValidationReport report,
            ActivityContentProfileAsset profile,
            string label)
        {
            if (profile == null)
            {
                report.AddOptionalSkip($"Model Readiness: {label} is absent. Activity content scene validation is skipped.", null);
                return;
            }

            if (!profile.HasScenes)
            {
                report.AddOptionalSkip(
                    $"Model Readiness: {label} has no scenes. Activity content scene validation is skipped.",
                    profile);
                return;
            }

            for (int i = 0; i < profile.Scenes.Count; i++)
            {
                var entry = profile.Scenes[i];
                string entryLabel = $"{label} scene index {i}";
                if (entry == null)
                {
                    report.AddError($"Model Readiness: {entryLabel} is null.", profile);
                    continue;
                }

                ValidateSceneEntry(report, profile, entry.ScenePath, entry.HasScene, entry.Requiredness, entryLabel);
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
            bool required = requiredness == FrameworkContentRequiredness.Required;
            if (!hasScene)
            {
                if (required)
                {
                    report.AddError($"Model Readiness: {label} is Required but has no scene assigned.", context);
                }
                else
                {
                    report.AddOptionalSkip($"Model Readiness: {label} has no scene assigned and is Optional.", context);
                }

                return;
            }

            ValidateSceneAsset(report, context, scenePath, label, required);
            ValidateSceneBuildSettings(report, context, scenePath, label, required);
        }

        private static void ValidateGlobalUiSurfaceModel(
            FrameworkAuthoringValidationReport report,
            GameApplicationAsset gameApplication)
        {
            if (gameApplication.GlobalUiScenePolicyValue == GlobalUiScenePolicy.NoneConfigured)
            {
                report.AddOptionalSkip(
                    "Model Readiness: UIGlobal policy is NoneConfigured. Shared Transition, Loading and resident Pause surface validation is skipped by explicit no-op policy.",
                    gameApplication);
                return;
            }

            if (!gameApplication.HasGlobalUiScene)
            {
                return;
            }

            ValidateSceneBuildSettings(report, gameApplication, gameApplication.GlobalUiScenePath, "UIGlobal Scene", true);

            var sceneScope = default(SceneValidationScope);
            try
            {
                sceneScope = FrameworkEditorSceneValidationUtility.OpenSceneForValidation(gameApplication.GlobalUiScenePath);
                var scene = sceneScope.Scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    report.AddError(
                        $"Model Readiness: UIGlobal Scene '{gameApplication.GlobalUiScenePath}' could not be loaded for surface readiness validation.",
                        gameApplication);
                    return;
                }

                int transitionAdapterCount = CountSceneAdapters<ITransitionEffectAdapter>(scene);
                int loadingAdapterCount = CountSceneAdapters<ILoadingSurfaceAdapter>(scene);
                int pauseAdapterCount = CountSceneAdapters<IPauseSurfaceAdapter>(scene);

                if (transitionAdapterCount > 0)
                {
                    report.AddInfo(
                        $"Model Readiness: UIGlobal Scene '{gameApplication.GlobalUiScenePath}' contains {transitionAdapterCount} Transition adapter(s).",
                        gameApplication);
                }

                if (loadingAdapterCount > 0)
                {
                    report.AddInfo(
                        $"Model Readiness: UIGlobal Scene '{gameApplication.GlobalUiScenePath}' contains {loadingAdapterCount} Loading adapter(s).",
                        gameApplication);
                }

                if (pauseAdapterCount == 0)
                {
                    report.AddOptionalSkip(
                        $"Model Readiness: UIGlobal Scene '{gameApplication.GlobalUiScenePath}' has no resident Pause adapter. This is skipped because the current Model has no serialized 'Pause expected' policy.",
                        gameApplication);
                }
                else
                {
                    report.AddInfo(
                        $"Model Readiness: UIGlobal Scene '{gameApplication.GlobalUiScenePath}' contains {pauseAdapterCount} resident Pause adapter(s).",
                        gameApplication);
                }
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Model Readiness: UIGlobal Scene '{gameApplication.GlobalUiScenePath}' could not be validated. {exception.Message}",
                    gameApplication);
            }
            finally
            {
                sceneScope.CloseIfOwned();
            }
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

            var serializedObject = new SerializedObject(owner);
            var property = serializedObject.FindProperty(serializedFieldName);
            if (property == null)
            {
                report.AddError($"Model Readiness: {label} field '{serializedFieldName}' could not be found.", owner);
                return;
            }

            var value = (TEnum)Enum.ToObject(typeof(TEnum), property.intValue);
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                report.AddError($"Model Readiness: {label} has invalid value '{property.intValue}'.", owner);
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
                    report.AddError($"Model Readiness: {label} path is empty.", context);
                }
                else
                {
                    report.AddOptionalSkip($"Model Readiness: {label} path is empty and optional.", context);
                }

                return;
            }

            if (!scenePath.StartsWith("Assets/", StringComparison.Ordinal) || !scenePath.EndsWith(".unity", StringComparison.Ordinal))
            {
                report.AddError($"Model Readiness: {label} path must be a project-relative Unity scene under Assets. Current path: '{scenePath}'.", context);
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                report.AddError($"Model Readiness: {label} scene asset could not be found at '{scenePath}'.", context);
            }
        }

        private static void ValidateSceneBuildSettings(
            FrameworkAuthoringValidationReport report,
            UnityEngine.Object context,
            string scenePath,
            string label,
            bool required)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return;
            }

            if (IsSceneInBuildSettings(scenePath))
            {
                return;
            }

            if (required)
            {
                report.AddError($"Model Readiness: {label} scene '{scenePath}' is not included in Build Settings.", context);
            }
            else
            {
                report.AddOptionalSkip($"Model Readiness: {label} scene '{scenePath}' is optional and not included in Build Settings.", context);
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

            for (int i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                if (scene != null && string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountSceneAdapters<TAdapter>(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            var roots = scene.GetRootGameObjects();
            if (roots == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < roots.Length; i++)
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

                for (int j = 0; j < behaviours.Length; j++)
                {
                    if (behaviours[j] is TAdapter)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void AddReadinessSummary(FrameworkAuthoringValidationReport report)
        {
            report.AddInfo(
                $"Model Readiness completed. totalIssues='{report.TotalIssueCount}' blockingIssues='{report.ErrorCount}' warnings='{report.WarningCount}' optionalSkips='{report.OptionalSkipCount}'.",
                null);
        }

        private static FrameworkValidationMode ResolveValidationMode(ImmersiveFrameworkSettingsAsset settings)
        {
            return settings != null && settings.ActiveGameApplication != null
                ? settings.ActiveGameApplication.ValidationMode
                : FrameworkValidationMode.Strict;
        }
    }
}
