using Immersive.Framework.Editor.Common;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.UnityInput
{
    [CustomEditor(typeof(UnityPlayerInputGateAdapter))]
    internal sealed class UnityPlayerInputGateAdapterEditor :
        UnityEditor.Editor
    {
        private static readonly GUIContent PlayerInputLabel =
            new GUIContent(
                "Player Input",
                "Gameplay-owned PlayerInput gated by this adapter. If empty, runtime resolution uses PlayerInput on the same GameObject.");

        private static readonly GUIContent GameplayActionMapLabel =
            new GUIContent(
                "Gameplay Action Map",
                "Exact gameplay Input Action Map controlled when the Framework Gate blocks gameplay input.");

        private static readonly GUIContent BlockInputAcceptanceLabel =
            new GUIContent(
                "Block Input Acceptance",
                "Block the gameplay Action Map when the canonical Gate blocks input acceptance.");

        private static readonly GUIContent BlockGameplayActionsLabel =
            new GUIContent(
                "Block Gameplay Actions",
                "Block the gameplay Action Map when the canonical Gate blocks gameplay actions.");

        private static readonly GUIContent RestorePreviousStateLabel =
            new GUIContent(
                "Restore Previous State",
                "Ask the canonical Unity input state writer to restore only state changed by this Gate block.");

        private static readonly GUIContent ApplyOnEnableLabel =
            new GUIContent(
                "Apply On Enable",
                "Evaluate and apply the current Gate state when this adapter becomes enabled.");

        private SerializedProperty _playerInput;
        private SerializedProperty _gameplayActionMap;
        private SerializedProperty _blockOnInputAcceptance;
        private SerializedProperty _blockOnGameplayAction;
        private SerializedProperty _restorePreviousState;
        private SerializedProperty _applyOnEnable;
        private SerializedProperty _logStateChanges;
        private SerializedProperty _logMissingRuntimeOnce;
        private SerializedProperty _logMissingTargetOnce;
        private SerializedProperty _gameplayActionMapName;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _playerInput =
                serializedObject.FindProperty(
                    "playerInput");

            _gameplayActionMap =
                serializedObject.FindProperty(
                    "gameplayActionMap");

            _blockOnInputAcceptance =
                serializedObject.FindProperty(
                    "blockOnInputAcceptance");

            _blockOnGameplayAction =
                serializedObject.FindProperty(
                    "blockOnGameplayAction");

            _restorePreviousState =
                serializedObject.FindProperty(
                    "restorePreviousState");

            _applyOnEnable =
                serializedObject.FindProperty(
                    "applyOnEnable");

            _logStateChanges =
                serializedObject.FindProperty(
                    "logStateChanges");

            _logMissingRuntimeOnce =
                serializedObject.FindProperty(
                    "logMissingRuntimeOnce");

            _logMissingTargetOnce =
                serializedObject.FindProperty(
                    "logMissingTargetOnce");

            _gameplayActionMapName =
                serializedObject.FindProperty(
                    "gameplayActionMapName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Unity PlayerInput Gate Adapter",
                    "Connects one gameplay-owned PlayerInput to the Framework Gate. Physical Input Action Map mutations remain delegated to the canonical Unity input state writer."),
                EditorStyles.boldLabel);

            DrawConfiguration();

            serializedObject.ApplyModifiedProperties();

            UnityPlayerInputGateAdapter adapter =
                (UnityPlayerInputGateAdapter)target;

            DrawConfigurationStatus(adapter);

            if (Application.isPlaying)
            {
                DrawRuntimeStatus(adapter);
                DrawRuntimeActions(adapter);
            }

            DrawAdvancedDebug();
        }

        private void DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            EditorGUILayout.PropertyField(
                _playerInput,
                PlayerInputLabel);

            PlayerInput selectedPlayerInput =
                _playerInput.objectReferenceValue
                    as PlayerInput;

            PlayerInputActionMapReferenceEditorGui.DrawForPlayerInput(
                GameplayActionMapLabel,
                _gameplayActionMap,
                selectedPlayerInput);

            EditorGUILayout.PropertyField(
                _blockOnInputAcceptance,
                BlockInputAcceptanceLabel);

            EditorGUILayout.PropertyField(
                _blockOnGameplayAction,
                BlockGameplayActionsLabel);
        }

        private static void DrawConfigurationStatus(
            UnityPlayerInputGateAdapter adapter)
        {
            bool valid =
                adapter.TryValidateAuthoring(
                    out string diagnostic);

            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            EditorGUILayout.LabelField(
                "Status",
                valid
                    ? "Ready"
                    : "Incomplete");

            if (!valid)
            {
                EditorGUILayout.HelpBox(
                    diagnostic,
                    MessageType.Warning);
            }
        }

        private static void DrawRuntimeStatus(
            UnityPlayerInputGateAdapter adapter)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Runtime Status");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Runtime Binding",
                        "Current binding state between this adapter and the Framework Input Gate runtime."),
                    new GUIContent(
                        adapter.InputGateRuntimeBindingStatus));

                EditorGUILayout.Toggle(
                    new GUIContent(
                        "Blocked By Adapter",
                        "Whether this adapter currently owns an applied gameplay-input block."),
                    adapter.IsBlockedByAdapter);

                EditorGUILayout.TextField(
                    new GUIContent(
                        "Last Status",
                        "Latest adapter runtime status."),
                    adapter.LastStatus);

                EditorGUILayout.TextField(
                    new GUIContent(
                        "Last Reason",
                        "Latest adapter runtime reason."),
                    adapter.LastReason);
            }

            if (!adapter.HasInputGateRuntimeBinding &&
                !string.IsNullOrWhiteSpace(
                    adapter.InputGateRuntimeBindingDiagnostic))
            {
                EditorGUILayout.HelpBox(
                    adapter.InputGateRuntimeBindingDiagnostic,
                    MessageType.Warning);
            }
        }

        private static void DrawRuntimeActions(
            UnityPlayerInputGateAdapter adapter)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Actions");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Apply Current Gate",
                            "Re-evaluate the current Framework Gate and explicitly apply the resulting state to this adapter's gameplay Action Map.")))
                {
                    adapter.ApplyCurrentGate();
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Restore",
                            "Explicitly restore state previously changed by this Gate adapter, subject to its configured restore policy.")))
                {
                    adapter.Restore();
                }
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(7f);
            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows physical application policy, diagnostic logging and legacy serialized evidence."),
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.indentLevel++;

            FrameworkAuthoringInspectorGui.Section(
                "Physical Application");

            EditorGUILayout.PropertyField(
                _restorePreviousState,
                RestorePreviousStateLabel);

            EditorGUILayout.PropertyField(
                _applyOnEnable,
                ApplyOnEnableLabel);

            FrameworkAuthoringInspectorGui.Section(
                "Diagnostics");

            EditorGUILayout.PropertyField(
                _logStateChanges,
                new GUIContent(
                    "Log State Changes",
                    "Logs adapter state changes for technical diagnosis."));

            EditorGUILayout.PropertyField(
                _logMissingRuntimeOnce,
                new GUIContent(
                    "Log Missing Runtime Once",
                    "Logs the first missing Input Gate runtime binding occurrence."));

            EditorGUILayout.PropertyField(
                _logMissingTargetOnce,
                new GUIContent(
                    "Log Missing Target Once",
                    "Logs the first missing PlayerInput or gameplay Action Map target occurrence."));

            serializedObject.ApplyModifiedProperties();

            if (_gameplayActionMapName != null &&
                !string.IsNullOrWhiteSpace(
                    _gameplayActionMapName.stringValue))
            {
                FrameworkAuthoringInspectorGui.Section(
                    "Legacy Evidence");

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Legacy Gameplay Map",
                            "Legacy serialized Action Map name retained only as migration/debug evidence."),
                        _gameplayActionMapName.stringValue);
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
