using System;
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
        private bool _validationOutdated;
        private bool _showActivityContent;
        private bool _showAdvancedDiagnostics;

        private void OnEnable()
        {
            _activityName = serializedObject.FindProperty("activityName");
            _activityId = serializedObject.FindProperty("activityId");
            _description = serializedObject.FindProperty("description");
            _playerParticipationProjectionMode =
                serializedObject.FindProperty("playerParticipationProjectionMode");
            _playerParticipationZeroParticipantPolicy =
                serializedObject.FindProperty("playerParticipationZeroParticipantPolicy");
            _playerParticipationExplicitSlotProfiles =
                serializedObject.FindProperty("playerParticipationExplicitSlotProfiles");
            _playerParticipationRequirementLevel =
                serializedObject.FindProperty("playerParticipationRequirementLevel");
            _activityContentProfile =
                serializedObject.FindProperty("activityContentProfile");
            _visualTransitionMode =
                serializedObject.FindProperty("visualTransitionMode");
            _transitionGateMode =
                serializedObject.FindProperty("transitionGateMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();

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

            bool modified = serializedObject.ApplyModifiedProperties();
            if (modified && _lastValidationReport != null)
            {
                _validationOutdated = true;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Activity", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "An Activity represents a step or mode inside a Route, such as Title, Character Select, Exploration, Results or Credits.",
                MessageType.Info);
        }

        private void DrawOverview()
        {
            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
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

            DrawIdentityProblem();
        }

        private void DrawIdentityProblem()
        {
            if (_activityId != null &&
                ActivityId.IsValidText(_activityId.stringValue))
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Activity ID is missing or malformed. Generate a canonical ID explicitly, or use Activity ID Migration for a coordinated project migration.",
                MessageType.Error);

            if (_activityId != null &&
                GUILayout.Button("Generate Activity ID"))
            {
                _activityId.stringValue =
                    ImmersiveFrameworkEditorSettingsUtility.GenerateActivityIdText();
            }
        }

        private void DrawPlayers()
        {
            EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose which local Players belong to this Activity and how ready they must be before it can proceed.",
                MessageType.None);

            EditorGUILayout.PropertyField(
                _playerParticipationProjectionMode,
                new GUIContent(
                    "Who Participates",
                    "Selects which Session Player Slots are included by this Activity."));

            ActivityParticipationProjectionMode projectionMode =
                GetProjectionMode();

            if (projectionMode ==
                ActivityParticipationProjectionMode.ExplicitSlots)
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
                BuildParticipationSummary(),
                ResolveParticipationMessageType());
        }

        private string BuildParticipationSummary()
        {
            if (!TryGetDefinedEnum(
                    _playerParticipationProjectionMode,
                    out ActivityParticipationProjectionMode projectionMode))
            {
                return "Who Participates contains an invalid serialized value. Run Validate Activity for details.";
            }

            if (!TryGetDefinedEnum(
                    _playerParticipationZeroParticipantPolicy,
                    out ActivityParticipationZeroParticipantPolicy zeroPolicy))
            {
                return "If No Players Are Available contains an invalid serialized value. Run Validate Activity for details.";
            }

            if (!TryGetDefinedEnum(
                    _playerParticipationRequirementLevel,
                    out PlayerParticipationRequirementLevel requirementLevel))
            {
                return "Ready When contains an invalid serialized value. Run Validate Activity for details.";
            }

            string participants = BuildProjectionSummary(projectionMode);
            string zeroParticipants = BuildZeroParticipantSummary(zeroPolicy);
            string readiness = BuildRequirementSummary(requirementLevel);

            return $"{participants}\n{zeroParticipants}\n{readiness}";
        }

        private MessageType ResolveParticipationMessageType()
        {
            if (!TryGetDefinedEnum(
                    _playerParticipationProjectionMode,
                    out ActivityParticipationProjectionMode projectionMode) ||
                !TryGetDefinedEnum(
                    _playerParticipationZeroParticipantPolicy,
                    out ActivityParticipationZeroParticipantPolicy zeroPolicy) ||
                !TryGetDefinedEnum(
                    _playerParticipationRequirementLevel,
                    out PlayerParticipationRequirementLevel requirementLevel))
            {
                return MessageType.Error;
            }

            if (projectionMode == ActivityParticipationProjectionMode.NoSlots &&
                (zeroPolicy != ActivityParticipationZeroParticipantPolicy.Allowed ||
                 requirementLevel != PlayerParticipationRequirementLevel.None))
            {
                return MessageType.Error;
            }

            if (projectionMode ==
                    ActivityParticipationProjectionMode.ExplicitSlots &&
                (zeroPolicy != ActivityParticipationZeroParticipantPolicy.Rejected ||
                 _playerParticipationExplicitSlotProfiles == null ||
                 _playerParticipationExplicitSlotProfiles.arraySize == 0))
            {
                return MessageType.Error;
            }

            return MessageType.Info;
        }

        private static string BuildProjectionSummary(
            ActivityParticipationProjectionMode mode)
        {
            switch (mode)
            {
                case ActivityParticipationProjectionMode.NoSlots:
                    return "No Players participate in this Activity. Use this for menus, credits or non-player sequences.";

                case ActivityParticipationProjectionMode.AllJoinedSlots:
                    return "All local Players currently joined to the Session participate.";

                case ActivityParticipationProjectionMode.ExplicitSlots:
                    return "Only the configured Player Slots participate.";

                default:
                    return $"Participation mode '{mode}' is not supported by this Inspector.";
            }
        }

        private static string BuildZeroParticipantSummary(
            ActivityParticipationZeroParticipantPolicy policy)
        {
            switch (policy)
            {
                case ActivityParticipationZeroParticipantPolicy.Allowed:
                    return "The Activity may proceed when no participating Players are available.";

                case ActivityParticipationZeroParticipantPolicy.Rejected:
                    return "The Activity is blocked when no participating Players are available.";

                default:
                    return $"Zero-player policy '{policy}' is not supported by this Inspector.";
            }
        }

        private static string BuildRequirementSummary(
            PlayerParticipationRequirementLevel level)
        {
            switch (level)
            {
                case PlayerParticipationRequirementLevel.None:
                    return "No Player readiness is required.";

                case PlayerParticipationRequirementLevel.JoinedSlots:
                    return "Every participating Player Slot must be joined.";

                case PlayerParticipationRequirementLevel.SelectedActors:
                    return "Every participating Player must have an Actor selected.";

                case PlayerParticipationRequirementLevel.LogicalActorsPrepared:
                    return "Every participating Player must have its logical Actor prepared.";

                case PlayerParticipationRequirementLevel.GameplayReady:
                    return "Every participating Player must satisfy complete gameplay readiness.";

                default:
                    return $"Readiness level '{level}' is not supported by this Inspector.";
            }
        }

        private ActivityParticipationProjectionMode GetProjectionMode()
        {
            return TryGetDefinedEnum(
                _playerParticipationProjectionMode,
                out ActivityParticipationProjectionMode value)
                    ? value
                    : ActivityParticipationProjectionMode.NoSlots;
        }

        private void DrawActivityContent()
        {
            _showActivityContent = EditorGUILayout.Foldout(
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

            UnityEngine.Object profile =
                _activityContentProfile.objectReferenceValue;

            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "No Activity-owned scenes. This Activity currently uses the scenes already provided by its Route.",
                    MessageType.None);

                if (GUILayout.Button("Add Content Profile"))
                {
                    ActivityContentProfileAsset created =
                        ImmersiveFrameworkEditorSettingsUtility
                            .CreateActivityContentProfileAsset();
                    if (created != null)
                    {
                        _activityContentProfile.objectReferenceValue = created;
                        Selection.activeObject = created;
                        EditorGUIUtility.PingObject(created);
                    }
                }

                return;
            }

            EditorGUILayout.HelpBox(
                "Scenes from this Profile are composed when the Activity starts and released through the existing Activity operation path.",
                MessageType.Info);

            if (GUILayout.Button("Open Content Profile"))
            {
                Selection.activeObject = profile;
                EditorGUIUtility.PingObject(profile);
            }
        }

        private void DrawTransition()
        {
            EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);

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
                BuildTransitionSummary(),
                ResolveTransitionMessageType());
        }

        private string BuildTransitionSummary()
        {
            if (!TryGetDefinedEnum(
                    _visualTransitionMode,
                    out ActivityVisualTransitionMode mode))
            {
                return "Presentation contains an invalid serialized value. Run Validate Activity for details.";
            }

            if (!TryGetDefinedEnum(
                    _transitionGateMode,
                    out TransitionGateMode gateMode))
            {
                return "Block During Transition contains an invalid serialized value. Run Validate Activity for details.";
            }

            string presentation;
            switch (mode)
            {
                case ActivityVisualTransitionMode.Seamless:
                    presentation =
                        "The Activity changes without the shared fade or loading presentation.";
                    break;

                case ActivityVisualTransitionMode.Fade:
                    presentation =
                        "The Activity change uses the shared fade presentation.";
                    break;

                case ActivityVisualTransitionMode.FadeWithLoading:
                    presentation =
                        "The Activity change uses the shared fade and loading presentation when requested.";
                    break;

                default:
                    presentation =
                        $"Presentation mode '{mode}' is not supported by this Inspector.";
                    break;
            }

            return $"{presentation}\nBlocking policy: {gateMode}.";
        }

        private MessageType ResolveTransitionMessageType()
        {
            if (!TryGetDefinedEnum(
                    _visualTransitionMode,
                    out ActivityVisualTransitionMode mode) ||
                !TryGetDefinedEnum(
                    _transitionGateMode,
                    out TransitionGateMode gateMode))
            {
                return MessageType.Error;
            }

            return mode != ActivityVisualTransitionMode.Seamless &&
                   gateMode != TransitionGateMode.InputInteractionAndGameplay
                ? MessageType.Warning
                : MessageType.Info;
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

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
                serializedObject.Update();
                RunValidation();
            }
        }

        private void RunValidation()
        {
            ActivityAsset activity = (ActivityAsset)target;

            _lastValidationReport =
                FrameworkAuthoringValidator.ValidateActivity(activity);
            _lastValidationReport.AddRange(
                FrameworkIdentityAuthoringValidator.ValidateProjectAssets(
                    FrameworkValidationMode.Standard));
            _lastValidationReport.AddRange(
                ActivityParticipationProjectionAuthoringValidator
                    .ValidateActivity(activity));

            _validationOutdated = false;
        }

        private void DrawAdvancedDiagnostics()
        {
            _showAdvancedDiagnostics = EditorGUILayout.Foldout(
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
            string activityId = _activityId != null
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
                           string.IsNullOrWhiteSpace(activityId)))
                {
                    if (GUILayout.Button("Copy ID", GUILayout.Width(70f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = activityId;
                    }
                }
            }
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
                    GetRawEnumName<ActivityParticipationProjectionMode>(
                        _playerParticipationProjectionMode));

                EditorGUILayout.TextField(
                    "Zero Participant Policy",
                    GetRawEnumName<ActivityParticipationZeroParticipantPolicy>(
                        _playerParticipationZeroParticipantPolicy));

                EditorGUILayout.IntField(
                    "Explicit Slot Count",
                    _playerParticipationExplicitSlotProfiles != null
                        ? _playerParticipationExplicitSlotProfiles.arraySize
                        : 0);

                EditorGUILayout.TextField(
                    "Requirement Level",
                    GetRawEnumName<PlayerParticipationRequirementLevel>(
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
                    GetRawEnumName<ActivityVisualTransitionMode>(
                        _visualTransitionMode));

                EditorGUILayout.TextField(
                    "Transition Gate",
                    GetRawEnumName<TransitionGateMode>(
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

        private static bool TryGetDefinedEnum<TEnum>(
            SerializedProperty property,
            out TEnum value)
            where TEnum : struct, Enum
        {
            value = default;

            if (property == null ||
                property.hasMultipleDifferentValues)
            {
                return false;
            }

            value = (TEnum)Enum.ToObject(
                typeof(TEnum),
                property.intValue);

            return Enum.IsDefined(typeof(TEnum), value);
        }

        private static string GetRawEnumName<TEnum>(
            SerializedProperty property)
            where TEnum : struct, Enum
        {
            if (property == null)
            {
                return "Unavailable";
            }

            if (property.hasMultipleDifferentValues)
            {
                return "Mixed Values";
            }

            TEnum value = (TEnum)Enum.ToObject(
                typeof(TEnum),
                property.intValue);

            return Enum.IsDefined(typeof(TEnum), value)
                ? value.ToString()
                : $"Invalid ({property.intValue})";
        }
    }
}
