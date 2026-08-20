using Immersive.Audio.Authoring;
using Immersive.Framework.Audio;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Audio
{
    [CustomEditor(typeof(FrameworkRouteBgmBinding))]
    [CanEditMultipleObjects]
    internal sealed class FrameworkRouteBgmBindingEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _routeBgm;
        private SerializedProperty _startupActivityBgmBinding;
        private FrameworkAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _routeBgm =
                serializedObject.FindProperty("routeBgm");

            _startupActivityBgmBinding =
                serializedObject.FindProperty(
                    "startupActivityBgmBinding");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Route BGM Binding",
                "Publishes Route-level BGM intent from Route lifecycle.");

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
                _routeBgm,
                new GUIContent(
                    "Route BGM",
                    "Optional Route-level Play request. None publishes No Request and preserves confirmed BGM."));

            EditorGUILayout.PropertyField(
                _startupActivityBgmBinding,
                new GUIContent(
                    "Startup Activity BGM",
                    "Optional explicit Startup Activity BGM binding. None is valid."));
        }

        private string BuildIntentSummary()
        {
            if (_routeBgm == null ||
                _startupActivityBgmBinding == null)
            {
                return "Configure Route BGM intent.";
            }

            if (_routeBgm.hasMultipleDifferentValues ||
                _startupActivityBgmBinding.hasMultipleDifferentValues)
            {
                return "Selected bindings contain mixed BGM intent.";
            }

            AudioBgmCueAsset routeCue =
                _routeBgm.objectReferenceValue
                    as AudioBgmCueAsset;

            FrameworkActivityBgmBinding startupBinding =
                _startupActivityBgmBinding.objectReferenceValue
                    as FrameworkActivityBgmBinding;

            string routeIntent =
                routeCue != null
                    ? $"Play '{routeCue.name}'"
                    : "No Request / Preserve";

            string startupIntent =
                startupBinding != null
                    ? $"Startup '{startupBinding.name}'"
                    : "No Startup Activity request";

            return $"{routeIntent}; {startupIntent}.";
        }

        private void DrawConfigurationStatus()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            if (serializedObject.isEditingMultipleObjects)
            {
                FrameworkAuthoringInspectorGui.Status(
                    "Mixed selection");
                return;
            }

            FrameworkRouteBgmBinding binding =
                target as FrameworkRouteBgmBinding;

            if (binding == null)
            {
                EditorGUILayout.HelpBox(
                    "Route BGM Binding is unavailable.",
                    MessageType.Error);
                return;
            }

            RouteContentBinding routeContent =
                binding.GetComponentInParent<RouteContentBinding>(
                    true);

            if (routeContent == null)
            {
                EditorGUILayout.HelpBox(
                    "Route BGM Binding must be on or below a Route Content Binding.",
                    MessageType.Error);
                return;
            }

            if (routeContent.Route == null)
            {
                EditorGUILayout.HelpBox(
                    "The owning Route Content Binding has no Route.",
                    MessageType.Error);
                return;
            }

            FrameworkActivityBgmBinding startupBinding =
                binding.StartupActivityBgmBinding;

            if (startupBinding != null &&
                (!routeContent.Route.HasStartupActivity ||
                 routeContent.Route.StartupActivity == null))
            {
                EditorGUILayout.HelpBox(
                    "Startup Activity BGM is assigned, but this Route has no Startup Activity.",
                    MessageType.Warning);
                return;
            }

            if (startupBinding != null &&
                routeContent.Route.StartupActivity != null &&
                startupBinding.AssignedActivity != null &&
                !ReferenceEquals(
                    startupBinding.AssignedActivity,
                    routeContent.Route.StartupActivity))
            {
                EditorGUILayout.HelpBox(
                    "Startup Activity BGM targets a different Activity from the Route Startup Activity.",
                    MessageType.Warning);
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
                            .ValidateRouteBinding(
                                targets[index]
                                    as FrameworkRouteBgmBinding));
                }
            }

            FrameworkAuthoringValidationGui.DrawIssues(
                _validationReport,
                false);
        }

        private void DrawAdvanced()
        {
            if (targets.Length != 1 ||
                !(target is FrameworkRouteBgmBinding binding))
            {
                EditorGUILayout.LabelField(
                    "Runtime Evidence",
                    "Single selection only");
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
        }
    }
}
