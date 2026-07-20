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
            if (binding.PlayerInput == null)
            {
                Debug.LogError("Pause PlayerInput Binding Apply/Rebuild requires PlayerInput.", binding);
                return;
            }
            UnityPlayerInputGateAdapter[] adapters = binding.GetComponents<UnityPlayerInputGateAdapter>();
            if (adapters.Length > 1)
            {
                Debug.LogError("Pause PlayerInput Binding Apply/Rebuild found more than one UnityPlayerInputGateAdapter. Resolve the conflict manually.", binding);
                return;
            }
            UnityPlayerInputGateAdapter adapter = adapters.Length == 0
                ? Undo.AddComponent<UnityPlayerInputGateAdapter>(binding.gameObject)
                : adapters[0];
            if (adapter == null || (adapter.PlayerInput != null && !ReferenceEquals(adapter.PlayerInput, binding.PlayerInput)))
            {
                Debug.LogError("Pause PlayerInput Binding Apply/Rebuild found an incompatible UnityPlayerInputGateAdapter and will not overwrite it.", binding);
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
