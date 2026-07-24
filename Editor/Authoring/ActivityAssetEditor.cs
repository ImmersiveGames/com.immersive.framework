using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Editor.Settings;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityAsset))]
    internal sealed class ActivityAssetEditor : UnityEditor.Editor
    {
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
        private bool _showActivityContent;
        private bool _showAdvancedDiagnostics;

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

            DrawInspectorHeader();

            EditorGUILayout.Space(6f);
            DrawOverview();

            EditorGUILayout.Space(8f);
            DrawPlayers();

            EditorGUILayout.Space(8f);
            DrawActivityContent();

            EditorGUILayout.Space(8f);
            DrawTransition();

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.Space(8f);
            DrawAdvancedDiagnostics();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (modified &&
                _lastValidationReport != null)
            {
                _validationOutdated = true;
            }
        }

        private void DrawInspectorHeader()
        {
            EditorGUILayout.LabelField(
                "Activity",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "An Activity represents a step or mode inside a Route, such as Title, Character Select, Exploration, Results or Credits.",
                MessageType.Info);
        }

        private void DrawOverview()
        {
            EditorGUILayout.LabelField(
                "Overview",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _activityName,
                new GUIContent(
                    "Activity Name",
                    "Designer-facing name used for presentation and diagnostics."));
            EditorGUILayout.PropertyField(
                _description,
                new GUIContent(
                    "Description",
                    "Optional note explaining the purpose of this Activity."));

            EditorGUILayout.HelpBox(
                "Stable identity checks run only through Validate Activity. Technical identity remains visible under Advanced / Diagnostics.",
                MessageType.None);
        }

        private void DrawPlayers()
        {
            EditorGUILayout.LabelField(
                "Players",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose which local Players belong to this Activity and how ready they must be before it can proceed.",
                MessageType.None);

            EditorGUILayout.PropertyField(
                _playerParticipationProjectionMode,
                new GUIContent(
                    "Who Participates",
                    "Selects which Session Player Slots are included by this Activity."));

            if (UsesExplicitSlots())
            {
                EditorGUILayout.PropertyField(
                    _playerParticipationExplicitSlotProfiles,
                    new GUIContent(
                        "Specific Player Slots",
                        "Ordered Player Slot Profiles included by this Activity."),
                    true);
            }

            EditorGUILayout.PropertyField(
                _playerParticipationZeroParticipantPolicy,
                new GUIContent(
                    "If No Players Are Available",
                    "Controls whether a dynamic Player selection may resolve to zero participants."));
            EditorGUILayout.PropertyField(
                _playerParticipationRequirementLevel,
                new GUIContent(
                    "Ready When",
                    "Progressive readiness required from every participating Player."));

            EditorGUILayout.HelpBox(
                "Projection, zero-participant and readiness coherence is evaluated only through Validate Activity.",
                MessageType.None);
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
            _showActivityContent =
                EditorGUILayout.Foldout(
                    _showActivityContent,
                    "Activity Content",
                    true);

            if (!_showActivityContent)
            {
                return;
            }

            EditorGUILayout.PropertyField(
                _activityContentProfile,
                new GUIContent(
                    "Content Profile",
                    "Optional Activity-owned scenes composed and released with this Activity."));

            Object profile =
                _activityContentProfile.objectReferenceValue;

            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "No Activity-owned content Profile is assigned.",
                    MessageType.None);

                if (GUILayout.Button("Add Content Profile"))
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

            EditorGUILayout.HelpBox(
                "The assigned Profile declares Activity-owned scene content.",
                MessageType.None);

            if (GUILayout.Button("Open Content Profile"))
            {
                Selection.activeObject = profile;
                EditorGUIUtility.PingObject(profile);
            }
        }

        private void DrawTransition()
        {
            EditorGUILayout.LabelField(
                "Transition",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _visualTransitionMode,
                new GUIContent(
                    "Presentation",
                    "Controls whether Activity changes are seamless, use a fade, or use fade with loading presentation."));
            EditorGUILayout.PropertyField(
                _transitionGateMode,
                new GUIContent(
                    "Block During Transition",
                    "Controls which requests and capabilities remain blocked while this Activity transition is running."));

            EditorGUILayout.HelpBox(
                "Presentation and transition-gate coherence is evaluated only through Validate Activity.",
                MessageType.None);
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (_lastValidationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Run validation after configuring the Activity.",
                    MessageType.None);
            }
            else if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the Activity changed.",
                    MessageType.Warning);
            }
            else if (_lastValidationReport.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "Ready — no blocking Activity configuration issues were found.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Needs Attention — {_lastValidationReport.ErrorCount} blocking issue(s) were found.",
                    MessageType.Error);
            }

            if (GUILayout.Button("Validate Activity"))
            {
                serializedObject.ApplyModifiedProperties();
                RunValidation();
                _serializedBindingsDirty = true;

                // Validation can scan project assets and invalidate cached
                // SerializedProperty instances during the current IMGUI event.
                // Repaint with a fresh Inspector state instead of continuing.
                GUIUtility.ExitGUI();
            }
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

        private void DrawAdvancedDiagnostics()
        {
            _showAdvancedDiagnostics =
                EditorGUILayout.Foldout(
                    _showAdvancedDiagnostics,
                    "Advanced / Diagnostics",
                    true);

            if (!_showAdvancedDiagnostics)
            {
                return;
            }

            DrawActivityId();
            DrawParticipationDiagnostics();
            DrawTechnicalReferences();
            DrawCurrentScope();
            DrawFullValidationReport();
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

            EditorGUILayout.HelpBox(
                "Activity ID validity is checked only by Validate Activity. Existing IDs are not replaced automatically.",
                MessageType.None);
        }

        private void DrawParticipationDiagnostics()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Player Participation",
                EditorStyles.boldLabel);

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
            }
        }

        private void DrawTechnicalReferences()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Technical References",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
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
            }
        }

        private void DrawCurrentScope()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Current Scope",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Activity declares which Player Slots participate, the readiness required from them, optional Activity-owned scene content and transition presentation. Runtime systems evaluate this immutable intent without storing mutable Session state in the asset.",
                MessageType.None);
        }

        private void DrawFullValidationReport()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel);

            if (_lastValidationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "No validation report is available.",
                    MessageType.None);
                return;
            }

            if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "This report is outdated. Run Validate Activity again.",
                    MessageType.Warning);
            }

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastValidationReport);
            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);
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
    }
}
