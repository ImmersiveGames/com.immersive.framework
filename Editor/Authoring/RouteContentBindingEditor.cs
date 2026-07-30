using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Editor.Validation;
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

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Route Content Binding",
                "Declares one scene-authored Route content boundary and dispatches local Route enter/exit callbacks.");
            FrameworkAuthoringInspectorGui.IntentSummary(BuildIntentSummary());

            EditorGUI.BeginChangeCheck();
            DrawPrimaryAuthoring();
            bool authoringChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();
            if (authoringChanged)
            {
                _validationReport = null;
            }

            DrawSuggestedIdentityAction();
            DrawConfigurationStatus();
            DrawValidation();

            _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
            if (_showAdvanced)
            {
                DrawAdvanced();
            }
        }

        private void DrawPrimaryAuthoring()
        {
            FrameworkAuthoringInspectorGui.Section("Route Binding");
            EditorGUILayout.PropertyField(
                _route,
                new GUIContent(
                    "Route",
                    "Route that owns this scene-authored content. Normally this is the Route whose Primary Scene contains the binding."));
            DrawSelectAssetAction(_route, "Select Route Asset");

            FrameworkAuthoringInspectorGui.Section("Local Content Identity");
            EditorGUILayout.PropertyField(
                _localContentId,
                new GUIContent(
                    "Local Content Id",
                    "Explicit identity for this local contribution. GameObject names and hierarchy paths are diagnostics only."));
            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Declares whether this contribution is required or optional for consumers that evaluate local content readiness."));
        }

        private string BuildIntentSummary()
        {
            if (_route == null || _localContentId == null || _requiredness == null)
            {
                return "Configure a Route, explicit local identity and requiredness for this scene-authored contribution.";
            }

            if (_route.hasMultipleDifferentValues ||
                _localContentId.hasMultipleDifferentValues ||
                _requiredness.hasMultipleDifferentValues)
            {
                return "The selected bindings contain mixed Route, identity or requiredness values.";
            }

            string routeName = _route.objectReferenceValue != null
                ? _route.objectReferenceValue.name
                : "<missing Route>";
            string identity = string.IsNullOrWhiteSpace(_localContentId.stringValue)
                ? "<missing local identity>"
                : _localContentId.stringValue.Trim();

            return $"Bind this scene-authored content to Route '{routeName}' as {GetRequirednessLabel()} local content '{identity}'.";
        }

        private void DrawSuggestedIdentityAction()
        {
            if (targets.Length != 1 ||
                _localContentId == null ||
                !string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                return;
            }

            if (GUILayout.Button("Use Suggested Local Content Id"))
            {
                FrameworkAuthoringInspectorGui.ApplySuggestion(
                    serializedObject,
                    _localContentId,
                    FrameworkAuthoringSuggestionUtility.SuggestIdentity(target, "route.local-content"),
                    "Suggest Route Local Content Id");
                _validationReport = null;
            }
        }

        private void DrawConfigurationStatus()
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");

            if (serializedObject.isEditingMultipleObjects &&
                (_route.hasMultipleDifferentValues ||
                 _localContentId.hasMultipleDifferentValues ||
                 _requiredness.hasMultipleDifferentValues))
            {
                EditorGUILayout.HelpBox(
                    "The selected bindings use mixed authoring values. Validate to review each binding independently.",
                    MessageType.Info);
                return;
            }

            bool hasError = false;
            if (_route.objectReferenceValue == null)
            {
                hasError = true;
                EditorGUILayout.HelpBox(
                    "Route is missing. Assign the Route that owns this scene-authored content.",
                    MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                hasError = true;
                EditorGUILayout.HelpBox(
                    "Local Content Id is missing. Enter an explicit identity or use the suggested value.",
                    MessageType.Error);
            }

            if (!serializedObject.isEditingMultipleObjects && target is RouteContentBinding binding)
            {
                RouteContentBinding parent = FindParentBinding(binding);
                if (parent != null)
                {
                    EditorGUILayout.HelpBox(
                        $"A parent GameObject also contains a Route Content Binding ('{parent.gameObject.name}'). Nested Route content ownership is not defined; keep binding roots flat.",
                        MessageType.Warning);
                }

                int childCount = CountChildBindings(binding);
                if (childCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"This GameObject contains {childCount} child Route Content Binding component(s). Nested Route content ownership is not defined; keep binding roots flat.",
                        MessageType.Warning);
                }
            }

            if (!hasError)
            {
                EditorGUILayout.HelpBox(
                    "Ready for authoring validation. Route Content Binding dispatches lifecycle callbacks; it does not control GameObject visibility.",
                    MessageType.Info);
            }
        }

        private void DrawValidation()
        {
            FrameworkAuthoringInspectorGui.Section("Validation");
            if (GUILayout.Button("Validate Configuration"))
            {
                _validationReport = new FrameworkAuthoringValidationReport();
                for (int index = 0; index < targets.Length; index++)
                {
                    _validationReport.AddRange(
                        FrameworkAuthoringValidator.ValidateRouteContentBinding(
                            targets[index] as RouteContentBinding));
                }
            }

            if (_validationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "Validation is explicit and non-mutating. Run it after changing the binding or identity.",
                    MessageType.None);
                return;
            }

            FrameworkAuthoringValidationGui.DrawSummary(_validationReport);
            FrameworkAuthoringValidationGui.DrawIssues(_validationReport, false);
        }

        private void DrawAdvanced()
        {
            if (targets.Length != 1 || !(target is RouteContentBinding binding))
            {
                EditorGUILayout.HelpBox(
                    "Advanced evidence is available for one selected binding at a time.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Assigned Route", binding.Route != null ? binding.Route.name : "<missing>");
            EditorGUILayout.LabelField("Normalized Local Content Id", binding.HasExplicitLocalContentId ? binding.LocalContentIdText : "<missing>");
            EditorGUILayout.LabelField("Requiredness", binding.Requiredness.ToString());
            EditorGUILayout.LabelField("Local Scope Kind", binding.LocalScopeKind.ToString());
            EditorGUILayout.LabelField("Scene", binding.gameObject.scene.IsValid() ? binding.gameObject.scene.name : "<no scene>");
            EditorGUILayout.HelpBox(
                "This component is a Route lifecycle boundary. Visibility remains consumer-authored through explicit receivers or other components.",
                MessageType.None);
        }

        private void DrawSelectAssetAction(SerializedProperty property, string label)
        {
            if (serializedObject.isEditingMultipleObjects ||
                property == null ||
                property.hasMultipleDifferentValues ||
                property.objectReferenceValue == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(label, GUILayout.Width(160f)))
                {
                    Selection.activeObject = property.objectReferenceValue;
                }
            }
        }

        private string GetRequirednessLabel()
        {
            return _requiredness != null && !_requiredness.hasMultipleDifferentValues
                ? _requiredness.enumDisplayNames[_requiredness.enumValueIndex]
                : "Mixed";
        }

        private static RouteContentBinding FindParentBinding(RouteContentBinding binding)
        {
            for (Transform parent = binding.transform.parent; parent != null; parent = parent.parent)
            {
                if (parent.TryGetComponent(out RouteContentBinding parentBinding))
                {
                    return parentBinding;
                }
            }

            return null;
        }

        private static int CountChildBindings(RouteContentBinding binding)
        {
            RouteContentBinding[] bindings = binding.GetComponentsInChildren<RouteContentBinding>(true);
            int count = 0;
            for (int index = 0; index < bindings.Length; index++)
            {
                if (bindings[index] != null && bindings[index] != binding)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
