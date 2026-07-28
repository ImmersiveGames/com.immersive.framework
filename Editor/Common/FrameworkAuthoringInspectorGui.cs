using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Common
{
    internal static class FrameworkAuthoringInspectorGui
    {
        internal static void ProductHeader(string title, string responsibility)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(responsibility, MessageType.Info);
        }

        internal static void IntentSummary(string text)
        {
            EditorGUILayout.LabelField("Intent Summary", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(text, MessageType.None);
        }

        internal static void Section(string title)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        internal static void RuntimeBinding(string status, string diagnostic, string correctiveAction)
        {
            Section("Runtime Binding");
            EditorGUILayout.LabelField("Status", status);
            if (status != "Bound")
            {
                EditorGUILayout.HelpBox(correctiveAction, MessageType.Warning);
            }

            if (!string.IsNullOrWhiteSpace(diagnostic))
            {
                EditorGUILayout.HelpBox(diagnostic, MessageType.None);
            }
        }

        internal static bool AdvancedFoldout(bool expanded)
        {
            Section("Advanced / Debug");
            return EditorGUILayout.Foldout(expanded, "Advanced / Debug", true);
        }

        internal static void ApplySuggestion(SerializedObject serializedObject, SerializedProperty property, string value, string undoName)
        {
            Undo.RecordObjects(serializedObject.targetObjects, undoName);
            property.stringValue = value;
            serializedObject.ApplyModifiedProperties();
            foreach (Object item in serializedObject.targetObjects)
            {
                EditorUtility.SetDirty(item);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            }
        }
    }
}
