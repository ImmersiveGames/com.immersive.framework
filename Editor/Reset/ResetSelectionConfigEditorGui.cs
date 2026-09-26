using Immersive.Framework.Reset;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    /// <summary>
    /// Shared compact drawer for the inline ResetSelectionConfig struct used by
    /// ObjectResetGroupTrigger and ActivityRestartTrigger. Editor-only; it renders the
    /// existing serialized fields and does not change ResetSelectionConfig semantics.
    /// </summary>
    internal static class ResetSelectionConfigEditorGui
    {
        private static readonly GUIContent ModeLabel = new GUIContent(
            "Selection Mode",
            "Which Reset Subjects this request targets.");
        private static readonly GUIContent ExplicitSubjectsLabel = new GUIContent("Explicit Subjects");
        private static readonly GUIContent AllowNoSubjectsLabel = new GUIContent(
            "Allow No Subjects",
            "When enabled, a resolved selection with no Subjects succeeds instead of failing.");
        private static readonly GUIContent AllowNoParticipantsLabel = new GUIContent(
            "Allow No Participants",
            "When enabled, a selected ResetSubject with no participants succeeds as SucceededNoParticipants.");
        private static readonly GUIContent StopOnFailureLabel = new GUIContent(
            "Stop On Failure",
            "Stops execution of remaining Subjects after the first blocking failure.");
        private static readonly GUIContent YieldBetweenSubjectsLabel = new GUIContent(
            "Yield Between Subjects",
            "When enabled, execution yields a frame between Subjects instead of running the full selection synchronously.");

        internal static void DrawSelection(SerializedProperty selection)
        {
            SerializedProperty mode = selection.FindPropertyRelative("mode");
            EditorGUILayout.PropertyField(mode, ModeLabel);

            if (mode.intValue == (int)ResetSelectionMode.ExplicitSubjects)
            {
                SerializedProperty explicitSubjects = selection.FindPropertyRelative("explicitSubjects");
                EditorGUILayout.PropertyField(explicitSubjects, ExplicitSubjectsLabel, true);
                EditorGUILayout.LabelField("Configured Subjects", explicitSubjects.arraySize.ToString());
            }
        }

        internal static void DrawAdvanced(SerializedProperty selection)
        {
            EditorGUILayout.PropertyField(selection.FindPropertyRelative("allowNoSubjects"), AllowNoSubjectsLabel);
            EditorGUILayout.PropertyField(selection.FindPropertyRelative("allowNoParticipants"), AllowNoParticipantsLabel);
            EditorGUILayout.PropertyField(selection.FindPropertyRelative("stopOnFailure"), StopOnFailureLabel);
            EditorGUILayout.PropertyField(selection.FindPropertyRelative("yieldBetweenSubjects"), YieldBetweenSubjectsLabel);
        }

        internal static bool IsExplicitSelectionEmpty(SerializedProperty selection)
        {
            SerializedProperty mode = selection.FindPropertyRelative("mode");
            if (mode.intValue != (int)ResetSelectionMode.ExplicitSubjects)
            {
                return false;
            }

            SerializedProperty explicitSubjects = selection.FindPropertyRelative("explicitSubjects");
            return explicitSubjects.arraySize == 0;
        }

        internal static bool AllowsNoSubjects(SerializedProperty selection)
        {
            SerializedProperty allowNoSubjects = selection.FindPropertyRelative("allowNoSubjects");
            return allowNoSubjects != null && allowNoSubjects.boolValue;
        }
    }
}
