using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.Editor.Settings;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal sealed class CameraOutputAuthoringTopology
    {
        internal readonly List<CameraOutputId> Outputs = new List<CameraOutputId>();
        internal readonly List<string> Labels = new List<string>();
        internal readonly List<string> Issues = new List<string>();
        internal bool IsResolved;
        internal string ScenePath;
        internal string Diagnostic;

        internal int Count(CameraOutputId id)
        {
            int count = 0;
            foreach (var output in Outputs)
                if (output == id) count++;
            return count;
        }
    }

    internal static class CameraOutputAuthoringResolver
    {
        internal static CameraOutputAuthoringTopology Resolve()
        {
            var result = new CameraOutputAuthoringTopology();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                result.Diagnostic = "Output topology inspection is available in Edit Mode. Manual entry remains available.";
                return result;
            }
            if (!ImmersiveFrameworkEditorSettingsUtility.TryLoadExistingSettingsAsset(
                    out var settings, out string issue))
            {
                result.Diagnostic = issue;
                return result;
            }
            var application = settings.ActiveGameApplication;
            if (application == null)
            {
                result.Diagnostic = "Framework Settings has no Active GameApplication.";
                return result;
            }
            var sceneAsset = application.PersistentContent?.ContainerScene as SceneAsset;
            if (sceneAsset == null)
            {
                result.Diagnostic = "Active GameApplication requires Persistent Content with a Container Scene asset.";
                return result;
            }
            result.ScenePath = AssetDatabase.GetAssetPath(sceneAsset);
            SceneValidationScope scope = default;
            try
            {
                scope = FrameworkEditorSceneValidationUtility.OpenSceneForValidation(result.ScenePath);
                if (!scope.Scene.IsValid() || !scope.Scene.isLoaded)
                    throw new InvalidOperationException("Container Scene could not be loaded.");
                foreach (GameObject root in scope.Scene.GetRootGameObjects())
                foreach (var output in root.GetComponentsInChildren<CameraOutputAuthoring>(true))
                {
                    var id = new CameraOutputId(output.OutputIdText);
                    string label = $"{output.name} ({id})";
                    string definitionIssue = CameraIdentityAuthoringValidation.OutputIdIssue(output.OutputIdText);
                    if (definitionIssue != null)
                        result.Issues.Add($"{output.name}: {definitionIssue}");
                    else if (result.Count(id) > 0)
                        result.Issues.Add($"Duplicate Output Id '{id}' in active Session Output topology.");
                    result.Outputs.Add(id);
                    result.Labels.Add(label);
                }
                if (result.Outputs.Count == 0)
                    result.Issues.Add("Active Session Output topology contains no Camera Output definitions.");
                result.IsResolved = true;
                result.Diagnostic = $"Active Persistent Content: {result.ScenePath}";
            }
            catch (Exception exception)
            {
                result.Outputs.Clear();
                result.Labels.Clear();
                result.Issues.Clear();
                result.Diagnostic = $"Could not inspect active Output topology: {exception.Message}";
            }
            finally
            {
                scope.CloseIfOwned();
            }
            return result;
        }
    }
}
