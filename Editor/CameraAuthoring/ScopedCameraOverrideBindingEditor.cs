using Immersive.Framework.Camera;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(ScopedCameraOverrideBinding), true)]
    public sealed class ScopedCameraOverrideBindingEditor : UnityEditor.Editor
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("outputSession"));
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
            var binding = (ScopedCameraOverrideBinding)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Request Override")) binding.RequestOverride();
                if (GUILayout.Button("Release Override")) binding.ReleaseOverride();
            }
        }
    }
}
