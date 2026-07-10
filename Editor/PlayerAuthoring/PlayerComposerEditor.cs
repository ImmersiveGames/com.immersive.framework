using Immersive.Framework.PlayerAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerAuthoring
{
    [CustomEditor(typeof(PlayerComposer))]
    public sealed class PlayerComposerEditor : UnityEditor.Editor
    {
        private SerializedProperty recipe;
        private SerializedProperty actorId;
        private SerializedProperty playerSlotId;
        private SerializedProperty validationMode;

        private SerializedProperty controlEnabled;
        private SerializedProperty playerInput;
        private SerializedProperty gameplayActionMap;
        private SerializedProperty inputBindingRequired;
        private SerializedProperty gateParticipation;

        private SerializedProperty cameraBindingRequired;
        private SerializedProperty cameraTarget;
        private SerializedProperty lookAtTarget;
        private SerializedProperty lookAtPolicy;

        private SerializedProperty resetEnabled;
        private SerializedProperty resetScope;
        private SerializedProperty resetParticipantPolicy;

        private SerializedProperty frameworkBindingsRoot;
        private SerializedProperty createBindingsRootIfMissing;
        private SerializedProperty createAnchorsIfMissing;
        private SerializedProperty logApplyRebuildDiagnostics;

        private bool showAdvanced;
        private bool showDebug;

        private void OnEnable()
        {
            recipe = serializedObject.FindProperty("recipe");
            actorId = serializedObject.FindProperty("actorId");
            playerSlotId = serializedObject.FindProperty("playerSlotId");
            validationMode = serializedObject.FindProperty("validationMode");

            controlEnabled = serializedObject.FindProperty("controlEnabled");
            playerInput = serializedObject.FindProperty("playerInput");
            gameplayActionMap = serializedObject.FindProperty("gameplayActionMap");
            inputBindingRequired = serializedObject.FindProperty("inputBindingRequired");
            gateParticipation = serializedObject.FindProperty("gateParticipation");

            cameraBindingRequired = serializedObject.FindProperty("cameraBindingRequired");
            cameraTarget = serializedObject.FindProperty("cameraTarget");
            lookAtTarget = serializedObject.FindProperty("lookAtTarget");
            lookAtPolicy = serializedObject.FindProperty("lookAtPolicy");

            resetEnabled = serializedObject.FindProperty("resetEnabled");
            resetScope = serializedObject.FindProperty("resetScope");
            resetParticipantPolicy = serializedObject.FindProperty("resetParticipantPolicy");

            frameworkBindingsRoot = serializedObject.FindProperty("frameworkBindingsRoot");
            createBindingsRootIfMissing = serializedObject.FindProperty("createBindingsRootIfMissing");
            createAnchorsIfMissing = serializedObject.FindProperty("createAnchorsIfMissing");
            logApplyRebuildDiagnostics = serializedObject.FindProperty("logApplyRebuildDiagnostics");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Player Composer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Author one concrete Player. Apply/Rebuild materializes only identity, slot, Gate and optional Reset integration. Movement remains game-owned; camera binding belongs to CameraComposer.",
                MessageType.Info);

            DrawDesigner();
            DrawInput();
            DrawCamera();
            DrawReset();
            DrawActions();
            DrawAdvanced();
            DrawDebug();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDesigner()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Designer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(recipe);

            using (new EditorGUI.DisabledScope(recipe.objectReferenceValue == null))
            {
                if (GUILayout.Button("Apply Recipe Defaults"))
                {
                    ApplyRecipeDefaults();
                }
            }

            EditorGUILayout.PropertyField(actorId);
            EditorGUILayout.PropertyField(playerSlotId);
            EditorGUILayout.PropertyField(validationMode);
        }

        private void DrawInput()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(controlEnabled);

            using (new EditorGUI.DisabledScope(!controlEnabled.boolValue))
            {
                EditorGUILayout.PropertyField(playerInput);
                DrawActionMapPopup();
                EditorGUILayout.PropertyField(inputBindingRequired, new GUIContent("Required"));
                EditorGUILayout.PropertyField(gateParticipation);
            }

            PlayerInputStatus status = ResolvePlayerInputStatus();
            EditorGUILayout.HelpBox(status.Message, status.Type);
        }

        private void DrawCamera()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Camera Anchors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cameraBindingRequired);
            EditorGUILayout.PropertyField(cameraTarget);
            EditorGUILayout.PropertyField(lookAtPolicy);

            bool explicitLookAt = lookAtPolicy.enumValueIndex == (int)PlayerComposerLookAtPolicy.ExplicitTarget;
            using (new EditorGUI.DisabledScope(!explicitLookAt))
            {
                EditorGUILayout.PropertyField(lookAtTarget);
            }

            EditorGUILayout.HelpBox(
                "These are typed actor anchors. When required references are empty and automatic anchor creation is enabled, Apply/Rebuild creates Anchors/CameraTarget and Anchors/LookAtTarget as children of this logical Player object. CameraComposer must reference this PlayerComposer and materialize the Cinemachine Follow/LookAt binding.",
                MessageType.None);
        }

        private void DrawReset()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(resetEnabled);

            using (new EditorGUI.DisabledScope(!resetEnabled.boolValue))
            {
                EditorGUILayout.PropertyField(resetScope);
                EditorGUILayout.PropertyField(resetParticipantPolicy);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply / Rebuild"))
                {
                    serializedObject.ApplyModifiedProperties();
                    PlayerComposerApplyRebuildUtility.ApplyOrRebuild((PlayerComposer)target, true, true);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Validate"))
                {
                    serializedObject.ApplyModifiedProperties();
                    PlayerComposerApplyRebuildUtility.Validate((PlayerComposer)target, true);
                    serializedObject.Update();
                }
            }

            if (GUILayout.Button("Select Technical Root"))
            {
                PlayerComposer composer = (PlayerComposer)target;
                Selection.activeObject = composer.FrameworkBindingsRoot != null
                    ? composer.FrameworkBindingsRoot.gameObject
                    : composer.gameObject;
            }
        }

        private void DrawAdvanced()
        {
            EditorGUILayout.Space();
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
            if (!showAdvanced)
            {
                return;
            }

            EditorGUILayout.PropertyField(frameworkBindingsRoot);
            EditorGUILayout.PropertyField(createBindingsRootIfMissing);
            EditorGUILayout.PropertyField(createAnchorsIfMissing);
            EditorGUILayout.PropertyField(logApplyRebuildDiagnostics);
        }

        private void DrawDebug()
        {
            EditorGUILayout.Space();
            showDebug = EditorGUILayout.Foldout(showDebug, "Debug", true);
            if (!showDebug)
            {
                return;
            }

            PlayerComposerDebugSnapshot snapshot = ((PlayerComposer)target).CreateDebugSnapshot();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Actor Id", snapshot.ActorId);
                EditorGUILayout.TextField("Player Slot Id", snapshot.PlayerSlotId);
                EditorGUILayout.Toggle("Control Enabled", snapshot.ControlEnabled);
                EditorGUILayout.TextField("PlayerInput", snapshot.PlayerInputName);
                EditorGUILayout.TextField("Authored Default Action Map", snapshot.GameplayActionMap);
                EditorGUILayout.Toggle("Default Map Found", snapshot.ActionMapFound);
                EditorGUILayout.Toggle("Gate Participation", snapshot.GateParticipation);
                EditorGUILayout.TextField("Technical Root", snapshot.FrameworkBindingsRootName);
                EditorGUILayout.TextField("Camera Target", snapshot.CameraTargetName);
                EditorGUILayout.TextField("Look At Target", snapshot.LookAtTargetName);
                EditorGUILayout.Toggle("Reset Enabled", snapshot.ResetEnabled);
                EditorGUILayout.TextField("Last Status", snapshot.LastApplyRebuildStatus);
                EditorGUILayout.TextField("Last Blocking Issue", snapshot.LastBlockingIssue);
                EditorGUILayout.TextArea(snapshot.LastMaterializationSummary, GUILayout.MinHeight(72));
            }
        }

        private void ApplyRecipeDefaults()
        {
            serializedObject.ApplyModifiedProperties();
            var composer = (PlayerComposer)target;
            Undo.RecordObject(composer, "Apply Player Recipe Defaults");

            if (!composer.EditorApplyRecipeDefaults(true, out string issue))
            {
                Debug.LogWarning(
                    $"[Immersive.Framework][PlayerComposer] Apply Recipe Defaults failed. player='{composer.name}' issue='{issue}'",
                    composer);
                serializedObject.Update();
                return;
            }

            EditorUtility.SetDirty(composer);
            Debug.Log(
                $"[Immersive.Framework][PlayerComposer] Recipe defaults applied. player='{composer.name}' " +
                $"recipe='{composer.Recipe.name}' actorId='{composer.ActorId}' playerSlotId='{composer.PlayerSlotId}'.",
                composer);
            serializedObject.Update();
        }

        private void DrawActionMapPopup()
        {
            var input = playerInput.objectReferenceValue as UnityEngine.InputSystem.PlayerInput;
            if (input == null || input.actions == null || input.actions.actionMaps.Count == 0)
            {
                EditorGUILayout.PropertyField(
                    gameplayActionMap,
                    new GUIContent("Default Action Map", "Authoring fallback used by Apply/Rebuild."));
                return;
            }

            string[] names = new string[input.actions.actionMaps.Count];
            int selectedIndex = -1;
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = input.actions.actionMaps[i].name;
                if (names[i] == gameplayActionMap.stringValue)
                {
                    selectedIndex = i;
                }
            }

            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            int nextIndex = EditorGUILayout.Popup(
                new GUIContent(
                    "Default Action Map",
                    "Serialized authoring default. Apply/Rebuild writes it to PlayerInput.defaultActionMap; runtime pipelines may replace the active map."),
                selectedIndex,
                names);

            gameplayActionMap.stringValue = names[nextIndex];
        }

        private PlayerInputStatus ResolvePlayerInputStatus()
        {
            if (!controlEnabled.boolValue)
            {
                return new PlayerInputStatus("Control integration is disabled. No Gate adapter will be materialized.", MessageType.None);
            }

            var input = playerInput.objectReferenceValue as UnityEngine.InputSystem.PlayerInput;
            if (input == null)
            {
                return new PlayerInputStatus(
                    inputBindingRequired.boolValue
                        ? "Required PlayerInput is missing."
                        : "Optional PlayerInput is not assigned.",
                    inputBindingRequired.boolValue ? MessageType.Error : MessageType.Warning);
            }

            if (input.actions == null)
            {
                return new PlayerInputStatus("PlayerInput has no InputActionAsset.", MessageType.Error);
            }

            string authoredMap = gameplayActionMap.stringValue;
            if (string.IsNullOrWhiteSpace(authoredMap))
            {
                return new PlayerInputStatus("Authored Default Action Map is empty.", MessageType.Error);
            }

            if (input.actions.FindActionMap(authoredMap, false) == null)
            {
                return new PlayerInputStatus(
                    $"Authored Default Action Map '{authoredMap}' does not exist in the assigned InputActionAsset.",
                    MessageType.Error);
            }

            return new PlayerInputStatus(
                $"Apply/Rebuild will set PlayerInput.defaultActionMap to '{authoredMap}'. Runtime pipelines may switch the active map later.",
                MessageType.Info);
        }

        private readonly struct PlayerInputStatus
        {
            public PlayerInputStatus(string message, MessageType type)
            {
                Message = message;
                Type = type;
            }

            public string Message { get; }
            public MessageType Type { get; }
        }
    }
}
