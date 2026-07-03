using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(RouteContentBinding))]
    [CanEditMultipleObjects]
    internal sealed class RouteContentBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty _route;
        private SerializedProperty _localContentId;
        private SerializedProperty _requiredness;

        private void OnEnable()
        {
            _route = serializedObject.FindProperty("route");
            _localContentId = serializedObject.FindProperty("localContentId");
            _requiredness = serializedObject.FindProperty("requiredness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Route Content Binding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _route,
                new GUIContent(
                    "Route",
                    "Route asset that owns this scene. Use the Route whose Primary Scene is this scene."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Local Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _localContentId,
                new GUIContent(
                    "Local Content Id",
                    "Explicit local id required by F5 local identity. GameObject name and hierarchy path are diagnostics only and are not used as fallback."));

            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Authoring policy recorded by F5F. Required contributions can block future consumers; Optional contributions can be skipped with diagnostics. Absence validation is not active yet."));

            if (_localContentId != null && _localContentId.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "Multiple selected Route Content Bindings have different Local Content Id values.",
                    MessageType.Info);
            }
            else if (_localContentId == null || string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Local Content Id is required for F5 local identity. GameObject names and hierarchy paths are diagnostics only and are not fallback identities.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"This Route local contribution uses explicit local id '{_localContentId.stringValue.Trim()}' and requiredness '{GetRequirednessLabel()}'.",
                    MessageType.Info);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private string GetRequirednessLabel()
        {
            return _requiredness != null && !_requiredness.hasMultipleDifferentValues
                ? _requiredness.enumDisplayNames[_requiredness.enumValueIndex]
                : "Mixed";
        }
    }
}
