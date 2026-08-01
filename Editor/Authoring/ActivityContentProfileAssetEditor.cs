using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityContentProfileAsset))]
    internal sealed class ActivityContentProfileAssetEditor : UnityEditor.Editor
    {
        private static readonly GUIContent DescriptionLabel =
            new GUIContent(
                "Description",
                "Optional note describing the Activity-owned scene composition.");

        private static readonly GUIContent SceneLabel =
            new GUIContent(
                "Scene",
                "Activity-owned Unity scene loaded by Activity scene composition.");

        private static readonly GUIContent SceneToAddLabel =
            new GUIContent(
                "Scene to Add",
                "Select one Scene, then add it explicitly. Duplicate Scene declarations are rejected.");

        private static readonly GUIContent ContentIdLabel =
            new GUIContent(
                "Content Id",
                "Stable identity for this scene within the Activity Content Profile. It does not change when the scene asset is renamed.");

        private static readonly GUIContent RequirednessLabel =
            new GUIContent(
                "Requiredness",
                "Defines whether failure to resolve this scene blocks Activity content composition.");

        private static readonly GUIContent LoadModeLabel =
            new GUIContent(
                "Load Mode",
                "How Activity scene composition loads this scene.");

        private static readonly GUIContent ReleasePolicyLabel =
            new GUIContent(
                "Release Policy",
                "Controls when this Activity-owned scene is released.");

        private static readonly GUIContent ValidateLabel =
            new GUIContent(
                "Validate",
                "Validates this profile without modifying it.");

        private SerializedProperty _profileId;
        private SerializedProperty _scenes;
        private SerializedProperty _description;

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _validationOutdated;
        private bool _showAdvancedDebug;
        private SceneAsset _sceneToAdd;
        private string _sceneAddMessage = string.Empty;

        private void OnEnable()
        {
            _profileId =
                serializedObject.FindProperty("profileId");
            _scenes =
                serializedObject.FindProperty("scenes");
            _description =
                serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField(
                "Activity Content Profile",
                EditorStyles.boldLabel);

            DrawProfile();
            DrawScenes();
            DrawValidation();
            DrawAdvancedDebug();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (modified &&
                _lastValidationReport != null)
            {
                _validationOutdated = true;
            }
        }

        private void DrawProfile()
        {
            DrawSection("Profile");

            EditorGUILayout.PropertyField(
                _description,
                DescriptionLabel);
        }

        private void DrawScenes()
        {
            DrawSection($"Activity Scenes ({_scenes.arraySize})");

            if (_scenes.arraySize == 0)
            {
                EditorGUILayout.LabelField(
                    "No Activity-owned scenes configured.",
                    EditorStyles.miniLabel);
            }

            for (int index = 0;
                 index < _scenes.arraySize;
                 index++)
            {
                SerializedProperty entry =
                    _scenes.GetArrayElementAtIndex(index);

                if (DrawSceneEntry(index, entry))
                {
                    return;
                }
            }

            EditorGUILayout.Space(3f);

            _sceneToAdd =
                (SceneAsset)EditorGUILayout.ObjectField(
                    SceneToAddLabel,
                    _sceneToAdd,
                    typeof(SceneAsset),
                    false);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           _sceneToAdd == null))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "Add Selected Scene",
                                "Adds the selected Scene once, with a suggested unique Content Id.")))
                    {
                        AddSelectedScene();
                    }
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Add Empty",
                            "Adds an empty entry for manual authoring.")))
                {
                    AddSceneEntry(null);
                    GUIUtility.ExitGUI();
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    _sceneAddMessage))
            {
                EditorGUILayout.LabelField(
                    _sceneAddMessage,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private bool DrawSceneEntry(
            int index,
            SerializedProperty entry)
        {
            SerializedProperty contentId =
                entry.FindPropertyRelative("contentId");
            SerializedProperty scenePath =
                entry.FindPropertyRelative("scenePath");
            SerializedProperty sceneName =
                entry.FindPropertyRelative("sceneName");
            SerializedProperty requiredness =
                entry.FindPropertyRelative("requiredness");
            SerializedProperty loadMode =
                entry.FindPropertyRelative("loadMode");
            SerializedProperty releasePolicy =
                entry.FindPropertyRelative("releasePolicy");

            SceneAsset currentScene =
                LoadSceneAsset(scenePath.stringValue);

            string title =
                currentScene != null
                    ? currentScene.name
                    : !string.IsNullOrWhiteSpace(
                        sceneName.stringValue)
                        ? sceneName.stringValue
                        : $"Scene {index + 1}";

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    title,
                    EditorStyles.boldLabel);

                if (GUILayout.Button(
                        "Remove",
                        GUILayout.Width(72f)))
                {
                    Undo.RecordObject(
                        target,
                        "Remove Activity Content Scene");

                    _scenes.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();

                    EditorUtility.SetDirty(target);
                    _lastValidationReport = null;

                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                    return true;
                }
            }

            SceneAsset selectedScene =
                (SceneAsset)EditorGUILayout.ObjectField(
                    SceneLabel,
                    currentScene,
                    typeof(SceneAsset),
                    false);

            if (selectedScene != currentScene)
            {
                Undo.RecordObject(
                    target,
                    "Assign Activity Content Scene");

                SetScene(
                    scenePath,
                    sceneName,
                    selectedScene);

                _lastValidationReport = null;
            }

            EditorGUILayout.PropertyField(
                contentId,
                ContentIdLabel);

            if (selectedScene != null &&
                string.IsNullOrWhiteSpace(
                    contentId.stringValue))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(
                            new GUIContent(
                                "Use Suggested Id",
                                "Creates a deterministic Content Id from the selected Scene name."),
                            GUILayout.Width(132f)))
                    {
                        ApplySuggestedContentId(
                            contentId,
                            selectedScene);
                    }
                }
            }

            EditorGUILayout.PropertyField(
                requiredness,
                RequirednessLabel);

            EditorGUILayout.PropertyField(
                loadMode,
                LoadModeLabel);

            EditorGUILayout.PropertyField(
                releasePolicy,
                ReleasePolicyLabel);

            if (selectedScene == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a Scene.",
                    MessageType.Error);
            }

            EditorGUILayout.EndVertical();
            return false;
        }

        private void DrawValidation()
        {
            DrawSection("Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        ValidateLabel,
                        GUILayout.Width(96f)))
                {
                    serializedObject.ApplyModifiedProperties();

                    _lastValidationReport =
                        FrameworkAuthoringValidator
                            .ValidateActivityContentProfile(
                                (ActivityContentProfileAsset)target);

                    _validationOutdated = false;
                }

                GUILayout.Space(8f);

                EditorGUILayout.LabelField(
                    GetValidationStatus(),
                    EditorStyles.miniBoldLabel);

                GUILayout.FlexibleSpace();
            }

            DrawFirstActionableIssue();
        }

        private void DrawFirstActionableIssue()
        {
            if (_lastValidationReport == null ||
                _validationOutdated ||
                (_lastValidationReport.ErrorCount == 0 &&
                 _lastValidationReport.WarningCount == 0))
            {
                return;
            }

            for (int index = 0;
                 index < _lastValidationReport.Issues.Count;
                 index++)
            {
                FrameworkAuthoringValidationIssue issue =
                    _lastValidationReport.Issues[index];

                if (issue.Severity !=
                        FrameworkAuthoringValidationSeverity.Error &&
                    issue.Severity !=
                        FrameworkAuthoringValidationSeverity.Warning)
                {
                    continue;
                }

                EditorGUILayout.HelpBox(
                    issue.Message,
                    issue.Severity ==
                        FrameworkAuthoringValidationSeverity.Error
                            ? MessageType.Error
                            : MessageType.Warning);

                return;
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(7f);

            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows stable profile identity, cached scene paths and the complete validation report."),
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawProfileId();

            using (new EditorGUI.DisabledScope(true))
            {
                for (int index = 0;
                     index < _scenes.arraySize;
                     index++)
                {
                    SerializedProperty entry =
                        _scenes.GetArrayElementAtIndex(index);

                    SerializedProperty scenePath =
                        entry.FindPropertyRelative("scenePath");

                    SerializedProperty sceneName =
                        entry.FindPropertyRelative("sceneName");

                    string label =
                        !string.IsNullOrWhiteSpace(
                            sceneName.stringValue)
                            ? sceneName.stringValue
                            : $"Scene {index + 1}";

                    EditorGUILayout.TextField(
                        $"{label} Path",
                        scenePath.stringValue ?? string.Empty);
                }

                EditorGUILayout.TextField(
                    "Validation Status",
                    GetValidationStatus());
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel);

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastValidationReport);

            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);

            EditorGUI.indentLevel--;
        }

        private void DrawProfileId()
        {
            string profileId =
                _profileId != null
                    ? _profileId.stringValue ?? string.Empty
                    : string.Empty;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(
                    _profileId,
                    new GUIContent(
                        "Profile Id",
                        "Stable profile identity used in diagnostics. Existing values are never regenerated automatically."));

                using (new EditorGUI.DisabledScope(
                           !string.IsNullOrWhiteSpace(
                               profileId)))
                {
                    if (GUILayout.Button(
                            "Suggest",
                            GUILayout.Width(70f)))
                    {
                        FrameworkAuthoringInspectorGui
                            .ApplySuggestion(
                                serializedObject,
                                _profileId,
                                FrameworkAuthoringSuggestionUtility
                                    .SuggestIdentity(
                                        target,
                                        "activity.content-profile"),
                                "Suggest Activity Content Profile Id");

                        _lastValidationReport = null;
                    }
                }
            }
        }

        private void AddSelectedScene()
        {
            if (_sceneToAdd == null)
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();

            bool added =
                ContentProfileSceneAuthoringUtility
                    .TryAddActivityScene(
                        (ActivityContentProfileAsset)target,
                        _sceneToAdd,
                        out _sceneAddMessage);

            if (!added)
            {
                EditorUtility.DisplayDialog(
                    "Activity Scene Not Added",
                    _sceneAddMessage,
                    "OK");

                return;
            }

            _sceneToAdd = null;
            _lastValidationReport = null;
            _validationOutdated = false;

            serializedObject.UpdateIfRequiredOrScript();
            _scenes =
                serializedObject.FindProperty(
                    "scenes");

            GUI.FocusControl(null);
            GUIUtility.ExitGUI();
        }

        private void AddSceneEntry(
            SceneAsset sceneAsset)
        {
            Undo.RecordObject(
                target,
                "Add Activity Content Scene");

            int index =
                _scenes.arraySize;

            _scenes.InsertArrayElementAtIndex(index);

            SerializedProperty entry =
                _scenes.GetArrayElementAtIndex(index);

            ResetSceneEntry(entry);

            if (sceneAsset != null)
            {
                SetScene(
                    entry.FindPropertyRelative("scenePath"),
                    entry.FindPropertyRelative("sceneName"),
                    sceneAsset);

                entry.FindPropertyRelative("contentId")
                    .stringValue =
                    FrameworkAuthoringSuggestionUtility
                        .SuggestIdentity(
                            sceneAsset,
                            "activity.content");
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);

            _lastValidationReport = null;
        }

        private void ApplySuggestedContentId(
            SerializedProperty contentId,
            SceneAsset sceneAsset)
        {
            FrameworkAuthoringInspectorGui.ApplySuggestion(
                serializedObject,
                contentId,
                FrameworkAuthoringSuggestionUtility
                    .SuggestIdentity(
                        sceneAsset,
                        "activity.content"),
                "Suggest Activity Content Id");

            _lastValidationReport = null;
        }

        private string GetValidationStatus()
        {
            if (_lastValidationReport == null)
            {
                return "Not Validated";
            }

            if (_validationOutdated)
            {
                return "Outdated";
            }

            if (_lastValidationReport.ErrorCount > 0)
            {
                return "Invalid";
            }

            if (_lastValidationReport.WarningCount > 0)
            {
                return "Warning";
            }

            return "Valid";
        }

        private static SceneAsset LoadSceneAsset(
            string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return null;
            }

            return AssetDatabase
                .LoadAssetAtPath<SceneAsset>(
                    scenePath);
        }

        private static void SetScene(
            SerializedProperty scenePath,
            SerializedProperty sceneName,
            SceneAsset sceneAsset)
        {
            if (sceneAsset == null)
            {
                scenePath.stringValue =
                    string.Empty;

                sceneName.stringValue =
                    string.Empty;

                return;
            }

            scenePath.stringValue =
                AssetDatabase.GetAssetPath(
                    sceneAsset);

            sceneName.stringValue =
                sceneAsset.name;
        }

        private static void ResetSceneEntry(
            SerializedProperty entry)
        {
            entry.FindPropertyRelative("contentId")
                .stringValue = string.Empty;

            entry.FindPropertyRelative("scenePath")
                .stringValue = string.Empty;

            entry.FindPropertyRelative("sceneName")
                .stringValue = string.Empty;

            entry.FindPropertyRelative("requiredness")
                .enumValueIndex = 0;

            entry.FindPropertyRelative("loadMode")
                .enumValueIndex = 0;

            entry.FindPropertyRelative("releasePolicy")
                .enumValueIndex = 0;
        }

        private static void DrawSection(
            string title)
        {
            EditorGUILayout.Space(7f);

            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }
    }
}
