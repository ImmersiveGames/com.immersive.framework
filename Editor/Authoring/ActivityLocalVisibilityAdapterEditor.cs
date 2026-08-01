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
        private static readonly GUIContent ActivityLabel = new GUIContent(
            "Activity",
            "The Activity that makes this GameObject visible while it is active.");

        private static readonly GUIContent LocalContentIdLabel = new GUIContent(
            "Local Content Id",
            "Stable local identity for this content within the Activity scope. Do not derive it from the GameObject name.");

        private static readonly GUIContent RequirednessLabel = new GUIContent(
            "Requiredness",
            "Defines whether this local content is required or optional in the Activity content contract.");

        private static readonly GUIContent ValidateLabel = new GUIContent(
            "Validate",
            "Checks the authored configuration without modifying it.");

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

            EditorGUILayout.LabelField(
                "Activity Local Visibility Adapter",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            DrawAuthoring();
            bool authoringChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (authoringChanged)
            {
                _validationReport = null;
            }

            DrawSuggestedIdentityAction();
            DrawImmediateAuthoringIssues();
            DrawValidation();
            DrawAdvancedFoldout();
        }

        private void DrawAuthoring()
        {
            FrameworkAuthoringInspectorGui.Section("Activity");
            EditorGUILayout.PropertyField(_activity, ActivityLabel);

            FrameworkAuthoringInspectorGui.Section("Local Content");
            EditorGUILayout.PropertyField(_localContentId, LocalContentIdLabel);
            EditorGUILayout.PropertyField(_requiredness, RequirednessLabel);
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
                            "Creates an explicit local content identity from the selected GameObject. This action is manual and Undo-compatible."),
                        GUILayout.Width(132f)))
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

        private void DrawImmediateAuthoringIssues()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            if (_activity != null &&
                !_activity.hasMultipleDifferentValues &&
                _activity.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Select the Activity that controls this GameObject.",
                    MessageType.Error);
            }

            if (_localContentId != null &&
                !_localContentId.hasMultipleDifferentValues &&
                string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Enter an explicit Local Content Id.",
                    MessageType.Error);
            }

            if (!(target is ActivityLocalVisibilityAdapter adapter))
            {
                return;
            }

            ActivityLocalVisibilityAdapter parent = FindParentBinding(adapter);
            if (parent != null)
            {
                EditorGUILayout.HelpBox(
                    $"A parent GameObject also contains an Activity Local Visibility Adapter ('{parent.gameObject.name}'). Keep visibility adapter roots flat.",
                    MessageType.Warning);
            }

            int childCount = CountChildBindings(adapter);
            if (childCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"This GameObject contains {childCount} child Activity Local Visibility Adapter component(s). Keep visibility adapter roots flat.",
                    MessageType.Warning);
            }
        }

        private void DrawValidation()
        {
            FrameworkAuthoringInspectorGui.Section("Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ValidateLabel, GUILayout.Width(96f)))
                {
                    RunValidation();
                }

                GUILayout.Space(8f);
                EditorGUILayout.LabelField(
                    GetValidationStatus(),
                    EditorStyles.miniBoldLabel);

                GUILayout.FlexibleSpace();
            }

            if (_validationReport == null ||
                (_validationReport.ErrorCount == 0 &&
                 _validationReport.WarningCount == 0))
            {
                return;
            }

            FrameworkAuthoringValidationGui.DrawIssues(
                _validationReport,
                false);
        }

        private void RunValidation()
        {
            _validationReport = new FrameworkAuthoringValidationReport();

            for (int index = 0; index < targets.Length; index++)
            {
                _validationReport.AddRange(
                    FrameworkAuthoringValidator
                        .ValidateActivityLocalVisibilityAdapter(
                            targets[index] as ActivityLocalVisibilityAdapter));
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

        private void DrawAdvancedFoldout()
        {
            EditorGUILayout.Space(6f);

            _showAdvanced = EditorGUILayout.Foldout(
                _showAdvanced,
                new GUIContent(
                    "Advanced / Debug",
                    "Shows technical authoring and read-only runtime evidence."),
                true);

            if (!_showAdvanced)
            {
                return;
            }

            EditorGUI.indentLevel++;
            DrawAdvanced();
            EditorGUI.indentLevel--;
        }

        private void DrawAdvanced()
        {
            if (targets.Length != 1 ||
                !(target is ActivityLocalVisibilityAdapter adapter))
            {
                EditorGUILayout.HelpBox(
                    "Advanced evidence is available for one selected adapter at a time.",
                    MessageType.None);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Assigned Activity",
                    adapter.Activity != null
                        ? adapter.Activity.name
                        : "<missing>");

                EditorGUILayout.TextField(
                    "Normalized Local Content Id",
                    adapter.HasExplicitLocalContentId
                        ? adapter.LocalContentIdText
                        : "<missing>");

                EditorGUILayout.TextField(
                    "Requiredness",
                    adapter.Requiredness.ToString());

                EditorGUILayout.TextField(
                    "Local Scope Kind",
                    adapter.LocalScopeKind.ToString());

                EditorGUILayout.TextField(
                    "Scene",
                    adapter.gameObject.scene.IsValid()
                        ? adapter.gameObject.scene.name
                        : "<no scene>");

                EditorGUILayout.TextField(
                    "Runtime Authority",
                    "Activity Content Runtime");

                EditorGUILayout.TextField(
                    "Runtime Status",
                    Application.isPlaying
                        ? adapter.gameObject.activeSelf
                            ? "Visible"
                            : "Hidden"
                        : "Not Available in Edit Mode");
            }

            if (adapter.Activity == null)
            {
                return;
            }

            if (GUILayout.Button(
                    new GUIContent(
                        "Open Activity",
                        "Selects and pings the assigned Activity asset.")))
            {
                Selection.activeObject = adapter.Activity;
                EditorGUIUtility.PingObject(adapter.Activity);
            }
        }

        private static ActivityLocalVisibilityAdapter FindParentBinding(
            ActivityLocalVisibilityAdapter binding)
        {
            for (Transform parent = binding.transform.parent;
                 parent != null;
                 parent = parent.parent)
            {
                if (parent.TryGetComponent(
                        out ActivityLocalVisibilityAdapter parentBinding))
                {
                    return parentBinding;
                }
            }

            return null;
        }

        private static int CountChildBindings(
            ActivityLocalVisibilityAdapter binding)
        {
            ActivityLocalVisibilityAdapter[] bindings =
                binding.GetComponentsInChildren<ActivityLocalVisibilityAdapter>(
                    true);

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
