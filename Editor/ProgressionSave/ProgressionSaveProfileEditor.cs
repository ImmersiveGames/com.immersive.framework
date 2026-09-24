using Immersive.Framework.ProgressionSave;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.ProgressionSave
{
    [CustomEditor(typeof(ProgressionSaveProfile))]
    internal sealed class ProgressionSaveProfileEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _backend;
        private SerializedProperty _customProvider;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _backend =
                serializedObject.FindProperty("backend");

            _customProvider =
                serializedObject.FindProperty("customProvider");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawIntent();
            DrawConfigurationStatus();
            DrawAdvancedDebug();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIntent()
        {
            EditorGUILayout.LabelField(
                "Backend",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _backend,
                new GUIContent(
                    "Selection",
                    "Explicit Progression Save backend. A selected Custom Provider never falls back to Built-in JSON."));

            ProgressionSaveBackendSelection selection =
                (ProgressionSaveBackendSelection)
                _backend.intValue;

            if (selection ==
                ProgressionSaveBackendSelection.CustomProvider)
            {
                EditorGUILayout.PropertyField(
                    _customProvider,
                    new GUIContent(
                        "Provider (Required)",
                        "Typed provider asset that materializes the selected custom/third-party IProgressionSaveStore."));

                var provider =
                    _customProvider.objectReferenceValue as
                        ProgressionSaveStoreProviderAsset;

                if (provider != null &&
                    GUILayout.Button("Open Provider"))
                {
                    Selection.activeObject = provider;
                    EditorGUIUtility.PingObject(provider);
                }
            }
        }

        private void DrawConfigurationStatus()
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField(
                "Configuration Status",
                EditorStyles.boldLabel);

            var profile =
                (ProgressionSaveProfile)target;

            if (profile.TryValidate(
                    out string issue))
            {
                string status =
                    profile.Backend ==
                        ProgressionSaveBackendSelection.BuiltInJson
                        ? "Ready — Built-in JSON"
                        : $"Ready — Custom Provider: {profile.CustomProvider.name}";

                DrawStatusRow(
                    "Status",
                    status);
                return;
            }

            EditorGUILayout.HelpBox(
                issue,
                MessageType.Error);
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(7f);

            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            var profile =
                (ProgressionSaveProfile)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Backend Selection",
                    profile.Backend.ToString());

                EditorGUILayout.TextField(
                    "Runtime Ownership",
                    "FrameworkRuntimeHost — Application Scope");

                EditorGUILayout.TextField(
                    "Fallback",
                    "None");

                if (profile.Backend ==
                    ProgressionSaveBackendSelection.BuiltInJson)
                {
                    EditorGUILayout.TextField(
                        "Backend Contract",
                        nameof(IProgressionSaveStore));

                    EditorGUILayout.TextField(
                        "Built-in Store",
                        nameof(JsonProgressionSaveStore));

                    EditorGUILayout.TextField(
                        "Storage Strategy",
                        "Application.persistentDataPath / ImmersiveFramework / ProgressionSave / <Application>");
                }
                else
                {
                    EditorGUILayout.ObjectField(
                        "Custom Provider",
                        profile.CustomProvider,
                        typeof(ProgressionSaveStoreProviderAsset),
                        false);

                    EditorGUILayout.TextField(
                        "Provider Type",
                        profile.CustomProvider != null
                            ? profile.CustomProvider.GetType().FullName
                            : "<missing>");
                }
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawStatusRow(
            string label,
            string status)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.LabelField(
                    status,
                    EditorStyles.miniBoldLabel);
            }
        }
    }
}
