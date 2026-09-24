using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraOutputDefinition))]
    internal sealed class CameraOutputDefinitionEditor : CameraDefinitionEditor
    {
        protected override void DrawDefinitionFields()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        }
    }

    [CustomEditor(typeof(CameraPresentationDefinition))]
    internal sealed class CameraPresentationDefinitionEditor : CameraDefinitionEditor
    {
        protected override void DrawDefinitionFields()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outputDefinition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rigPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("subjectPolicy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requestPrecedence"));
        }

        protected override string ValidateDefinitionConfiguration()
        {
            return ((CameraPresentationDefinition)target).TryValidate(
                out string issue)
                ? null
                : issue;
        }
    }

    internal abstract class CameraDefinitionEditor : UnityEditor.Editor
    {
        private bool _advanced;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                target is CameraPresentationDefinition
                    ? "Camera Presentation is reusable authoring intent. Its Rig Prefab must already be Apply/Rebuild materialized. Runtime occurrence state never lives in this asset."
                    : "Share this exact definition asset through typed references. Its description is intent; its stable ID is technical evidence.",
                MessageType.Info);

            DrawDefinitionFields();
            serializedObject.ApplyModifiedProperties();

            var definition = (ScriptableObject)target;
            string identityIssue =
                CameraDefinitionIdentityEditorUtility.Validate(definition);
            if (identityIssue != null)
            {
                EditorGUILayout.HelpBox(identityIssue, MessageType.Error);
            }
            else
            {
                string configurationIssue = ValidateDefinitionConfiguration();
                if (configurationIssue != null)
                {
                    EditorGUILayout.HelpBox(
                        configurationIssue,
                        MessageType.Error);
                }
            }

            var id = serializedObject.FindProperty("stableId");
            if (string.IsNullOrEmpty(id.stringValue))
            {
                if (GUILayout.Button("Generate Stable Identity"))
                {
                    CameraDefinitionIdentityEditorUtility
                        .GenerateMissingId(definition);
                }
            }
            else if (CameraDefinitionIdentityEditorUtility
                         .HasCollision(definition))
            {
                if (GUILayout.Button(
                        "Repair Collision — New Identity for This Definition"))
                {
                    CameraDefinitionIdentityEditorUtility
                        .RepairCollision(definition);
                }
            }

            _advanced =
                EditorGUILayout.Foldout(
                    _advanced,
                    "Advanced / Debug",
                    true);
            if (_advanced)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        "Stable ID",
                        id.stringValue);
                }

                if (GUILayout.Button("Copy Stable ID"))
                {
                    EditorGUIUtility.systemCopyBuffer = id.stringValue;
                }
            }
        }

        protected abstract void DrawDefinitionFields();

        protected virtual string ValidateDefinitionConfiguration() => null;
    }
}
