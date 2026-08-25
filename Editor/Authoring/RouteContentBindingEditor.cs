using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(RouteContentContribution))]
    [CanEditMultipleObjects]
    internal sealed class RouteContentContributionEditor : UnityEditor.Editor
    {
        private static readonly GUIContent HeaderLabel = new GUIContent(
            "Route Content Contribution",
            "Declares one scene-local Route content boundary. Route lifecycle callbacks are dispatched to receivers in this GameObject subtree.");

        private static readonly GUIContent RouteLabel = new GUIContent(
            "Route",
            "Route that owns this scene-authored content boundary.");

        private static readonly GUIContent LocalContentIdLabel = new GUIContent(
            "Local Content Id",
            "Stable explicit identity for this local contribution. GameObject names and hierarchy paths are diagnostic only and are not used as fallback.");

        private static readonly GUIContent RequirednessLabel = new GUIContent(
            "Requiredness",
            "Defines whether this local contribution is required or optional for framework content/readiness consumers.");

        private static readonly GUIContent ValidateLabel = new GUIContent(
            "Validate Configuration",
            "Checks the authored configuration without modifying it.");

        private SerializedProperty _route;
        private SerializedProperty _localContentId;
        private SerializedProperty _requiredness;
        private FrameworkAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _route = serializedObject.FindProperty("route");
            _localContentId = serializedObject.FindProperty("localContentId");
            _requiredness = serializedObject.FindProperty("requiredness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField(HeaderLabel, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_route, RouteLabel);
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
                            "route.local-content"),
                        "Suggest Route Local Content Id");

                    _validationReport = null;
                }
            }
        }

        private void DrawImmediateIssues()
        {
            if (_route != null &&
                !_route.hasMultipleDifferentValues &&
                _route.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Route is missing.",
                    MessageType.Error);
            }

            if (_localContentId != null &&
                !_localContentId.hasMultipleDifferentValues &&
                string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Local Content Id is missing.",
                    MessageType.Error);
            }

            if (serializedObject.isEditingMultipleObjects ||
                !(target is RouteContentContribution binding))
            {
                return;
            }

            RouteContentContribution parent = FindParentBinding(binding);
            if (parent != null)
            {
                EditorGUILayout.HelpBox(
                    $"Nested Route Content Contribution detected under '{parent.gameObject.name}'. Keep content binding roots flat.",
                    MessageType.Warning);
            }

            int childCount = CountChildBindings(binding);
            if (childCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{childCount} child Route Content Contribution component(s) detected. Keep content binding roots flat.",
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
                    FrameworkAuthoringValidator.ValidateRouteContentContribution(
                        targets[index] as RouteContentContribution));
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
                !(target is RouteContentContribution binding))
            {
                DrawLabelValue(
                    new GUIContent(
                        "Runtime Evidence",
                        "Technical evidence is shown only for a single selected binding."),
                    "Single selection only");
                return;
            }

            EditorGUI.indentLevel++;

            DrawLabelValue(
                new GUIContent(
                    "Normalized Local Content Id",
                    "Normalized stable identity used by the framework for this local contribution."),
                binding.HasExplicitLocalContentId
                    ? binding.LocalContentIdText
                    : "<missing>");

            DrawLabelValue(
                new GUIContent(
                    "Local Scope Kind",
                    "Technical scope classification used by local contribution discovery."),
                binding.LocalScopeKind.ToString());

            DrawLabelValue(
                new GUIContent(
                    "Scene",
                    "Scene that currently owns this binding."),
                binding.gameObject.scene.IsValid()
                    ? binding.gameObject.scene.name
                    : "<no scene>");

            EditorGUI.indentLevel--;
        }

        private static void DrawLabelValue(
            GUIContent label,
            string value)
        {
            EditorGUILayout.LabelField(
                label,
                new GUIContent(value ?? string.Empty));
        }

        private static RouteContentContribution FindParentBinding(
            RouteContentContribution binding)
        {
            for (Transform parent = binding.transform.parent;
                 parent != null;
                 parent = parent.parent)
            {
                if (parent.TryGetComponent(
                        out RouteContentContribution parentBinding))
                {
                    return parentBinding;
                }
            }

            return null;
        }

        private static int CountChildBindings(
            RouteContentContribution binding)
        {
            RouteContentContribution[] bindings =
                binding.GetComponentsInChildren<RouteContentContribution>(true);

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
