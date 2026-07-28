using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    internal static class UnityResetParticipantEditorUtility
    {
        internal static void DrawCommon(
            SerializedProperty displayName,
            SerializedProperty requiredness,
            SerializedProperty order)
        {
            EditorGUILayout.LabelField("Reset Participant", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("One part of the Subject state restored during reset. Lower Order values execute first.", MessageType.Info);
            EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(requiredness, new GUIContent("Requiredness", "Required failures block the Subject reset result. Optional failures follow the runtime optional-participant policy."));
            EditorGUILayout.PropertyField(order, new GUIContent("Order", "Lower values execute first."));
        }

        internal static void DrawIdentityAndDiagnostics(
            UnityResetParticipantBehaviour participant,
            SerializedProperty participantId,
            SerializedProperty source,
            SerializedProperty reason,
            ref bool showAdvanced,
            ref bool showDiagnostics)
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
            if (showAdvanced)
            {
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(participantId, new GUIContent("Participant ID"));
                EditorGUILayout.PropertyField(source, new GUIContent("Source"));
                EditorGUILayout.PropertyField(reason, new GUIContent("Reason"));
            }

            showDiagnostics = EditorGUILayout.Foldout(showDiagnostics, "Diagnostics", true);
            if (!showDiagnostics) return;
            EditorGUILayout.LabelField("Serialized ID", participantId.stringValue.ToString());
            EditorGUILayout.LabelField("Identity", string.IsNullOrWhiteSpace(participantId.stringValue) ? "Missing" : "Present");
            UnityResetSubjectAdapter subject = participant.GetComponentInParent<UnityResetSubjectAdapter>();
            EditorGUILayout.LabelField("Parent Subject", subject != null ? subject.name : "Not found in parent hierarchy");
            EditorGUILayout.LabelField("Runtime Registration", Application.isPlaying && subject != null ? (subject.IsRegistered ? "Registered" : "Not registered") : "Runtime-dependent");
        }

        internal static bool ValidateCommon(
            SerializedProperty participantId,
            SerializedProperty requiredness,
            out string issue)
        {
            if (string.IsNullOrWhiteSpace(participantId.stringValue)) { issue = "Participant ID is missing. Use Generate Missing IDs."; return false; }
            if (requiredness.intValue == (int)ResetParticipantRequiredness.Unknown) { issue = "Requiredness must be explicit."; return false; }
            issue = string.Empty;
            return true;
        }
    }
}
