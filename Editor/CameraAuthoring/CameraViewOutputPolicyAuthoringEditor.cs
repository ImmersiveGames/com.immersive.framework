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
            EditorGUILayout.HelpBox(
                "Advanced explicit multi-binding surface. A simple one View / one Output association can be authored on Shared Camera Composition instead.",
                MessageType.Info);
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
                    SerializedProperty view = binding.FindPropertyRelative("viewDefinition");
                    CameraOutputReferenceGUI.DrawDefinitionReference(view, "View Definition");
                    _outputs.DrawReference(binding.FindPropertyRelative("outputDefinition"));
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
                added.FindPropertyRelative("viewDefinition").objectReferenceValue = null;
                added.FindPropertyRelative("outputDefinition").objectReferenceValue = null;
                added.FindPropertyRelative("viewport").rectValue = new Rect(0, 0, 1, 1);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
