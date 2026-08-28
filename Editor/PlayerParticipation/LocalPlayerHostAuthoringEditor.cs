using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(LocalPlayerHostAuthoring))]
    internal sealed class LocalPlayerHostAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty _playerInput;
        private SerializedProperty _actorMount;
        private SerializedProperty _playerActorRuntimeHostPrefab;

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _validationOutdated;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _playerInput =
                serializedObject.FindProperty("playerInput");
            _actorMount =
                serializedObject.FindProperty("actorMount");
            _playerActorRuntimeHostPrefab =
                serializedObject.FindProperty("playerActorRuntimeHostPrefab");
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
                _lastValidationReport != null)
            {
                _validationOutdated = true;
            }

            DrawConfigurationStatus(host);

            if (Application.isPlaying)
            {
                DrawRuntimeStatus(host);
            }

            DrawAdvanced(host);
        }

        private void DrawConfiguration()
        {
            DrawSection("Player / Input");

            EditorGUILayout.PropertyField(
                _playerInput,
                new GUIContent(
                    "Player Input",
                    "PlayerInput owned by this exact Local Player Host root."));

            EditorGUILayout.PropertyField(
                _actorMount,
                new GUIContent(
                    "Actor Mount",
                    "Child transform that contains or receives the contextual Player Actor Runtime Host. Scene-Provided hosts may already contain an authored Runtime Host; Manager-Provisioned hosts begin with an empty mount."));

            DrawSection("Actor Runtime");

            EditorGUILayout.PropertyField(
                _playerActorRuntimeHostPrefab,
                new GUIContent(
                    "Player Actor Runtime Host Prefab",
                    "Generic Framework-owned Actor runtime host supplied by this Local Player Host composition. It is materialized after Actor selection and receives the selected Actor Profile Presentation."));
        }

        private void DrawConfigurationStatus(
            LocalPlayerHostAuthoring host)
        {
            DrawSection("Configuration Status");

            if (GUILayout.Button(
                    new GUIContent(
                        "Validate Host",
                        "Validate the shared Local Player Host invariants without modifying the Host.")))
            {
                serializedObject.ApplyModifiedProperties();
                _lastValidationReport =
                    LocalPlayerHostAuthoringValidator.Validate(host);
                _validationOutdated = false;
            }

            DrawValidationSummary();
        }

        private void DrawValidationSummary()
        {
            if (_lastValidationReport == null)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated");
                return;
            }

            if (_validationOutdated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated — configuration changed");
                return;
            }

            if (_lastValidationReport.IsValid)
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
                $"{_lastValidationReport.ErrorCount} blocking issue(s) were found. Correct the configuration and validate again.",
                MessageType.Error);

            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
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
                    "Actor Runtime",
                    host.HasPlayerActorRuntime
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
            _showAdvanced =
                EditorGUILayout.Foldout(
                    _showAdvanced,
                    "Advanced / Debug",
                    true);

            if (!_showAdvanced)
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
                    "Actor Runtime Present",
                    host.HasPlayerActorRuntime);
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

            if (_lastValidationReport == null)
            {
                return;
            }

            DrawSection("Validation Evidence");

            if (_validationOutdated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated — configuration changed");
                return;
            }

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastValidationReport);
            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
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
