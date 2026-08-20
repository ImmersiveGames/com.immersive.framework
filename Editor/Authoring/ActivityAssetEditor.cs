using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Settings;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ActivityAsset))]
    internal sealed class ActivityAssetEditor : UnityEditor.Editor
    {
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

        private static readonly GUIContent EntryReadinessPolicyLabel =
            new GUIContent(
                "Policy",
                "Observe Only releases presentation normally. Wait Covered retains visual cover. Wait Visible reveals preparation while retaining the gameplay capability gate.");

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
        private SerializedProperty _activityEntryReadinessPolicy;
        private SerializedProperty _visualTransitionMode;
        private SerializedProperty _transitionGateMode;

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private FrameworkAuthoringValidationReport _lastProjectIdentityAudit;
        private bool _serializedBindingsDirty = true;
        private bool _validationOutdated;
        private bool _showAdvancedDebug;

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
            _activityEntryReadinessPolicy =
                serializedObject.FindProperty(
                    "activityEntryReadinessPolicy");
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

            EditorGUILayout.LabelField(
                "Activity",
                EditorStyles.boldLabel);

            DrawOverview();
            DrawPlayers();
            DrawActivityContent();
            DrawSceneList();
            DrawActivityEntryReadiness();
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

            if (GUILayout.Button(
                    new GUIContent(
                        "Open Content Profile",
                        "Opens the assigned Profile to add Scenes, configure policies and run detailed validation.")))
            {
                Selection.activeObject = profile;
                EditorGUIUtility.PingObject(profile);
            }
        }

        private void DrawSceneList()
        {
            DrawSection("Build Profile Scene List");

            BuildSceneListAuthoringUtility
                .DrawAction(
                    BuildSceneListAuthoringUtility
                        .GetScenePaths(
                            (ActivityAsset)target));
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

        private void DrawActivityEntryReadiness()
        {
            DrawSection("Activity Entry Readiness");

            EditorGUILayout.PropertyField(
                _activityEntryReadinessPolicy,
                EntryReadinessPolicyLabel);

            if (_activityEntryReadinessPolicy == null ||
                _activityEntryReadinessPolicy.hasMultipleDifferentValues)
            {
                return;
            }

            var policy =
                (ActivityEntryReadinessPolicy)
                _activityEntryReadinessPolicy.intValue;

            if (!System.Enum.IsDefined(
                    typeof(ActivityEntryReadinessPolicy),
                    policy))
            {
                EditorGUILayout.HelpBox(
                    "Activity Entry Readiness Policy has an invalid serialized value.",
                    MessageType.Error);
                return;
            }

            switch (policy)
            {
                case ActivityEntryReadinessPolicy.ObserveOnly:
                    EditorGUILayout.HelpBox(
                        "Readiness remains observable after the normal Activity transition and capability-gate release. This preserves existing behavior.",
                        MessageType.Info);
                    break;
                case ActivityEntryReadinessPolicy.WaitCovered:
                    EditorGUILayout.HelpBox(
                        "The entry flow keeps visual cover and the gameplay capability gate until the initial readiness occurrence reaches Ready.",
                        MessageType.Info);
                    break;
                case ActivityEntryReadinessPolicy.WaitVisible:
                    EditorGUILayout.HelpBox(
                        "The entry flow reveals preparation after materialization but keeps input, interaction and gameplay blocked until Ready.",
                        MessageType.Info);
                    break;
            }

            if (policy ==
                    ActivityEntryReadinessPolicy.WaitCovered &&
                _visualTransitionMode != null &&
                !_visualTransitionMode.hasMultipleDifferentValues &&
                _visualTransitionMode.intValue ==
                    (int)ActivityVisualTransitionMode.Seamless)
            {
                EditorGUILayout.HelpBox(
                    "Wait Covered requires an authored visual cover. Select Fade or Fade With Loading; the framework will not replace Seamless silently.",
                    MessageType.Error);
            }

            bool waitsForEntryReadiness =
                policy == ActivityEntryReadinessPolicy.WaitCovered ||
                policy == ActivityEntryReadinessPolicy.WaitVisible;

            if (waitsForEntryReadiness &&
                _transitionGateMode != null &&
                !_transitionGateMode.hasMultipleDifferentValues &&
                _transitionGateMode.intValue !=
                    (int)TransitionGateMode
                        .InputInteractionAndGameplay)
            {
                EditorGUILayout.HelpBox(
                    "Waiting policies require Block During Transition = Input Interaction And Gameplay. The framework will not strengthen the authored gate silently.",
                    MessageType.Error);
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

            // IF-ID-06: definition-local identity only (collisions involving this asset).
            _lastValidationReport.AddRange(
                FrameworkIdentityAuthoringValidator
                    .ValidateActivityDefinitionLocal(
                        activity,
                        FrameworkValidationMode.Standard));

            _lastValidationReport.AddRange(
                ActivityParticipationProjectionAuthoringValidator
                    .ValidateActivity(activity));

            _lastProjectIdentityAudit =
                FrameworkIdentityAuthoringValidator
                    .ValidateProjectIdentityAudit(
                        FrameworkValidationMode.Standard);

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
                    FormatValidationIssueMessage(issue),
                    issue.Severity ==
                        FrameworkAuthoringValidationSeverity.Error
                            ? MessageType.Error
                            : MessageType.Warning);

                DrawOpenConflictingAsset(issue.Context);

                return;
            }
        }

        private static string FormatValidationIssueMessage(
            FrameworkAuthoringValidationIssue issue)
        {
            if (issue.Context is ActivityContentProfileAsset profile)
            {
                return
                    $"Activity Content Profile '{profile.name}': " +
                    issue.Message;
            }

            return issue.Message;
        }

        private void DrawOpenConflictingAsset(Object context)
        {
            if (context == null || context == target)
            {
                return;
            }

            if (GUILayout.Button(
                    new GUIContent(
                        "Open Conflicting Asset",
                        "Selects and pings the other asset involved in this identity finding.")))
            {
                Selection.activeObject = context;
                EditorGUIUtility.PingObject(context);
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
                    "Entry Readiness Policy",
                    GetSerializedEnumLabel(
                        _activityEntryReadinessPolicy));

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

            using (new EditorGUI.DisabledScope(
                       string.IsNullOrWhiteSpace(activityId)))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Regenerate Stable ID...",
                            "Creates a new stable ID for this asset only. Requires confirmation. Does not run automatically on rename, move or duplicate.")))
                {
                    RegenerateActivityStableId();
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Project Identity Audit",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Project-wide collisions are audit evidence. They do not automatically block this definition unless this asset participates in the collision. Run Validate to refresh.",
                MessageType.None);
            if (_lastProjectIdentityAudit == null)
            {
                EditorGUILayout.HelpBox(
                    "Project identity audit not run yet. Click Validate.",
                    MessageType.Info);
            }
            else
            {
                FrameworkAuthoringValidationGui.DrawSummary(
                    _lastProjectIdentityAudit);
                FrameworkAuthoringValidationGui.DrawIssues(
                    _lastProjectIdentityAudit,
                    false);
            }
        }

        private void RegenerateActivityStableId()
        {
            ActivityAsset activity = (ActivityAsset)target;
            string currentId =
                _activityId != null
                    ? _activityId.stringValue ?? string.Empty
                    : string.Empty;

            if (!EditorUtility.DisplayDialog(
                    "Regenerate Activity Stable ID",
                    "This replaces the stable Activity ID for this asset only.\n\n" +
                    $"Current ID:\n{currentId}\n\n" +
                    "Rename and move do not change the ID. Duplicate assets often keep the copied ID and need this action.\n\n" +
                    "Continue?",
                    "Regenerate",
                    "Cancel"))
            {
                return;
            }

            if (!FrameworkIdentityAuthoringValidator.TryRegenerateStableId(
                    activity,
                    out string previousId,
                    out string newId,
                    out string issue))
            {
                EditorUtility.DisplayDialog(
                    "Regenerate Activity Stable ID",
                    string.IsNullOrWhiteSpace(issue)
                        ? "Regeneration failed."
                        : issue,
                    "OK");
                return;
            }

            serializedObject.Update();
            _validationOutdated = true;
            RunValidation();
            EditorUtility.DisplayDialog(
                "Activity Stable ID Regenerated",
                $"Previous ID:\n{previousId}\n\nNew ID:\n{newId}",
                "OK");
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
