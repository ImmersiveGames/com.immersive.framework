using Immersive.Framework.Actors;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Designer-first inspector shared by ActorDeclaration and specialized declarations.
    /// PlayerInput is runtime binding evidence for Player Actors and is not authored here.
    /// </summary>
    [CustomEditor(typeof(ActorDeclaration), true)]
    internal sealed class ActorDeclarationEditor : UnityEditor.Editor
    {
        private SerializedProperty _actorId;
        private SerializedProperty _actorKind;
        private SerializedProperty _actorRole;
        private SerializedProperty _displayName;
        private SerializedProperty _reason;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _actorId = serializedObject.FindProperty("actorId");
            _actorKind = serializedObject.FindProperty("actorKind");
            _actorRole = serializedObject.FindProperty("actorRole");
            _displayName = serializedObject.FindProperty("displayName");
            _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ActorDeclaration declaration = (ActorDeclaration)target;
            bool isPlayer = declaration is PlayerActorDeclaration;

            EditorGUILayout.LabelField(
                isPlayer ? "Player Actor Declaration" : "Actor Declaration",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                isPlayer
                    ? "Declares one contextual Logical Player Actor. PlayerInput belongs to the stable Local Player Host and is injected later by explicit composition."
                    : "Declares one Actor identity. Lifetime, materialization, presentation and gameplay remain owned by explicit runtime systems.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_actorId, new GUIContent("Actor Id"));
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Classification", EditorStyles.boldLabel);
            if (isPlayer)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.EnumPopup("Actor Kind", ActorKind.Player);
                    EditorGUILayout.EnumPopup("Actor Role", ActorRole.Protagonist);
                }
                EditorGUILayout.HelpBox(
                    "Player Actor classification is fixed. Do not add PlayerInput to the Logical Actor Host prefab.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.PropertyField(_actorKind, new GUIContent("Actor Kind"));
                EditorGUILayout.PropertyField(_actorRole, new GUIContent("Actor Role"));
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_reason, new GUIContent("Reason"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced / Debug", true);
            if (_showAdvanced)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Runtime Type", declaration.GetType().FullName);
                    EditorGUILayout.TextField("Actor Id", SafeActorId(declaration));
                    EditorGUILayout.EnumPopup("Effective Actor Kind", declaration.ActorKind);
                    EditorGUILayout.EnumPopup("Effective Actor Role", declaration.ActorRole);
                    if (declaration is PlayerActorDeclaration player)
                    {
                        EditorGUILayout.Toggle(
                            "Local Host PlayerInput Bound",
                            player.HasPlayerInputEvidence);
                        EditorGUILayout.ObjectField(
                            "Bound PlayerInput",
                            player.PlayerInput,
                            typeof(Object),
                            true);
                    }
                }
            }
        }

        private static string SafeActorId(ActorDeclaration declaration)
        {
            try
            {
                return declaration.ActorId.StableText;
            }
            catch
            {
                return "<invalid>";
            }
        }
    }
}
