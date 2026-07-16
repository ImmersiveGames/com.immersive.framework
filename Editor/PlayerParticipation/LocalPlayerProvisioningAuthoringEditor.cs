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
        private SerializedProperty _playerInputManager;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _playerInputManager = serializedObject.FindProperty("playerInputManager");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Local Player Provisioning", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Declares the one Session-authorized PlayerInputManager. Its Player Prefab must be a stable Local Player Host, not a Logical Actor prefab.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Provisioning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _playerInputManager,
                new GUIContent("Player Input Manager"));
            EditorGUILayout.HelpBox(
                "The framework reserves a configured Slot, provisions the technical host, stages its Slot declaration and commits admission. Actor selection and materialization remain separate operations.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            LocalPlayerProvisioningAuthoring authoring =
                (LocalPlayerProvisioningAuthoring)target;

            EditorGUILayout.Space(6);
            DrawRuntimeState(authoring);

            EditorGUILayout.Space(6);
            _showAdvanced = EditorGUILayout.Foldout(
                _showAdvanced,
                "Advanced / Debug",
                true);
            if (_showAdvanced)
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
                    "Runtime binding is created during Framework boot. The authoring component never joins automatically from Unity lifecycle callbacks.",
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
                EditorGUILayout.ObjectField(
                    "Last Local Player Host",
                    authoring.LastJoinResult != null
                        ? authoring.LastJoinResult.LocalPlayerHost
                        : null,
                    typeof(LocalPlayerHostAuthoring),
                    true);
            }

            EditorGUILayout.HelpBox(
                authoring.RuntimeDiagnostic,
                authoring.RuntimeReady ? MessageType.Info : MessageType.Warning);
        }

        private static void DrawAdvanced(LocalPlayerProvisioningAuthoring authoring)
        {
            LocalPlayerHostAuthoring prefabHost = authoring.PlayerPrefab != null
                ? authoring.PlayerPrefab.GetComponent<LocalPlayerHostAuthoring>()
                : null;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Explicit Manager", authoring.HasPlayerInputManager);
                EditorGUILayout.Toggle("Manual Join", authoring.UsesManualJoin);
                EditorGUILayout.Toggle("C# Join Notifications", authoring.UsesCSharpJoinNotifications);
                EditorGUILayout.ObjectField(
                    "Player Prefab",
                    authoring.PlayerPrefab,
                    typeof(GameObject),
                    false);
                EditorGUILayout.ObjectField(
                    "Local Player Host",
                    prefabHost,
                    typeof(LocalPlayerHostAuthoring),
                    false);
                EditorGUILayout.ObjectField(
                    "Actor Mount",
                    prefabHost != null ? prefabHost.ActorMount : null,
                    typeof(Transform),
                    false);
                EditorGUILayout.IntField(
                    "Technical Max Players",
                    authoring.TechnicalMaxPlayerCount);
            }
        }

        private static void DrawValidation(LocalPlayerProvisioningAuthoring authoring)
        {
            GameApplicationAsset gameApplication = ResolveActiveGameApplication();
            FrameworkAuthoringValidationReport report =
                LocalPlayerProvisioningValidator.Validate(
                    authoring,
                    gameApplication);

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private static GameApplicationAsset ResolveActiveGameApplication()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            return settings != null ? settings.ActiveGameApplication : null;
        }
    }
}
