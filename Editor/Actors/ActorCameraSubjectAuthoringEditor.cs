using Immersive.Framework.Actors;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Actors
{
    [CustomEditor(typeof(ActorCameraSubjectAuthoring))]
    internal sealed class ActorCameraSubjectAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty _observationTransform;
        private SerializedProperty _framingRadius;

        private void OnEnable()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            _observationTransform =
                current.FindProperty("observationTransform");
            _framingRadius =
                current.FindProperty("framingRadius");
        }

        public override void OnInspectorGUI()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            current.UpdateIfRequiredOrScript();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            EditorGUILayout.LabelField("Camera Subject", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Owner", "Actor Occurrence");
                EditorGUILayout.TextField("Role", "Observation Transform");
            }

            EditorGUILayout.PropertyField(
                _observationTransform,
                new GUIContent(
                    "Transform",
                    "Explicit Transform observed for this Actor occurrence. Assign the Actor root explicitly when desired."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Framing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _framingRadius,
                new GUIContent(
                    "Radius",
                    "Optional framing radius centered on the Observation Transform. Use 0 when unspecified."));

            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            current.ApplyModifiedProperties();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            var authoring = inspectedTarget as ActorCameraSubjectAuthoring;
            if (authoring == null)
                return;
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

        private bool TryGetValidSerializedObject(
            UnityEngine.Object inspectedTarget,
            out SerializedObject current)
        {
            current = null;
            if (this == null || inspectedTarget == null || target != inspectedTarget)
                return false;

            current = serializedObject;
            return current != null && current.targetObject == inspectedTarget;
        }

        private bool IsCurrentTargetValid(
            UnityEngine.Object inspectedTarget,
            SerializedObject current)
        {
            return current != null && inspectedTarget != null &&
                   target == inspectedTarget &&
                   current.targetObject == inspectedTarget;
        }
    }
}
