using Immersive.Framework.ObjectEntry;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ObjectEntryDeclaration))]
    [CanEditMultipleObjects]
    internal sealed class ObjectEntryDeclarationEditor : UnityEditor.Editor
    {
        private SerializedProperty _objectEntryId;
        private SerializedProperty _scope;
        private SerializedProperty _routeOwner;
        private SerializedProperty _activityOwner;
        private SerializedProperty _requiredness;
        private SerializedProperty _displayName;

        private void OnEnable()
        {
            _objectEntryId = serializedObject.FindProperty("objectEntryId");
            _scope = serializedObject.FindProperty("scope");
            _routeOwner = serializedObject.FindProperty("routeOwner");
            _activityOwner = serializedObject.FindProperty("activityOwner");
            _requiredness = serializedObject.FindProperty("requiredness");
            _displayName = serializedObject.FindProperty("displayName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Object Entry Declaration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Declares a logical object entry known by the framework. This is passive authoring metadata only; it does not bind a GameObject as a runtime object, reset components, create Player/Actor semantics, spawn prefabs, or participate in Object Reset yet.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _objectEntryId,
                new GUIContent(
                    "Object Entry Id",
                    "Stable functional identity for this logical object entry. Do not use GameObject names or hierarchy paths as canonical identity."));
            EditorGUILayout.PropertyField(
                _scope,
                new GUIContent(
                    "Scope",
                    "Lifecycle scope that owns this logical object entry: Session, Route, or Activity."));
            DrawOwnerField();
            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Whether this declaration is required or optional for future validation/entry flows."));
            EditorGUILayout.PropertyField(
                _displayName,
                new GUIContent(
                    "Display Name",
                    "Optional human-facing label for diagnostics. It is not a functional identity."));

            DrawGuardrails();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGuardrails()
        {
            if (_objectEntryId != null && !_objectEntryId.hasMultipleDifferentValues && string.IsNullOrWhiteSpace(_objectEntryId.stringValue))
            {
                EditorGUILayout.HelpBox("Object Entry Id is required. It must be a stable logical id, not a GameObject name or hierarchy path.", MessageType.Error);
            }

            if (_scope != null && !_scope.hasMultipleDifferentValues && _scope.enumValueIndex == (int)ObjectEntryScope.Unspecified)
            {
                EditorGUILayout.HelpBox("Scope must be explicit.", MessageType.Error);
            }

            if (_requiredness != null && !_requiredness.hasMultipleDifferentValues && _requiredness.enumValueIndex == (int)ObjectEntryRequiredness.Unspecified)
            {
                EditorGUILayout.HelpBox("Requiredness must be explicit.", MessageType.Error);
            }

            if (_scope != null && !_scope.hasMultipleDifferentValues)
            {
                var selectedScope = (ObjectEntryScope)_scope.intValue;
                if (selectedScope == ObjectEntryScope.Route
                    && _routeOwner != null
                    && !_routeOwner.hasMultipleDifferentValues
                    && _routeOwner.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Route Owner is required for a Route-scoped Object Entry.", MessageType.Error);
                }

                if (selectedScope == ObjectEntryScope.Activity
                    && _activityOwner != null
                    && !_activityOwner.hasMultipleDifferentValues
                    && _activityOwner.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Activity Owner is required for an Activity-scoped Object Entry.", MessageType.Error);
                }
            }

            EditorGUILayout.HelpBox(
                "F13 only declares logical object entries. Object Reset, Component Reset, Player Reset, Actor Reset, Transform/Rigidbody reset and runtime binding are later phases.",
                MessageType.Info);
        }

        private void DrawOwnerField()
        {
            if (_scope == null || _scope.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Select a single Scope to edit its owner.", MessageType.Info);
                return;
            }

            switch ((ObjectEntryScope)_scope.intValue)
            {
                case ObjectEntryScope.Session:
                    EditorGUILayout.HelpBox("Session owner is resolved from the active Application Runtime. No asset owner is authored here.", MessageType.Info);
                    break;
                case ObjectEntryScope.Route:
                    EditorGUILayout.PropertyField(
                        _routeOwner,
                        new GUIContent(
                            "Route Owner",
                            "Explicit Route that owns this Object Entry. Scene and hierarchy names are not owner fallbacks."));
                    break;
                case ObjectEntryScope.Activity:
                    EditorGUILayout.PropertyField(
                        _activityOwner,
                        new GUIContent(
                            "Activity Owner",
                            "Explicit Activity that owns this Object Entry. The currently active Activity is used only to filter this authored owner."));
                    break;
            }
        }
    }
}
