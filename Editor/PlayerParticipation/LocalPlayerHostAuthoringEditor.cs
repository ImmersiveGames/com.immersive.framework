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
        private bool showAdvanced;

        private void OnEnable()
        {
            playerInput = serializedObject.FindProperty("playerInput");
            actorMount = serializedObject.FindProperty("actorMount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Local Player Host", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Defines the stable technical object provisioned by PlayerInputManager. It owns PlayerInput, an Actor Mount and the runtime Slot binding; it is not a Logical Actor.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Technical Host", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playerInput, new GUIContent("Player Input"));
            EditorGUILayout.PropertyField(actorMount, new GUIContent("Actor Mount"));
            EditorGUILayout.HelpBox(
                "Actor Mount must be an explicit child. Keep it empty in the provisioning prefab; the selected Logical Actor Host is materialized later.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            LocalPlayerHostAuthoring host = (LocalPlayerHostAuthoring)target;
            EditorGUILayout.Space(6);
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced / Debug", true);
            if (showAdvanced)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Toggle("PlayerInput Evidence", host.HasPlayerInputEvidence);
                    EditorGUILayout.Toggle("Actor Mount Assigned", host.HasActorMount);
                    EditorGUILayout.Toggle("Admission Staged", host.IsAdmissionStaged);
                    EditorGUILayout.Toggle("Joined", host.IsJoined);
                    EditorGUILayout.TextField(
                        "Joined Slot",
                        host.HasJoinedSlot ? host.JoinedPlayerSlotId.StableText : string.Empty);
                    EditorGUILayout.IntField("Configured Index", host.JoinedConfiguredIndex);
                    EditorGUILayout.Toggle("Logical Actor Prepared", host.HasLogicalActor);
                }
            }

            EditorGUILayout.Space(6);
            FrameworkAuthoringValidationReport report =
                LocalPlayerHostAuthoringValidator.Validate(host);
            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}
