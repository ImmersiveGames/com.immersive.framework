using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(RouteContentBinding))]
    [CanEditMultipleObjects]
    internal sealed class RouteContentBindingEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _route;
        private SerializedProperty _localContentId;
        private SerializedProperty _requiredness;
        private FrameworkAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _route =
                serializedObject.FindProperty("route");

            _localContentId =
                serializedObject.FindProperty("localContentId");

            _requiredness =
                serializedObject.FindProperty("requiredness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Route Content Binding",
                "Declares one scene-local Route content boundary and dispatches Route enter/exit callbacks.");

            FrameworkAuthoringInspectorGui.IntentSummary(
                BuildIntentSummary());

            EditorGUI.BeginChangeCheck();
            DrawPrimaryAuthoring();
            bool authoringChanged =
                EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (authoringChanged)
            {
                _validationReport = null;
            }

            DrawConfigurationStatus();
            DrawValidation();

            _showAdvanced =
                FrameworkAuthoringInspectorGui.AdvancedFoldout(
                    _showAdvanced);

            if (_showAdvanced)
            {
                DrawAdvanced();
            }
        }

        private void DrawPrimaryAuthoring()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Route Binding");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(
                    _route,
                    new GUIContent(
                        "Route",
                        "Route that owns this scene-authored contribution."));

                DrawSelectAssetButton(_route);
            }

            FrameworkAuthoringInspectorGui.Section(
                "Local Content");

            EditorGUILayout.PropertyField(
                _localContentId,
                new GUIContent(
                    "Local Content Id",
                    "Explicit identity for this local contribution. GameObject names and hierarchy paths are diagnostic only."));

            DrawSuggestedIdentityAction();

            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Declares whether this contribution is required or optional for readiness consumers."));
        }

        private string BuildIntentSummary()
        {
            if (_route == null ||
                _localContentId == null ||
                _requiredness == null)
            {
                return
                    "Configure Route ownership, local identity and requiredness.";
            }

            if (_route.hasMultipleDifferentValues ||
                _localContentId.hasMultipleDifferentValues ||
                _requiredness.hasMultipleDifferentValues)
            {
                return
                    "Selected bindings contain mixed authoring values.";
            }

            string routeName =
                _route.objectReferenceValue != null
                    ? _route.objectReferenceValue.name
                    : "<missing Route>";

            string identity =
                string.IsNullOrWhiteSpace(
                    _localContentId.stringValue)
                    ? "<missing Id>"
                    : _localContentId.stringValue.Trim();

            return
                $"{GetRequirednessLabel()} local content '{identity}' owned by Route '{routeName}'.";
        }

        private void DrawSuggestedIdentityAction()
        {
            if (targets.Length != 1 ||
                _localContentId == null ||
                !string.IsNullOrWhiteSpace(
                    _localContentId.stringValue))
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        "Use Suggested Id",
                        EditorStyles.miniButton,
                        GUILayout.Width(112f)))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(
                        serializedObject,
                        _localContentId,
                        FrameworkAuthoringSuggestionUtility
                            .SuggestIdentity(
                                target,
                                "route.local-content"),
                        "Suggest Route Local Content Id");

                    _validationReport = null;
                }
            }
        }

        private void DrawConfigurationStatus()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            if (serializedObject.isEditingMultipleObjects &&
                (_route.hasMultipleDifferentValues ||
                 _localContentId.hasMultipleDifferentValues ||
                 _requiredness.hasMultipleDifferentValues))
            {
                FrameworkAuthoringInspectorGui.Status(
                    "Mixed values");
                return;
            }

            bool hasIssue = false;

            if (_route.objectReferenceValue == null)
            {
                hasIssue = true;

                EditorGUILayout.HelpBox(
                    "Route is missing.",
                    MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(
                    _localContentId.stringValue))
            {
                hasIssue = true;

                EditorGUILayout.HelpBox(
                    "Local Content Id is missing.",
                    MessageType.Error);
            }

            if (!serializedObject.isEditingMultipleObjects &&
                target is RouteContentBinding binding)
            {
                RouteContentBinding parent =
                    FindParentBinding(binding);

                if (parent != null)
                {
                    hasIssue = true;

                    EditorGUILayout.HelpBox(
                        $"Nested Route Content Binding detected under '{parent.gameObject.name}'. Keep Route content roots flat.",
                        MessageType.Warning);
                }

                int childCount =
                    CountChildBindings(binding);

                if (childCount > 0)
                {
                    hasIssue = true;

                    EditorGUILayout.HelpBox(
                        $"{childCount} child Route Content Binding component(s) detected. Keep Route content roots flat.",
                        MessageType.Warning);
                }
            }

            if (!hasIssue)
            {
                FrameworkAuthoringInspectorGui.Status("Ready");
            }
        }

        private void DrawValidation()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Validation");

            FrameworkAuthoringValidationGui.DrawSummary(
                _validationReport);

            if (GUILayout.Button("Validate Configuration"))
            {
                _validationReport =
                    new FrameworkAuthoringValidationReport();

                for (int index = 0;
                     index < targets.Length;
                     index++)
                {
                    _validationReport.AddRange(
                        FrameworkAuthoringValidator
                            .ValidateRouteContentBinding(
                                targets[index]
                                    as RouteContentBinding));
                }
            }

            FrameworkAuthoringValidationGui.DrawIssues(
                _validationReport,
                false);
        }

        private void DrawAdvanced()
        {
            if (targets.Length != 1 ||
                !(target is RouteContentBinding binding))
            {
                EditorGUILayout.LabelField(
                    "Runtime Evidence",
                    "Single selection only");
                return;
            }

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(
                "Assigned Route",
                binding.Route != null
                    ? binding.Route.name
                    : "<missing>");

            EditorGUILayout.LabelField(
                "Normalized Local Content Id",
                binding.HasExplicitLocalContentId
                    ? binding.LocalContentIdText
                    : "<missing>");

            EditorGUILayout.LabelField(
                "Requiredness",
                binding.Requiredness.ToString());

            EditorGUILayout.LabelField(
                "Local Scope Kind",
                binding.LocalScopeKind.ToString());

            EditorGUILayout.LabelField(
                "Scene",
                binding.gameObject.scene.IsValid()
                    ? binding.gameObject.scene.name
                    : "<no scene>");

            EditorGUI.indentLevel--;
        }

        private void DrawSelectAssetButton(
            SerializedProperty property)
        {
            if (serializedObject.isEditingMultipleObjects ||
                property == null ||
                property.hasMultipleDifferentValues ||
                property.objectReferenceValue == null)
            {
                return;
            }

            if (GUILayout.Button(
                    "Select",
                    EditorStyles.miniButton,
                    GUILayout.Width(52f)))
            {
                Selection.activeObject =
                    property.objectReferenceValue;

                EditorGUIUtility.PingObject(
                    property.objectReferenceValue);
            }
        }

        private string GetRequirednessLabel()
        {
            return _requiredness != null &&
                   !_requiredness.hasMultipleDifferentValues
                ? _requiredness.enumDisplayNames[
                    _requiredness.enumValueIndex]
                : "Mixed";
        }

        private static RouteContentBinding FindParentBinding(
            RouteContentBinding binding)
        {
            for (Transform parent = binding.transform.parent;
                 parent != null;
                 parent = parent.parent)
            {
                if (parent.TryGetComponent(
                        out RouteContentBinding parentBinding))
                {
                    return parentBinding;
                }
            }

            return null;
        }

        private static int CountChildBindings(
            RouteContentBinding binding)
        {
            RouteContentBinding[] bindings =
                binding.GetComponentsInChildren<RouteContentBinding>(
                    true);

            int count = 0;

            for (int index = 0;
                 index < bindings.Length;
                 index++)
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
