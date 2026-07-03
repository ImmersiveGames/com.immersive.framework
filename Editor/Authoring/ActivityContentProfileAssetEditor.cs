using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityContentProfileAsset))]
    internal sealed class ActivityContentProfileAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _profileId;
        private SerializedProperty _scenes;
        private SerializedProperty _description;

        private void OnEnable()
        {
            _profileId = serializedObject.FindProperty("profileId");
            _scenes = serializedObject.FindProperty("scenes");
            _description = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Activity Content Profile", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Status: Experimental. This asset declares Activity-owned scenes used by Activity operation planning, additive composition and release.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_profileId, new GUIContent("Profile Id"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            DrawScenes();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }

        private void DrawAuthoringValidation()
        {
            var report = FrameworkAuthoringValidator.ValidateActivityContentProfile((ActivityContentProfileAsset)target);

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private void DrawScenes()
        {
            EditorGUILayout.LabelField("Activity Scenes", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These scenes are Activity-owned content declarations. Execution-ready entries are planned, loaded additively and released by Activity scene composition according to Activity operation policy.",
                MessageType.Info);

            for (var i = 0; i < _scenes.arraySize; i++)
            {
                var entry = _scenes.GetArrayElementAtIndex(i);
                DrawSceneEntry(i, entry);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Scene"))
                {
                    _scenes.InsertArrayElementAtIndex(_scenes.arraySize);
                    var entry = _scenes.GetArrayElementAtIndex(_scenes.arraySize - 1);
                    ResetSceneEntry(entry);
                }

                using (new EditorGUI.DisabledScope(_scenes.arraySize == 0))
                {
                    if (GUILayout.Button("Remove Last"))
                    {
                        _scenes.DeleteArrayElementAtIndex(_scenes.arraySize - 1);
                    }
                }
            }
        }

        private void DrawSceneEntry(int index, SerializedProperty entry)
        {
            var contentId = entry.FindPropertyRelative("contentId");
            var scenePath = entry.FindPropertyRelative("scenePath");
            var sceneName = entry.FindPropertyRelative("sceneName");
            var requiredness = entry.FindPropertyRelative("requiredness");
            var loadMode = entry.FindPropertyRelative("loadMode");
            var releasePolicy = entry.FindPropertyRelative("releasePolicy");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Scene {index + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    _scenes.DeleteArrayElementAtIndex(index);
                    EditorGUILayout.EndVertical();
                    return;
                }
            }

            EditorGUILayout.PropertyField(contentId, new GUIContent("Content Id"));

            var currentScene = LoadSceneAsset(scenePath.stringValue);
            var selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent("Scene"),
                currentScene,
                typeof(SceneAsset),
                false);

            if (selectedScene != currentScene)
            {
                SetScene(scenePath, sceneName, selectedScene);
            }

            EditorGUILayout.PropertyField(requiredness, new GUIContent("Requiredness"));
            EditorGUILayout.PropertyField(loadMode, new GUIContent("Load Mode"));
            EditorGUILayout.PropertyField(releasePolicy, new GUIContent("Release Policy"));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Scene Path", scenePath.stringValue ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(scenePath.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Scene is missing. Execution-ready Activity content requires valid scene data.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private static SceneAsset LoadSceneAsset(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        private static void SetScene(
            SerializedProperty scenePath,
            SerializedProperty sceneName,
            SceneAsset sceneAsset)
        {
            if (sceneAsset == null)
            {
                scenePath.stringValue = string.Empty;
                sceneName.stringValue = string.Empty;
                return;
            }

            scenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
            sceneName.stringValue = sceneAsset.name;
        }

        private static void ResetSceneEntry(SerializedProperty entry)
        {
            entry.FindPropertyRelative("contentId").stringValue = string.Empty;
            entry.FindPropertyRelative("scenePath").stringValue = string.Empty;
            entry.FindPropertyRelative("sceneName").stringValue = string.Empty;
            entry.FindPropertyRelative("requiredness").enumValueIndex = 0;
            entry.FindPropertyRelative("loadMode").enumValueIndex = 0;
            entry.FindPropertyRelative("releasePolicy").enumValueIndex = 0;
        }
    }
}
