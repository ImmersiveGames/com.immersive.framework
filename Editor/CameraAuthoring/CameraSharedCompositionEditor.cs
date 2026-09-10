using Immersive.Framework.CameraAuthoring;
using UnityEditor;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraSharedComposition))]
    public sealed class CameraSharedCompositionEditor : UnityEditor.Editor
    {
        private readonly CameraOutputReferenceGUI _outputs = new CameraOutputReferenceGUI();

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            CameraIdentityAuthoringGUI.DrawDefinition(serializedObject.FindProperty("viewId"), true);
            _outputs.DrawTopology(serializedObject);
            _outputs.DrawReference(serializedObject.FindProperty("outputId"));
            DrawPropertiesExcluding(serializedObject, "m_Script", "viewId", "outputId");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
