using Immersive.Framework.Actors;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Designer-first inspector shared by ActorDeclaration and specialized declarations.
    /// Fixed specialization classification is shown read-only and hidden serialized backing fields
    /// are not exposed as editable product intent.
    /// </summary>
    [CustomEditor(typeof(ActorDeclaration), true)]
    internal sealed class ActorDeclarationEditor : UnityEditor.Editor
    {
        private SerializedProperty _actorId;
        private SerializedProperty _actorKind;
        private SerializedProperty _actorRole;
        private SerializedProperty _displayName;
        private SerializedProperty _reason;
        private SerializedProperty _playerInput;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _actorId = serializedObject.FindProperty("actorId");
            _actorKind = serializedObject.FindProperty("actorKind");
            _actorRole = serializedObject.FindProperty("actorRole");
            _displayName = serializedObject.FindProperty("displayName");
            _reason = serializedObject.FindProperty("reason");
            _playerInput = serializedObject.FindProperty("playerInput");
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
                "Declares one Actor identity. Lifetime, materialization, input behavior, presentation and gameplay remain owned by their explicit runtime systems.",
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
                    "Player Actor classification is fixed by the specialized declaration and cannot drift from its base Actor identity.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.PropertyField(_actorKind, new GUIContent("Actor Kind"));
                EditorGUILayout.PropertyField(_actorRole, new GUIContent("Actor Role"));
            }

            if (isPlayer && _playerInput != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Player Evidence", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_playerInput, new GUIContent("Player Input"));
                EditorGUILayout.HelpBox(
                    "Same-object PlayerInput evidence is retained for P3G compatibility. P3J.2 moves PlayerInput authority to LocalPlayerHostAuthoring.",
                    MessageType.None);
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
                    EditorGUILayout.Toggle(
                        "Canonical Base Declaration",
                        declaration is ActorDeclaration);
                    if (declaration is PlayerActorDeclaration player)
                    {
                        EditorGUILayout.Toggle("PlayerInput Evidence", player.HasPlayerInputEvidence);
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
