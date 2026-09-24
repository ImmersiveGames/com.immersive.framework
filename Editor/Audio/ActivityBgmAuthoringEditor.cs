using Immersive.Audio.Authoring;
using Immersive.Framework.Audio;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Audio
{
    [CustomEditor(typeof(ActivityBgmAuthoring))]
    [CanEditMultipleObjects]
    internal sealed class ActivityBgmAuthoringEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _assignedActivity;
        private SerializedProperty _activityBgm;
        private SerializedProperty _policy;
        private FrameworkAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _assignedActivity =
                serializedObject.FindProperty("assignedActivity");

            _activityBgm =
                serializedObject.FindProperty("activityBgm");

            _policy =
                serializedObject.FindProperty("policy");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Activity BGM Authoring",
                "Publishes Activity-level BGM intent from Activity lifecycle.");

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
                "BGM Intent");

            EditorGUILayout.PropertyField(
                _assignedActivity,
                new GUIContent(
                    "Assigned Activity",
                    "Optional explicit Activity ownership evidence. None uses local Activity ownership/lifecycle evidence."));

            EditorGUILayout.PropertyField(
                _policy,
                new GUIContent(
                    "Policy",
                    "Defines the BGM intent published by this Activity."));

            if (ShouldShowActivityCue())
            {
                EditorGUILayout.PropertyField(
                    _activityBgm,
                    new GUIContent(
                        "Activity BGM",
                        "Activity cue used by policies that support Activity-owned BGM."));
            }
        }

        private bool ShouldShowActivityCue()
        {
            if (_policy == null ||
                _policy.hasMultipleDifferentValues)
            {
                return true;
            }

            FrameworkBgmActivityPolicy policy =
                (FrameworkBgmActivityPolicy)
                _policy.intValue;

            return policy ==
                       FrameworkBgmActivityPolicy.UseOwnOrRoute ||
                   policy ==
                       FrameworkBgmActivityPolicy
                           .UseOwnOrPreserveCurrent;
        }

        private string BuildIntentSummary()
        {
            if (_policy == null ||
                _activityBgm == null)
            {
                return "Configure Activity BGM intent.";
            }

            if (_policy.hasMultipleDifferentValues ||
                _activityBgm.hasMultipleDifferentValues)
            {
                return "Selected bindings contain mixed BGM intent.";
            }

            FrameworkBgmActivityPolicy policy =
                (FrameworkBgmActivityPolicy)
                _policy.intValue;

            AudioBgmCueAsset cue =
                _activityBgm.objectReferenceValue
                    as AudioBgmCueAsset;

            switch (policy)
            {
                case FrameworkBgmActivityPolicy
                    .UseOwnOrPreserveCurrent:
                    return cue != null
                        ? $"Play '{cue.name}'."
                        : "No Request / Preserve current confirmed BGM.";

                case FrameworkBgmActivityPolicy.UseRoute:
                    return "Inherit the complete current Route intent.";

                case FrameworkBgmActivityPolicy.Silence:
                    return "Explicit Silence.";

                default:
                    return cue != null
                        ? $"Play '{cue.name}'."
                        : "Inherit the complete current Route intent.";
            }
        }

        private void DrawConfigurationStatus()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            if (_policy.hasMultipleDifferentValues)
            {
                FrameworkAuthoringInspectorGui.Status(
                    "Mixed selection");
                return;
            }

            FrameworkBgmActivityPolicy policy =
                (FrameworkBgmActivityPolicy)
                _policy.intValue;

            if (!System.Enum.IsDefined(
                    typeof(FrameworkBgmActivityPolicy),
                    policy))
            {
                EditorGUILayout.HelpBox(
                    "Activity BGM Policy has an invalid serialized value.",
                    MessageType.Error);
                return;
            }

            FrameworkAuthoringInspectorGui.Status("Ready");
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
                        FrameworkBgmAuthoringValidator
                            .ValidateActivityBinding(
                                targets[index]
                                    as ActivityBgmAuthoring));
                }
            }

            FrameworkAuthoringValidationGui.DrawIssues(
                _validationReport,
                false);
        }

        private void DrawAdvanced()
        {
            if (targets.Length != 1 ||
                !(target is ActivityBgmAuthoring binding))
            {
                EditorGUILayout.LabelField(
                    new GUIContent("Runtime Evidence"),
                    new GUIContent("Single selection only"));
                return;
            }

            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Injected Director",
                    binding.Director,
                    typeof(FrameworkBgmDirector),
                    true);

                EditorGUILayout.ObjectField(
                    "Stored Activity BGM",
                    binding.ActivityBgm,
                    typeof(AudioBgmCueAsset),
                    false);

                EditorGUILayout.TextField(
                    "Last Operation",
                    binding.LastOperationResult.Operation.ToString());

                EditorGUILayout.TextField(
                    "Last Outcome",
                    binding.LastOperationResult.Outcome.ToString());

                EditorGUILayout.TextField(
                    "Last Reason",
                    string.IsNullOrWhiteSpace(
                        binding.LastOperationResult.Reason)
                            ? "<none>"
                            : binding.LastOperationResult.Reason);

                FrameworkBgmDirector director =
                    binding.Director;

                EditorGUILayout.ObjectField(
                    "Confirmed BGM",
                    director != null
                        ? director.ConfirmedBgm
                        : null,
                    typeof(AudioBgmCueAsset),
                    false);

                EditorGUILayout.Toggle(
                    "Confirmed Silence",
                    director != null &&
                    director.ConfirmedExplicitSilence);
            }

            EditorGUI.indentLevel--;

            if (_assignedActivity != null &&
                _assignedActivity.objectReferenceValue == null)
            {
                EditorGUILayout.LabelField(
                    new GUIContent("Activity Ownership"),
                    new GUIContent(
                        "Resolved from local lifecycle evidence"));
            }
        }
    }
}
