using Immersive.Framework.Actors;
using Immersive.Framework.PlayerAuthoring;
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
                "Technical materialization owned by PreAuthoredPlayerComposer. Edit Actor identity and PlayerInput on the PreAuthoredPlayerComposer, then run Apply/Rebuild.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawDefaultInspector();
            }

            DrawComposerButton((PlayerActorDeclaration)target);
        }

        private static void DrawComposerButton(Component declaration)
        {
            PreAuthoredPlayerComposer composer = declaration.GetComponent<PreAuthoredPlayerComposer>();
            if (composer == null)
            {
                EditorGUILayout.HelpBox(
                    "No PreAuthoredPlayerComposer exists on this GameObject. This declaration is outside canonical Composer authority.",
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
