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
        private SerializedProperty _pauseAction;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _pauseAction = serializedObject.FindProperty("pauseAction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Pause Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_pauseAction, new GUIContent("Pause Action"));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Global Action Map", ResolveGlobalMapName());
            }

            serializedObject.ApplyModifiedProperties();
            DrawGateComposition();
            DrawAuthoringStatus();

            EditorGUILayout.Space();
            _showAdvancedDebug = EditorGUILayout.Foldout(
                _showAdvancedDebug,
                "Advanced / Debug",
                true);

            if (_showAdvancedDebug)
            {
                DrawAdvancedDebug();
            }
        }

        private void DrawGateComposition()
        {
            var binding = (PlayerPauseInput)target;
            UnityPlayerInputGateAdapter[] adapters =
                binding.GetComponents<UnityPlayerInputGateAdapter>();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Required Gate Composition",
                EditorStyles.boldLabel);

            if (adapters.Length == 1 && adapters[0] != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "Unity PlayerInput Gate Adapter",
                        adapters[0],
                        typeof(UnityPlayerInputGateAdapter),
                        true);
                    EditorGUILayout.ObjectField(
                        "Player Input",
                        adapters[0].PlayerInput,
                        typeof(PlayerInput),
                        true);
                    EditorGUILayout.TextField(
                        "Gameplay Action Map",
                        adapters[0].GameplayActionMapName);
                }

                EditorGUILayout.HelpBox(
                    "Player Input and Gameplay Action Map are authored exclusively by the co-located Gate Adapter.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "Add exactly one Unity PlayerInput Gate Adapter on this GameObject and configure its Player Input and Gameplay Action Map.",
                MessageType.Warning);
        }

        private void DrawAuthoringStatus()
        {
            var binding = (PlayerPauseInput)target;
            bool valid = binding.TryValidateAuthoring(out string diagnostic);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Authoring", valid ? "Ready" : "Incomplete");
            EditorGUILayout.HelpBox(
                valid ? "Pause input authoring is ready." : diagnostic,
                valid ? MessageType.Info : MessageType.Warning);
        }

        private void DrawAdvancedDebug()
        {
            var binding = (PlayerPauseInput)target;
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Runtime Binding Status", binding.BindingStatus);
            EditorGUILayout.LabelField(
                "Has Active Binding",
                binding.HasActiveBinding ? "True" : "False");
            EditorGUILayout.HelpBox(binding.BindingDiagnostic, MessageType.None);
            EditorGUI.indentLevel--;
        }

        private string ResolveGlobalMapName()
        {
            InputActionReference reference =
                _pauseAction.objectReferenceValue as InputActionReference;

            return reference != null &&
                reference.action != null &&
                reference.action.actionMap != null
                    ? reference.action.actionMap.name
                    : "<derived from Pause Action>";
        }
    }
}
