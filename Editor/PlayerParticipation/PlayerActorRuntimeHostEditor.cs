using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerActorRuntimeHost))]
    internal sealed class PlayerActorRuntimeHostEditor : UnityEditor.Editor
    {
        private SerializedProperty _playerActorDeclaration;
        private SerializedProperty _presentationMount;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _playerActorDeclaration =
                serializedObject.FindProperty("playerActorDeclaration");
            _presentationMount =
                serializedObject.FindProperty("presentationMount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawSection("Actor Occurrence");
            EditorGUILayout.PropertyField(
                _playerActorDeclaration,
                new GUIContent(
                    "Player Actor Declaration",
                    "The one Framework Actor declaration owned by this generic runtime host."));
            EditorGUILayout.PropertyField(
                _presentationMount,
                new GUIContent(
                    "Optional Visual Content Mount",
                    "Optional child mount for configured visual content. The Actor root owns physical state and spatial pose."));

            serializedObject.ApplyModifiedProperties();

            DrawConfigurationStatus((PlayerActorRuntimeHost)target);
            DrawAdvanced((PlayerActorRuntimeHost)target);
        }

        private static void DrawConfigurationStatus(
            PlayerActorRuntimeHost runtimeHost)
        {
            DrawSection("Configuration Status");

            if (runtimeHost.TryValidateConfiguration(out string issue))
            {
                EditorGUILayout.LabelField("Status", "Valid");
                return;
            }

            EditorGUILayout.LabelField("Status", "Invalid");
            EditorGUILayout.HelpBox(issue, MessageType.Error);
        }

        private void DrawAdvanced(PlayerActorRuntimeHost runtimeHost)
        {
            EditorGUILayout.Space(6f);
            _showAdvanced = EditorGUILayout.Foldout(
                _showAdvanced,
                "Advanced / Debug",
                true);

            if (!_showAdvanced)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "Player Actor Declaration Assigned",
                    runtimeHost.HasPlayerActorDeclaration);
                EditorGUILayout.Toggle(
                    "Visual Content Mount Assigned",
                    runtimeHost.HasVisualContentMount);
            }
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }
}
