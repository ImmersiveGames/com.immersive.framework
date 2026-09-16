using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(PlayerCameraOutputPolicyAuthoring))]
    public sealed class PlayerCameraOutputPolicyAuthoringEditor : UnityEditor.Editor
    {
        private readonly CameraOutputReferenceGUI _outputs =
            new CameraOutputReferenceGUI();

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.HelpBox(
                "Explicitly associates Player Slot Profile assets with Camera Output Definition assets. Runtime materialization contains only PlayerSlotId to CameraOutputId.",
                MessageType.Info);

            _outputs.DrawTopology(serializedObject);
            SerializedProperty bindings =
                serializedObject.FindProperty("bindings");
            if (bindings.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Add explicit Player Slot / Camera Output bindings when Player Camera integration is required.",
                    MessageType.Warning);
            }

            var slots = new HashSet<PlayerSlotId>();
            for (int index = 0; index < bindings.arraySize; index++)
            {
                SerializedProperty binding =
                    bindings.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"Binding {index + 1}",
                        EditorStyles.boldLabel);
                    SerializedProperty playerSlot =
                        binding.FindPropertyRelative("playerSlotProfile");
                    EditorGUILayout.PropertyField(
                        playerSlot,
                        new GUIContent("Player Slot Profile"));
                    DrawPlayerSlotIssue(playerSlot, slots);
                    _outputs.DrawReference(
                        binding.FindPropertyRelative("outputDefinition"));

                    if (GUILayout.Button("Remove Binding"))
                    {
                        bindings.DeleteArrayElementAtIndex(index);
                        break;
                    }
                }
            }

            if (GUILayout.Button("Add Binding"))
            {
                int index = bindings.arraySize;
                bindings.InsertArrayElementAtIndex(index);
                SerializedProperty added =
                    bindings.GetArrayElementAtIndex(index);
                added.FindPropertyRelative("playerSlotProfile")
                    .objectReferenceValue = null;
                added.FindPropertyRelative("outputDefinition")
                    .objectReferenceValue = null;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawPlayerSlotIssue(
            SerializedProperty property,
            ISet<PlayerSlotId> slots)
        {
            var profile =
                property.objectReferenceValue as PlayerSlotProfile;
            if (profile == null)
            {
                CameraIdentityAuthoringGUI.DrawIssue(
                    "Assign an exact Player Slot Profile asset.");
                return;
            }

            if (!profile.TryGetPlayerSlotId(
                    out PlayerSlotId playerSlotId,
                    out string issue))
            {
                CameraIdentityAuthoringGUI.DrawIssue(issue);
                return;
            }

            if (!slots.Add(playerSlotId))
            {
                CameraIdentityAuthoringGUI.DrawIssue(
                    $"Duplicate or conflicting binding for Player Slot '{playerSlotId.StableText}'.");
            }
        }
    }
}
