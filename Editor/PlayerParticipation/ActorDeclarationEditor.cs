using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Designer-first Inspector shared by ActorDeclaration and specialized declarations.
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

        private bool showAdvancedDebug;

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

            DrawHeader(isPlayer);
            DrawConfiguration(isPlayer);

            serializedObject.ApplyModifiedProperties();

            DrawConfigurationStatus(declaration);

            if (Application.isPlaying && isPlayer)
            {
                DrawRuntimeStatus(
                    (PlayerActorDeclaration)declaration);
            }

            DrawAdvancedDebug(declaration);
        }

        private static void DrawHeader(
            bool isPlayer)
        {
            EditorGUILayout.LabelField(
                new GUIContent(
                    isPlayer
                        ? "Player Actor Declaration"
                        : "Actor Declaration",
                    isPlayer
                        ? "Declares the stable identity of one contextual Logical Player Actor. PlayerInput remains owned by the Local Player Host and is shown here only as runtime evidence."
                        : "Declares stable framework identity and classification for one Actor. Lifetime, movement, input, reset, snapshot and save behavior are owned elsewhere."),
                EditorStyles.boldLabel);
        }

        private void DrawConfiguration(
            bool isPlayer)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            EditorGUILayout.PropertyField(
                displayName,
                new GUIContent(
                    "Display Name",
                    "Human-readable Actor label used in authoring and diagnostics. It is not functional identity."));

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(
                "Classification",
                EditorStyles.miniBoldLabel);

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

        private static void DrawConfigurationStatus(
            ActorDeclaration declaration)
        {
            FrameworkAuthoringValidationReport report =
                ActorDeclarationAuthoringValidator.Validate(
                    declaration);

            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            EditorGUILayout.LabelField(
                "Status",
                report.IsValid
                    ? "Ready"
                    : "Needs Attention");

            if (report.IsValid)
            {
                return;
            }

            DrawFirstActionableIssue(report);
        }

        private static void DrawFirstActionableIssue(
            FrameworkAuthoringValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            for (int index = 0;
                 index < report.Issues.Count;
                 index++)
            {
                FrameworkAuthoringValidationIssue issue =
                    report.Issues[index];

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

        private static void DrawRuntimeStatus(
            PlayerActorDeclaration player)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Runtime Status");

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

        private void DrawAdvancedDebug(
            ActorDeclaration declaration)
        {
            EditorGUILayout.Space(6f);
            showAdvancedDebug =
                EditorGUILayout.Foldout(
                    showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (!showAdvancedDebug)
            {
                return;
            }

            DrawStableIdentity(declaration);
            DrawDiagnostics();

            serializedObject.ApplyModifiedProperties();

            DrawTechnicalEvidence(declaration);
        }

        private void DrawStableIdentity(
            ActorDeclaration declaration)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Stable Identity");

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
                        GenerateActorId(declaration);
                        currentActorId =
                            actorId.stringValue ?? string.Empty;
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

        private void GenerateActorId(
            ActorDeclaration declaration)
        {
            Undo.RecordObject(
                declaration,
                "Generate Actor ID");

            actorId.stringValue =
                $"actor.{Guid.NewGuid():N}";

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(declaration);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                declaration);

            serializedObject.UpdateIfRequiredOrScript();
        }

        private void DrawDiagnostics()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Diagnostics");

            EditorGUILayout.PropertyField(
                reason,
                new GUIContent(
                    "Reason",
                    "Diagnostic declaration source or reason. It is not Actor identity."));
        }

        private static void DrawTechnicalEvidence(
            ActorDeclaration declaration)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Technical Evidence");

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
    }
}
