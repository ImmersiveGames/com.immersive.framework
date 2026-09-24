using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(ActorCameraSubjectAuthoring))]
    internal sealed class ActorCameraSubjectAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty _observationTransform;
        private SerializedProperty _framingRadius;

        private void OnEnable()
        {
            _observationTransform =
                serializedObject.FindProperty("observationTransform");
            _framingRadius =
                serializedObject.FindProperty("framingRadius");
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

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Framing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _framingRadius,
                new GUIContent(
                    "Radius",
                    "Optional presentation-space radius centered on the Observation Transform. " +
                    "Use 0 to let the consuming presentation use its fallback radius."));

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
