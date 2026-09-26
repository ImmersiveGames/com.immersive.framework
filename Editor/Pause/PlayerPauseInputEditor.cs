using Immersive.Framework.Editor.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.Pause
{
    [CustomEditor(typeof(PlayerPauseInput))]
    internal sealed class PlayerPauseInputEditor : UnityEditor.Editor
    {
        private const string Unresolved = "<unresolved>";

        private static readonly GUIContent PauseActionLabel =
            new GUIContent(
                "Pause Action",
                "Input Action that requests Pause. It must exist in the PlayerInput actions of the co-located Gate Adapter.");

        private SerializedProperty _pauseAction;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _pauseAction = serializedObject.FindProperty("pauseAction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var binding = (PlayerPauseInput)target;

            FrameworkAuthoringInspectorGui.ProductHeader("Pause PlayerInput Binding", string.Empty);

            FrameworkAuthoringInspectorGui.Section("Configuration");
            EditorGUILayout.PropertyField(_pauseAction, PauseActionLabel);
            serializedObject.ApplyModifiedProperties();

            DrawConfigurationStatus(binding);

            if (Application.isPlaying)
            {
                DrawRuntimeStatus(binding);
            }

            _showAdvancedDebug = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvancedDebug);
            if (_showAdvancedDebug)
            {
                DrawAdvancedDebug(binding);
            }
        }

        private static void DrawConfigurationStatus(PlayerPauseInput binding)
        {
            bool valid = binding.TryValidateAuthoring(out string diagnostic);

            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            FrameworkAuthoringInspectorGui.Status(valid ? "Ready" : "Incomplete");

            if (!valid)
            {
                EditorGUILayout.HelpBox(diagnostic, MessageType.Warning);
            }
        }

        private static void DrawRuntimeStatus(PlayerPauseInput binding)
        {
            FrameworkAuthoringInspectorGui.Section("Runtime Status");
            EditorGUILayout.LabelField("Binding", binding.BindingStatus);

            if (!binding.HasActiveBinding)
            {
                EditorGUILayout.HelpBox(
                    "No active Pause binding. See Advanced / Debug for the binding diagnostic.",
                    MessageType.Warning);
            }
        }

        private void DrawAdvancedDebug(PlayerPauseInput binding)
        {
            EditorGUI.indentLevel++;

            UnityPlayerInputGateAdapter[] adapters =
                binding.GetComponents<UnityPlayerInputGateAdapter>();
            UnityPlayerInputGateAdapter adapter =
                adapters.Length == 1 ? adapters[0] : null;
            InputActionReference reference =
                _pauseAction.objectReferenceValue as InputActionReference;
            InputAction sourceAction = reference != null ? reference.action : null;

            FrameworkAuthoringInspectorGui.Section("Resolved Configuration");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Gate Adapter",
                    adapter,
                    typeof(UnityPlayerInputGateAdapter),
                    true);
                EditorGUILayout.ObjectField(
                    "Player Input",
                    adapter != null ? adapter.PlayerInput : null,
                    typeof(PlayerInput),
                    true);
                EditorGUILayout.TextField(
                    "Global Action Map",
                    ValueOrUnresolved(binding.GlobalActionMapName));
                EditorGUILayout.TextField(
                    "Gameplay Action Map",
                    ValueOrUnresolved(binding.GameplayActionMapName));
                EditorGUILayout.TextField(
                    "Pause Action ID",
                    sourceAction != null ? sourceAction.id.ToString() : Unresolved);
            }

            FrameworkAuthoringInspectorGui.Section("Runtime Binding");
            EditorGUILayout.LabelField("Status", binding.BindingStatus);
            EditorGUILayout.LabelField(
                "Active Binding",
                binding.HasActiveBinding ? "Yes" : "No");
            if (!string.IsNullOrWhiteSpace(binding.BindingDiagnostic))
            {
                EditorGUILayout.LabelField(
                    binding.BindingDiagnostic,
                    EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUI.indentLevel--;
        }

        private static string ValueOrUnresolved(string value) =>
            string.IsNullOrWhiteSpace(value) ? Unresolved : value;
    }
}
