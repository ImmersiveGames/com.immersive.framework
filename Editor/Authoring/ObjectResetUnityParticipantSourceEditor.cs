using Immersive.Framework.ObjectReset;
using Immersive.Framework.ObjectReset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ObjectResetUnityParticipantSource))]
    [CanEditMultipleObjects]
    internal sealed class ObjectResetUnityParticipantSourceEditor : UnityEditor.Editor
    {
        private SerializedProperty _registerOnEnable;
        private SerializedProperty _participants;

        private void OnEnable()
        {
            _registerOnEnable = serializedObject.FindProperty("registerOnEnable");
            _participants = serializedObject.FindProperty("participants");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Object Reset Unity Participant Source", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Registers an explicit list of Unity Object Reset participants with the framework runtime. It does not search the scene and does not use GameObject names, hierarchy paths, scene paths or InstanceIDs as functional identity.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _registerOnEnable,
                new GUIContent(
                    "Register On Enable",
                    "When enabled, this source registers itself with the current FrameworkRuntimeHost on enable/start and clears registration on disable."));
            EditorGUILayout.PropertyField(
                _participants,
                new GUIContent(
                    "Participants",
                    "Explicit Unity reset participants handled by this source, such as Transform Reset or GameObject Active Reset participants."),
                true);

            DrawAuthoringMessages();
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAuthoringMessages()
        {
            if (_participants != null
                && !_participants.hasMultipleDifferentValues
                && _participants.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Participants list is empty. This is valid for foundation tests, but a physical F15 reset that requires an adapter should register at least one participant.",
                    MessageType.Warning);
            }

            EditorGUILayout.HelpBox(
                "Recommended scene shape: one organizational source object per Route/Activity authoring context, with explicit participant references from objects that declare ObjectEntryDeclaration.",
                MessageType.None);
        }

        private void DrawRuntimeControls()
        {
            if (!Application.isPlaying || targets.Length != 1)
            {
                return;
            }

            var source = target as ObjectResetUnityParticipantSource;
            if (source == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Registration", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Authored Participants", source.AuthoredParticipantCount.ToString());
            EditorGUILayout.LabelField("Registered", source.IsRegistered ? "Yes" : "No");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Register Now"))
                {
                    source.RegisterWithCurrentHost();
                }

                if (GUILayout.Button("Clear Registration"))
                {
                    source.ClearRegistration();
                }
            }
        }
    }
}
