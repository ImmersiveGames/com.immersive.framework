using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(RouteContentAnchor))]
    [CanEditMultipleObjects]
    internal sealed class RouteContentAnchorEditor : UnityEditor.Editor
    {
        private SerializedProperty _route;
        private SerializedProperty _anchorId;
        private SerializedProperty _kind;
        private SerializedProperty _requiredness;
        private SerializedProperty _displayName;
        private SerializedProperty _description;

        private void OnEnable()
        {
            _route = serializedObject.FindProperty("route");
            _anchorId = serializedObject.FindProperty("anchorId");
            _kind = serializedObject.FindProperty("kind");
            _requiredness = serializedObject.FindProperty("requiredness");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Route Content Anchor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Declares a passive Route-scoped Content Anchor. F7F discovery and F7H authoring validation can inspect it; it does not register, materialize, bind or instantiate runtime content.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Owner", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _route,
                new GUIContent(
                    "Route",
                    "Route asset that owns this Content Anchor. This owner is explicit; GameObject scene and hierarchy are diagnostic only."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Content Anchor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _anchorId,
                new GUIContent(
                    "Anchor Id",
                    "Explicit stable Content Anchor Id. GameObject names, hierarchy paths, scene names and scene paths are diagnostics only and are not fallback identities."));
            EditorGUILayout.PropertyField(
                _kind,
                new GUIContent(
                    "Kind",
                    "Root marks a semantic container, Slot marks a future placement/mount slot, and Point marks a semantic reference point."));
            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Authoring validation policy. F7H reports invalid authoring, but Required anchors do not block Route lifecycle yet."));

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
                "This component only declares a Route Content Anchor. It does not load scenes, create runtime roots, instantiate prefabs, spawn actors, bind cameras, bind input, save state, use pooling or provide a global registry.",
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

            var anchor = target as RouteContentAnchor;
            if (anchor == null)
            {
                return;
            }

            if (_route.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Route is missing. Route-scoped discovery and authoring validation require an explicit owner.",
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
                var route = _route.objectReferenceValue as RouteAsset;
                var routeName = route != null ? route.RouteName : _route.objectReferenceValue.name;
                EditorGUILayout.HelpBox(
                    $"This GameObject declares Route Content Anchor '{declaration.AnchorId.StableText}' for Route '{routeName}' as {declaration.Kind} / {declaration.Requiredness}.",
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

            var anchor = target as RouteContentAnchor;
            if (anchor == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            var report = FrameworkAuthoringValidator.ValidateRouteContentAnchor(anchor);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private void DrawMultiObjectStatus()
        {
            if (_route != null && _route.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "Multiple selected Route Content Anchors have different Route owners.",
                    MessageType.Info);
                return;
            }

            if (_anchorId != null && _anchorId.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "Multiple selected Route Content Anchors have different Anchor Id values.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "Multiple Route Content Anchors selected. Use Project Settings validation or QA Authoring Validation to check duplicates across loaded scenes.",
                MessageType.Info);
        }
    }
}
