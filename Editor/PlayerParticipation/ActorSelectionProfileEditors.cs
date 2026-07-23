using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    [CustomEditor(typeof(ActorProfile))]
    internal sealed class ActorProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty _actorProfileId;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _icon;
        private SerializedProperty _actorKind;
        private SerializedProperty _actorRole;
        private SerializedProperty _logicalActorHostPrefab;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _actorProfileId = serializedObject.FindProperty("actorProfileId");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _icon = serializedObject.FindProperty("icon");
            _actorKind = serializedObject.FindProperty("actorKind");
            _actorRole = serializedObject.FindProperty("actorRole");
            _logicalActorHostPrefab = serializedObject.FindProperty("logicalActorHostPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Actor Profile", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Defines one immutable selectable Actor identity. Current Player Slot selection, ActorId and runtime materialization state never live in this asset.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_actorProfileId, new GUIContent("Actor Profile Id"));
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));
            EditorGUILayout.PropertyField(_icon, new GUIContent("Icon"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Classification", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_actorKind, new GUIContent("Actor Kind"));
            EditorGUILayout.PropertyField(_actorRole, new GUIContent("Actor Role"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Logical Composition", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _logicalActorHostPrefab,
                new GUIContent("Logical Actor Host Prefab"));
            EditorGUILayout.HelpBox(
                "The Profile references its canonical host explicitly but does not instantiate it. Runtime composition and presentation remain later materialization stages.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced / Debug", true);
            if (_showAdvanced)
            {
                DrawAdvanced((ActorProfile)target);
            }

            EditorGUILayout.Space(6);
            FrameworkAuthoringValidationReport report =
                PlayerActorSelectionAuthoringValidator.ValidateActorProfile(
                    (ActorProfile)target,
                    true);
            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private static void DrawAdvanced(ActorProfile profile)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Asset Path", AssetDatabase.GetAssetPath(profile));
                EditorGUILayout.TextField("Normalized Identity", profile.ActorProfileIdText);

                string typedIdentity = profile.TryGetActorProfileId(
                    out ActorProfileId actorProfileId,
                    out string issue)
                    ? actorProfileId.ToString()
                    : $"Invalid: {issue}";
                EditorGUILayout.TextField("Typed ActorProfileId", typedIdentity);
                EditorGUILayout.Toggle("Defined Actor Kind", profile.HasDefinedActorKind);
                EditorGUILayout.Toggle("Defined Actor Role", profile.HasDefinedActorRole);
                EditorGUILayout.Toggle("Logical Host Assigned", profile.HasLogicalActorHostPrefab);
            }
        }
    }

}
