using Immersive.Framework.Editor.Settings;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.ObjectEntry;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ObjectEntryDeclaration))]
    [CanEditMultipleObjects]
    internal sealed class ObjectEntryDeclarationEditor : UnityEditor.Editor
    {
        private SerializedProperty _objectEntryId;
        private SerializedProperty _requiredness;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _objectEntryId = serializedObject.FindProperty("objectEntryId");
            _requiredness = serializedObject.FindProperty("requiredness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Object Entry Declaration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_requiredness, new GUIContent("Requiredness", "Whether this declaration is required or optional for Object Entry validation."));

            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced / Debug", true);
            if (_showAdvanced)
            {
                EditorGUI.indentLevel++;
                DrawStableId();
                DrawIdentityValidation();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStableId()
        {
            string id = _objectEntryId != null ? _objectEntryId.stringValue ?? string.Empty : string.Empty;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField(new GUIContent("Object Entry ID", "Stable identity independent of GameObject name, hierarchy and scene location."), id);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!string.IsNullOrWhiteSpace(id) || serializedObject.isEditingMultipleObjects))
                {
                    if (GUILayout.Button("Generate ID"))
                    {
                        _objectEntryId.stringValue = ImmersiveFrameworkEditorSettingsUtility.GenerateObjectEntryIdText();
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                    }
                }
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(id)))
                {
                    if (GUILayout.Button("Copy ID")) EditorGUIUtility.systemCopyBuffer = id;
                }
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(id) || serializedObject.isEditingMultipleObjects))
            {
                if (GUILayout.Button(new GUIContent("Regenerate Stable ID...", "Replaces this ID only after confirmation. Rename, move and import never regenerate it.")))
                    RegenerateStableId();
            }

            if (string.IsNullOrWhiteSpace(id))
                EditorGUILayout.HelpBox("Generate a valid Object Entry ID for this declaration.", MessageType.Error);
        }

        private void RegenerateStableId()
        {
            string current = _objectEntryId.stringValue ?? string.Empty;
            if (!EditorUtility.DisplayDialog("Regenerate Object Entry Stable ID",
                    "This replaces the stable ID for this declaration. Existing StableReference values must be updated. Rename and move do not change this ID.\n\n" +
                    $"Current ID:\n{current}\n\nContinue?", "Regenerate", "Cancel")) return;

            if (!FrameworkIdentityAuthoringValidator.TryRegenerateStableId((ObjectEntryDeclaration)target,
                    out _, out _, out string issue))
            {
                EditorUtility.DisplayDialog("Regenerate Object Entry Stable ID", issue, "OK");
                return;
            }
            serializedObject.Update();
        }

        private void DrawIdentityValidation()
        {
            EditorGUILayout.LabelField("Object Entry Identity Validation", EditorStyles.boldLabel);
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Select one declaration to validate its Object Entry ID.", MessageType.Info);
                return;
            }

            FrameworkAuthoringValidationReport report = FrameworkIdentityAuthoringValidator.ValidateObjectEntryDeclaration(
                (ObjectEntryDeclaration)target);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}
