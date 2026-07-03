using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityContentAnchor))]
    [CanEditMultipleObjects]
    internal sealed class ActivityContentAnchorEditor : UnityEditor.Editor
    {
        private SerializedProperty _activity;
        private SerializedProperty _anchorId;
        private SerializedProperty _kind;
        private SerializedProperty _requiredness;
        private SerializedProperty _displayName;
        private SerializedProperty _description;

        private void OnEnable()
        {
            _activity = serializedObject.FindProperty("activity");
            _anchorId = serializedObject.FindProperty("anchorId");
            _kind = serializedObject.FindProperty("kind");
            _requiredness = serializedObject.FindProperty("requiredness");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Activity Content Anchor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Declares a passive Activity-scoped Content Anchor. F9G discovery and authoring validation can inspect it; it does not register, materialize, bind or instantiate runtime content.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Owner", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _activity,
                new GUIContent(
                    "Activity",
                    "Activity asset that owns this Content Anchor. This owner is explicit; GameObject scene and hierarchy are diagnostic only."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Content Anchor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _anchorId,
                new GUIContent(
                    "Anchor Id",
                    "Explicit stable Content Anchor Id. GameObject names and hierarchy paths are diagnostics only and are not fallback identities."));
            EditorGUILayout.PropertyField(
                _kind,
                new GUIContent(
                    "Kind",
                    "Root marks a semantic container, Slot marks a future placement/mount slot, and Point marks a semantic reference point."));
            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Authoring validation policy. Required anchors are reported diagnostically only; they do not block Activity lifecycle yet."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _displayName,
                new GUIContent(
                    "Display Name",
                    "Optional human-readable label for diagnostics. It is not functional identity."));
            EditorGUILayout.PropertyField(
                _description,
                new GUIContent(
                    "Description",
                    "Optional authoring note. It has no runtime behavior."));

            DrawAuthoringStatus();
            DrawValidationReport();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This component only declares an Activity Content Anchor. It does not load scenes, create runtime roots, instantiate prefabs, spawn actors, bind cameras, bind input, save state, use pooling or provide a global registry.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAuthoringStatus()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                DrawMultiObjectStatus();
                return;
            }

            var anchor = target as ActivityContentAnchor;
            if (anchor == null)
            {
                return;
            }

            if (_activity.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Activity is missing. Activity-scoped discovery and authoring validation require an explicit owner.",
                    MessageType.Error);
                return;
            }

            if (_anchorId == null || string.IsNullOrWhiteSpace(_anchorId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Anchor Id is required. GameObject names and hierarchy paths are diagnostics only and are not fallback identities.",
                    MessageType.Error);
                return;
            }

            if (_kind != null && _kind.enumValueIndex == 0)
            {
                EditorGUILayout.HelpBox(
                    "Kind must be explicit: Root, Slot or Point.",
                    MessageType.Error);
                return;
            }

            if (anchor.TryCreateDeclaration(out var declaration))
            {
                var activity = _activity.objectReferenceValue as ActivityAsset;
                var activityName = activity != null ? activity.ActivityName : _activity.objectReferenceValue.name;
                EditorGUILayout.HelpBox(
                    $"This GameObject declares Activity Content Anchor '{declaration.AnchorId.StableText}' for Activity '{activityName}' as {declaration.Kind} / {declaration.Requiredness}.",
                    MessageType.Info);
                EditorGUILayout.HelpBox(
                    declaration.ToDiagnosticString(),
                    MessageType.None);
            }
        }

        private void DrawValidationReport()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            var anchor = target as ActivityContentAnchor;
            if (anchor == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            var report = FrameworkAuthoringValidator.ValidateActivityContentAnchor(anchor);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private void DrawMultiObjectStatus()
        {
            if (_activity != null && _activity.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "Multiple selected Activity Content Anchors have different Activity owners.",
                    MessageType.Info);
                return;
            }

            if (_anchorId != null && _anchorId.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "Multiple selected Activity Content Anchors have different Anchor Id values.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "Multiple Activity Content Anchors selected. Use Project Settings validation or QA Authoring Validation to check duplicates across loaded scenes.",
                MessageType.Info);
        }
    }
}
