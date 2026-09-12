using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraViewDefinition))]
    internal sealed class CameraViewDefinitionEditor : CameraDefinitionEditor { }

    [CustomEditor(typeof(CameraOutputDefinition))]
    internal sealed class CameraOutputDefinitionEditor : CameraDefinitionEditor { }

    internal abstract class CameraDefinitionEditor : UnityEditor.Editor
    {
        private bool _advanced;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "Share this exact definition asset through typed references. Its description is intent; its stable ID is technical evidence.",
                MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
            serializedObject.ApplyModifiedProperties();
            var definition = (ScriptableObject)target;
            string issue = CameraDefinitionIdentityEditorUtility.Validate(definition);
            if (issue != null) EditorGUILayout.HelpBox(issue, MessageType.Error);
            var id = serializedObject.FindProperty("stableId");
            if (string.IsNullOrEmpty(id.stringValue))
            {
                if (GUILayout.Button("Generate Stable Identity"))
                    CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition);
            }
            else if (CameraDefinitionIdentityEditorUtility.HasCollision(definition))
            {
                if (GUILayout.Button("Repair Collision — New Identity for This Definition"))
                    CameraDefinitionIdentityEditorUtility.RepairCollision(definition);
            }
            _advanced = EditorGUILayout.Foldout(_advanced, "Advanced / Debug", true);
            if (_advanced)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField("Stable ID", id.stringValue);
                if (GUILayout.Button("Copy Stable ID")) EditorGUIUtility.systemCopyBuffer = id.stringValue;
            }
        }
    }
}
