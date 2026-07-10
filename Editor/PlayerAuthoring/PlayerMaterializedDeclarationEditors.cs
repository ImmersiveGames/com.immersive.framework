using Immersive.Framework.Actors;
using Immersive.Framework.PlayerAuthoring;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerAuthoring
{
    [CustomEditor(typeof(PlayerActorDeclaration))]
    public sealed class PlayerActorDeclarationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Player Actor Declaration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Technical materialization owned by PlayerComposer. Edit Actor identity and PlayerInput on the PlayerComposer, then run Apply/Rebuild.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawDefaultInspector();
            }

            DrawComposerButton((PlayerActorDeclaration)target);
        }

        private static void DrawComposerButton(Component declaration)
        {
            PlayerComposer composer = declaration.GetComponent<PlayerComposer>();
            if (composer == null)
            {
                EditorGUILayout.HelpBox(
                    "No PlayerComposer exists on this GameObject. This declaration is outside canonical Composer authority.",
                    MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Select Player Composer"))
            {
                Selection.activeObject = composer;
            }
        }
    }

    [CustomEditor(typeof(PlayerSlotDeclaration))]
    public sealed class PlayerSlotDeclarationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Player Slot Declaration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Technical materialization owned by PlayerComposer. Edit Slot identity and PlayerInput on the PlayerComposer, then run Apply/Rebuild.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawDefaultInspector();
            }

            var declaration = (PlayerSlotDeclaration)target;
            PlayerComposer composer = declaration.GetComponent<PlayerComposer>();
            if (composer == null)
            {
                EditorGUILayout.HelpBox(
                    "No PlayerComposer exists on this GameObject. This declaration is outside canonical Composer authority.",
                    MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Select Player Composer"))
            {
                Selection.activeObject = composer;
            }
        }
    }
}
