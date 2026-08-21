using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ActivityContentBinding))]
    [CanEditMultipleObjects]
    internal sealed class ActivityContentBindingEditor : UnityEditor.Editor
    {
        private static readonly GUIContent HeaderLabel = new GUIContent(
            "Activity Content Binding",
            "Declares one scene-local Activity content boundary. It evaluates Activity-driven visibility and dispatches Activity lifecycle callbacks to receivers in this GameObject subtree.");

        private static readonly GUIContent ActivitiesLabel = new GUIContent(
            "Activities",
            "Activities evaluated by this local content boundary. Order is authored and preserved.");

        private static readonly GUIContent MatchModeLabel = new GUIContent(
            "Match Mode",
            "Defines whether a listed active Activity makes this content root visible or hidden.");

        private static readonly GUIContent NoActivePolicyLabel = new GUIContent(
            "No Active Activity",
            "Defines this content root visibility when no Activity is active.");

        private static readonly GUIContent LocalContentIdLabel = new GUIContent(
            "Local Content Id",
            "Stable explicit identity for this local contribution. GameObject names and hierarchy paths are diagnostic only and are not used as fallback.");

        private static readonly GUIContent RequirednessLabel = new GUIContent(
            "Requiredness",
            "Defines whether this local contribution is required or optional in the Activity content contract.");

        private static readonly GUIContent ValidateLabel = new GUIContent(
            "Validate Configuration",
            "Checks the authored configuration without modifying it.");

        private SerializedProperty _activities;
        private SerializedProperty _matchMode;
        private SerializedProperty _noActiveActivityPolicy;
        private SerializedProperty _localContentId;
        private SerializedProperty _requiredness;
        private FrameworkAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _activities = serializedObject.FindProperty("activities");
            _matchMode = serializedObject.FindProperty("matchMode");
            _noActiveActivityPolicy = serializedObject.FindProperty("noActiveActivityPolicy");
            _localContentId = serializedObject.FindProperty("localContentId");
            _requiredness = serializedObject.FindProperty("requiredness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField(HeaderLabel, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_activities, ActivitiesLabel, true);
            EditorGUILayout.PropertyField(_matchMode, MatchModeLabel);
            EditorGUILayout.PropertyField(_noActiveActivityPolicy, NoActivePolicyLabel);
            EditorGUILayout.PropertyField(_localContentId, LocalContentIdLabel);
            DrawSuggestedIdentityAction();
            EditorGUILayout.PropertyField(_requiredness, RequirednessLabel);
            bool authoringChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (authoringChanged)
            {
                _validationReport = null;
            }

            DrawImmediateIssues();
            DrawValidation();

            _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
            if (_showAdvanced)
            {
                DrawAdvanced();
            }
        }

        private void DrawSuggestedIdentityAction()
        {
            if (targets.Length != 1 ||
                _localContentId == null ||
                !string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        new GUIContent(
                            "Use Suggested Id",
                            "Creates an explicit local content identity from the selected GameObject. The action is manual and Undo-compatible."),
                        EditorStyles.miniButton,
                        GUILayout.Width(112f)))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(
                        serializedObject,
                        _localContentId,
                        FrameworkAuthoringSuggestionUtility.SuggestIdentity(
                            target,
                            "activity.local-content"),
                        "Suggest Activity Local Content Id");

                    _validationReport = null;
                }
            }
        }

        private void DrawImmediateIssues()
        {
            if (serializedObject.isEditingMultipleObjects ||
                !(target is ActivityContentBinding binding))
            {
                return;
            }

            ActivityVisibilityEvaluation evaluation = binding.EvaluateVisibility(null);
            if (!evaluation.IsValid &&
                evaluation.DiagnosticReason != "MissingLocalContentId")
            {
                EditorGUILayout.HelpBox(
                    $"Invalid Activity configuration: {evaluation.DiagnosticReason}.",
                    MessageType.Error);
            }

            if (_localContentId is { hasMultipleDifferentValues: false } &&
                string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Local Content Id is missing.",
                    MessageType.Error);
            }

            ActivityContentBinding parent = FindParentBinding(binding);
            if (parent != null)
            {
                EditorGUILayout.HelpBox(
                    $"Nested ActivityContentBinding detected under '{parent.gameObject.name}'. Keep content binding roots flat.",
                    MessageType.Warning);
            }

            int childCount = CountChildBindings(binding);
            if (childCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{childCount} child ActivityContentBinding component(s) detected. Keep content binding roots flat.",
                    MessageType.Warning);
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ValidateLabel))
                {
                    RunValidation();
                }

                GUILayout.Space(8f);
                EditorGUILayout.LabelField(
                    GetValidationStatus(),
                    EditorStyles.miniBoldLabel,
                    GUILayout.Width(92f));
            }

            if (_validationReport != null &&
                (_validationReport.ErrorCount > 0 ||
                 _validationReport.WarningCount > 0))
            {
                FrameworkAuthoringValidationGui.DrawIssues(
                    _validationReport,
                    false);
            }
        }

        private void RunValidation()
        {
            _validationReport = new FrameworkAuthoringValidationReport();

            for (int index = 0; index < targets.Length; index++)
            {
                _validationReport.AddRange(
                    FrameworkAuthoringValidator.ValidateActivityContentBinding(
                        targets[index] as ActivityContentBinding));
            }
        }

        private string GetValidationStatus()
        {
            if (_validationReport == null)
            {
                return "Not Validated";
            }

            if (_validationReport.ErrorCount > 0)
            {
                return "Invalid";
            }

            if (_validationReport.WarningCount > 0)
            {
                return "Warning";
            }

            return "Valid";
        }

        private void DrawAdvanced()
        {
            if (targets.Length != 1 ||
                !(target is ActivityContentBinding binding))
            {
                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Runtime Evidence",
                        "Technical evidence is shown only for a single selected binding."),
                    "Single selection only");
                return;
            }

            EditorGUI.indentLevel++;

            ActivityVisibilityEvaluation evaluation = binding.EvaluateVisibility(null);

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Desired Visibility",
                    "Visibility result produced by the current authored rule when evaluated with no active Activity."),
                evaluation.DesiredVisibility.ToString());

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Diagnostic",
                    "Technical result of the current Activity visibility evaluation."),
                evaluation.DiagnosticReason);

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Normalized Local Content Id",
                    "Normalized stable identity used by the framework for this local contribution."),
                binding.HasExplicitLocalContentId
                    ? binding.LocalContentIdText
                    : "<missing>");

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Local Scope Kind",
                    "Technical scope classification used by local contribution discovery."),
                binding.LocalScopeKind.ToString());

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Scene",
                    "Scene that currently owns this binding."),
                binding.gameObject.scene.IsValid()
                    ? binding.gameObject.scene.name
                    : "<no scene>");

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Runtime Status",
                    "Current active state of this content root while the application is running."),
                Application.isPlaying
                    ? binding.gameObject.activeSelf
                        ? "Visible"
                        : "Hidden"
                    : "Not Available in Edit Mode");

            EditorGUI.indentLevel--;
        }

        private static ActivityContentBinding FindParentBinding(
            ActivityContentBinding binding)
        {
            for (Transform parent = binding.transform.parent;
                 parent != null;
                 parent = parent.parent)
            {
                if (parent.TryGetComponent(
                        out ActivityContentBinding parentBinding))
                {
                    return parentBinding;
                }
            }

            return null;
        }

        private static int CountChildBindings(
            ActivityContentBinding binding)
        {
            ActivityContentBinding[] bindings =
                binding.GetComponentsInChildren<ActivityContentBinding>(true);

            int count = 0;
            for (int index = 0; index < bindings.Length; index++)
            {
                if (bindings[index] != null &&
                    bindings[index] != binding)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
