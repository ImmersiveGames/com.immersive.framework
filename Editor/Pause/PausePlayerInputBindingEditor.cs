using Immersive.Framework.Diagnostics;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Pause
{
    [CustomEditor(typeof(PausePlayerInputBinding))]
    internal sealed class PausePlayerInputBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var binding = (PausePlayerInputBinding)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Binding Status", binding.BindingStatus);
            EditorGUILayout.HelpBox(binding.BindingDiagnostic, MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced / Debug", EditorStyles.boldLabel);
            if (GUILayout.Button("Apply/Rebuild Technical Binding"))
            {
                Apply(binding);
            }
        }

        private static void Apply(PausePlayerInputBinding binding)
        {
            var logger = FrameworkLogger.Create<PausePlayerInputBindingEditor>();
            if (binding.PlayerInput == null)
            {
                logger.Error("Pause PlayerInput Binding Apply/Rebuild requires PlayerInput.");
                return;
            }
            UnityPlayerInputGateAdapter[] adapters = binding.GetComponents<UnityPlayerInputGateAdapter>();
            if (adapters.Length > 1)
            {
                logger.Error("Pause PlayerInput Binding Apply/Rebuild found more than one UnityPlayerInputGateAdapter. Resolve the conflict manually.");
                return;
            }
            UnityPlayerInputGateAdapter adapter = adapters.Length == 0
                ? Undo.AddComponent<UnityPlayerInputGateAdapter>(binding.gameObject)
                : adapters[0];
            if (adapter == null || (adapter.PlayerInput != null && !ReferenceEquals(adapter.PlayerInput, binding.PlayerInput)))
            {
                logger.Error("Pause PlayerInput Binding Apply/Rebuild found an incompatible UnityPlayerInputGateAdapter and will not overwrite it.");
                return;
            }
            var serialized = new SerializedObject(adapter);
            serialized.FindProperty("playerInput").objectReferenceValue = binding.PlayerInput;
            serialized.FindProperty("gameplayActionMapName").stringValue = binding.GameplayActionMapName;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(adapter);
            EditorUtility.SetDirty(binding);
        }
    }
}
