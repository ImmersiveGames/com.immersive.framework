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

    [CustomEditor(typeof(CameraDefinition))]
    internal sealed class SessionCameraDefinitionEditor : CameraDefinitionEditor
    {
        protected override void DrawDefinitionFields()
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("rigPrefab"),
                new GUIContent(
                    "Fixed Rig Prefab",
                    "Reusable, already materialized Fixed Camera Rig. Session Assignments create independent runtime instances."));
        }

        protected override string ValidateDefinitionConfiguration() =>
            ((CameraDefinition)target).TryValidateSessionCamera(out string issue)
                ? null
                : issue;
    }

    internal abstract class CameraDefinitionEditor : UnityEditor.Editor
    {
        private bool _advanced;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Share this exact definition asset through typed references. Its description is intent; its stable ID is technical evidence.",
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
