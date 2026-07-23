using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSlotProfile))]
    internal sealed class PlayerSlotProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty _playerSlotId;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _accentColor;
        private SerializedProperty _icon;
        private SerializedProperty _displayOrder;
        private SerializedProperty _defaultActorProfile;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _playerSlotId = serializedObject.FindProperty("playerSlotId");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _accentColor = serializedObject.FindProperty("accentColor");
            _icon = serializedObject.FindProperty("icon");
            _displayOrder = serializedObject.FindProperty("displayOrder");
            _defaultActorProfile = serializedObject.FindProperty("defaultActorProfile");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Player Slot Profile", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Defines one immutable local participation seat. Runtime join, selection, occupancy and device state never live in this asset.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_playerSlotId, new GUIContent("Player Slot Id"));
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Presentation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_accentColor, new GUIContent("Accent Color"));
            EditorGUILayout.PropertyField(_icon, new GUIContent("Icon"));
            EditorGUILayout.PropertyField(_displayOrder, new GUIContent("Display Order"));
            EditorGUILayout.HelpBox(
                "Display Order is presentation metadata. Game Application array order controls default local Slot allocation.",
                MessageType.None);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Actor Selection Default", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _defaultActorProfile,
                new GUIContent("Default Actor Profile"));
            EditorGUILayout.HelpBox(
                "Optional static intent only. The Slot may remain Joined and unselected; Session runtime must apply this default through the same explicit selection transaction as any UI request.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced / Debug", true);
            if (_showAdvanced)
            {
                DrawAdvanced((PlayerSlotProfile)target);
            }

            EditorGUILayout.Space(6);
            DrawValidation((PlayerSlotProfile)target);
        }

        private static void DrawAdvanced(PlayerSlotProfile profile)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Asset Path", AssetDatabase.GetAssetPath(profile));
                EditorGUILayout.TextField("Normalized Identity", profile.PlayerSlotIdText);

                string typedIdentity = profile.TryGetPlayerSlotId(out var playerSlotId, out string issue)
                    ? playerSlotId.ToString()
                    : $"Invalid: {issue}";
                EditorGUILayout.TextField("Typed PlayerSlotId", typedIdentity);
                EditorGUILayout.Toggle("Has Default Actor", profile.HasDefaultActorProfile);

                ActorProfile defaultActor = profile.DefaultActorProfile;
                string defaultIdentity = defaultActor != null &&
                    defaultActor.TryGetActorProfileId(out ActorProfileId actorProfileId, out _)
                    ? actorProfileId.ToString()
                    : string.Empty;
                EditorGUILayout.TextField("Default ActorProfileId", defaultIdentity);
            }
        }

        private static void DrawValidation(PlayerSlotProfile profile)
        {
            FrameworkAuthoringValidationReport report =
                PlayerParticipationAuthoringValidator.ValidatePlayerSlotProfile(profile, true);
            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }

}
