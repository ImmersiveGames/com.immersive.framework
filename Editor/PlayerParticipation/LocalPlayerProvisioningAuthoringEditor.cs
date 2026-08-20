using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(LocalPlayerProvisioningAuthoring))]
    internal sealed class LocalPlayerProvisioningAuthoringEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _playerInputManager;
        private SerializedProperty _localPlayerHostPrefab;

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _validationOutdated;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _playerInputManager =
                serializedObject.FindProperty(
                    "playerInputManager");
            _localPlayerHostPrefab =
                serializedObject.FindProperty(
                    "localPlayerHostPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            LocalPlayerProvisioningAuthoring authoring =
                (LocalPlayerProvisioningAuthoring)target;

            EditorGUI.BeginChangeCheck();
            DrawProvisioningSetup();
            bool authoringChanged =
                EditorGUI.EndChangeCheck();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (authoringChanged || modified)
            {
                MarkValidationOutdated();
            }

            DrawExplicitMigrationAction();
            DrawDerivedPlayerInputManagerLimit(authoring);
            DrawActions(authoring);
            DrawValidationSummary();
            DrawAdvanced(authoring);
        }

        private void DrawProvisioningSetup()
        {
            DrawSection("Provisioning Setup");

            EditorGUILayout.PropertyField(
                _playerInputManager,
                new GUIContent(
                    "Player Input Manager",
                    "Explicit PlayerInputManager authorized by this Session. Automatic manager discovery is not used."));

            EditorGUILayout.PropertyField(
                _localPlayerHostPrefab,
                new GUIContent(
                    "Local Player Host Prefab",
                    "Technical Host prefab used for future Manager-Provisioned Players. It must contain PlayerInput and LocalPlayerHostAuthoring. It is not a Logical Actor prefab."));
        }

        private void DrawExplicitMigrationAction()
        {
            PlayerInputManager manager =
                _playerInputManager.objectReferenceValue
                    as PlayerInputManager;

            if (_localPlayerHostPrefab.objectReferenceValue != null ||
                manager == null ||
                manager.playerPrefab == null)
            {
                return;
            }

            DrawSection("Migration");

            EditorGUILayout.HelpBox(
                "The PlayerInputManager already references a Player Prefab while the framework-owned Local Player Host Prefab is empty. Adopt it explicitly only when that prefab is the intended technical Host.",
                MessageType.Warning);

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying))
            {
                if (!GUILayout.Button(
                        new GUIContent(
                            "Use Existing Manager Prefab",
                            "Assign the current PlayerInputManager Player Prefab as the authored Local Player Host Prefab. Existing authored references are never replaced.")))
                {
                    return;
                }

                Undo.RecordObjects(
                    serializedObject.targetObjects,
                    "Use Existing Manager Player Prefab");

                _localPlayerHostPrefab.objectReferenceValue =
                    manager.playerPrefab;

                serializedObject.ApplyModifiedProperties();

                foreach (Object item in
                         serializedObject.targetObjects)
                {
                    EditorUtility.SetDirty(item);
                    PrefabUtility
                        .RecordPrefabInstancePropertyModifications(
                            item);
                }

                MarkValidationOutdated();
            }
        }

        private void DrawActions(
            LocalPlayerProvisioningAuthoring authoring)
        {
            DrawSection("Actions");

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Validate Provisioning Setup",
                            "Validate the authored manager, technical Host prefab and Game Application Supported Slots. This does not create or inspect a runtime Player.")))
                {
                    serializedObject.ApplyModifiedProperties();

                    _lastValidationReport =
                        LocalPlayerProvisioningValidator.Validate(
                            authoring,
                            ResolveActiveGameApplication());

                    _validationOutdated = false;
                }
            }
        }

        private void DrawDerivedPlayerInputManagerLimit(
            LocalPlayerProvisioningAuthoring authoring)
        {
            PlayerInputManager manager = authoring.PlayerInputManager;
            GameApplicationAsset gameApplication =
                ResolveActiveGameApplication();
            if (!TryGetConfiguredSlotCount(
                    gameApplication,
                    out int configuredSlotCount,
                    out string issue))
            {
                return;
            }

            DrawSection("Derived PlayerInputManager Limit");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(
                    "Supported Slots",
                    configuredSlotCount);
                EditorGUILayout.IntField(
                    "Manager Limit",
                    manager != null ? manager.maxPlayerCount : 0);
            }

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying || manager == null ||
                       manager.maxPlayerCount == configuredSlotCount))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Apply Derived Player Limit",
                            "Writes PlayerInputManager's serialized limit from the active Game Application Player Session Profile Supported Slots.")))
                {
                    ApplyDerivedPlayerInputManagerLimit(
                        manager,
                        configuredSlotCount);
                    MarkValidationOutdated();
                }
            }
        }

        private static bool TryGetConfiguredSlotCount(
            GameApplicationAsset gameApplication,
            out int configuredSlotCount,
            out string issue)
        {
            configuredSlotCount = 0;
            if (gameApplication == null)
            {
                issue = "No active Game Application is available.";
                return false;
            }

            if (!gameApplication.PlayerSessionEnabled)
            {
                issue = "The active Game Application has Player Session disabled.";
                return false;
            }

            PlayerSessionProfile profile =
                gameApplication.DefaultPlayerSessionProfile;
            if (profile == null)
            {
                issue = "The active Game Application has no Player Session Profile.";
                return false;
            }

            if (!profile.TryValidate(out issue))
            {
                return false;
            }

            configuredSlotCount = profile.SupportedSlotCount;
            if (configuredSlotCount <= 0)
            {
                issue =
                    "The active Player Session Profile has no Supported Slots.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static void ApplyDerivedPlayerInputManagerLimit(
            PlayerInputManager manager,
            int configuredSlotCount)
        {
            var managerSerializedObject = new SerializedObject(manager);
            SerializedProperty maxPlayerCount =
                managerSerializedObject.FindProperty("m_MaxPlayerCount");
            if (maxPlayerCount == null)
            {
                Debug.LogError(
                    $"PlayerInputManager '{manager.name}' does not expose the serialized max player limit required by this installed Input System version.",
                    manager);
                return;
            }

            Undo.RecordObject(manager, "Apply Derived PlayerInputManager Limit");
            maxPlayerCount.intValue = configuredSlotCount;
            managerSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
        }

        private void DrawValidationSummary()
        {
            DrawSection("Validation Summary");

            if (_lastValidationReport == null)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated");
                return;
            }

            if (_validationOutdated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated — configuration changed");
                return;
            }

            if (_lastValidationReport.IsValid)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Valid");
                return;
            }

            EditorGUILayout.LabelField(
                "Status",
                "Invalid");

            EditorGUILayout.HelpBox(
                $"{_lastValidationReport.ErrorCount} blocking issue(s) were found. Correct the provisioning setup and validate again.",
                MessageType.Error);

            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);
        }

        private void DrawAdvanced(
            LocalPlayerProvisioningAuthoring authoring)
        {
            EditorGUILayout.Space(6f);

            _showAdvanced =
                EditorGUILayout.Foldout(
                    _showAdvanced,
                    "Advanced / Debug",
                    true);

            if (!_showAdvanced)
            {
                return;
            }

            DrawManagerConfiguration(authoring);
            DrawPrefabMaterialization(authoring);
            DrawHostEvidence(authoring);

            if (Application.isPlaying)
            {
                DrawRuntimeEvidence(authoring);
            }

            DrawValidationEvidence();
        }

        private static void DrawManagerConfiguration(
            LocalPlayerProvisioningAuthoring authoring)
        {
            DrawSection("Manager Configuration");

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

                EditorGUILayout.IntField(
                    "PlayerInputManager Limit (Derived Bridge)",
                    authoring.PlayerInputManager != null
                        ? authoring.PlayerInputManager.maxPlayerCount
                        : 0);
            }
        }

        private static void DrawPrefabMaterialization(
            LocalPlayerProvisioningAuthoring authoring)
        {
            DrawSection("Prefab Materialization");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Authored Host Prefab",
                    authoring.LocalPlayerHostPrefab,
                    typeof(GameObject),
                    false);

                EditorGUILayout.ObjectField(
                    "Manager Player Prefab",
                    authoring.PlayerInputManager != null
                        ? authoring.PlayerInputManager
                            .playerPrefab
                        : null,
                    typeof(GameObject),
                    false);

                EditorGUILayout.Toggle(
                    "Materialized",
                    authoring.IsManagerPrefabMaterialized);

                EditorGUILayout.Toggle(
                    "Divergent",
                    authoring.HasManagerPrefabDivergence);
            }
        }

        private static void DrawHostEvidence(
            LocalPlayerProvisioningAuthoring authoring)
        {
            DrawSection("Host Evidence");

            LocalPlayerHostAuthoring prefabHost =
                authoring.LocalPlayerHostPrefab != null
                    ? authoring.LocalPlayerHostPrefab
                        .GetComponent<
                            LocalPlayerHostAuthoring>()
                    : null;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Local Player Host",
                    prefabHost,
                    typeof(LocalPlayerHostAuthoring),
                    false);

                EditorGUILayout.ObjectField(
                    "Actor Mount",
                    prefabHost != null
                        ? prefabHost.ActorMount
                        : null,
                    typeof(Transform),
                    false);
            }
        }

        private static void DrawRuntimeEvidence(
            LocalPlayerProvisioningAuthoring authoring)
        {
            DrawSection("Runtime Evidence");

            PlayerParticipationSnapshot snapshot =
                authoring.RuntimeSnapshot;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(
                    "Runtime",
                    authoring.RuntimeReady
                        ? "Ready"
                        : "Unavailable");

                EditorGUILayout.TextField(
                    "Context",
                    snapshot.ContextId);

                EditorGUILayout.IntField(
                    "Configured Slots",
                    snapshot.ConfiguredSlotCount);


                EditorGUILayout.Toggle(
                    "Joining Open",
                    snapshot.JoiningOpen);

                EditorGUILayout.IntField(
                    "Joined Slots",
                    snapshot.JoinedCount);

                EditorGUILayout.TextField(
                    "Last Join Status",
                    authoring.LastJoinResult != null
                        ? authoring.LastJoinResult.Status
                            .ToString()
                        : "None");

                EditorGUILayout.ObjectField(
                    "Last Local Player Host",
                    authoring.LastJoinResult != null
                        ? authoring.LastJoinResult
                            .LocalPlayerHost
                        : null,
                    typeof(LocalPlayerHostAuthoring),
                    true);
            }

            if (!string.IsNullOrWhiteSpace(
                    authoring.RuntimeDiagnostic))
            {
                EditorGUILayout.TextArea(
                    authoring.RuntimeDiagnostic,
                    GUILayout.MinHeight(42f));
            }
        }

        private void DrawValidationEvidence()
        {
            if (_lastValidationReport == null)
            {
                return;
            }

            DrawSection("Validation Evidence");

            if (_validationOutdated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated — configuration changed");
                return;
            }

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastValidationReport);

            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);
        }

        private void MarkValidationOutdated()
        {
            if (_lastValidationReport == null)
            {
                return;
            }

            _validationOutdated = true;
            Repaint();
        }

        private static GameApplicationAsset
            ResolveActiveGameApplication()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<
                    ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset
                        .ResourcesPath);

            return settings != null
                ? settings.ActiveGameApplication
                : null;
        }

        private static void DrawSection(
            string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }
    }
}
