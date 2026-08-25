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
        private SerializedProperty _policy;
        private FrameworkAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _routeBgm =
                serializedObject.FindProperty("routeBgm");

            _policy =
                serializedObject.FindProperty("policy");

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
                _policy,
                new GUIContent(
                    "Route BGM Policy",
                    "Defines the complete BGM intent published by this Route."));

            if (ShouldShowRouteCue())
            {
                EditorGUILayout.PropertyField(
                    _routeBgm,
                    new GUIContent(
                        "Route BGM",
                        "Cue required by Play Own."));
            }

            DrawPolicyExplanation();
        }

        private bool ShouldShowRouteCue()
        {
            if (_policy == null || _policy.hasMultipleDifferentValues)
            {
                return true;
            }

            return (FrameworkBgmRoutePolicy)_policy.intValue ==
                FrameworkBgmRoutePolicy.PlayOwn;
        }

        private void DrawPolicyExplanation()
        {
            if (_policy == null || _policy.hasMultipleDifferentValues)
            {
                return;
            }

            FrameworkBgmRoutePolicy policy =
                (FrameworkBgmRoutePolicy)_policy.intValue;

            switch (policy)
            {
                case FrameworkBgmRoutePolicy.PreserveCurrent:
                    EditorGUILayout.HelpBox(
                        "Preserve Current publishes No Request and keeps the confirmed presentation.",
                        MessageType.Info);
                    break;

                case FrameworkBgmRoutePolicy.Silence:
                    EditorGUILayout.HelpBox(
                        "Silence publishes an explicit Silence / Stop intent. No cue is required.",
                        MessageType.Info);
                    break;
            }
        }

        private string BuildIntentSummary()
        {
            if (_routeBgm == null || _policy == null)
            {
                return "Configure Route BGM intent.";
            }

            if (_routeBgm.hasMultipleDifferentValues ||
                _policy.hasMultipleDifferentValues)
            {
                return "Selected bindings contain mixed BGM intent.";
            }

            AudioBgmCueAsset routeCue =
                _routeBgm.objectReferenceValue
                    as AudioBgmCueAsset;

            FrameworkBgmRoutePolicy policy =
                (FrameworkBgmRoutePolicy)_policy.intValue;

            return BuildRouteIntentSummary(policy, routeCue) + ".";
        }

        private static string BuildRouteIntentSummary(
            FrameworkBgmRoutePolicy policy,
            AudioBgmCueAsset cue)
        {
            switch (policy)
            {
                case FrameworkBgmRoutePolicy.PreserveCurrent:
                    return "Preserve Current";

                case FrameworkBgmRoutePolicy.Silence:
                    return "Silence";

                case FrameworkBgmRoutePolicy.PlayOwn:
                    return cue != null
                        ? $"Play '{cue.name}'"
                        : "Play Own requires Route BGM";

                default:
                    return "Invalid Route BGM Policy";
            }
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

            if (!System.Enum.IsDefined(
                    typeof(FrameworkBgmRoutePolicy),
                    binding.Policy))
            {
                EditorGUILayout.HelpBox(
                    "Route BGM Policy has an invalid serialized value.",
                    MessageType.Error);
                return;
            }

            if (binding.Policy == FrameworkBgmRoutePolicy.PlayOwn &&
                binding.RouteBgm == null)
            {
                EditorGUILayout.HelpBox(
                    "Play Own requires a Route BGM cue.",
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

                EditorGUILayout.TextField(
                    "Route Policy",
                    binding.Policy.ToString());

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
