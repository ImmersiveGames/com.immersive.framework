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
            var declaration = (PlayerActorDeclaration)target;
            PreAuthoredPlayerComposer composer =
                declaration.GetComponent<PreAuthoredPlayerComposer>();

            EditorGUILayout.LabelField(
                "Player Actor Declaration",
                EditorStyles.boldLabel);

            if (composer != null)
            {
                EditorGUILayout.HelpBox(
                    "This declaration is technical materialization owned by the alternative Pre-Authored Player Composer model. Edit Actor identity and PlayerInput on the Pre-Authored Player Composer, then run Apply/Rebuild.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "This declaration is valid on a join-based Logical Actor Host prefab. The canonical local join/materialization workflow injects runtime Actor identity and PlayerInput evidence when the selected ActorProfile is prepared. A Pre-Authored Player Composer is not required in this context.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                DrawDefaultInspector();
            }

            if (composer != null &&
                GUILayout.Button("Select Pre-Authored Player Composer"))
            {
                Selection.activeObject = composer;
            }
        }
    }
}