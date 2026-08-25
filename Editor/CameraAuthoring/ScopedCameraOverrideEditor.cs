using Immersive.Framework.Camera;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(ScopedCameraOverride), true)]
    public sealed class ScopedCameraOverrideEditor : UnityEditor.Editor
    {
        private bool _showAdvanced;
        private bool _showDebug = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "This is an explicit camera override. It never activates merely because its owner enters a scope.",
                MessageType.Info);

            DrawPropertiesExcluding(serializedObject,
                "m_Script", "scopeId", "requestId", "rigComposer", "targetSource", "precedence", "tieBreakerId",
                "logDiagnostics", "overrideActive", "ownerActive", "lastStatus", "lastDiagnostic", "outputSession");

            EditorGUILayout.LabelField("Override", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scopeId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requestId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rigComposer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetSource"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("precedence"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tieBreakerId"));

            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (_showAdvanced)
            {
                var inspectedBinding = (ScopedCameraOverride)target;
                EditorGUILayout.LabelField(
                    "Camera Output",
                    inspectedBinding.OutputSession != null
                        ? inspectedBinding.OutputSession.OutputIdText
                        : "Injected at runtime");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("logDiagnostics"));
            }

            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug", true);
            if (_showDebug)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideActive"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ownerActive"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lastStatus"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lastDiagnostic"));
            }

            serializedObject.ApplyModifiedProperties();

            if (!Application.isPlaying) return;
            var binding = (ScopedCameraOverride)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Request Override")) binding.RequestOverride();
                if (GUILayout.Button("Release Override")) binding.ReleaseOverride();
            }
        }
    }
}
