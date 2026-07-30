using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Editor.Common;
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

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _validationOutdated;
        private bool _advanced;

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

            ActivityContentAnchor anchor =
                target as ActivityContentAnchor;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Activity Content Anchor",
                "Declares a passive semantic location owned by one Activity. " +
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
                _activity,
                new GUIContent(
                    "Owner Activity",
                    "The ActivityAsset that explicitly owns this anchor. " +
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
                    "Authoring validation policy. Required anchors are diagnostic declarations and do not currently block Activity lifecycle."));

            EditorGUILayout.HelpBox(
                "Requiredness is not Activity Readiness. " +
                "Required Content Anchors are reported by validation, but do not currently block Activity entry.",
                MessageType.None);
        }

        private void DrawIdentity(
            ActivityContentAnchor anchor)
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
                    "Select one Activity Content Anchor to generate an explicit identity suggestion.",
                    MessageType.Info);
                return;
            }

            ActivityAsset owner =
                _activity.objectReferenceValue as ActivityAsset;
            ContentAnchorKind kind =
                (ContentAnchorKind)_kind.intValue;
            string suggestion =
                ContentAnchorAuthoringSuggestionUtility
                    .SuggestActivityAnchorId(
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
                            "Suggest Activity Content Anchor ID");

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
            ActivityContentAnchor anchor)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox(
                    "Multiple Activity Content Anchors selected. " +
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
            ActivityContentAnchor anchor)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Authoring Validation");

            EditorGUILayout.HelpBox(
                "Validation is explicit and non-mutating. " +
                "It checks authoring but does not repair data or query runtime discovery.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(
                       serializedObject.isEditingMultipleObjects ||
                       anchor == null))
            {
                if (GUILayout.Button(
                        "Validate Activity Content Anchor"))
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
            ActivityContentAnchor anchor =
                target as ActivityContentAnchor;

            _lastValidationReport =
                FrameworkAuthoringValidator
                    .ValidateActivityContentAnchor(
                        anchor);
            _validationOutdated = false;
        }

        private void DrawAdvanced(
            ActivityContentAnchor anchor)
        {
            if (serializedObject.isEditingMultipleObjects ||
                anchor == null)
            {
                EditorGUILayout.HelpBox(
                    "Select one Activity Content Anchor to inspect technical diagnostics.",
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
                    anchor.Activity != null &&
                    anchor.Activity.HasValidActivityId
                        ? anchor.Activity.ActivityId.StableText
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
            ActivityAsset owner =
                _activity.objectReferenceValue as ActivityAsset;
            ContentAnchorKind kind =
                (ContentAnchorKind)_kind.intValue;
            ContentAnchorRequiredness requiredness =
                (ContentAnchorRequiredness)
                    _requiredness.intValue;

            string ownerLabel =
                owner != null
                    ? owner.ActivityName
                    : "<unassigned Activity>";
            string requirement =
                requiredness ==
                ContentAnchorRequiredness.Required
                    ? "required"
                    : "optional";

            return
                $"Declare a {requirement} {kind} anchor owned by Activity '{ownerLabel}'.";
        }
    }
}
