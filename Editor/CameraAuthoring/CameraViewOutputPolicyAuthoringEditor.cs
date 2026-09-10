using Immersive.Framework.Camera;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraViewOutputPolicyAuthoring))]
    public sealed class CameraViewOutputPolicyAuthoringEditor : UnityEditor.Editor
    {
        private readonly CameraOutputReferenceGUI _outputs = new CameraOutputReferenceGUI();

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            _outputs.DrawTopology(serializedObject);
            SerializedProperty bindings = serializedObject.FindProperty("bindings");
            if (bindings.arraySize == 0)
                EditorGUILayout.HelpBox("Add at least one explicit View / Output binding.", MessageType.Error);
            for (int index = 0; index < bindings.arraySize; index++)
            {
                SerializedProperty binding = bindings.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"Binding {index + 1}", EditorStyles.boldLabel);
                    SerializedProperty view = binding.FindPropertyRelative("viewId");
                    EditorGUILayout.PropertyField(view, new GUIContent("View Id"));
                    CameraIdentityAuthoringGUI.DrawIssue(CameraIdentityAuthoringValidation.ViewIdIssue(view.stringValue));
                    _outputs.DrawReference(binding.FindPropertyRelative("outputId"));
                    EditorGUILayout.PropertyField(binding.FindPropertyRelative("viewport"));
                    if (GUILayout.Button("Remove Binding"))
                    {
                        bindings.DeleteArrayElementAtIndex(index);
                        break;
                    }
                }
            }
            if (GUILayout.Button("Add Binding"))
            {
                int index = bindings.arraySize;
                bindings.InsertArrayElementAtIndex(index);
                SerializedProperty added = bindings.GetArrayElementAtIndex(index);
                added.FindPropertyRelative("viewId").stringValue = string.Empty;
                added.FindPropertyRelative("outputId").stringValue = string.Empty;
                added.FindPropertyRelative("viewport").rectValue = new Rect(0, 0, 1, 1);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
