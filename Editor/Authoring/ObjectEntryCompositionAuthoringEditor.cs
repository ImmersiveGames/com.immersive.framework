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
                new GUIContent("Composition Profile", "Reusable Base composition for this Object Entry. It may be omitted only when runtime contributions are enabled."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("allowDynamicContributions"),
                new GUIContent(
                    "Allow Runtime Contributions",
                    "Allows opted-in consumers to open independent contributions for this exact admitted occurrence. It does not select a Player or authorize gameplay."));
            serializedObject.ApplyModifiedProperties();

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Select one object to validate its optional composition authoring.", MessageType.Info);
                return;
            }

            var authoring = (ObjectEntryCompositionAuthoring)target;
            if (!authoring.TryValidateConfiguration(out string issue))
                EditorGUILayout.HelpBox(issue, MessageType.Error);
            else if (authoring.AllowsDynamicContributions)
                EditorGUILayout.HelpBox(
                    authoring.Profile == null
                        ? "Runtime access is available after the owning Route or Activity commits this exact occurrence. Its Composer is created on the first authorized contribution open."
                        : "The authored Base must activate before runtime access is available for this exact occurrence.",
                    MessageType.Info);
            else
                EditorGUILayout.HelpBox("The authored Base uses B2 composition. Runtime consumer contributions are disabled.", MessageType.Info);
        }
    }
}
