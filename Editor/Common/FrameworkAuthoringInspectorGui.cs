using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Common
{
    internal static class FrameworkAuthoringInspectorGui
    {
        internal static void ProductHeader(string title, string responsibility)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (!string.IsNullOrWhiteSpace(responsibility))
            {
                EditorGUILayout.LabelField(
                    responsibility,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        internal static void IntentSummary(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Intent", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedMiniLabel);
        }

        internal static void Section(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        internal static void Status(string value)
        {
            DrawLabelValue("Status", value);
        }

        internal static void RuntimeBinding(
            string status,
            string diagnostic,
            string correctiveAction)
        {
            Section("Runtime Binding");
            DrawLabelValue("Status", status);

            if (status != "Bound" &&
                !string.IsNullOrWhiteSpace(correctiveAction))
            {
                EditorGUILayout.HelpBox(
                    correctiveAction,
                    MessageType.Warning);
            }

            if (!string.IsNullOrWhiteSpace(diagnostic))
            {
                EditorGUILayout.LabelField(
                    diagnostic,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        internal static bool AdvancedFoldout(bool expanded)
        {
            EditorGUILayout.Space(7f);
            return EditorGUILayout.Foldout(
                expanded,
                "Advanced / Debug",
                true);
        }

        internal static void ApplySuggestion(
            SerializedObject serializedObject,
            SerializedProperty property,
            string value,
            string undoName)
        {
            Undo.RecordObjects(
                serializedObject.targetObjects,
                undoName);

            property.stringValue = value;
            serializedObject.ApplyModifiedProperties();

            foreach (Object item in serializedObject.targetObjects)
            {
                EditorUtility.SetDirty(item);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            }
        }

        private static void DrawLabelValue(string label, string value)
        {
            EditorGUILayout.LabelField(
                new GUIContent(label),
                new GUIContent(value ?? string.Empty));
        }
    }
}
