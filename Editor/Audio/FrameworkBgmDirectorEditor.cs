using Immersive.Audio.Authoring;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.Audio;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Audio
{
    [CustomEditor(typeof(FrameworkBgmDirector))]
    [CanEditMultipleObjects]
    internal sealed class FrameworkBgmDirectorEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _audioRuntimeHost;
        private SerializedProperty _logTransitions;

        private FrameworkAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _audioRuntimeHost =
                serializedObject.FindProperty("audioRuntimeHost");

            _logTransitions =
                serializedObject.FindProperty("logTransitions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            FrameworkAuthoringInspectorGui.ProductHeader(
                "BGM Director",
                null);

            EditorGUI.BeginChangeCheck();
            DrawProvider();
            bool providerChanged =
                EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (providerChanged)
            {
                _validationReport = null;
            }

            DrawValidation();

            _showAdvanced =
                FrameworkAuthoringInspectorGui.AdvancedFoldout(
                    _showAdvanced);

            if (_showAdvanced)
            {
                serializedObject.UpdateIfRequiredOrScript();

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(
                    _logTransitions,
                    new GUIContent(
                        "Log Transitions",
                        "Emit BGM intent and provider transition diagnostics."));

                bool advancedChanged =
                    EditorGUI.EndChangeCheck();

                serializedObject.ApplyModifiedProperties();

                if (advancedChanged)
                {
                    _validationReport = null;
                }

                DrawRuntimeEvidence();
            }
        }

        private void DrawProvider()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Audio Provider");

            EditorGUILayout.PropertyField(
                _audioRuntimeHost,
                new GUIContent(
                    "Audio Runtime Host",
                    "Explicit physical playback authority used for BGM Play and Silence."));

            if (_audioRuntimeHost != null &&
                !_audioRuntimeHost.hasMultipleDifferentValues &&
                _audioRuntimeHost.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Audio Runtime Host is required.",
                    MessageType.Error);
            }
        }

        private void DrawValidation()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Validate",
                            "Validate the BGM Director authoring without changing runtime state."),
                        GUILayout.Width(90f)))
                {
                    RunValidation();
                }

                EditorGUILayout.LabelField(
                    ValidationStatus(),
                    EditorStyles.miniLabel);
            }

            if (_validationReport != null &&
                !_validationReport.IsValid &&
                _validationReport.Issues.Count > 0)
            {
                FrameworkAuthoringValidationIssue issue =
                    _validationReport.Issues[0];

                EditorGUILayout.HelpBox(
                    issue.Message,
                    MessageType.Error);
            }
        }

        private string ValidationStatus()
        {
            if (_validationReport == null)
            {
                return "Not Validated";
            }

            return _validationReport.IsValid
                ? "Valid"
                : "Issue";
        }

        private void RunValidation()
        {
            _validationReport =
                new FrameworkAuthoringValidationReport();

            for (int index = 0;
                 index < targets.Length;
                 index++)
            {
                _validationReport.AddRange(
                    FrameworkBgmAuthoringValidator
                        .ValidateDirector(
                            targets[index]
                                as FrameworkBgmDirector));
            }
        }

        private void DrawRuntimeEvidence()
        {
            if (targets.Length != 1 ||
                !(target is FrameworkBgmDirector director))
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Runtime",
                EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Route Policy",
                    director.CurrentRoutePolicy.ToString());

                EditorGUILayout.ObjectField(
                    "Route BGM",
                    director.CurrentRouteBgm,
                    typeof(AudioBgmCueAsset),
                    false);

                EditorGUILayout.TextField(
                    "Activity Policy",
                    director.CurrentActivityPolicy.ToString());

                EditorGUILayout.ObjectField(
                    "Activity BGM",
                    director.CurrentActivityBgm,
                    typeof(AudioBgmCueAsset),
                    false);

                EditorGUILayout.ObjectField(
                    "Effective BGM",
                    director.CurrentEffectiveBgm,
                    typeof(AudioBgmCueAsset),
                    false);

                EditorGUILayout.ObjectField(
                    "Confirmed BGM",
                    director.ConfirmedBgm,
                    typeof(AudioBgmCueAsset),
                    false);

                EditorGUILayout.Toggle(
                    "Confirmed Silence",
                    director.ConfirmedExplicitSilence);

                EditorGUILayout.TextField(
                    "Last Outcome",
                    director.LastOperationResult.Outcome.ToString());

                EditorGUILayout.TextField(
                    "Last Reason",
                    string.IsNullOrWhiteSpace(
                        director.LastOperationResult.Reason)
                            ? "<none>"
                            : director.LastOperationResult.Reason);
            }
        }
    }
}
