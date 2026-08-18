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

        private SerializedProperty playerInput;
        private SerializedProperty gameplayActionMap;
        private SerializedProperty blockOnInputAcceptance;
        private SerializedProperty blockOnGameplayAction;
        private SerializedProperty restorePreviousState;
        private SerializedProperty applyOnEnable;
        private SerializedProperty logStateChanges;
        private SerializedProperty logMissingRuntimeOnce;
        private SerializedProperty logMissingTargetOnce;
        private SerializedProperty gameplayActionMapName;
        private bool showAdvancedDebug;

        private void OnEnable()
        {
            playerInput =
                serializedObject.FindProperty(
                    "playerInput");

            gameplayActionMap =
                serializedObject.FindProperty(
                    "gameplayActionMap");

            blockOnInputAcceptance =
                serializedObject.FindProperty(
                    "blockOnInputAcceptance");

            blockOnGameplayAction =
                serializedObject.FindProperty(
                    "blockOnGameplayAction");

            restorePreviousState =
                serializedObject.FindProperty(
                    "restorePreviousState");

            applyOnEnable =
                serializedObject.FindProperty(
                    "applyOnEnable");

            logStateChanges =
                serializedObject.FindProperty(
                    "logStateChanges");

            logMissingRuntimeOnce =
                serializedObject.FindProperty(
                    "logMissingRuntimeOnce");

            logMissingTargetOnce =
                serializedObject.FindProperty(
                    "logMissingTargetOnce");

            gameplayActionMapName =
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
                playerInput,
                PlayerInputLabel);

            PlayerInput selectedPlayerInput =
                playerInput.objectReferenceValue
                    as PlayerInput;

            PlayerInputActionMapReferenceEditorGui.DrawForPlayerInput(
                GameplayActionMapLabel,
                gameplayActionMap,
                selectedPlayerInput);

            EditorGUILayout.PropertyField(
                blockOnInputAcceptance,
                BlockInputAcceptanceLabel);

            EditorGUILayout.PropertyField(
                blockOnGameplayAction,
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
            showAdvancedDebug =
                EditorGUILayout.Foldout(
                    showAdvancedDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows physical application policy, diagnostic logging and legacy serialized evidence."),
                    true);

            if (!showAdvancedDebug)
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.indentLevel++;

            FrameworkAuthoringInspectorGui.Section(
                "Physical Application");

            EditorGUILayout.PropertyField(
                restorePreviousState,
                RestorePreviousStateLabel);

            EditorGUILayout.PropertyField(
                applyOnEnable,
                ApplyOnEnableLabel);

            FrameworkAuthoringInspectorGui.Section(
                "Diagnostics");

            EditorGUILayout.PropertyField(
                logStateChanges,
                new GUIContent(
                    "Log State Changes",
                    "Logs adapter state changes for technical diagnosis."));

            EditorGUILayout.PropertyField(
                logMissingRuntimeOnce,
                new GUIContent(
                    "Log Missing Runtime Once",
                    "Logs the first missing Input Gate runtime binding occurrence."));

            EditorGUILayout.PropertyField(
                logMissingTargetOnce,
                new GUIContent(
                    "Log Missing Target Once",
                    "Logs the first missing PlayerInput or gameplay Action Map target occurrence."));

            serializedObject.ApplyModifiedProperties();

            if (gameplayActionMapName != null &&
                !string.IsNullOrWhiteSpace(
                    gameplayActionMapName.stringValue))
            {
                FrameworkAuthoringInspectorGui.Section(
                    "Legacy Evidence");

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Legacy Gameplay Map",
                            "Legacy serialized Action Map name retained only as migration/debug evidence."),
                        gameplayActionMapName.stringValue);
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
