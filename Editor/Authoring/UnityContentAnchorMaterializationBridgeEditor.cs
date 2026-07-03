using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(UnityContentAnchorMaterializationBridge))]
    [CanEditMultipleObjects]
    internal sealed class UnityContentAnchorMaterializationBridgeEditor : UnityEditor.Editor
    {
        private SerializedProperty _prefab;
        private SerializedProperty _anchorTransform;
        private SerializedProperty _resetLocalTransform;
        private SerializedProperty _runtimeScope;
        private SerializedProperty _runtimeOwnerId;
        private SerializedProperty _runtimeOwnerName;
        private SerializedProperty _createScopeRootIfMissing;
        private SerializedProperty _anchorScope;
        private SerializedProperty _anchorKind;
        private SerializedProperty _anchorRequiredness;
        private SerializedProperty _anchorOwnerId;
        private SerializedProperty _anchorId;
        private SerializedProperty _anchorDisplayName;
        private SerializedProperty _anchorDescription;
        private SerializedProperty _runtimeContentId;
        private SerializedProperty _resourceKey;
        private SerializedProperty _releasePolicy;
        private SerializedProperty _reason;
        private SerializedProperty _logResults;

        private void OnEnable()
        {
            _prefab = serializedObject.FindProperty("prefab");
            _anchorTransform = serializedObject.FindProperty("anchorTransform");
            _resetLocalTransform = serializedObject.FindProperty("resetLocalTransform");
            _runtimeScope = serializedObject.FindProperty("runtimeScope");
            _runtimeOwnerId = serializedObject.FindProperty("runtimeOwnerId");
            _runtimeOwnerName = serializedObject.FindProperty("runtimeOwnerName");
            _createScopeRootIfMissing = serializedObject.FindProperty("createScopeRootIfMissing");
            _anchorScope = serializedObject.FindProperty("anchorScope");
            _anchorKind = serializedObject.FindProperty("anchorKind");
            _anchorRequiredness = serializedObject.FindProperty("anchorRequiredness");
            _anchorOwnerId = serializedObject.FindProperty("anchorOwnerId");
            _anchorId = serializedObject.FindProperty("anchorId");
            _anchorDisplayName = serializedObject.FindProperty("anchorDisplayName");
            _anchorDescription = serializedObject.FindProperty("anchorDescription");
            _runtimeContentId = serializedObject.FindProperty("runtimeContentId");
            _resourceKey = serializedObject.FindProperty("resourceKey");
            _releasePolicy = serializedObject.FindProperty("releasePolicy");
            _reason = serializedObject.FindProperty("reason");
            _logResults = serializedObject.FindProperty("logResults");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Unity Content Anchor Materialization Bridge", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Explicit authored bridge for prefab materialization into a Content Anchor Transform. It only runs when called directly by ContextMenu/script/QA; it does not subscribe to Route or Activity lifecycle.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Unity Physical Inputs", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_prefab, new GUIContent("Prefab", "Explicit template/prefab GameObject to instantiate. Required."));
            EditorGUILayout.PropertyField(_anchorTransform, new GUIContent("Anchor Transform", "Explicit physical parent Transform used by the placement adapter. Required."));
            EditorGUILayout.PropertyField(_resetLocalTransform, new GUIContent("Reset Local Transform", "If enabled, the placement adapter resets local position/rotation/scale after parenting."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Owner", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_runtimeScope, new GUIContent("Runtime Scope", "RuntimeContent scope that owns the materialized instance."));
            EditorGUILayout.PropertyField(_runtimeOwnerId, new GUIContent("Runtime Owner Id", "Explicit owner id. GameObject names and paths are diagnostics only."));
            EditorGUILayout.PropertyField(_runtimeOwnerName, new GUIContent("Runtime Owner Name", "Human-readable diagnostic label; not functional identity."));
            EditorGUILayout.PropertyField(_createScopeRootIfMissing, new GUIContent("Create Scope Root If Missing", "Allows this explicit bridge call to create the logical RuntimeContent scope root if it does not exist."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Content Anchor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_anchorScope, new GUIContent("Anchor Scope", "Logical ContentAnchor scope for the declaration."));
            EditorGUILayout.PropertyField(_anchorKind, new GUIContent("Anchor Kind", "Root, Slot or Point. Unknown is invalid."));
            EditorGUILayout.PropertyField(_anchorRequiredness, new GUIContent("Requiredness", "Required/Optional policy recorded in the declaration."));
            EditorGUILayout.PropertyField(_anchorOwnerId, new GUIContent("Anchor Owner Id", "Explicit owner id for the ContentAnchor declaration."));
            EditorGUILayout.PropertyField(_anchorId, new GUIContent("Anchor Id", "Explicit ContentAnchor id. Required."));
            EditorGUILayout.PropertyField(_anchorDisplayName, new GUIContent("Display Name", "Human-readable diagnostic label."));
            EditorGUILayout.PropertyField(_anchorDescription, new GUIContent("Description", "Authoring note for diagnostics."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Content", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_runtimeContentId, new GUIContent("Runtime Content Id", "Explicit RuntimeContent id. Required and must be unique inside a bridge set for a given owner."));
            EditorGUILayout.PropertyField(_resourceKey, new GUIContent("Resource Key", "Diagnostic resource key for the prefab materialization request. Required."));
            EditorGUILayout.PropertyField(_releasePolicy, new GUIContent("Release Policy", "Logical release policy to apply on explicit release."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_reason, new GUIContent("Reason", "Default diagnostic reason for explicit bridge calls."));
            EditorGUILayout.PropertyField(_logResults, new GUIContent("Log Results", "Logs explicit materialize/release results from this component."));

            DrawValidationReport();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Guardrails: this bridge does not discover anchors, create ContentAnchor objects, destroy anchors, use Addressables, use pooling, spawn actors, join players, or bind camera/audio/save/gameplay consumers.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidationReport()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Multiple Content Anchor materialization bridges selected. Use Project Settings authoring validation to check bridge set uniqueness.", MessageType.Info);
                return;
            }

            var bridge = target as UnityContentAnchorMaterializationBridge;
            if (bridge == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            var report = FrameworkAuthoringValidator.ValidateUnityContentAnchorMaterializationBridge(bridge);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}
