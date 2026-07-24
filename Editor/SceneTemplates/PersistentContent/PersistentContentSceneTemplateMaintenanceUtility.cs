using System;
using System.Collections.Generic;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Editor.Editor.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Logging.Records;
using UnityEditor;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Editor.Editor.SceneTemplates
{
    /// <summary>
    /// Explicit package-maintenance action for the official Persistent Content
    /// Scene Template.
    ///
    /// It does not create assets. It validates the existing source scene, binds
    /// the existing pipeline script and synchronizes the required referenced
    /// Input System dependencies after source-scene edits.
    /// </summary>
    internal static class PersistentContentSceneTemplateMaintenanceUtility
    {
        private const string MenuPath =
            "Tools/Immersive Framework/Package Maintenance/Refresh Persistent Content Template";

        private const string TemplatePath =
            "Packages/com.immersive.framework/Editor/SceneTemplates/PersistentContent/ImmersivePersistentContent.scenetemplate";

        private const string SourceScenePath =
            "Packages/com.immersive.framework/Editor/SceneTemplates/PersistentContent/PersistentContentTemplateSource.unity";

        private const string PipelineScriptPath =
            "Packages/com.immersive.framework/Editor/SceneTemplates/PersistentContent/PersistentContentSceneTemplatePipeline.cs";

        [MenuItem(MenuPath)]
        private static void RefreshTemplate()
        {
            var logger =
                FrameworkLogger.Create(
                    typeof(PersistentContentSceneTemplateMaintenanceUtility));

            SceneTemplateAsset template =
                AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(
                    TemplatePath);
            SceneAsset sourceScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    SourceScenePath);
            MonoScript pipelineScript =
                AssetDatabase.LoadAssetAtPath<MonoScript>(
                    PipelineScriptPath);

            if (template == null)
            {
                logger.Error(
                    $"Persistent Content Scene Template is missing. path='{TemplatePath}'.");
                return;
            }

            if (sourceScene == null)
            {
                logger.Error(
                    $"Persistent Content template source scene is missing. path='{SourceScenePath}'.");
                return;
            }

            if (pipelineScript == null)
            {
                logger.Error(
                    $"Persistent Content Scene Template pipeline script is missing. path='{PipelineScriptPath}'.");
                return;
            }

            Type pipelineType =
                pipelineScript.GetClass();
            if (pipelineType == null ||
                !typeof(ISceneTemplatePipeline)
                    .IsAssignableFrom(pipelineType))
            {
                logger.Error(
                    $"Persistent Content Scene Template pipeline script does not implement ISceneTemplatePipeline. path='{PipelineScriptPath}'.");
                return;
            }

            if (template.templateScene != sourceScene)
            {
                logger.Error(
                    $"Persistent Content Scene Template references an unexpected source scene. expected='{SourceScenePath}' actual='{AssetDatabase.GetAssetPath(template.templateScene)}'.");
                return;
            }

            if (!AssetDatabase.IsOpenForEdit(template))
            {
                logger.Error(
                    $"Persistent Content Scene Template is read-only. Refresh must run from the editable framework package repository. path='{TemplatePath}'.");
                return;
            }

            SceneValidationScope sceneScope = default;

            try
            {
                sceneScope =
                    FrameworkEditorSceneValidationUtility
                        .OpenSceneForValidation(
                            SourceScenePath);

                FrameworkAuthoringValidationReport report =
                    FrameworkAuthoringValidator
                        .ValidatePersistentContentTemplateScene(
                            sceneScope.Scene,
                            template);

                FrameworkAuthoringValidationGui.LogReport(
                    "Persistent Content Template Source",
                    report);

                if (!report.IsValid)
                {
                    logger.Error(
                        "Persistent Content Scene Template refresh was blocked because the source scene contract is invalid.");
                    return;
                }

                InputSystemUIInputModule inputModule =
                    FindSceneComponent<InputSystemUIInputModule>(
                        sceneScope.Scene);
                if (inputModule == null)
                {
                    logger.Error(
                        "Persistent Content Scene Template refresh requires exactly one InputSystemUIInputModule in the validated source scene.");
                    return;
                }

                MonoScript inputModuleScript =
                    MonoScript.FromMonoBehaviour(
                        inputModule);
                if (inputModuleScript == null)
                {
                    logger.Error(
                        "Persistent Content Scene Template refresh could not resolve the InputSystemUIInputModule script asset.");
                    return;
                }

                if (inputModule.actionsAsset == null)
                {
                    logger.Error(
                        "Persistent Content Scene Template refresh requires an explicit Input System UI Actions Asset.");
                    return;
                }

                Undo.RecordObject(
                    template,
                    "Refresh Persistent Content Scene Template");

                template.templatePipeline =
                    pipelineScript;

                SynchronizeRequiredReferenceDependencies(
                    template,
                    inputModuleScript,
                    inputModule.actionsAsset);

                EditorUtility.SetDirty(
                    template);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    TemplatePath,
                    ImportAssetOptions.ForceUpdate);

                Selection.activeObject =
                    template;
                EditorGUIUtility.PingObject(
                    template);

                int dependencyCount =
                    template.dependencies != null
                        ? template.dependencies.Length
                        : 0;

                logger.Info(
                    "Persistent Content Scene Template refreshed.",
                    LogFields.Of(
                        LogFields.Field("template", TemplatePath),
                        LogFields.Field("sourceScene", SourceScenePath),
                        LogFields.Field("pipeline", PipelineScriptPath),
                        LogFields.Field("dependencyCount", dependencyCount)));
            }
            catch (Exception exception)
            {
                logger.Error(
                    $"Persistent Content Scene Template refresh failed explicitly. exception='{exception.GetType().Name}' message='{exception.Message}'.");
            }
            finally
            {
                sceneScope.CloseIfOwned();
            }
        }

        private static void SynchronizeRequiredReferenceDependencies(
            SceneTemplateAsset template,
            params Object[] requiredDependencies)
        {
            var dependencies =
                new List<DependencyInfo>();

            DependencyInfo[] existingDependencies =
                template.dependencies ??
                Array.Empty<DependencyInfo>();

            for (int index = 0;
                 index < existingDependencies.Length;
                 index++)
            {
                DependencyInfo dependencyInfo =
                    existingDependencies[index];
                if (dependencyInfo == null ||
                    dependencyInfo.dependency == null ||
                    ContainsDependency(
                        dependencies,
                        dependencyInfo.dependency))
                {
                    continue;
                }

                dependencies.Add(
                    dependencyInfo);
            }

            for (int index = 0;
                 index < requiredDependencies.Length;
                 index++)
            {
                Object dependency =
                    requiredDependencies[index];
                if (dependency == null)
                {
                    continue;
                }

                DependencyInfo existing =
                    FindDependency(
                        dependencies,
                        dependency);

                if (existing != null)
                {
                    existing.instantiationMode =
                        TemplateInstantiationMode.Reference;
                    continue;
                }

                dependencies.Add(
                    new DependencyInfo
                    {
                        dependency = dependency,
                        instantiationMode =
                            TemplateInstantiationMode.Reference
                    });
            }

            template.dependencies =
                dependencies.ToArray();
        }

        private static DependencyInfo FindDependency(
            IReadOnlyList<DependencyInfo> dependencies,
            Object dependency)
        {
            for (int index = 0;
                 index < dependencies.Count;
                 index++)
            {
                DependencyInfo dependencyInfo =
                    dependencies[index];
                if (dependencyInfo != null &&
                    dependencyInfo.dependency == dependency)
                {
                    return dependencyInfo;
                }
            }

            return null;
        }

        private static bool ContainsDependency(
            IReadOnlyList<DependencyInfo> dependencies,
            Object dependency)
        {
            return FindDependency(
                       dependencies,
                       dependency) != null;
        }

        private static TComponent FindSceneComponent<TComponent>(
            Scene scene)
            where TComponent : Component
        {
            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                TComponent component =
                    roots[rootIndex]
                        .GetComponentInChildren<TComponent>(
                            true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        [MenuItem(MenuPath, true)]
        private static bool CanRefreshTemplate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
