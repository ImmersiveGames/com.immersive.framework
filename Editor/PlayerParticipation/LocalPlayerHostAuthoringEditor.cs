using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    [CustomEditor(typeof(LocalPlayerHostAuthoring))]
    internal sealed class LocalPlayerHostAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty playerInput;
        private SerializedProperty actorMount;

        private FrameworkAuthoringValidationReport lastValidationReport;
        private bool validationOutdated;
        private bool showAdvanced;

        private void OnEnable()
        {
            playerInput =
                serializedObject.FindProperty("playerInput");
            actorMount =
                serializedObject.FindProperty("actorMount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            LocalPlayerHostAuthoring host =
                (LocalPlayerHostAuthoring)target;

            EditorGUI.BeginChangeCheck();
            DrawConfiguration();
            bool authoringChanged = EditorGUI.EndChangeCheck();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if ((authoringChanged || modified) &&
                lastValidationReport != null)
            {
                validationOutdated = true;
            }

            DrawActions(host);
            DrawValidationSummary();

            if (Application.isPlaying)
            {
                DrawRuntimeStatus(host);
            }

            DrawAdvanced(host);
        }

        private void DrawConfiguration()
        {
            DrawSection("Host Configuration");

            EditorGUILayout.PropertyField(
                playerInput,
                new GUIContent(
                    "Player Input",
                    "PlayerInput owned by this exact Local Player Host root."));

            EditorGUILayout.PropertyField(
                actorMount,
                new GUIContent(
                    "Actor Mount",
                    "Child transform that contains or receives the contextual Logical Actor. Scene-Provided hosts may already contain an authored Actor; Manager-Provisioned hosts begin with an empty mount."));
        }

        private void DrawActions(
            LocalPlayerHostAuthoring host)
        {
            DrawSection("Actions");

            if (GUILayout.Button(
                    new GUIContent(
                        "Validate Host",
                        "Validate the shared Local Player Host invariants without modifying the Host.")))
            {
                serializedObject.ApplyModifiedProperties();
                lastValidationReport =
                    LocalPlayerHostAuthoringValidator.Validate(host);
                validationOutdated = false;
            }
        }

        private void DrawValidationSummary()
        {
            DrawSection("Validation Summary");

            if (lastValidationReport == null)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated");
                return;
            }

            if (validationOutdated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated — configuration changed");
                return;
            }

            if (lastValidationReport.IsValid)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Valid");
                return;
            }

            EditorGUILayout.LabelField(
                "Status",
                "Invalid");

            EditorGUILayout.HelpBox(
                $"{lastValidationReport.ErrorCount} blocking issue(s) were found. Correct the configuration and validate again.",
                MessageType.Error);

            FrameworkAuthoringValidationGui.DrawIssues(
                lastValidationReport,
                false);
        }

        private static void DrawRuntimeStatus(
            LocalPlayerHostAuthoring host)
        {
            DrawSection("Runtime Status");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(
                    "Participation",
                    host.IsJoined
                        ? "Joined"
                        : "Not Joined");

                EditorGUILayout.TextField(
                    "Player Slot",
                    host.HasJoinedSlot
                        ? host.JoinedPlayerSlotId.StableText
                        : string.Empty);

                EditorGUILayout.LabelField(
                    "Logical Actor",
                    host.HasLogicalActor
                        ? "Present"
                        : "Not Present");

                EditorGUILayout.LabelField(
                    "Admission",
                    host.IsAdmissionStaged
                        ? "Staged"
                        : "Not Staged");
            }
        }

        private void DrawAdvanced(
            LocalPlayerHostAuthoring host)
        {
            EditorGUILayout.Space(6f);
            showAdvanced =
                EditorGUILayout.Foldout(
                    showAdvanced,
                    "Advanced / Debug",
                    true);

            if (!showAdvanced)
            {
                return;
            }

            DrawSection("Technical Evidence");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "PlayerInput Evidence",
                    host.HasPlayerInputEvidence);
                EditorGUILayout.Toggle(
                    "Actor Mount Assigned",
                    host.HasActorMount);
                EditorGUILayout.Toggle(
                    "Logical Actor Present",
                    host.HasLogicalActor);
            }

            DrawSection("Participation Evidence");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "Admission Staged",
                    host.IsAdmissionStaged);
                EditorGUILayout.Toggle(
                    "Joined",
                    host.IsJoined);
                EditorGUILayout.TextField(
                    "Joined Slot",
                    host.HasJoinedSlot
                        ? host.JoinedPlayerSlotId.StableText
                        : string.Empty);
                EditorGUILayout.IntField(
                    "Configured Index",
                    host.JoinedConfiguredIndex);
                EditorGUILayout.TextField(
                    "Admission Source",
                    host.AdmissionSource);
                EditorGUILayout.TextField(
                    "Admission Reason",
                    host.AdmissionReason);
            }

            if (lastValidationReport == null)
            {
                return;
            }

            DrawSection("Validation Evidence");

            if (validationOutdated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated — configuration changed");
                return;
            }

            FrameworkAuthoringValidationGui.DrawSummary(
                lastValidationReport);
            FrameworkAuthoringValidationGui.DrawIssues(
                lastValidationReport,
                false);
        }

        private static void DrawSection(
            string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }
    }
}
