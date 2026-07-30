using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityLocalVisibilityAdapter))]
    [CanEditMultipleObjects]
    internal sealed class ActivityLocalVisibilityAdapterEditor : UnityEditor.Editor
    {
        private SerializedProperty _activity;
        private SerializedProperty _localContentId;
        private SerializedProperty _requiredness;
        private FrameworkAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _activity = serializedObject.FindProperty("activity");
            _localContentId = serializedObject.FindProperty("localContentId");
            _requiredness = serializedObject.FindProperty("requiredness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Activity Local Visibility Adapter",
                "Makes one scene-authored GameObject visible only while its assigned Activity is active.");
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
            FrameworkAuthoringInspectorGui.Section("Activity Binding");
            EditorGUILayout.PropertyField(
                _activity,
                new GUIContent(
                    "Activity",
                    "Activity that owns this scene-authored GameObject and controls its local visibility."));
            DrawSelectAssetAction(_activity, "Select Activity Asset");

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
            if (_activity == null || _localContentId == null || _requiredness == null)
            {
                return "Configure an Activity, explicit local identity and requiredness for this scene-authored contribution.";
            }

            if (_activity.hasMultipleDifferentValues ||
                _localContentId.hasMultipleDifferentValues ||
                _requiredness.hasMultipleDifferentValues)
            {
                return "The selected adapters contain mixed Activity, identity or requiredness values.";
            }

            string activityName = _activity.objectReferenceValue != null
                ? _activity.objectReferenceValue.name
                : "<missing Activity>";
            string identity = string.IsNullOrWhiteSpace(_localContentId.stringValue)
                ? "<missing local identity>"
                : _localContentId.stringValue.Trim();

            return $"Show this GameObject only for Activity '{activityName}' as {GetRequirednessLabel()} local content '{identity}'.";
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
                    FrameworkAuthoringSuggestionUtility.SuggestIdentity(target, "activity.local-content"),
                    "Suggest Activity Local Content Id");
                _validationReport = null;
            }
        }

        private void DrawConfigurationStatus()
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");

            if (serializedObject.isEditingMultipleObjects &&
                (_activity.hasMultipleDifferentValues ||
                 _localContentId.hasMultipleDifferentValues ||
                 _requiredness.hasMultipleDifferentValues))
            {
                EditorGUILayout.HelpBox(
                    "The selected adapters use mixed authoring values. Validate to review each adapter independently.",
                    MessageType.Info);
                return;
            }

            bool hasError = false;
            if (_activity.objectReferenceValue == null)
            {
                hasError = true;
                EditorGUILayout.HelpBox(
                    "Activity is missing. Assign the Activity that owns this scene-authored content.",
                    MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                hasError = true;
                EditorGUILayout.HelpBox(
                    "Local Content Id is missing. Enter an explicit identity or use the suggested value.",
                    MessageType.Error);
            }

            if (!serializedObject.isEditingMultipleObjects && target is ActivityLocalVisibilityAdapter adapter)
            {
                ActivityLocalVisibilityAdapter parent = FindParentBinding(adapter);
                if (parent != null)
                {
                    EditorGUILayout.HelpBox(
                        $"A parent GameObject also contains an Activity Local Visibility Adapter ('{parent.gameObject.name}'). Nested visibility ownership is not defined; keep adapter roots flat.",
                        MessageType.Warning);
                }

                int childCount = CountChildBindings(adapter);
                if (childCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"This GameObject contains {childCount} child Activity Local Visibility Adapter component(s). Nested visibility ownership is not defined; keep adapter roots flat.",
                        MessageType.Warning);
                }
            }

            if (!hasError)
            {
                EditorGUILayout.HelpBox(
                    "Ready for authoring validation. Runtime visibility remains owned by the official Activity Content Runtime.",
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
                        FrameworkAuthoringValidator.ValidateActivityLocalVisibilityAdapter(
                            targets[index] as ActivityLocalVisibilityAdapter));
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
            if (targets.Length != 1 || !(target is ActivityLocalVisibilityAdapter adapter))
            {
                EditorGUILayout.HelpBox(
                    "Advanced evidence is available for one selected adapter at a time.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Assigned Activity", adapter.Activity != null ? adapter.Activity.name : "<missing>");
            EditorGUILayout.LabelField("Normalized Local Content Id", adapter.HasExplicitLocalContentId ? adapter.LocalContentIdText : "<missing>");
            EditorGUILayout.LabelField("Requiredness", adapter.Requiredness.ToString());
            EditorGUILayout.LabelField("Local Scope Kind", adapter.LocalScopeKind.ToString());
            EditorGUILayout.LabelField("Scene", adapter.gameObject.scene.IsValid() ? adapter.gameObject.scene.name : "<no scene>");
            EditorGUILayout.LabelField("GameObject Active", adapter.gameObject.activeSelf ? "Yes" : "No");
            EditorGUILayout.HelpBox(
                "This Inspector never toggles visibility. Activity Content Runtime applies the active state through the official lifecycle.",
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

        private static ActivityLocalVisibilityAdapter FindParentBinding(ActivityLocalVisibilityAdapter binding)
        {
            for (Transform parent = binding.transform.parent; parent != null; parent = parent.parent)
            {
                if (parent.TryGetComponent(out ActivityLocalVisibilityAdapter parentBinding))
                {
                    return parentBinding;
                }
            }

            return null;
        }

        private static int CountChildBindings(ActivityLocalVisibilityAdapter binding)
        {
            ActivityLocalVisibilityAdapter[] bindings =
                binding.GetComponentsInChildren<ActivityLocalVisibilityAdapter>(true);
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
