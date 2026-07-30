using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Editor.Common;
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

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _validationOutdated;
        private bool _advanced;

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

            RouteContentAnchor anchor =
                target as RouteContentAnchor;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Route Content Anchor",
                "Declares a passive semantic location owned by one Route. " +
                "It does not register, materialize, bind, move or instantiate runtime content.");

            FrameworkAuthoringInspectorGui.IntentSummary(
                BuildIntentSummary());

            DrawPrimaryAuthoring();
            DrawIdentity(anchor);
            DrawPresentation();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (modified &&
                _lastValidationReport != null)
            {
                _validationOutdated = true;
            }

            DrawConfigurationStatus(anchor);
            DrawAuthoringValidation(anchor);

            _advanced =
                FrameworkAuthoringInspectorGui.AdvancedFoldout(
                    _advanced);

            if (_advanced)
            {
                DrawAdvanced(anchor);
            }
        }

        private void DrawPrimaryAuthoring()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Primary Authoring");

            EditorGUILayout.PropertyField(
                _route,
                new GUIContent(
                    "Owner Route",
                    "The RouteAsset that explicitly owns this anchor. " +
                    "Scene and hierarchy are diagnostics only and never replace ownership."));

            FrameworkAuthoringInspectorGui.Section(
                "Anchor Intent");

            EditorGUILayout.PropertyField(
                _kind,
                new GUIContent(
                    "Kind",
                    "Root declares a semantic container, Slot declares a future placement or mount location, and Point declares a semantic reference point."));

            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Authoring validation policy. Required anchors are diagnostic declarations and do not currently block Route lifecycle."));

            EditorGUILayout.HelpBox(
                "Requiredness is not Activity Readiness. " +
                "Required Content Anchors are reported by validation, but do not currently block Route entry.",
                MessageType.None);
        }

        private void DrawIdentity(
            RouteContentAnchor anchor)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Identity");

            EditorGUILayout.PropertyField(
                _anchorId,
                new GUIContent(
                    "Anchor ID",
                    "Explicit stable functional identity. " +
                    "GameObject names, hierarchy paths and scene names are diagnostics only."));

            if (serializedObject.isEditingMultipleObjects ||
                anchor == null)
            {
                EditorGUILayout.HelpBox(
                    "Select one Route Content Anchor to generate an explicit identity suggestion.",
                    MessageType.Info);
                return;
            }

            RouteAsset owner =
                _route.objectReferenceValue as RouteAsset;
            ContentAnchorKind kind =
                (ContentAnchorKind)_kind.intValue;
            string suggestion =
                ContentAnchorAuthoringSuggestionUtility
                    .SuggestRouteAnchorId(
                        anchor,
                        owner,
                        kind);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Suggested ID",
                    suggestion);
            }

            using (new EditorGUI.DisabledScope(
                       !string.IsNullOrWhiteSpace(
                           _anchorId.stringValue)))
            {
                if (GUILayout.Button(
                        "Use Suggested ID"))
                {
                    FrameworkAuthoringInspectorGui
                        .ApplySuggestion(
                            serializedObject,
                            _anchorId,
                            suggestion,
                            "Suggest Route Content Anchor ID");

                    _validationOutdated =
                        _lastValidationReport != null;
                    serializedObject.Update();
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    _anchorId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Identity generation never overwrites a populated Anchor ID. " +
                    "Edit the field deliberately when a migration is required.",
                    MessageType.None);
            }
        }

        private void DrawPresentation()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Presentation");

            EditorGUILayout.PropertyField(
                _displayName,
                new GUIContent(
                    "Display Name",
                    "Optional human-readable label used by diagnostics. It is not functional identity."));

            EditorGUILayout.PropertyField(
                _description,
                new GUIContent(
                    "Description",
                    "Optional authoring note. It has no runtime behavior."));
        }

        private void DrawConfigurationStatus(
            RouteContentAnchor anchor)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox(
                    "Multiple Route Content Anchors selected. " +
                    "Inspect one declaration at a time for owner, identity and corrective guidance.",
                    MessageType.Info);
                return;
            }

            ContentAnchorAuthoringValidationResult result =
                ContentAnchorAuthoringValidator.Validate(
                    anchor);

            EditorGUILayout.HelpBox(
                result.Message,
                result.IsValid
                    ? MessageType.Info
                    : MessageType.Error);

            if (!result.IsValid)
            {
                EditorGUILayout.HelpBox(
                    $"Impact: {result.Impact}\n" +
                    $"Corrective action: {result.CorrectiveAction}",
                    MessageType.Warning);
            }
        }

        private void DrawAuthoringValidation(
            RouteContentAnchor anchor)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Authoring Validation");

            EditorGUILayout.HelpBox(
                "Validation is explicit and non-mutating. " +
                "It checks authoring and scene ownership but does not repair data or query runtime discovery.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(
                       serializedObject.isEditingMultipleObjects ||
                       anchor == null))
            {
                if (GUILayout.Button(
                        "Validate Route Content Anchor"))
                {
                    RunAuthoringValidation();
                }
            }

            if (_lastValidationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "Validation has not been run for this Inspector instance.",
                    MessageType.None);
                return;
            }

            if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Authoring changed after the last validation. Run validation again.",
                    MessageType.Warning);
            }

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastValidationReport);
            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);
        }

        private void RunAuthoringValidation()
        {
            RouteContentAnchor anchor =
                target as RouteContentAnchor;

            _lastValidationReport =
                FrameworkAuthoringValidator
                    .ValidateRouteContentAnchor(
                        anchor);
            _validationOutdated = false;
        }

        private void DrawAdvanced(
            RouteContentAnchor anchor)
        {
            if (serializedObject.isEditingMultipleObjects ||
                anchor == null)
            {
                EditorGUILayout.HelpBox(
                    "Select one Route Content Anchor to inspect technical diagnostics.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Scope",
                    anchor.Scope.ToString());

                EditorGUILayout.TextField(
                    "Owner Stable ID",
                    anchor.Route != null &&
                    anchor.Route.HasValidRouteId
                        ? anchor.Route.RouteId.StableText
                        : "<missing or invalid>");

                EditorGUILayout.TextField(
                    "Normalized Anchor ID",
                    anchor.AnchorIdText);

                EditorGUILayout.TextField(
                    "GameObject",
                    anchor.ObjectName);

                EditorGUILayout.TextField(
                    "Scene",
                    anchor.SceneName);

                EditorGUILayout.TextField(
                    "Resource Path",
                    anchor.ResourcePath);
            }

            if (anchor.TryCreateDeclaration(
                    out ContentAnchorDeclaration declaration))
            {
                EditorGUILayout.HelpBox(
                    declaration.ToDiagnosticString(),
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Canonical declaration is unavailable until authoring is valid.",
                    MessageType.None);
            }

            EditorGUILayout.HelpBox(
                "This section shows authored technical evidence only. " +
                "Runtime discovery, acceptance, mismatch and duplication require Play Mode or QA evidence.",
                MessageType.None);
        }

        private string BuildIntentSummary()
        {
            RouteAsset owner =
                _route.objectReferenceValue as RouteAsset;
            ContentAnchorKind kind =
                (ContentAnchorKind)_kind.intValue;
            ContentAnchorRequiredness requiredness =
                (ContentAnchorRequiredness)
                    _requiredness.intValue;

            string ownerLabel =
                owner != null
                    ? owner.RouteName
                    : "<unassigned Route>";
            string requirement =
                requiredness ==
                ContentAnchorRequiredness.Required
                    ? "required"
                    : "optional";

            return
                $"Declare a {requirement} {kind} anchor owned by Route '{ownerLabel}'.";
        }
    }
}
