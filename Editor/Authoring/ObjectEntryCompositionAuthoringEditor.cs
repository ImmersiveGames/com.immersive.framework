using Immersive.Framework.ObjectEntry;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ObjectEntryCompositionAuthoring))]
    [CanEditMultipleObjects]
    internal sealed class ObjectEntryCompositionAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Object Entry Composition", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("profile"),
                new GUIContent("Composition Profile", "Optional reusable composition configuration for this Object Entry declaration."));
            serializedObject.ApplyModifiedProperties();

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Select one object to validate its optional composition authoring.", MessageType.Info);
                return;
            }

            var authoring = (ObjectEntryCompositionAuthoring)target;
            if (!authoring.TryValidateConfiguration(out string issue))
                EditorGUILayout.HelpBox(issue, MessageType.Error);
            else
                EditorGUILayout.HelpBox("Authoring validated. Runtime composition is not connected in this cut (B1).", MessageType.Info);
        }
    }
}
