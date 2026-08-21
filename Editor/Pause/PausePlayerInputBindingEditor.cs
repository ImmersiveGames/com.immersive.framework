using Immersive.Framework.Diagnostics;
using Immersive.Framework.Editor.UnityInput;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.Pause
{
    [CustomEditor(typeof(PausePlayerInputBinding))]
    internal sealed class PausePlayerInputBindingEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _playerInput;
        private SerializedProperty _pauseAction;
        private SerializedProperty _gameplayActionMap;
        private SerializedProperty _gameplayActionMapName;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _playerInput =
                serializedObject.FindProperty(
                    "playerInput");

            _pauseAction =
                serializedObject.FindProperty(
                    "pauseAction");

            _gameplayActionMap =
                serializedObject.FindProperty(
                    "gameplayActionMap");

            _gameplayActionMapName =
                serializedObject.FindProperty(
                    "gameplayActionMapName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(
                "Player",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _playerInput,
                new GUIContent(
                    "Player Input"));

            PlayerInput selectedPlayerInput =
                _playerInput.objectReferenceValue
                    as PlayerInput;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Pause Input",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _pauseAction,
                new GUIContent(
                    "Pause Action"));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Global Action Map",
                    ResolveGlobalMapName());
            }

            PlayerInputActionMapReferenceEditorGui.DrawForPlayerInput(
                new GUIContent(
                    "Gameplay Action Map"),
                _gameplayActionMap,
                selectedPlayerInput);

            serializedObject.ApplyModifiedProperties();

            DrawAuthoringStatus();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Configuration",
                EditorStyles.boldLabel);

            if (GUILayout.Button(
                    "Apply / Rebuild"))
            {
                Apply(
                    (PausePlayerInputBinding)target);
            }

            EditorGUILayout.Space();
            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (_showAdvancedDebug)
            {
                DrawAdvancedDebug();
            }
        }

        private void DrawAuthoringStatus()
        {
            var binding =
                (PausePlayerInputBinding)target;

            bool valid =
                binding.TryValidateAuthoring(
                    out string diagnostic);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Status",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Authoring",
                valid
                    ? "Ready"
                    : "Incomplete");

            EditorGUILayout.HelpBox(
                valid
                    ? "Pause PlayerInput authoring is ready."
                    : diagnostic,
                valid
                    ? MessageType.Info
                    : MessageType.Warning);
        }

        private void DrawAdvancedDebug()
        {
            var binding =
                (PausePlayerInputBinding)target;

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(
                "Runtime Binding Status",
                binding.BindingStatus);

            EditorGUILayout.LabelField(
                "Has Active Binding",
                binding.HasActiveBinding
                    ? "True"
                    : "False");

            EditorGUILayout.HelpBox(
                binding.BindingDiagnostic,
                MessageType.None);

            UnityPlayerInputGateAdapter[] adapters =
                binding.GetComponents<
                    UnityPlayerInputGateAdapter>();

            EditorGUILayout.LabelField(
                "Technical Adapter Count",
                adapters.Length.ToString());

            if (_gameplayActionMapName != null &&
                !string.IsNullOrWhiteSpace(
                    _gameplayActionMapName.stringValue))
            {
                EditorGUILayout.LabelField(
                    "Legacy Gameplay Map",
                    _gameplayActionMapName.stringValue);
            }

            EditorGUI.indentLevel--;
        }

        private string ResolveGlobalMapName()
        {
            InputActionReference reference =
                _pauseAction.objectReferenceValue
                    as InputActionReference;

            return reference != null &&
                reference.action != null &&
                reference.action.actionMap != null
                    ? reference.action.actionMap.name
                    : "<derived from Pause Action>";
        }

        private static void Apply(
            PausePlayerInputBinding binding)
        {
            var logger =
                FrameworkLogger.Create<
                    PausePlayerInputBindingEditor>();

            if (binding == null ||
                binding.PlayerInput == null ||
                binding.PlayerInput.actions == null)
            {
                logger.Error(
                    "Pause PlayerInput Binding Apply/Rebuild requires PlayerInput actions.");
                return;
            }

            if (binding.PauseAction == null ||
                binding.PauseAction.action == null ||
                binding.PauseAction.action.actionMap == null)
            {
                logger.Error(
                    "Pause PlayerInput Binding Apply/Rebuild requires an InputActionReference that belongs to an Action Map.");
                return;
            }

            var bindingSerialized =
                new SerializedObject(
                    binding);

            bindingSerialized.Update();

            SerializedProperty gameplayReference =
                bindingSerialized.FindProperty(
                    "gameplayActionMap");

            if (!binding.GameplayActionMapReference.IsConfigured)
            {
                SerializedProperty legacy =
                    bindingSerialized.FindProperty(
                        "gameplayActionMapName");

                if (!PlayerInputActionMapReferenceEditorGui.TryAssignByName(
                        gameplayReference,
                        binding.PlayerInput.actions,
                        legacy != null
                            ? legacy.stringValue
                            : string.Empty,
                        out string migrationDiagnostic))
                {
                    logger.Error(
                        $"Pause PlayerInput Binding Apply/Rebuild requires an explicit Gameplay Action Map. {migrationDiagnostic}");
                    return;
                }
            }

            bindingSerialized.ApplyModifiedPropertiesWithoutUndo();

            UnityPlayerInputGateAdapter[] adapters =
                binding.GetComponents<
                    UnityPlayerInputGateAdapter>();

            if (adapters.Length > 1)
            {
                logger.Error(
                    "Pause PlayerInput Binding Apply/Rebuild found more than one UnityPlayerInputGateAdapter. Resolve the conflict manually.");
                return;
            }

            UnityPlayerInputGateAdapter adapter =
                adapters.Length == 0
                    ? Undo.AddComponent<
                        UnityPlayerInputGateAdapter>(
                            binding.gameObject)
                    : adapters[0];

            if (adapter == null ||
                adapter.PlayerInput != null &&
                !ReferenceEquals(
                    adapter.PlayerInput,
                    binding.PlayerInput))
            {
                logger.Error(
                    "Pause PlayerInput Binding Apply/Rebuild found an incompatible UnityPlayerInputGateAdapter and will not overwrite it.");
                return;
            }

            if (!binding.GameplayActionMapReference.TryResolve(
                    binding.PlayerInput.actions,
                    out InputActionMap gameplayMap,
                    out string mapDiagnostic))
            {
                logger.Error(
                    $"Pause PlayerInput Binding Apply/Rebuild could not resolve the Gameplay Action Map. {mapDiagnostic}");
                return;
            }

            var adapterSerialized =
                new SerializedObject(
                    adapter);

            adapterSerialized.Update();

            adapterSerialized.FindProperty(
                    "playerInput")
                .objectReferenceValue =
                binding.PlayerInput;

            PlayerInputActionMapReferenceEditorGui.Assign(
                adapterSerialized.FindProperty(
                    "gameplayActionMap"),
                gameplayMap);

            adapterSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                adapter);

            EditorUtility.SetDirty(
                binding);

            PrefabUtility.RecordPrefabInstancePropertyModifications(
                adapter);

            PrefabUtility.RecordPrefabInstancePropertyModifications(
                binding);

            if (!binding.TryValidateAuthoring(
                    out string diagnostic))
            {
                logger.Error(
                    $"Pause PlayerInput Binding Apply/Rebuild completed materialization but validation failed. {diagnostic}");
                return;
            }

            logger.Info(
                $"Pause PlayerInput Binding Apply/Rebuild completed. gameplayMap='{gameplayMap.name}' gameplayMapId='{gameplayMap.id:D}'.");
        }
    }
}
