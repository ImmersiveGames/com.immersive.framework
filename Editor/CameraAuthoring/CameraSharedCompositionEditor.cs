using Immersive.Framework.CameraAuthoring;
using UnityEditor;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraSharedComposition))]
    public sealed class CameraSharedCompositionEditor : UnityEditor.Editor
    {
        private readonly CameraOutputReferenceGUI _outputs = new CameraOutputReferenceGUI();
        private bool _advanced;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            CameraOutputReferenceGUI.DrawDefinitionReference(serializedObject.FindProperty("viewDefinition"), "View Definition");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("subjectPolicy"));
            _outputs.DrawReference(serializedObject.FindProperty("outputDefinition"));
            _advanced = EditorGUILayout.Foldout(_advanced, "Advanced / Debug", true);
            if (_advanced)
            {
                var composition = (CameraSharedComposition)target;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("View ID", composition.ViewIdText);
                    EditorGUILayout.TextField("Output ID", composition.OutputIdText);
                    EditorGUILayout.TextField("Membership Context ID", composition.MembershipContextIdText);
                    if (composition.TryCreateAssociationBinding(out var binding, out _))
                    {
                        EditorGUILayout.TextField(
                            "Projected Binding",
                            $"{binding.ViewId} -> {binding.OutputId}");
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
