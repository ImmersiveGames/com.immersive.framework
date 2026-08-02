using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Designer-first inspector shared by ActorDeclaration and specialized declarations.
    /// Stable identity and technical evidence remain available under Advanced / Debug.
    /// </summary>
    [CustomEditor(typeof(ActorDeclaration), true)]
    internal sealed class ActorDeclarationEditor : UnityEditor.Editor
    {
        private const string LegacyQaActorId = "qa.actor.generic";

        private SerializedProperty actorId;
        private SerializedProperty actorKind;
        private SerializedProperty actorRole;
        private SerializedProperty displayName;
        private SerializedProperty reason;

        private FrameworkAuthoringValidationReport lastValidationReport;
        private bool validationOutdated;
        private bool showAdvanced;

        private void OnEnable()
        {
            actorId = serializedObject.FindProperty("actorId");
            actorKind = serializedObject.FindProperty("actorKind");
            actorRole = serializedObject.FindProperty("actorRole");
            displayName = serializedObject.FindProperty("displayName");
            reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            ActorDeclaration declaration =
                (ActorDeclaration)target;
            bool isPlayer =
                declaration is PlayerActorDeclaration;

            EditorGUI.BeginChangeCheck();
            DrawActor();
            DrawClassification(isPlayer);
            bool primaryChanged =
                EditorGUI.EndChangeCheck();

            bool primaryModified =
                serializedObject.ApplyModifiedProperties();

            if (primaryChanged || primaryModified)
            {
                MarkValidationOutdated();
            }

            DrawActions(declaration);
            DrawValidationSummary();

            if (Application.isPlaying && isPlayer)
            {
                DrawRuntimeStatus(
                    (PlayerActorDeclaration)declaration);
            }

            DrawAdvanced(declaration);
        }

        private void DrawActor()
        {
            DrawSection("Actor");

            EditorGUILayout.PropertyField(
                displayName,
                new GUIContent(
                    "Display Name",
                    "Human-readable Actor label used in authoring and diagnostics. It is not functional identity."));
        }

        private void DrawClassification(
            bool isPlayer)
        {
            DrawSection("Classification");

            if (isPlayer)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        "Actor Kind",
                        ActorKind.Player.ToString());
                    EditorGUILayout.TextField(
                        "Actor Role",
                        ActorRole.Protagonist.ToString());
                }

                return;
            }

            EditorGUILayout.PropertyField(
                actorKind,
                new GUIContent(
                    "Actor Kind",
                    "Broad Actor category used by framework contracts."));

            EditorGUILayout.PropertyField(
                actorRole,
                new GUIContent(
                    "Actor Role",
                    "Broad gameplay role used by framework contracts."));
        }

        private void DrawActions(
            ActorDeclaration declaration)
        {
            DrawSection("Actions");

            if (GUILayout.Button(
                    new GUIContent(
                        "Validate Actor",
                        "Validate Actor identity, classification and Player-specific hierarchy rules without modifying runtime state.")))
            {
                serializedObject.ApplyModifiedProperties();
                lastValidationReport =
                    ActorDeclarationAuthoringValidator.Validate(
                        declaration);
                validationOutdated = false;
            }
        }

        private void DrawValidationSummary()
        {
            DrawSection("Validation Summary");

            if (lastValidationReport == null)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated");
                return;
            }

            if (validationOutdated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated — configuration changed");
                return;
            }

            if (lastValidationReport.IsValid)
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
                $"{lastValidationReport.ErrorCount} blocking issue(s) were found. Correct the Actor configuration and validate again.",
                MessageType.Error);

            FrameworkAuthoringValidationGui.DrawIssues(
                lastValidationReport,
                false);
        }

        private static void DrawRuntimeStatus(
            PlayerActorDeclaration player)
        {
            DrawSection("Runtime Status");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(
                    "Player Input Binding",
                    player.HasPlayerInputEvidence
                        ? "Bound"
                        : "Unbound");

                EditorGUILayout.ObjectField(
                    "Player Input",
                    player.PlayerInput,
                    typeof(UnityEngine.Object),
                    true);
            }
        }

        private void DrawAdvanced(
            ActorDeclaration declaration)
        {
            EditorGUILayout.Space(6f);
            showAdvanced =
                EditorGUILayout.Foldout(
                    showAdvanced,
                    "Advanced / Debug",
                    true);

            if (!showAdvanced)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            DrawStableIdentity();
            DrawDiagnostics();

            bool advancedChanged =
                EditorGUI.EndChangeCheck();

            bool advancedModified =
                serializedObject.ApplyModifiedProperties();

            if (advancedChanged || advancedModified)
            {
                MarkValidationOutdated();
            }

            DrawTechnicalEvidence(declaration);
            DrawValidationEvidence();
        }

        private void DrawStableIdentity()
        {
            DrawSection("Stable Identity");

            string currentActorId =
                actorId != null
                    ? actorId.stringValue ?? string.Empty
                    : string.Empty;

            bool isLegacyPlaceholder =
                string.Equals(
                    currentActorId.Trim(),
                    LegacyQaActorId,
                    StringComparison.Ordinal);

            bool canGenerate =
                string.IsNullOrWhiteSpace(currentActorId) ||
                isLegacyPlaceholder;

            if (isLegacyPlaceholder)
            {
                EditorGUILayout.HelpBox(
                    "This Actor uses the legacy QA placeholder. Replace it before product use.",
                    MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Actor ID",
                            "Stable functional identity. It must not change when the GameObject, prefab or Display Name changes."),
                        currentActorId);
                }

                using (new EditorGUI.DisabledScope(!canGenerate))
                {
                    string buttonLabel =
                        isLegacyPlaceholder
                            ? "Replace ID"
                            : "Generate ID";

                    if (GUILayout.Button(
                            buttonLabel,
                            GUILayout.Width(90f)))
                    {
                        actorId.stringValue =
                            $"actor.{Guid.NewGuid():N}";
                    }
                }

                using (new EditorGUI.DisabledScope(
                           string.IsNullOrWhiteSpace(
                               currentActorId)))
                {
                    if (GUILayout.Button(
                            "Copy ID",
                            GUILayout.Width(70f)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            currentActorId.Trim();
                    }
                }
            }
        }

        private void DrawDiagnostics()
        {
            DrawSection("Diagnostics");

            EditorGUILayout.PropertyField(
                reason,
                new GUIContent(
                    "Reason",
                    "Diagnostic declaration source or reason. It is not Actor identity."));
        }

        private static void DrawTechnicalEvidence(
            ActorDeclaration declaration)
        {
            DrawSection("Technical Evidence");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Runtime Type",
                    declaration.GetType().FullName);

                EditorGUILayout.TextField(
                    "Effective Actor Kind",
                    declaration.ActorKind.ToString());

                EditorGUILayout.TextField(
                    "Effective Actor Role",
                    declaration.ActorRole.ToString());

                if (declaration is PlayerActorDeclaration player)
                {
                    EditorGUILayout.Toggle(
                        "PlayerInput Evidence",
                        player.HasPlayerInputEvidence);

                    EditorGUILayout.ObjectField(
                        "Bound PlayerInput",
                        player.PlayerInput,
                        typeof(UnityEngine.Object),
                        true);
                }
            }
        }

        private void DrawValidationEvidence()
        {
            if (lastValidationReport == null)
            {
                return;
            }

            DrawSection("Validation Evidence");

            if (validationOutdated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated — configuration changed");
                return;
            }

            FrameworkAuthoringValidationGui.DrawSummary(
                lastValidationReport);

            FrameworkAuthoringValidationGui.DrawIssues(
                lastValidationReport,
                false);
        }

        private void MarkValidationOutdated()
        {
            if (lastValidationReport == null)
            {
                return;
            }

            validationOutdated = true;
            Repaint();
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
