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
                "Declares the one Session-authorized Unity PlayerInputManager. Framework Core injects the Session runtime after boot; Players are created only by explicit request operations.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Provisioning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                playerInputManager,
                new GUIContent("Player Input Manager"));
            EditorGUILayout.HelpBox(
                "The referenced manager must use Join Players Manually. The framework reserves a configured Slot before calling PlayerInputManager.JoinPlayer.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            LocalPlayerProvisioningAuthoring authoring =
                (LocalPlayerProvisioningAuthoring)target;

            EditorGUILayout.Space(6);
            DrawRuntimeState(authoring);

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

        private static void DrawRuntimeState(LocalPlayerProvisioningAuthoring authoring)
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Runtime binding is created during Framework boot. Open/Close Join and Request Join are explicit script/UI operations; the authoring component does not execute them automatically.",
                    MessageType.None);
                return;
            }

            PlayerParticipationSnapshot snapshot = authoring.RuntimeSnapshot;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Runtime Ready", authoring.RuntimeReady);
                EditorGUILayout.TextField("Context", snapshot.ContextId);
                EditorGUILayout.IntField("Configured Slots", snapshot.ConfiguredSlotCount);
                EditorGUILayout.IntField("Dynamic Capacity", snapshot.DynamicCapacity);
                EditorGUILayout.Toggle("Joining Open", snapshot.JoiningOpen);
                EditorGUILayout.IntField("Joined Slots", snapshot.JoinedCount);
                EditorGUILayout.EnumPopup(
                    "Last Join Status",
                    authoring.LastJoinResult != null
                        ? authoring.LastJoinResult.Status
                        : LocalPlayerJoinStatus.None);
            }

            EditorGUILayout.HelpBox(
                authoring.RuntimeDiagnostic,
                authoring.RuntimeReady ? MessageType.Info : MessageType.Warning);
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
                EditorGUILayout.Toggle(
                    "C# Join Notifications",
                    authoring.UsesCSharpJoinNotifications);
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
