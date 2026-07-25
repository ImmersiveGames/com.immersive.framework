using System;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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

            DrawHeader();
            DrawTechnicalHost();
            DrawValidation(host);
            DrawAdvanced(host);

            bool modified =
                serializedObject.ApplyModifiedProperties();
            if (modified && lastValidationReport != null)
            {
                validationOutdated = true;
            }
        }

        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Local Player Host",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Defines the stable technical root for one local Player. It owns PlayerInput and an explicit Actor Mount. A Manager-Provisioned Host keeps that Mount empty until composition; a Scene-Provided Host may already contain its authored Logical Actor.",
                MessageType.Info);
        }

        private void DrawTechnicalHost()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Technical Host",
                EditorStyles.boldLabel);

            PlayerInput currentPlayerInput =
                playerInput.objectReferenceValue as PlayerInput;
            PlayerInput selectedPlayerInput =
                (PlayerInput)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Player Input",
                        "PlayerInput owned by this exact technical Host root."),
                    currentPlayerInput,
                    typeof(PlayerInput),
                    true);
            if (!ReferenceEquals(
                    currentPlayerInput,
                    selectedPlayerInput))
            {
                playerInput.objectReferenceValue =
                    selectedPlayerInput;
            }

            Transform currentActorMount =
                actorMount.objectReferenceValue as Transform;
            Transform selectedActorMount =
                (Transform)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Actor Mount",
                        "Explicit child that contains or receives the contextual Logical Actor."),
                    currentActorMount,
                    typeof(Transform),
                    true);
            if (!ReferenceEquals(
                    currentActorMount,
                    selectedActorMount))
            {
                actorMount.objectReferenceValue =
                    selectedActorMount;
            }

            EditorGUILayout.HelpBox(
                "The Host Inspector validates only shared technical invariants. Whether Actor Mount must be empty or contain one authored Actor is decided by the source-specific composer or provisioning surface.",
                MessageType.None);
        }

        private void DrawValidation(
            LocalPlayerHostAuthoring host)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (lastValidationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Run validation after assigning PlayerInput and Actor Mount.",
                    MessageType.None);
            }
            else if (validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the Host configuration changed.",
                    MessageType.Warning);
            }
            else if (lastValidationReport.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "Ready — shared Local Player Host invariants are valid.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Needs Attention — {lastValidationReport.ErrorCount} blocking issue(s) were found.",
                    MessageType.Error);
            }

            if (GUILayout.Button("Validate Host"))
            {
                serializedObject.ApplyModifiedProperties();
                lastValidationReport =
                    LocalPlayerHostAuthoringValidator.Validate(host);
                validationOutdated = false;
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

            if (lastValidationReport != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(
                    "Validation Report",
                    EditorStyles.boldLabel);

                if (validationOutdated)
                {
                    EditorGUILayout.HelpBox(
                        "This report is outdated. Run Validate Host again.",
                        MessageType.Warning);
                }

                FrameworkAuthoringValidationGui.DrawSummary(
                    lastValidationReport);
                FrameworkAuthoringValidationGui.DrawIssues(
                    lastValidationReport,
                    false);
            }
        }
    }
}
