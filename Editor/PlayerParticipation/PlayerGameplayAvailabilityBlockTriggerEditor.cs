using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerGameplayAvailabilityBlockTrigger))]
    internal sealed class PlayerGameplayAvailabilityBlockTriggerEditor : UnityEditor.Editor
    {
        private bool _hasValidation;
        private bool _isValid;
        private string _validationIssue;
        private bool _showAdvanced;

        public override void OnInspectorGUI()
        {
            var trigger = target as PlayerGameplayAvailabilityBlockTrigger;
            if (trigger == null)
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            FrameworkAuthoringInspectorGui.ProductHeader("GAMEPLAY AVAILABILITY BLOCK", string.Empty);
            FrameworkAuthoringInspectorGui.Section("Scope");
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("scope"),
                new GUIContent("Scope", "Route or Activity scope that owns this consumer component."));
            FrameworkAuthoringInspectorGui.Section("Player Slot Profile");
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("playerSlot"),
                new GUIContent("Player Slot Profile", "Selects the exact Player Slot targeted by this block."));
            FrameworkAuthoringInspectorGui.Section("Reason");
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("reason"),
                new GUIContent("Reason", "Optional diagnostic metadata; it does not introduce gameplay-rule semantics."));
            FrameworkAuthoringInspectorGui.Section("Validation");
            if (GUILayout.Button("Validate"))
            {
                serializedObject.ApplyModifiedProperties();
                _hasValidation = true;
                _isValid = trigger.TryValidateConfiguration(out _validationIssue);
            }

            EditorGUILayout.LabelField("Status", !_hasValidation ? "Not Validated" : _isValid ? "Valid" : "Issue");
            if (_hasValidation && !_isValid)
            {
                EditorGUILayout.LabelField("Issue", _validationIssue, EditorStyles.wordWrappedMiniLabel);
            }

            _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
            if (_showAdvanced)
            {
                FrameworkAuthoringInspectorGui.Section("Diagnostics");
                EditorGUILayout.LabelField("Scoped Access", trigger.ScopedAccessState.ToString());
                EditorGUILayout.LabelField("Owns Block", trigger.OwnsActiveBlock.ToString());
                EditorGUILayout.LabelField("Last Diagnostic", trigger.LastDiagnostic, EditorStyles.wordWrappedMiniLabel);
                using (new EditorGUI.DisabledScope(!Application.isPlaying || targets.Length != 1))
                {
                    if (GUILayout.Button("Request Block")) trigger.RequestBlock();
                    if (GUILayout.Button("Request Release")) trigger.RequestRelease();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
