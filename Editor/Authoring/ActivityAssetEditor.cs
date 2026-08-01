using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Editor.Settings;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityAsset))]
    internal sealed class ActivityAssetEditor : UnityEditor.Editor
    {
        private const int ActivityScenePickerControlId = 1842201;
        private static readonly GUIContent ActivityNameLabel =
            new GUIContent(
                "Activity Name",
                "Designer-facing name used for presentation and diagnostics.");

        private static readonly GUIContent DescriptionLabel =
            new GUIContent(
                "Description",
                "Optional note describing the purpose of this Activity.");

        private static readonly GUIContent ParticipationLabel =
            new GUIContent(
                "Who Participates",
                "Selects which local Player Slots participate in this Activity.");

        private static readonly GUIContent ExplicitSlotsLabel =
            new GUIContent(
                "Specific Player Slots",
                "Ordered Player Slot Profiles included when participation uses explicit slots.");

        private static readonly GUIContent ZeroParticipantsLabel =
            new GUIContent(
                "If No Players Are Available",
                "Allowed lets this Activity continue with zero admitted participants. Rejected blocks that state.");

        private static readonly GUIContent ReadinessLabel =
            new GUIContent(
                "Ready When",
                "Minimum cumulative readiness required from every participating Player: joined, Actor selected, logical Actor prepared, or fully gameplay ready.");

        private static readonly GUIContent ContentProfileLabel =
            new GUIContent(
                "Content Profile",
                "Optional Activity-owned scenes composed and released with this Activity.");

        private static readonly GUIContent PresentationLabel =
            new GUIContent(
                "Presentation",
                "Controls whether Activity changes are seamless, use a fade, or use fade with loading presentation.");

        private static readonly GUIContent TransitionGateLabel =
            new GUIContent(
                "Block During Transition",
                "Controls which requests and capabilities remain blocked while this Activity transition runs.");

        private static readonly GUIContent ValidateLabel =
            new GUIContent(
                "Validate",
                "Validates this Activity and its configured dependencies without modifying them.");

        private SerializedProperty _activityName;
        private SerializedProperty _activityId;
        private SerializedProperty _description;
        private SerializedProperty _playerParticipationProjectionMode;
        private SerializedProperty _playerParticipationZeroParticipantPolicy;
        private SerializedProperty _playerParticipationExplicitSlotProfiles;
        private SerializedProperty _playerParticipationRequirementLevel;
        private SerializedProperty _activityContentProfile;
        private SerializedProperty _visualTransitionMode;
        private SerializedProperty _transitionGateMode;

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _serializedBindingsDirty = true;
        private bool _validationOutdated;
        private bool _showAdvancedDebug;
        private string _sceneActionMessage = string.Empty;

        private void OnEnable()
        {
            _serializedBindingsDirty = true;
        }

        private void RefreshSerializedBindings()
        {
            _activityName =
                serializedObject.FindProperty("activityName");
            _activityId =
                serializedObject.FindProperty("activityId");
            _description =
                serializedObject.FindProperty("description");
            _playerParticipationProjectionMode =
                serializedObject.FindProperty(
                    "playerParticipationProjectionMode");
            _playerParticipationZeroParticipantPolicy =
                serializedObject.FindProperty(
                    "playerParticipationZeroParticipantPolicy");
            _playerParticipationExplicitSlotProfiles =
                serializedObject.FindProperty(
                    "playerParticipationExplicitSlotProfiles");
            _playerParticipationRequirementLevel =
                serializedObject.FindProperty(
                    "playerParticipationRequirementLevel");
            _activityContentProfile =
                serializedObject.FindProperty(
                    "activityContentProfile");
            _visualTransitionMode =
                serializedObject.FindProperty(
                    "visualTransitionMode");
            _transitionGateMode =
                serializedObject.FindProperty(
                    "transitionGateMode");

            _serializedBindingsDirty = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            if (_serializedBindingsDirty)
            {
                RefreshSerializedBindings();
            }

            HandleActivityScenePicker();

            EditorGUILayout.LabelField(
                "Activity",
                EditorStyles.boldLabel);

            DrawOverview();
            DrawPlayers();
            DrawActivityContent();
            DrawTransition();
            DrawValidation();
            DrawAdvancedDebug();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (modified &&
                _lastValidationReport != null)
            {
                _validationOutdated = true;
            }
        }

        private void DrawOverview()
        {
            DrawSection("Overview");

            EditorGUILayout.PropertyField(
                _activityName,
                ActivityNameLabel);

            EditorGUILayout.PropertyField(
                _description,
                DescriptionLabel);
        }

        private void DrawPlayers()
        {
            DrawSection("Players");

            EditorGUILayout.PropertyField(
                _playerParticipationProjectionMode,
                ParticipationLabel);

            if (UsesExplicitSlots())
            {
                EditorGUILayout.PropertyField(
                    _playerParticipationExplicitSlotProfiles,
                    ExplicitSlotsLabel,
                    true);
            }

            EditorGUILayout.PropertyField(
                _playerParticipationZeroParticipantPolicy,
                ZeroParticipantsLabel);

            EditorGUILayout.PropertyField(
                _playerParticipationRequirementLevel,
                ReadinessLabel);
        }

        private bool UsesExplicitSlots()
        {
            return _playerParticipationProjectionMode != null &&
                   !_playerParticipationProjectionMode
                       .hasMultipleDifferentValues &&
                   _playerParticipationProjectionMode.intValue ==
                   (int)ActivityParticipationProjectionMode
                       .ExplicitSlots;
        }

        private void DrawActivityContent()
        {
            DrawSection("Activity Content");

            EditorGUILayout.PropertyField(
                _activityContentProfile,
                ContentProfileLabel);

            ActivityContentProfileAsset profile =
                _activityContentProfile.objectReferenceValue
                    as ActivityContentProfileAsset;

            if (profile == null)
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Create Content Profile",
                            "Creates and assigns an Activity Content Profile before scenes are added.")))
                {
                    ActivityContentProfileAsset created =
                        ImmersiveFrameworkEditorSettingsUtility
                            .CreateActivityContentProfileAsset();

                    if (created != null)
                    {
                        _activityContentProfile
                            .objectReferenceValue = created;
                        Selection.activeObject = created;
                        EditorGUIUtility.PingObject(created);
                    }
                }

                return;
            }

            DrawActivitySceneSummary(profile);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Add Scene...",
                            "Adds an Activity-owned Scene to the assigned Content Profile and suggests a stable Content Id.")))
                {
                    EditorGUIUtility.ShowObjectPicker<SceneAsset>(
                        null,
                        false,
                        string.Empty,
                        ActivityScenePickerControlId);
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Open Content Profile",
                            "Opens the assigned Profile for policies, IDs and detailed validation.")))
                {
                    Selection.activeObject = profile;
                    EditorGUIUtility.PingObject(profile);
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    _sceneActionMessage))
            {
                EditorGUILayout.LabelField(
                    _sceneActionMessage,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawActivitySceneSummary(
            ActivityContentProfileAsset profile)
        {
            EditorGUILayout.LabelField(
                "Scenes",
                profile.SceneCount.ToString());

            for (int index = 0;
                 index < profile.SceneCount;
                 index++)
            {
                ActivityContentSceneEntry entry =
                    profile.Scenes[index];

                string sceneName =
                    entry != null &&
                    !string.IsNullOrWhiteSpace(
                        entry.SceneName)
                        ? entry.SceneName
                        : "<missing Scene>";

                EditorGUILayout.LabelField(
                    $"- {sceneName}",
                    EditorStyles.miniLabel);
            }
        }

        private void HandleActivityScenePicker()
        {
            Event currentEvent =
                Event.current;

            if (currentEvent == null ||
                currentEvent.commandName !=
                    "ObjectSelectorClosed" ||
                EditorGUIUtility
                    .GetObjectPickerControlID() !=
                    ActivityScenePickerControlId)
            {
                return;
            }

            SceneAsset selectedScene =
                EditorGUIUtility
                    .GetObjectPickerObject()
                    as SceneAsset;

            ActivityContentProfileAsset profile =
                _activityContentProfile != null
                    ? _activityContentProfile
                        .objectReferenceValue
                        as ActivityContentProfileAsset
                    : null;

            if (selectedScene != null &&
                profile != null)
            {
                if (!ContentProfileSceneAuthoringUtility
                        .TryAddActivityScene(
                            profile,
                            selectedScene,
                            out _sceneActionMessage))
                {
                    EditorUtility.DisplayDialog(
                        "Activity Scene Not Added",
                        _sceneActionMessage,
                        "OK");
                }
                else
                {
                    _lastValidationReport = null;
                    Repaint();
                }
            }

            if (currentEvent.type != EventType.Layout)
            {
                currentEvent.Use();
            }
        }

        private void DrawTransition()
        {
            DrawSection("Transition");

            EditorGUILayout.PropertyField(
                _visualTransitionMode,
                PresentationLabel);

            EditorGUILayout.PropertyField(
                _transitionGateMode,
                TransitionGateLabel);
        }

        private void DrawValidation()
        {
            DrawSection("Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        ValidateLabel,
                        GUILayout.Width(96f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    RunValidation();
                    _serializedBindingsDirty = true;

                    GUIUtility.ExitGUI();
                }

                GUILayout.Space(8f);

                EditorGUILayout.LabelField(
                    GetValidationStatus(),
                    EditorStyles.miniBoldLabel);

                GUILayout.FlexibleSpace();
            }

            DrawFirstActionableValidationIssue();
        }

        private void RunValidation()
        {
            ActivityAsset activity =
                (ActivityAsset)target;

            _lastValidationReport =
                FrameworkAuthoringValidator.ValidateActivity(
                    activity);

            _lastValidationReport.AddRange(
                FrameworkIdentityAuthoringValidator
                    .ValidateProjectAssets(
                        FrameworkValidationMode.Standard));

            _lastValidationReport.AddRange(
                ActivityParticipationProjectionAuthoringValidator
                    .ValidateActivity(activity));

            _validationOutdated = false;
        }

        private void DrawFirstActionableValidationIssue()
        {
            if (_lastValidationReport == null ||
                _validationOutdated ||
                (_lastValidationReport.ErrorCount == 0 &&
                 _lastValidationReport.WarningCount == 0))
            {
                return;
            }

            for (int index = 0;
                 index < _lastValidationReport.Issues.Count;
                 index++)
            {
                FrameworkAuthoringValidationIssue issue =
                    _lastValidationReport.Issues[index];

                if (issue.Severity !=
                        FrameworkAuthoringValidationSeverity.Error &&
                    issue.Severity !=
                        FrameworkAuthoringValidationSeverity.Warning)
                {
                    continue;
                }

                EditorGUILayout.HelpBox(
                    issue.Message,
                    issue.Severity ==
                        FrameworkAuthoringValidationSeverity.Error
                            ? MessageType.Error
                            : MessageType.Warning);

                return;
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(7f);

            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows stable identity, technical references and the complete validation report."),
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawActivityId();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Projection Mode",
                    GetSerializedEnumLabel(
                        _playerParticipationProjectionMode));

                EditorGUILayout.TextField(
                    "Zero Participant Policy",
                    GetSerializedEnumLabel(
                        _playerParticipationZeroParticipantPolicy));

                EditorGUILayout.IntField(
                    "Explicit Slot Count",
                    _playerParticipationExplicitSlotProfiles != null
                        ? _playerParticipationExplicitSlotProfiles
                            .arraySize
                        : 0);

                EditorGUILayout.TextField(
                    "Requirement Level",
                    GetSerializedEnumLabel(
                        _playerParticipationRequirementLevel));

                EditorGUILayout.ObjectField(
                    "Content Profile Reference",
                    _activityContentProfile != null
                        ? _activityContentProfile.objectReferenceValue
                        : null,
                    typeof(ActivityContentProfileAsset),
                    false);

                EditorGUILayout.TextField(
                    "Transition Presentation",
                    GetSerializedEnumLabel(
                        _visualTransitionMode));

                EditorGUILayout.TextField(
                    "Transition Gate",
                    GetSerializedEnumLabel(
                        _transitionGateMode));

                EditorGUILayout.TextField(
                    "Validation Status",
                    GetValidationStatus());
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel);

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastValidationReport);

            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);

            EditorGUI.indentLevel--;
        }

        private void DrawActivityId()
        {
            string activityId =
                _activityId != null
                    ? _activityId.stringValue ?? string.Empty
                    : string.Empty;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Activity ID",
                            "Stable functional identity independent from Activity Name and asset filename."),
                        activityId);
                }

                using (new EditorGUI.DisabledScope(
                           !string.IsNullOrWhiteSpace(
                               activityId)))
                {
                    if (GUILayout.Button(
                            "Generate ID",
                            GUILayout.Width(90f)))
                    {
                        _activityId.stringValue =
                            ImmersiveFrameworkEditorSettingsUtility
                                .GenerateActivityIdText();
                    }
                }

                using (new EditorGUI.DisabledScope(
                           string.IsNullOrWhiteSpace(
                               activityId)))
                {
                    if (GUILayout.Button(
                            "Copy ID",
                            GUILayout.Width(70f)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            activityId;
                    }
                }
            }
        }

        private string GetValidationStatus()
        {
            if (_lastValidationReport == null)
            {
                return "Not Validated";
            }

            if (_validationOutdated)
            {
                return "Outdated";
            }

            if (_lastValidationReport.ErrorCount > 0)
            {
                return "Invalid";
            }

            if (_lastValidationReport.WarningCount > 0)
            {
                return "Warning";
            }

            return "Valid";
        }

        private static string GetSerializedEnumLabel(
            SerializedProperty property)
        {
            if (property == null)
            {
                return "Unavailable";
            }

            if (property.hasMultipleDifferentValues)
            {
                return "Mixed Values";
            }

            string[] displayNames =
                property.enumDisplayNames;

            int selectedIndex =
                property.enumValueIndex;

            if (selectedIndex >= 0 &&
                selectedIndex < displayNames.Length)
            {
                return displayNames[selectedIndex];
            }

            return $"Serialized value {property.intValue}";
        }

        private static void DrawSection(
            string title)
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }
    }
}
