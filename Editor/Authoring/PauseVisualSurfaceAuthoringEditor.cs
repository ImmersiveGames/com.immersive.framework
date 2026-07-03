using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(PauseVisualSurfaceAuthoring))]
    [CanEditMultipleObjects]
    internal sealed class PauseVisualSurfaceAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty _surfaceId;
        private SerializedProperty _surfaceKind;
        private SerializedProperty _pauseState;
        private SerializedProperty _visualPrefab;
        private SerializedProperty _resetLocalTransform;
        private SerializedProperty _runtimeScope;
        private SerializedProperty _runtimeOwnerId;
        private SerializedProperty _runtimeOwnerName;
        private SerializedProperty _runtimeContentId;
        private SerializedProperty _resourceKey;
        private SerializedProperty _releasePolicy;
        private SerializedProperty _anchorScope;
        private SerializedProperty _anchorKind;
        private SerializedProperty _requiredness;
        private SerializedProperty _anchorOwnerId;
        private SerializedProperty _anchorId;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _reason;

        private void OnEnable()
        {
            _surfaceId = serializedObject.FindProperty("surfaceId");
            _surfaceKind = serializedObject.FindProperty("surfaceKind");
            _pauseState = serializedObject.FindProperty("pauseState");
            _visualPrefab = serializedObject.FindProperty("visualPrefab");
            _resetLocalTransform = serializedObject.FindProperty("resetLocalTransform");
            _runtimeScope = serializedObject.FindProperty("runtimeScope");
            _runtimeOwnerId = serializedObject.FindProperty("runtimeOwnerId");
            _runtimeOwnerName = serializedObject.FindProperty("runtimeOwnerName");
            _runtimeContentId = serializedObject.FindProperty("runtimeContentId");
            _resourceKey = serializedObject.FindProperty("resourceKey");
            _releasePolicy = serializedObject.FindProperty("releasePolicy");
            _anchorScope = serializedObject.FindProperty("anchorScope");
            _anchorKind = serializedObject.FindProperty("anchorKind");
            _requiredness = serializedObject.FindProperty("requiredness");
            _anchorOwnerId = serializedObject.FindProperty("anchorOwnerId");
            _anchorId = serializedObject.FindProperty("anchorId");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Pause Visual Surface Authoring", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Declares a passive Pause visual surface contract and binding-request source for a future ContentAnchor consumer. It does not materialize UI, execute binding, subscribe to Pause input, change InputMode, change Time.timeScale or control Route/Activity lifecycle.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Pause Visual Surface", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_surfaceId, new GUIContent("Surface Id", "Stable Pause visual surface id. GameObject names are diagnostics only."));
            EditorGUILayout.PropertyField(_surfaceKind, new GUIContent("Surface Kind", "OverlayRoot or MenuRoot. Unknown is invalid."));
            EditorGUILayout.PropertyField(_pauseState, new GUIContent("Pause State", "The current Pause visual consumer path supports only the Paused visual state."));
            EditorGUILayout.PropertyField(_visualPrefab, new GUIContent("Visual Prefab", "Explicit future visual prefab/template. Required, but not instantiated by this component."));
            EditorGUILayout.PropertyField(_resetLocalTransform, new GUIContent("Reset Local Transform", "Authoring preference for the future placement request."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Content", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_runtimeScope, new GUIContent("Runtime Scope", "RuntimeContent scope that will own the visual content when a later cut materializes it."));
            EditorGUILayout.PropertyField(_runtimeOwnerId, new GUIContent("Runtime Owner Id", "Explicit owner id. Names and hierarchy paths are diagnostics only."));
            EditorGUILayout.PropertyField(_runtimeOwnerName, new GUIContent("Runtime Owner Name", "Human-readable diagnostic label."));
            EditorGUILayout.PropertyField(_runtimeContentId, new GUIContent("Runtime Content Id", "Explicit RuntimeContent id for the future visual materialization."));
            EditorGUILayout.PropertyField(_resourceKey, new GUIContent("Resource Key", "Diagnostic resource key for the visual prefab/template."));
            EditorGUILayout.PropertyField(_releasePolicy, new GUIContent("Release Policy", "Future logical release policy. F10B does not execute release."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Content Anchor Requirement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_anchorScope, new GUIContent("Anchor Scope", "ContentAnchor scope targeted by the derived binding request."));
            EditorGUILayout.PropertyField(_anchorKind, new GUIContent("Anchor Kind", "Root, Slot or Point. Unknown is invalid."));
            EditorGUILayout.PropertyField(_requiredness, new GUIContent("Requiredness", "Required/Optional policy for the future Pause visual anchor request."));
            EditorGUILayout.PropertyField(_anchorOwnerId, new GUIContent("Anchor Owner Id", "Explicit ContentAnchor owner id. Required by F10C binding request derivation; anchor id alone is not enough."));
            EditorGUILayout.PropertyField(_anchorId, new GUIContent("Anchor Id", "Explicit ContentAnchor id."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name", "Human-readable diagnostic label."));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description", "Authoring note for diagnostics."));
            EditorGUILayout.PropertyField(_reason, new GUIContent("Reason", "Default diagnostic reason for the authored contract."));

            DrawAuthoringStatus();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Guardrails: this component is not a Pause surface adapter and does not use Addressables, pooling, actor spawn, player join, camera, audio, save/progression or gameplay consumers.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAuthoringStatus()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Multiple Pause Visual Surface Authoring components selected. Use the QA smoke or scene review to validate individual contracts.", MessageType.Info);
                return;
            }

            var authoring = target as PauseVisualSurfaceAuthoring;
            if (authoring == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Authoring Contract", EditorStyles.boldLabel);
            if (authoring.TryCreateContract(out var contract, out var message))
            {
                EditorGUILayout.HelpBox("Pause visual surface contract is valid. F10C can derive a ContentAnchor binding request from it, but binding/materialization still require explicit later execution.", MessageType.Info);
                EditorGUILayout.HelpBox(contract.ToDiagnosticString(), MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(message, MessageType.Error);
        }
    }
}
