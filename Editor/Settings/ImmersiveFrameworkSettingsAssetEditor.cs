using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Settings
{
    [CustomEditor(typeof(ImmersiveFrameworkSettingsAsset))]
    internal sealed class ImmersiveFrameworkSettingsAssetEditor : UnityEditor.Editor
    {
        private const string ProjectSettingsPath = "Project/Immersive Framework";

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(
                "Immersive Framework Settings",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "This asset is managed by Project Settings > Immersive Framework. "
                + "Edit its configuration through the Project Settings surface.",
                MessageType.Info);

            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawDefaultInspector();
            }

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Open Immersive Framework Settings"))
            {
                SettingsService.OpenProjectSettings(ProjectSettingsPath);
            }
        }
    }
}
