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
            serializedObject.Update();

            EditorGUILayout.LabelField(
                "Target",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                playerInput,
                new GUIContent(
                    "Player Input"));

            PlayerInput selectedPlayerInput =
                playerInput.objectReferenceValue
                    as PlayerInput;

            PlayerInputActionMapReferenceEditorGui.DrawForPlayerInput(
                new GUIContent(
                    "Gameplay Action Map"),
                gameplayActionMap,
                selectedPlayerInput);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Gate Policy",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                blockOnInputAcceptance,
                new GUIContent(
                    "Block Input Acceptance"));

            EditorGUILayout.PropertyField(
                blockOnGameplayAction,
                new GUIContent(
                    "Block Gameplay Actions"));

            serializedObject.ApplyModifiedProperties();

            DrawStatus();

            EditorGUILayout.Space();
            showAdvancedDebug =
                EditorGUILayout.Foldout(
                    showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (showAdvancedDebug)
            {
                DrawAdvancedDebug();
            }
        }

        private void DrawStatus()
        {
            var adapter =
                (UnityPlayerInputGateAdapter)target;

            bool valid =
                adapter.TryValidateAuthoring(
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

            EditorGUILayout.LabelField(
                "Runtime Binding",
                adapter.InputGateRuntimeBindingStatus);

            EditorGUILayout.HelpBox(
                valid
                    ? "Gate Adapter authoring is ready."
                    : diagnostic,
                valid
                    ? MessageType.Info
                    : MessageType.Warning);
        }

        private void DrawAdvancedDebug()
        {
            serializedObject.Update();

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(
                "Physical Application",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                restorePreviousState);

            EditorGUILayout.PropertyField(
                applyOnEnable);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Diagnostics",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                logStateChanges);

            EditorGUILayout.PropertyField(
                logMissingRuntimeOnce);

            EditorGUILayout.PropertyField(
                logMissingTargetOnce);

            serializedObject.ApplyModifiedProperties();

            var adapter =
                (UnityPlayerInputGateAdapter)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Runtime Evidence",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Blocked By Adapter",
                adapter.IsBlockedByAdapter
                    ? "True"
                    : "False");

            EditorGUILayout.LabelField(
                "Last Status",
                adapter.LastStatus);

            EditorGUILayout.LabelField(
                "Last Reason",
                adapter.LastReason);

            EditorGUILayout.HelpBox(
                adapter.InputGateRuntimeBindingDiagnostic,
                MessageType.None);

            if (gameplayActionMapName != null &&
                !string.IsNullOrWhiteSpace(
                    gameplayActionMapName.stringValue))
            {
                EditorGUILayout.LabelField(
                    "Legacy Gameplay Map",
                    gameplayActionMapName.stringValue);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Technical Commands",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(
                       !Application.isPlaying))
            {
                if (GUILayout.Button(
                        "Apply Current Gate"))
                {
                    adapter.ApplyCurrentGate();
                }

                if (GUILayout.Button(
                        "Restore"))
                {
                    adapter.Restore();
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
