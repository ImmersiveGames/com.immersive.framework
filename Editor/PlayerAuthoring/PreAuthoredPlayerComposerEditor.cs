using Immersive.Framework.PlayerAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerAuthoring
{
    [CustomEditor(typeof(PreAuthoredPlayerComposer))]
    public sealed class PreAuthoredPlayerComposerEditor : UnityEditor.Editor
    {
        private SerializedProperty _recipe;
        private SerializedProperty _actorId;
        private SerializedProperty _validationMode;

        private SerializedProperty _controlEnabled;
        private SerializedProperty _playerInput;
        private SerializedProperty _gameplayActionMap;
        private SerializedProperty _inputBindingRequired;
        private SerializedProperty _gateParticipation;

        private SerializedProperty _cameraBindingRequired;
        private SerializedProperty _cameraTarget;
        private SerializedProperty _lookAtTarget;
        private SerializedProperty _lookAtPolicy;

        private SerializedProperty _resetEnabled;
        private SerializedProperty _resetScope;
        private SerializedProperty _resetParticipantPolicy;

        private SerializedProperty _frameworkBindingsRoot;
        private SerializedProperty _createBindingsRootIfMissing;
        private SerializedProperty _createAnchorsIfMissing;
        private SerializedProperty _logApplyRebuildDiagnostics;

        private bool _showAdvanced;
        private bool _showDebug;

        private void OnEnable()
        {
            _recipe = serializedObject.FindProperty("recipe");
            _actorId = serializedObject.FindProperty("actorId");
            _validationMode = serializedObject.FindProperty("validationMode");

            _controlEnabled = serializedObject.FindProperty("controlEnabled");
            _playerInput = serializedObject.FindProperty("playerInput");
            _gameplayActionMap = serializedObject.FindProperty("gameplayActionMap");
            _inputBindingRequired = serializedObject.FindProperty("inputBindingRequired");
            _gateParticipation = serializedObject.FindProperty("gateParticipation");

            _cameraBindingRequired = serializedObject.FindProperty("cameraBindingRequired");
            _cameraTarget = serializedObject.FindProperty("cameraTarget");
            _lookAtTarget = serializedObject.FindProperty("lookAtTarget");
            _lookAtPolicy = serializedObject.FindProperty("lookAtPolicy");

            _resetEnabled = serializedObject.FindProperty("resetEnabled");
            _resetScope = serializedObject.FindProperty("resetScope");
            _resetParticipantPolicy = serializedObject.FindProperty("resetParticipantPolicy");

            _frameworkBindingsRoot = serializedObject.FindProperty("frameworkBindingsRoot");
            _createBindingsRootIfMissing = serializedObject.FindProperty("createBindingsRootIfMissing");
            _createAnchorsIfMissing = serializedObject.FindProperty("createAnchorsIfMissing");
            _logApplyRebuildDiagnostics = serializedObject.FindProperty("logApplyRebuildDiagnostics");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Player Composer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Author one concrete Player. Apply/Rebuild materializes only identity, slot, Gate and optional Reset integration. Movement remains game-owned; camera rig materialization belongs to CameraRigComposer.",
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
            EditorGUILayout.PropertyField(_recipe);

            using (new EditorGUI.DisabledScope(_recipe.objectReferenceValue == null))
            {
                if (GUILayout.Button("Apply Recipe Defaults"))
                {
                    ApplyRecipeDefaults();
                }
            }

            EditorGUILayout.PropertyField(_actorId);
            EditorGUILayout.PropertyField(_validationMode);
        }

        private void DrawInput()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_controlEnabled);

            using (new EditorGUI.DisabledScope(!_controlEnabled.boolValue))
            {
                EditorGUILayout.PropertyField(_playerInput);
                DrawActionMapPopup();
                EditorGUILayout.PropertyField(_inputBindingRequired, new GUIContent("Required"));
                EditorGUILayout.PropertyField(_gateParticipation);
            }

            PlayerInputStatus status = ResolvePlayerInputStatus();
            EditorGUILayout.HelpBox(status.Message, status.Type);
        }

        private void DrawCamera()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Camera Anchors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_cameraBindingRequired);
            EditorGUILayout.PropertyField(_cameraTarget);
            EditorGUILayout.PropertyField(_lookAtPolicy);

            bool explicitLookAt = _lookAtPolicy.enumValueIndex == (int)PlayerComposerLookAtPolicy.ExplicitTarget;
            using (new EditorGUI.DisabledScope(!explicitLookAt))
            {
                EditorGUILayout.PropertyField(_lookAtTarget);
            }

            EditorGUILayout.HelpBox(
                "These are typed actor anchors. When required references are empty and automatic anchor creation is enabled, Apply/Rebuild creates Anchors/CameraTarget and Anchors/LookAtTarget as children of this logical Player object. CameraRigComposer may reference this PreAuthoredPlayerComposer and materialize the Cinemachine Follow/LookAt binding.",
                MessageType.None);
        }

        private void DrawReset()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_resetEnabled);

            using (new EditorGUI.DisabledScope(!_resetEnabled.boolValue))
            {
                EditorGUILayout.PropertyField(_resetScope);
                EditorGUILayout.PropertyField(_resetParticipantPolicy);
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
                    PreAuthoredPlayerComposerApplyRebuildUtility.ApplyOrRebuild((PreAuthoredPlayerComposer)target, true, true);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Validate"))
                {
                    serializedObject.ApplyModifiedProperties();
                    PreAuthoredPlayerComposerApplyRebuildUtility.Validate((PreAuthoredPlayerComposer)target, true);
                    serializedObject.Update();
                }
            }

            if (GUILayout.Button("Select Technical Root"))
            {
                PreAuthoredPlayerComposer composer = (PreAuthoredPlayerComposer)target;
                Selection.activeObject = composer.FrameworkBindingsRoot != null
                    ? composer.FrameworkBindingsRoot.gameObject
                    : composer.gameObject;
            }
        }

        private void DrawAdvanced()
        {
            EditorGUILayout.Space();
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (!_showAdvanced)
            {
                return;
            }

            EditorGUILayout.PropertyField(_frameworkBindingsRoot);
            EditorGUILayout.PropertyField(_createBindingsRootIfMissing);
            EditorGUILayout.PropertyField(_createAnchorsIfMissing);
            EditorGUILayout.PropertyField(_logApplyRebuildDiagnostics);
        }

        private void DrawDebug()
        {
            EditorGUILayout.Space();
            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug", true);
            if (!_showDebug)
            {
                return;
            }

            PlayerComposerDebugSnapshot snapshot = ((PreAuthoredPlayerComposer)target).CreateDebugSnapshot();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Actor Id", snapshot.ActorId);
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
            var composer = (PreAuthoredPlayerComposer)target;
            Undo.RecordObject(composer, "Apply Player Recipe Defaults");

            if (!composer.EditorApplyRecipeDefaults(true, out string issue))
            {
                Debug.LogWarning(
                    $"[Immersive.Framework][PreAuthoredPlayerComposer] Apply Recipe Defaults failed. player='{composer.name}' issue='{issue}'",
                    composer);
                serializedObject.Update();
                return;
            }

            EditorUtility.SetDirty(composer);
            Debug.Log(
                $"[Immersive.Framework][PreAuthoredPlayerComposer] Recipe defaults applied. player='{composer.name}' " +
                $"recipe='{composer.Recipe.name}' actorId='{composer.ActorId}'.",
                composer);
            serializedObject.Update();
        }

        private void DrawActionMapPopup()
        {
            var input = _playerInput.objectReferenceValue as UnityEngine.InputSystem.PlayerInput;
            if (input == null || input.actions == null || input.actions.actionMaps.Count == 0)
            {
                EditorGUILayout.PropertyField(
                    _gameplayActionMap,
                    new GUIContent("Default Action Map", "Authoring fallback used by Apply/Rebuild."));
                return;
            }

            string[] names = new string[input.actions.actionMaps.Count];
            int selectedIndex = -1;
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = input.actions.actionMaps[i].name;
                if (names[i] == _gameplayActionMap.stringValue)
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

            _gameplayActionMap.stringValue = names[nextIndex];
        }

        private PlayerInputStatus ResolvePlayerInputStatus()
        {
            if (!_controlEnabled.boolValue)
            {
                return new PlayerInputStatus("Control integration is disabled. No Gate adapter will be materialized.", MessageType.None);
            }

            var input = _playerInput.objectReferenceValue as UnityEngine.InputSystem.PlayerInput;
            if (input == null)
            {
                return new PlayerInputStatus(
                    _inputBindingRequired.boolValue
                        ? "Required PlayerInput is missing."
                        : "Optional PlayerInput is not assigned.",
                    _inputBindingRequired.boolValue ? MessageType.Error : MessageType.Warning);
            }

            if (input.actions == null)
            {
                return new PlayerInputStatus("PlayerInput has no InputActionAsset.", MessageType.Error);
            }

            string authoredMap = _gameplayActionMap.stringValue;
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
