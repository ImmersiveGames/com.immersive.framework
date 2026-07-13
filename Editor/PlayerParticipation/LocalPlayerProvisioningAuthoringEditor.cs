
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    [CustomEditor(typeof(LocalPlayerProvisioningAuthoring))]
    internal sealed class LocalPlayerProvisioningAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty playerInputManager;
        private bool showAdvanced;

        private void OnEnable()
        {
            playerInputManager = serializedObject.FindProperty("playerInputManager");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Local Player Provisioning", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Declares the one Session-authorized Unity PlayerInputManager used by framework manual join operations. This component does not join Players by itself.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Provisioning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                playerInputManager,
                new GUIContent("Player Input Manager"));
            EditorGUILayout.HelpBox(
                "Configure the referenced manager for Join Players Manually. Join requests will later reserve a framework Slot before calling PlayerInputManager.JoinPlayer.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            LocalPlayerProvisioningAuthoring authoring =
                (LocalPlayerProvisioningAuthoring)target;

            EditorGUILayout.Space(6);
            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                "Advanced / Debug",
                true);
            if (showAdvanced)
            {
                DrawAdvanced(authoring);
            }

            EditorGUILayout.Space(6);
            DrawValidation(authoring);
        }

        private static void DrawAdvanced(LocalPlayerProvisioningAuthoring authoring)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "Explicit Manager",
                    authoring.HasPlayerInputManager);
                EditorGUILayout.Toggle(
                    "Manual Join",
                    authoring.UsesManualJoin);
                EditorGUILayout.ObjectField(
                    "Player Prefab",
                    authoring.PlayerPrefab,
                    typeof(GameObject),
                    false);
                EditorGUILayout.IntField(
                    "Technical Max Players",
                    authoring.TechnicalMaxPlayerCount);
            }
        }

        private static void DrawValidation(
            LocalPlayerProvisioningAuthoring authoring)
        {
            GameApplicationAsset gameApplication = ResolveActiveGameApplication();
            FrameworkAuthoringValidationReport report =
                LocalPlayerProvisioningValidator.Validate(
                    authoring,
                    gameApplication);

            EditorGUILayout.LabelField(
                "Authoring Validation",
                EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private static GameApplicationAsset ResolveActiveGameApplication()
        {
            ImmersiveFrameworkSettingsAsset settings = Resources.Load<ImmersiveFrameworkSettingsAsset>(
                ImmersiveFrameworkSettingsAsset.ResourcesPath);
            return settings != null ? settings.ActiveGameApplication : null;
        }
    }
}
