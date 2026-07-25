using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Designer-first inspector shared by ActorDeclaration and specialized declarations.
    /// Stable identity and diagnostics remain available without dominating the product surface.
    /// </summary>
    [CustomEditor(typeof(ActorDeclaration), true)]
    internal sealed class ActorDeclarationEditor : UnityEditor.Editor
    {
        private const string LegacyQaActorId = "qa.actor.generic";

        private SerializedProperty _actorId;
        private SerializedProperty _actorKind;
        private SerializedProperty _actorRole;
        private SerializedProperty _displayName;
        private SerializedProperty _reason;

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _validationOutdated;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _actorId = serializedObject.FindProperty("actorId");
            _actorKind = serializedObject.FindProperty("actorKind");
            _actorRole = serializedObject.FindProperty("actorRole");
            _displayName = serializedObject.FindProperty("displayName");
            _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            ActorDeclaration declaration = (ActorDeclaration)target;
            bool isPlayer = declaration is PlayerActorDeclaration;

            DrawHeader(isPlayer);
            DrawIdentity();
            DrawClassification(isPlayer);
            DrawValidation(declaration);
            DrawAdvanced(declaration);

            bool modified = serializedObject.ApplyModifiedProperties();
            if (modified && _lastValidationReport != null)
            {
                _validationOutdated = true;
            }
        }

        private static void DrawHeader(bool isPlayer)
        {
            EditorGUILayout.LabelField(
                isPlayer ? "Player Actor" : "Actor",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                isPlayer
                    ? "Declares one Logical Player Actor. PlayerInput belongs to the Local Player Host; this component owns only Actor identity and classification."
                    : "Declares one logical Actor identity. Movement, input, presentation, materialization and lifetime remain owned by their explicit systems.",
                MessageType.Info);
        }

        private void DrawIdentity()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Identity",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _displayName,
                new GUIContent(
                    "Display Name",
                    "Human-readable label used in authoring and diagnostics. It is not functional identity."));
        }

        private void DrawClassification(bool isPlayer)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Classification",
                EditorStyles.boldLabel);

            if (isPlayer)
            {
                EditorGUILayout.HelpBox(
                    "This specialized declaration is always classified as Player / Protagonist.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.PropertyField(
                _actorKind,
                new GUIContent(
                    "Actor Kind",
                    "Broad Actor category used by framework contracts."));
            EditorGUILayout.PropertyField(
                _actorRole,
                new GUIContent(
                    "Actor Role",
                    "Broad gameplay role used by framework contracts."));
        }

        private void DrawValidation(ActorDeclaration declaration)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (_lastValidationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Run validation after configuring the Actor.",
                    MessageType.None);
            }
            else if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the Actor configuration changed.",
                    MessageType.Warning);
            }
            else if (_lastValidationReport.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "Ready — no blocking Actor authoring issues were found.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Needs Attention — {_lastValidationReport.ErrorCount} blocking issue(s) were found.",
                    MessageType.Error);
            }

            if (GUILayout.Button("Validate Actor"))
            {
                serializedObject.ApplyModifiedProperties();
                _lastValidationReport =
                    ActorDeclarationAuthoringValidator.Validate(declaration);
                _validationOutdated = false;
            }
        }

        private void DrawAdvanced(ActorDeclaration declaration)
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

            DrawActorId();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Diagnostics",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Reason",
                    "Diagnostic declaration source/reason. It is not Actor identity."));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Runtime Type",
                    declaration.GetType().FullName);
                EditorGUILayout.EnumPopup(
                    "Effective Actor Kind",
                    declaration.ActorKind);
                EditorGUILayout.EnumPopup(
                    "Effective Actor Role",
                    declaration.ActorRole);

                if (declaration is PlayerActorDeclaration player)
                {
                    EditorGUILayout.Toggle(
                        "Local Host PlayerInput Bound",
                        player.HasPlayerInputEvidence);
                    EditorGUILayout.ObjectField(
                        "Bound PlayerInput",
                        player.PlayerInput,
                        typeof(UnityEngine.Object),
                        true);
                }
            }

            if (_lastValidationReport != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(
                    "Validation Report",
                    EditorStyles.boldLabel);
                if (_validationOutdated)
                {
                    EditorGUILayout.HelpBox(
                        "This report is outdated. Run Validate Actor again.",
                        MessageType.Warning);
                }

                FrameworkAuthoringValidationGui.DrawSummary(
                    _lastValidationReport);
                FrameworkAuthoringValidationGui.DrawIssues(
                    _lastValidationReport,
                    false);
            }
        }

        private void DrawActorId()
        {
            EditorGUILayout.LabelField(
                "Stable Identity",
                EditorStyles.boldLabel);

            string actorId =
                _actorId != null
                    ? _actorId.stringValue ?? string.Empty
                    : string.Empty;
            bool isLegacyPlaceholder =
                string.Equals(
                    actorId.Trim(),
                    LegacyQaActorId,
                    StringComparison.Ordinal);
            bool canGenerate =
                string.IsNullOrWhiteSpace(actorId) ||
                isLegacyPlaceholder;

            if (isLegacyPlaceholder)
            {
                EditorGUILayout.HelpBox(
                    "This component uses the legacy QA placeholder. Replace it explicitly before product use.",
                    MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Actor ID",
                            "Stable functional identity. It must not change when the GameObject, prefab or display name changes."),
                        actorId);
                }

                using (new EditorGUI.DisabledScope(!canGenerate))
                {
                    if (GUILayout.Button(
                            isLegacyPlaceholder ? "Replace ID" : "Generate ID",
                            GUILayout.Width(90f)))
                    {
                        _actorId.stringValue =
                            $"actor.{Guid.NewGuid():N}";
                    }
                }

                using (new EditorGUI.DisabledScope(
                           string.IsNullOrWhiteSpace(actorId)))
                {
                    if (GUILayout.Button(
                            "Copy ID",
                            GUILayout.Width(70f)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            actorId.Trim();
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Actor ID is generated explicitly and remains read-only after assignment. Existing identities are never replaced automatically.",
                MessageType.None);
        }
    }
}
