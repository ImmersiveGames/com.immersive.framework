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
                "Owns Framework BGM intent and confirmed sticky presentation.");

            FrameworkAuthoringInspectorGui.IntentSummary(
                BuildIntentSummary());

            EditorGUI.BeginChangeCheck();

            FrameworkAuthoringInspectorGui.Section(
                "Audio Provider");

            EditorGUILayout.PropertyField(
                _audioRuntimeHost,
                new GUIContent(
                    "Audio Runtime Host",
                    "Explicit physical playback authority used for BGM Play and Silence."));

            bool providerChanged =
                EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (providerChanged)
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
                serializedObject.UpdateIfRequiredOrScript();

                EditorGUI.BeginChangeCheck();
                DrawAdvanced();
                bool advancedChanged =
                    EditorGUI.EndChangeCheck();

                serializedObject.ApplyModifiedProperties();

                if (advancedChanged)
                {
                    _validationReport = null;
                }
            }
        }

        private string BuildIntentSummary()
        {
            if (_audioRuntimeHost == null)
            {
                return "Configure the physical BGM provider.";
            }

            if (_audioRuntimeHost.hasMultipleDifferentValues)
            {
                return "Selected Directors use mixed providers.";
            }

            AudioRuntimeHost host =
                _audioRuntimeHost.objectReferenceValue
                    as AudioRuntimeHost;

            return host != null
                ? $"Physical provider: '{host.name}'."
                : "Physical provider is missing.";
        }

        private void DrawConfigurationStatus()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            if (_audioRuntimeHost.hasMultipleDifferentValues)
            {
                FrameworkAuthoringInspectorGui.Status(
                    "Mixed selection");
                return;
            }

            if (_audioRuntimeHost.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Audio Runtime Host is required.",
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
                            .ValidateDirector(
                                targets[index]
                                    as FrameworkBgmDirector));
                }
            }

            FrameworkAuthoringValidationGui.DrawIssues(
                _validationReport,
                false);
        }

        private void DrawAdvanced()
        {
            EditorGUILayout.PropertyField(
                _logTransitions,
                new GUIContent(
                    "Log Transitions",
                    "Emit BGM intent/provider transition diagnostics."));

            if (targets.Length != 1 ||
                !(target is FrameworkBgmDirector director))
            {
                EditorGUILayout.LabelField(
                    "Runtime Evidence",
                    "Single selection only");
                return;
            }

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(
                "Runtime Evidence",
                EditorStyles.miniBoldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "State",
                    "Available in Play Mode");
                return;
            }

            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Current Route BGM",
                    director.CurrentRouteBgm,
                    typeof(AudioBgmCueAsset),
                    false);

                EditorGUILayout.ObjectField(
                    "Current Activity BGM",
                    director.CurrentActivityBgm,
                    typeof(AudioBgmCueAsset),
                    false);

                EditorGUILayout.ObjectField(
                    "Current Effective BGM",
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
                    "Activity Policy",
                    director.CurrentActivityPolicy.ToString());

                EditorGUILayout.TextField(
                    "Last Operation",
                    director.LastOperationResult.Operation.ToString());

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

            EditorGUI.indentLevel--;
        }
    }
}
