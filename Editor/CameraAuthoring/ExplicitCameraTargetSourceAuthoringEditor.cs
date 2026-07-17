using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(ExplicitCameraTargetSourceAuthoring))]
    public sealed class ExplicitCameraTargetSourceAuthoringEditor :
        UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(
                "Explicit Camera Target Source",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Provides explicit Follow and Look At evidence. It does not own a rig, output, camera winner or lifecycle.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("logicalSourceId"),
                new GUIContent("Logical Source Id"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("followTarget"),
                new GUIContent("Follow Target"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("lookAtTarget"),
                new GUIContent("Look At Target"));

            serializedObject.ApplyModifiedProperties();

            var source = (ExplicitCameraTargetSourceAuthoring)target;
            CameraTargetResolveResult result = source.ResolveCameraTargets(
                CameraTargetRequirement.Required,
                CameraTargetRequirement.Optional);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                result.IsSucceeded
                    ? result.DiagnosticSummary
                    : result.BlockingIssue,
                result.IsSucceeded
                    ? MessageType.Info
                    : MessageType.Error);
        }
    }
}
