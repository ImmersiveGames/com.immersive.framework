using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(ActorCameraSubjectAuthoring))]
    internal sealed class ActorCameraSubjectAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty _observationTransform;

        private void OnEnable()
        {
            _observationTransform =
                serializedObject.FindProperty("observationTransform");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField("Camera Subject", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", "Actor Presentation");
                EditorGUILayout.TextField("Role", "Observation / Camera Mount");
            }

            EditorGUILayout.PropertyField(
                _observationTransform,
                new GUIContent(
                    "Transform",
                    "Exact Transform observed by Camera presentation for this Actor occurrence."));

            serializedObject.ApplyModifiedProperties();

            var authoring = (ActorCameraSubjectAuthoring)target;
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Configuration Status", EditorStyles.boldLabel);
            if (authoring.TryValidateConfiguration(out string issue))
            {
                EditorGUILayout.LabelField("Status", "Valid");
            }
            else
            {
                EditorGUILayout.LabelField("Status", "Invalid");
                EditorGUILayout.HelpBox(issue, MessageType.Error);
            }
        }
    }
}
