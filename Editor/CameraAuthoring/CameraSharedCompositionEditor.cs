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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("subjectPolicy"));
            _outputs.DrawReference(serializedObject.FindProperty("outputDefinition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("compositionRig"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requestPrecedence"));
            _advanced = EditorGUILayout.Foldout(_advanced, "Advanced / Debug", true);
            if (_advanced)
            {
                var composition = (CameraSharedComposition)target;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Output ID", composition.OutputIdText);
                    EditorGUILayout.TextField("Membership Context ID", composition.MembershipContextIdText);
                    EditorGUILayout.TextField("Request ID", composition.RequestId.ToString());
                    EditorGUILayout.Toggle("Request Published", composition.IsRequestPublished);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
